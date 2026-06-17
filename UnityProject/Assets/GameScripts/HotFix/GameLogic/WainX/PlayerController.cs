using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace GameLogic
{
    public class PlayerController : MonoBehaviour
    {
        private NavMeshAgent agent;
        public LayerMask groundLayer;

        void Start()
        {
            agent = GetComponent<NavMeshAgent>();
        }

        void Update()
        {
            if (Input.GetMouseButtonDown(0)) // 左键点击
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 100f, groundLayer))
                {
                    // 点击到可行走区域才会移动
                    if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas))
                    {
                        agent.SetDestination(navHit.position);
                        FaceTo(navHit.position);
                    }
                }
            }
        }

        void FaceTo(Vector3 targetPoint)
        {
            Vector3 dir = targetPoint - transform.position;
            dir.y = 0;

            if (dir != Vector3.zero)
            {
                Quaternion rot = Quaternion.LookRotation(dir);
                transform.rotation = rot;
            }
        }
    }
}
