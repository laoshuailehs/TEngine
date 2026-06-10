using System;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;
using TEngine;
using UnityEngine;
using Log = TEngine.Log;

namespace GameLogic
{
    [Window(UILayer.UI)]
    class LoginUI : UIWindow
    {
        protected override void OnCreate()
        {
            base.OnCreate();
            // Log.Error("恭喜恭喜！！！");
            DelayChange().Forget();
        }

        private async UniTask DelayChange()
        {
            await UniTask.Delay(TimeSpan.FromSeconds(1f));
            await GameModule.Scene.LoadSceneAsync("WainX");
            Close();
        }
    }
}

