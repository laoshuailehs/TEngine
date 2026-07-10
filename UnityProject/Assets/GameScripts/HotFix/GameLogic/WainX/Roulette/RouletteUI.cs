using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TEngine;
using UnityEngine;
using UnityEngine.UI;

namespace GameLogic
{
    /// <summary>
    /// 三层转盘 UI 窗口。
    ///
    /// 流程：
    ///   1. 打开 → Layer1 随机 8 个物品填充 → Layer1 自动旋转
    ///   2. 点击停止 → Layer1 减速停 → 判定结果
    ///   3. 下一层 → Layer2 出现+旋转 → 点击停止 → 正常奖励直接关闭
    ///   4. Layer3 同 Layer2
    /// </summary>
    [Window(UILayer.Top, location: "RouletteUI", fullScreen: true)]
    public class RouletteUI : UIWindow
    {
        #region UI 节点引用

        private Button _btnStop;
        private Text _textReward;
        private GameObject _goLayer1;
        private GameObject _goLayer2;
        private GameObject _goLayer3;
        private RouletteWheel _wheel1;
        private RouletteWheel _wheel2;
        private RouletteWheel _wheel3;

        #endregion

        private int _currentLayer = 1;
        private bool _canStop = true;

        // 存储各层物品（避免每次重新随机）
        private List<RouletteRewardData> _itemsLayer1;
        private List<RouletteRewardData> _itemsLayer2;
        private List<RouletteRewardData> _itemsLayer3;

        // 每层槽位数
        private static readonly int[] LayerSlots = { 8, 8, 4 };

        protected override void ScriptGenerator()
        {
            _btnStop = FindChildComponent<Button>("m_btn_Stop");
            _textReward = FindChildComponent<Text>("m_text_Reward");
            _goLayer1 = FindChild("m_tf_Layer1").gameObject;
            _goLayer2 = FindChild("m_tf_Layer2").gameObject;
            _goLayer3 = FindChild("m_tf_Layer3").gameObject;

            _wheel1 = FindChildComponent<RouletteWheel>("m_tf_Layer1");
            _wheel2 = FindChildComponent<RouletteWheel>("m_tf_Layer2");
            _wheel3 = FindChildComponent<RouletteWheel>("m_tf_Layer3");
        }

        protected override void RegisterEvent()
        {
            _btnStop.onClick.AddListener(OnClickStop);
        }

        protected override void OnCreate()
        {
            // 初始状态
            _canStop = true;
            _textReward.text = "";
            _goLayer2.SetActive(false);
            _goLayer3.SetActive(false);

            // 填充各层物品（一次生成，存储复用）
            _itemsLayer1 = RouletteConfig.GetLayerRewards(1, LayerSlots[0]);
            _itemsLayer2 = RouletteConfig.GetLayerRewards(2, LayerSlots[1]);
            _itemsLayer3 = RouletteConfig.GetLayerRewards(3, LayerSlots[2]);

            _wheel1.Init(LayerSlots[0], _itemsLayer1);
            _wheel2.Init(LayerSlots[1], _itemsLayer2);
            _wheel3.Init(LayerSlots[2], _itemsLayer3);

            // Layer1：预设结果并开始旋转
            _currentLayer = 1;
            StartLayerSpin(_wheel1, _itemsLayer1);
        }

        /// <summary>点击停止按钮。</summary>
        private async void OnClickStop()
        {
            if (!_canStop) return;
            _canStop = false;

            var (wheel, items) = _currentLayer switch
            {
                1 => (_wheel1, _itemsLayer1),
                2 => (_wheel2, _itemsLayer2),
                3 => (_wheel3, _itemsLayer3),
                _ => (null, (List<RouletteRewardData>)null),
            };
            if (wheel == null) return;

            int idx = await wheel.StopSpin();
            if (idx < 0 || idx >= items.Count) { _canStop = true; return; }

            // 用存储的物品（不是重新随机生成的！）
            var reward = items[idx];
            if (reward == null) { _canStop = true; return; }

            _textReward.text = $"获得: {reward.Name}";
            Log.Info($"[Roulette] Layer{_currentLayer} 选中: [{reward.Id}] {reward.Name}");

            if (reward.Type == RewardType.NextLayer && _currentLayer < 3)
            {
                // 解锁下一层
                await UniTask.Delay(500);
                ShowNextLayer();
            }
            else
            {
                // 发放奖励（TODO: 接入奖励系统）
                await UniTask.Delay(1000);
                Close();
            }
        }

        /// <summary>系统决定结果并启动一层旋转。</summary>
        private void StartLayerSpin(RouletteWheel wheel, List<RouletteRewardData> items)
        {
            int targetIdx = Random.Range(0, items.Count);
            wheel.SetTargetSlot(targetIdx);
            wheel.StartSpin();
            Log.Info($"[Roulette] Layer{_currentLayer} 预设: [{items[targetIdx].Id}] {items[targetIdx].Name}");
        }

        /// <summary>显示下一层并开始旋转。</summary>
        private void ShowNextLayer()
        {
            _currentLayer++;
            _textReward.text = $"第 {_currentLayer} 层";

            if (_currentLayer == 2)
            {
                _goLayer1.SetActive(false);
                _goLayer2.SetActive(true);
                StartLayerSpin(_wheel2, _itemsLayer2);
            }
            else if (_currentLayer == 3)
            {
                _goLayer2.SetActive(false);
                _goLayer3.SetActive(true);
                StartLayerSpin(_wheel3, _itemsLayer3);
            }

            _canStop = true;
        }

        protected override void OnDestroy()
        {
            _btnStop.onClick.RemoveListener(OnClickStop);
        }
    }
}
