using UnityEngine;
using UnityEngine.AI;

namespace GameLogic
{
    /// <summary>
    /// 亡灵杀手风格玩家控制器 — 点击移动 + 自动连招。
    ///
    /// 核心：
    ///   1. 点击移动，匀速行走，平滑转向
    ///   2. 敌人在攻击范围内时自动攻击，不需要按键
    ///   3. 连招：Attack1→Attack2→Attack3→循环，超时归零
    ///   4. 攻击时瞬间锁敌，移动时平滑转向
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        private static readonly int Run = Animator.StringToHash("run");
        private static readonly int AttackNum = Animator.StringToHash("attackNum");

        [Header("组件")]
        [SerializeField] private Animator _animator;

        [Header("移动")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 20f;
        [Tooltip("移动时转向速度（度/秒）")]
        [SerializeField] private float rotateSpeed = 720f;

        [Header("攻击")]
        [Tooltip("自动攻击检测范围")]
        [SerializeField] private float attackRadius = 2.5f;
        [Tooltip("连击触发间隔（秒），小于动画时长即可，Animator ExitTime 会控制实际过渡时机")]
        [SerializeField] private float attackInterval = 0.6f;
        [Tooltip("连招超时（秒），超过此时间没攻击则重置连招")]
        [SerializeField] private float comboTimeout = 2f;
        [Tooltip("基础伤害")]
        [SerializeField] private float attackDamage = 15f;
        [Tooltip("攻击时每帧向前冲的速度")]
        [SerializeField] private float lungeSpeed = 3f;

        [Header("地面")]
        public LayerMask groundLayer;

        private NavMeshAgent _agent;

        // 连招状态
        private int _comboCount;
        private float _lastAttackTime = float.MinValue;
        private float _comboTimer;

        // 攻击冷却计时器
        private float _cooldownTimer;

        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();

            _agent.updateRotation = false;
            _agent.speed = moveSpeed;
            _agent.acceleration = acceleration;
            _agent.angularSpeed = 0f; // 手动控制旋转，不让 agent 插手
            _agent.autoBraking = false;
            _agent.stoppingDistance = 0.1f;
        }

        void Update()
        {
            // 测试转盘：按 R 键打开
            if (Input.GetKeyDown(KeyCode.R))
            {
                GameModule.UI.ShowUIAsync<RouletteUI>();
            }

            HandleInput();
            UpdateCombo();
            TryAutoAttack();
            UpdateMovement();
            UpdateRotation();
        }

        /// <summary>
        /// 攻击期间：代码驱动前冲位移，同步给 NavMeshAgent。
        /// 动画不提取 Root Motion（FBX 已锁），位移完全由代码控制。
        /// </summary>
        void OnAnimatorMove()
        {
            if (_comboCount > 0 && _agent.enabled)
            {
                // 每帧向前冲（方向由 FaceTarget 锁定）
                transform.position += transform.forward * (lungeSpeed * Time.deltaTime);
                // 告诉 agent 我们的新位置
                _agent.nextPosition = transform.position;
            }
        }

        /// <summary>处理鼠标点击移动。</summary>
        private void HandleInput()
        {
            if (!Input.GetMouseButtonDown(1)) return;

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (!Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer)) return;
            if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas)) return;

            _agent.SetDestination(navHit.position);
        }

        /// <summary>连招超时检测：超过 comboTimeout 没攻击则重置。</summary>
        private void UpdateCombo()
        {
            if (_comboCount > 0 && Time.time - _lastAttackTime > comboTimeout)
            {
                _comboCount = 0;
                _comboTimer = 0;
            }
        }

        /// <summary>每帧检测：有敌人 + 冷却完毕 → 自动攻击。</summary>
        private void TryAutoAttack()
        {
            _animator.SetInteger(AttackNum, _comboCount);
            _cooldownTimer -= Time.deltaTime;
            if (_cooldownTimer > 0) return;

            EnemyAI target = FindClosestEnemy();
            if (target == null) return;

            // 执行攻击
            float multiplier = GetComboMultiplier();
            float dmg = attackDamage * multiplier;

            FaceTarget(target.transform.position);
            target.TakeDamage(dmg);

            // 更新连招
            _comboCount++;
            if (_comboCount > 3) _comboCount = 1;
            _lastAttackTime = Time.time;
            _comboTimer = 0;
            _cooldownTimer = attackInterval;
        }

        /// <summary>搜索范围内最近的存活敌人。</summary>
        private EnemyAI FindClosestEnemy()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, attackRadius);
            float closestDist = float.MaxValue;
            EnemyAI closest = null;

            foreach (var item in colliders)
            {
                if (!item.CompareTag("Enemy")) continue;

                var enemy = item.GetComponentInParent<EnemyAI>();
                if (enemy == null || enemy.IsDead) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy;
                }
            }

            return closest;
        }

        /// <summary>根据当前连招段数返回伤害倍率。</summary>
        private float GetComboMultiplier()
        {
            return _comboCount switch
            {
                0 => 1.0f,  // 第一刀
                1 => 1.2f,  // 第二刀
                2 => 1.5f,  // 第三刀
                _ => 1.0f,
            };
        }

        /// <summary>更新 Animator（agent 实际速度驱动）。</summary>
        private void UpdateMovement()
        {
            _animator.SetFloat(Run, _agent.velocity.magnitude);
        }

        /// <summary>朝移动方向平滑旋转（攻击中不旋转，由 FaceTarget 控制朝向）。</summary>
        private void UpdateRotation()
        {
            if (_comboCount > 0) return; // 攻击中不干预旋转

            Vector3 velocity = _agent.velocity;
            if (velocity.sqrMagnitude <= 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }

        /// <summary>瞬间面向目标（攻击锁敌用）。</summary>
        private void FaceTarget(Vector3 targetPoint)
        {
            Vector3 dir = targetPoint - transform.position;
            dir.y = 0;
            if (dir == Vector3.zero) return;

            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}
