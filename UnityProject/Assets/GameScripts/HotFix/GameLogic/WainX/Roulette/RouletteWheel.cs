using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace GameLogic
{
    /// <summary>
    /// 可控单层转盘 — 挂在转盘容器 RectTransform 上。
    ///
    /// 使用方式：
    ///   1. Init(slotCount, items) → 创建槽位 UI
    ///   2. SetTargetSlot(index) → 预设结果
    ///   3. StartSpin() → 开始旋转
    ///   4. await StopSpin() → 减速并精确停在预设目标槽位
    /// </summary>
    public class RouletteWheel : MonoBehaviour
    {
        [Header("槽位预制体")]
        [SerializeField] private GameObject _slotPrefab;

        [Header("旋转")]
        [SerializeField] private float _maxSpeed = 360f;       // 最大旋转速度 (度/秒)
        [SerializeField] private float _acceleration = 120f;    // 加速度 (度/秒²)
        [Header("停止")]
        [SerializeField] private float _stopDuration = 2f;   // 减速持续时间
        [SerializeField] private int _stopExtraRounds = 2;    // 减速时额外转的圈数

        private int _slotCount;
        private List<RouletteRewardData> _items;
        private List<GameObject> _slotObjects = new();

        private float _currentAngle;
        private float _currentSpeed;
        private bool _isSpinning;
        private int _targetSlot = -1; // -1 表示随机

        /// <summary>初始化转盘。</summary>
        public void Init(int slotCount, List<RouletteRewardData> items)
        {
            _slotCount = slotCount;
            _items = items;

            foreach (var obj in _slotObjects) Destroy(obj);
            _slotObjects.Clear();

            float radius = GetComponent<RectTransform>().rect.width * 0.35f;
            for (int i = 0; i < slotCount; i++)
            {
                var slot = Instantiate(_slotPrefab, transform);
                slot.name = $"Slot_{i}";

                float angle = i * (360f / slotCount);
                float rad = angle * Mathf.Deg2Rad;
                var slotRect = slot.GetComponent<RectTransform>();
                slotRect.anchoredPosition = new Vector2(
                    Mathf.Sin(rad) * radius,
                    Mathf.Cos(rad) * radius
                );

                var label = slot.GetComponentInChildren<TMPro.TextMeshProUGUI>();
                if (label != null && i < items.Count)
                    label.text = items[i].Name;

                if (i < items.Count && items[i].Type == RewardType.NextLayer)
                {
                    var img = slot.GetComponent<UnityEngine.UI.Image>();
                    if (img != null) img.color = new Color(1f, 0.6f, 0.2f);
                }

                _slotObjects.Add(slot);
            }
        }

        /// <summary>预设停止目标槽位（-1 为随机）。</summary>
        public void SetTargetSlot(int index)
        {
            _targetSlot = index;
        }

        /// <summary>开始旋转（从 0 加速到最大速度）。</summary>
        public void StartSpin()
        {
            _currentSpeed = 0;
            _isSpinning = true;
        }

        /// <summary>立即达到最大速度（跳过加速阶段）。</summary>
        public void StartSpinInstant()
        {
            _currentSpeed = _maxSpeed;
            _isSpinning = true;
        }

        /// <summary>
        /// 减速停止，精确对齐到目标槽位。
        /// 如果有预设目标则停在目标上，否则随机停止。
        /// </summary>
        public async UniTask<int> StopSpin()
        {
            if (!_isSpinning) return -1;

            _isSpinning = false;

            // 确定目标
            int target = (_targetSlot >= 0 && _targetSlot < _slotCount)
                ? _targetSlot : Random.Range(0, _slotCount);
            _targetSlot = -1; // 用完后重置

            // 计算目标角度：指针在顶部 (0°)，要让槽位 target 停在指针下
            // 槽位 target 的本地角度 = target * slotAngle
            // 轮子转到 currentAngle 时，该槽位的世界角度 = target*slotAngle + currentAngle
            // 要停在指针 (0°) 满足: target*slotAngle + finalAngle ≡ 0 (mod 360)
            // → finalAngle_norm = (360 - target*slotAngle) % 360
            float slotAngle = 360f / _slotCount;
            float targetNorm = (360f - target * slotAngle) % 360f;
            if (targetNorm < 0) targetNorm += 360f;

            // 当前归一化角度
            float currentNorm = ((_currentAngle % 360f) + 360f) % 360f;
            // 顺时针走到目标需要的差值
            float delta = (targetNorm - currentNorm + 360f) % 360f;
            // 加上额外圈数
            float totalDelta = delta + _stopExtraRounds * 360f;

            // 从当前角度平滑旋转到目标角度
            float startAngle = _currentAngle;
            float startSpeed = _currentSpeed;
            float elapsed = 0;

            while (elapsed < _stopDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / _stopDuration;
                t = t * t * (3f - 2f * t); // ease-out

                _currentAngle = Mathf.Lerp(startAngle, startAngle + totalDelta, t);
                _currentSpeed = Mathf.Lerp(startSpeed, 0, t);
                transform.rotation = Quaternion.Euler(0, 0, _currentAngle);

                await UniTask.Yield();
            }

            _currentAngle = startAngle + totalDelta;
            _currentSpeed = 0;
            transform.rotation = Quaternion.Euler(0, 0, _currentAngle);

            return target;
        }

        void Update()
        {
            if (!_isSpinning) return;

            // 加速到最大速度
            if (_currentSpeed < _maxSpeed)
            {
                _currentSpeed += _acceleration * Time.deltaTime;
                if (_currentSpeed > _maxSpeed)
                    _currentSpeed = _maxSpeed;
            }

            _currentAngle += _currentSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0, 0, _currentAngle);
        }

        public bool IsSpinning => _isSpinning;
    }
}

