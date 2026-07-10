using UnityEngine;
using UnityEngine.AI;

namespace GameLogic
{
    /// <summary>
    /// 亡灵杀手风格敌人 AI — 追击玩家，受击扣血，死亡消失。
    ///
    /// 行为：
    ///   1. 检测范围内玩家 → 追击
    ///   2. 超出追击范围 → 停在原地
    ///   3. 朝玩家方向平滑旋转
    ///   4. 受击扣血，HP 归零后死亡
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class EnemyAI : MonoBehaviour
    {
        private static readonly int Run = Animator.StringToHash("run");

        [Header("追击")]
        [SerializeField] private float chaseRange = 20f;
        [SerializeField] private float stopDistance = 1.5f;
        [SerializeField] private float moveSpeed = 3.5f;
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float rotateSpeed = 360f;

        [Header("战斗")]
        [SerializeField] private float maxHP = 100f;

        [Header("组件")]
        [SerializeField] private Animator _animator;

        private NavMeshAgent _agent;
        private Transform _player;
        private float _currentHP;
        private bool _isDead;

        public bool IsDead => _isDead;

        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
            _agent.speed = moveSpeed;
            _agent.acceleration = acceleration;
            _agent.angularSpeed = 0f;
            _agent.autoBraking = false;
            _agent.stoppingDistance = stopDistance;

            _currentHP = maxHP;

            FindPlayer();
        }

        /// <summary>查找玩家引用（通过 PlayerController 组件）。</summary>
        private void FindPlayer()
        {
            var pc = FindObjectOfType<PlayerController>();
            if (pc != null)
            {
                _player = pc.transform;
            }
        }

        void Update()
        {
            if (_isDead) return;
            if (_player == null) { FindPlayer(); return; }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist <= chaseRange && dist > stopDistance)
            {
                _agent.SetDestination(_player.position);
            }
            else if (dist <= stopDistance)
            {
                _agent.ResetPath();
            }
            else
            {
                _agent.ResetPath();
            }

            UpdateAnimator();
            UpdateRotation();
        }

        /// <summary>用 agent 实际速度驱动动画。</summary>
        private void UpdateAnimator()
        {
            if (_animator != null)
            {
                _animator.SetFloat(Run, _agent.velocity.magnitude);
            }
        }

        /// <summary>朝玩家方向平滑旋转。</summary>
        private void UpdateRotation()
        {
            if (_player == null) return;

            Vector3 velocity = _agent.velocity;
            Vector3 dir;

            if (velocity.sqrMagnitude > 0.01f)
            {
                // 移动中朝移动方向转
                dir = velocity.normalized;
            }
            else
            {
                // 站定时面朝玩家
                dir = (_player.position - transform.position).normalized;
            }

            dir.y = 0;
            if (dir == Vector3.zero) return;

            Quaternion targetRot = Quaternion.LookRotation(dir);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }

        /// <summary>受到伤害。返回是否死亡。</summary>
        public void TakeDamage(float damage)
        {
            if (_isDead) return;

            _currentHP -= damage;

            if (_currentHP <= 0)
            {
                _currentHP = 0;
                Die();
            }
        }

        /// <summary>死亡处理：禁移动、关碰撞、延迟销毁。</summary>
        private void Die()
        {
            _isDead = true;
            _agent.enabled = false;

            // 禁用碰撞体，避免重复检测
            var cols = GetComponentsInChildren<Collider>();
            foreach (var c in cols)
            {
                c.enabled = false;
            }

            // 1 秒后销毁
            Destroy(gameObject, 1f);
        }
    }
}
