using UnityEngine;
using UnityEngine.AI;

namespace GameLogic
{
    /// <summary>
    /// 亡灵杀手风格玩家控制器 — 鼠标点击移动，匀速行走，平滑转向。
    ///
    /// 核心：
    ///   1. 移动速度恒定，转向不减速（NavMeshAgent 高加速度）
    ///   2. 角色朝移动方向平滑旋转（不是瞬间转向目标点）
    ///   3. Animator 由 agent 实际速度驱动
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        private static readonly int Run = Animator.StringToHash("run");

        [Header("组件引用")]
        [SerializeField] private Animator _animator;

        [Header("移动")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float acceleration = 20f;
        [Tooltip("转向平滑速度（度/秒），越大转身越快")]
        [SerializeField] private float rotateSpeed = 720f;

        [Header("战斗")]
        [SerializeField] private float attackRadius = 2f;

        [Header("地面")]
        public LayerMask groundLayer;

        private NavMeshAgent _agent;

        void Start()
        {
            _agent = GetComponent<NavMeshAgent>();

            // 配置 NavMeshAgent：匀速 + 不自动旋转
            _agent.updateRotation = false;
            _agent.speed = moveSpeed;
            _agent.acceleration = acceleration;
            _agent.angularSpeed = 0f; // 我们自己控制旋转，不需要 agent 的角速度
            _agent.autoBraking = false; // 关闭自动刹车，防止到达目标时冲过头往回缩
            _agent.stoppingDistance = 0.1f; // 到目标 0.1 距离就停，避免微调抖动
        }

        void Update()
        {
            HandleInput();
            UpdateMovement();
            UpdateRotation();
            Attack();
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

        /// <summary>更新 Animator（用 agent 实际速度驱动动画混合）。</summary>
        private void UpdateMovement()
        {
            _animator.SetFloat(Run, _agent.velocity.magnitude);
        }

        /// <summary>朝移动方向平滑旋转（亡灵杀手风格：始终面朝行走方向）。</summary>
        private void UpdateRotation()
        {
            Vector3 velocity = _agent.velocity;
            if (velocity.sqrMagnitude <= 0.01f) return;

            Quaternion targetRot = Quaternion.LookRotation(velocity.normalized);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );
        }

        /// <summary>攻击：检测范围内敌人，朝最近敌人瞬间面向（攻击时允许快速锁定）。</summary>
        private void Attack()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, attackRadius);
            float closestDist = float.MaxValue;
            Transform closestEnemy = null;

            foreach (var item in colliders)
            {
                if (!item.CompareTag("Enemy")) continue;

                float dist = Vector3.Distance(transform.position, item.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestEnemy = item.transform;
                }
            }

            if (closestEnemy != null)
            {
                FaceTarget(closestEnemy.position);
                _agent.SetDestination(closestEnemy.position);
            }
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
