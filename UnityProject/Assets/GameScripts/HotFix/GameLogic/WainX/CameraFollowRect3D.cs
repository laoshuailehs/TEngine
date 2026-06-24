using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 亡灵杀手风格相机 — 固定角度跟随玩家，永不旋转
    ///
    /// 行为：
    ///   1. 玩家在边界内 → 相机平滑跟随，玩家始终在画面中间
    ///   2. 玩家走到边界 → 相机停在边界不动，玩家可继续走到屏幕边缘
    ///   3. 玩家回头 → 相机重新跟上，玩家回到画面中间
    ///   4. Y 轴高度固定（不随玩家上下坡晃动）
    /// </summary>
    public class CameraFollowRect3D : MonoBehaviour
    {
        [Header("跟随目标")]
        public Transform target;

        [Header("相机偏移")]
        [Tooltip("XZ=让玩家在画面中心的偏移, Y=相机世界高度")]
        public Vector3 offset = new Vector3(0, 15, -3);

        [Header("跟随平滑度")]
        [Range(0.1f, 20f)]
        public float followSpeed = 8f;

        [Header("世界边界（相机中心XZ范围）")]
        public float minX = -40f;
        public float maxX = 40f;
        public float minZ = -60f;
        public float maxZ = 20f;

        void Start()
        {
            if (target != null) SnapToTarget();
        }

        void LateUpdate()
        {
            if (target == null) return;

            // 1. 相机理想位置 = 玩家位置 + 偏移（玩家在画面中间）
            Vector3 desired = new Vector3(
                target.position.x + offset.x,
                offset.y,
                target.position.z + offset.z
            );

            // 2. 边界钳制：相机不能超出世界边界
            //    → 边界内：desired不变，相机跟玩家
            //    → 碰边界：相机停住，玩家继续走远
            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.z = Mathf.Clamp(desired.z, minZ, maxZ);

            // 3. 平滑位移（只移不转）
            transform.position = Vector3.Lerp(
                transform.position,
                desired,
                followSpeed * Time.unscaledDeltaTime
            );
        }

        public void SnapToTarget()
        {
            if (target == null) return;
            Vector3 pos = new Vector3(
                target.position.x + offset.x,
                offset.y,
                target.position.z + offset.z
            );
            pos.x = Mathf.Clamp(pos.x, minX, maxX);
            pos.z = Mathf.Clamp(pos.z, minZ, maxZ);
            transform.position = pos;
        }

        public void SetTarget(Transform t, bool snap = true)
        {
            target = t;
            if (snap) SnapToTarget();
        }

        public void SetBounds(float xMin, float xMax, float zMin, float zMax)
        {
            minX = xMin; maxX = xMax;
            minZ = zMin; maxZ = zMax;
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Vector3 c = new Vector3((minX + maxX) / 2f, offset.y, (minZ + maxZ) / 2f);
            Gizmos.DrawWireCube(c, new Vector3(maxX - minX, 0.1f, maxZ - minZ));

            if (target != null)
            {
                Gizmos.color = Color.green;
                Vector3 cam = new Vector3(target.position.x + offset.x, offset.y, target.position.z + offset.z);
                cam.x = Mathf.Clamp(cam.x, minX, maxX);
                cam.z = Mathf.Clamp(cam.z, minZ, maxZ);
                Gizmos.DrawWireSphere(cam, 0.5f);
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(cam, target.position);
            }
        }
#endif
    }
}
