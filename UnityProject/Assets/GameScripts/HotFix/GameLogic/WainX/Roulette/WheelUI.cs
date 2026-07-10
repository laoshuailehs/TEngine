using System.Collections;
using System.Collections.Generic;
using GameLogic.GameScripts.HotFix.GameLogic.MessageCode;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    [Window(UILayer.Top, location: "WheelUI", fullScreen: true)]
    public class WheelUI : UIWindow
    {
        #region 脚本工具生成的代码
        

        protected override void OnCreate()
        {
            base.OnCreate();
            GameEvent.Send(WainXMessageCode.WainXWheelOpen);
        }

        #endregion

    }
}
