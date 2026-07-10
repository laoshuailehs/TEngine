using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using GameLogic.GameScripts.HotFix.GameLogic.MessageCode;
using Spine.Unity;
using TEngine;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameLogic
{
    public class WheelLogic : MonoBehaviour
    {
        [SerializeField] private List<WheelSlot> mLayer0;
        [SerializeField] private List<WheelSlot> mLayer1;
        [SerializeField] private List<WheelSlot> mLayer2;
        [SerializeField] private Transform mRoteTransform0;
        [SerializeField] private Transform mRoteTransform1;
        [SerializeField] private Transform mRoteTransform2;
        [SerializeField] private float speedMax = 500f;
        [SerializeField] private float acceleration = 120f;
        [SerializeField] private SkeletonGraphic mLayer0Spine;
        [SerializeField] private SkeletonUtilityBone mLayer0SlotsSpine;
        [SerializeField] private SkeletonGraphic mLayer1Spine;
        [SerializeField] private SkeletonGraphic mLayer2Spine;

        private float _speed;
        private readonly Dictionary<int, List<WheelSlot>> _mAllSlots = new Dictionary<int, List<WheelSlot>>();
        private WheelState _mState;
        private int _currentLayer;

        private void OnEnable()
        {
            GameEvent.AddEventListener(WainXMessageCode.WainXWheelOpen, WainXWheelOpen);
        }

        private void OnDisable()
        {
            GameEvent.RemoveEventListener(WainXMessageCode.WainXWheelOpen, WainXWheelOpen);
        }

        private void Init()
        {
            _mAllSlots.Add(0,mLayer0);
            _mAllSlots.Add(1,mLayer1);
            _mAllSlots.Add(2,mLayer2);
        }
        
        private void WainXWheelOpen()
        {
            Init();
            OpenDelay().Forget();
        }

        private void StartAccelerate()
        {
            _speed = Mathf.Min(_speed + acceleration * Time.deltaTime,speedMax);   
            mRoteTransform0.Rotate(0,0,_speed * Time.deltaTime);
        }

        private async UniTask OpenDelay()
        {
            mLayer0Spine.AnimationState.SetAnimation(0, "Born01", false);
            mLayer1Spine.AnimationState.SetAnimation(0, "Born01", false);
            mLayer2Spine.AnimationState.SetAnimation(0, "Born01", false);
            
            await UniTask.Delay(TimeSpan.FromSeconds(1.5));
            mLayer0SlotsSpine.mode = SkeletonUtilityBone.Mode.Override;
            _mState = WheelState.StartAccelerate;
        }

        private void Update()
        {
            switch (_mState)
            {
                case WheelState.StartAccelerate:
                    StartAccelerate();
                    break;
                case WheelState.MaxSpeed:
                    break;
                case WheelState.StartSlowDown:
                    break;
                case WheelState.Stop:
                    break;
            }
        }

        private enum WheelState
        {
            None,
            StartAccelerate,
            MaxSpeed,
            StartSlowDown,
            Stop
        }
    }
}
