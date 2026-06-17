using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameLogic
{
    using UnityEngine;

    public class CameraFollowRect3D : MonoBehaviour
    {
        public Transform target;              // 玩家
        public float followSpeed = 5f;

        [Header("固定矩形范围（XZ平面）")]
        public float minX;
        public float maxX;
        public float minZ;
        public float maxZ;

        [Header("摄像机偏移")]
        public Vector3 offset = new Vector3(0, 15, -10);

        void LateUpdate()
        {
            if (target == null) return;

            // ✅ 忽略玩家 Y 轴
            Vector3 targetPos = new Vector3(
                target.position.x,
                transform.position.y,   // 摄像机高度固定
                target.position.z
            );

            Vector3 desiredPos = targetPos + offset;

            // 限制在矩形内（3D 用 X / Z）
            float clampedX = Mathf.Clamp(desiredPos.x, minX, maxX);
            float clampedZ = Mathf.Clamp(desiredPos.z, minZ, maxZ);

            Vector3 clampedPos = new Vector3(
                clampedX,
                desiredPos.y,
                clampedZ
            );

            // 平滑跟随
            transform.position = Vector3.Lerp(
                transform.position,
                clampedPos,
                Time.deltaTime * followSpeed
            );

            // 始终看向玩家
            transform.LookAt(target.position + Vector3.up * 1.5f);
        }
    }
}
