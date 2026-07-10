using System.Collections.Generic;

namespace GameLogic
{
    /// <summary>奖励类型。</summary>
    public enum RewardType
    {
        Normal,    // 普通奖励
        NextLayer, // 解锁下一层
    }

    /// <summary>奖励物品数据。</summary>
    public class RouletteRewardData
    {
        public int Id;
        public string Name;
        public RewardType Type;
        /// <summary>该物品图标资源名（用于 SetSprite）。</summary>
        public string Icon;
    }

    /// <summary>
    /// 转盘奖励配置（硬编码，后续迁移到 Luban）。
    /// </summary>
    public static class RouletteConfig
    {
        /// <summary>全部 20 个奖励物品。</summary>
        public static readonly List<RouletteRewardData> AllRewards = new()
        {
            new() { Id = 1,  Name = "金币 x100",    Type = RewardType.Normal,    Icon = "icon_gold" },
            new() { Id = 2,  Name = "金币 x500",    Type = RewardType.Normal,    Icon = "icon_gold" },
            new() { Id = 3,  Name = "钻石 x10",     Type = RewardType.Normal,    Icon = "icon_diamond" },
            new() { Id = 4,  Name = "钻石 x50",     Type = RewardType.Normal,    Icon = "icon_diamond" },
            new() { Id = 5,  Name = "体力药剂",     Type = RewardType.Normal,    Icon = "icon_potion" },
            new() { Id = 6,  Name = "攻击卷轴",     Type = RewardType.Normal,    Icon = "icon_scroll" },
            new() { Id = 7,  Name = "防御护符",     Type = RewardType.Normal,    Icon = "icon_charm" },
            new() { Id = 8,  Name = "经验书 x1",    Type = RewardType.Normal,    Icon = "icon_book" },
            new() { Id = 9,  Name = "经验书 x5",    Type = RewardType.Normal,    Icon = "icon_book" },
            new() { Id = 10, Name = "随机宝箱",     Type = RewardType.Normal,    Icon = "icon_chest" },
            new() { Id = 11, Name = "传说武器",     Type = RewardType.Normal,    Icon = "icon_weapon" },
            new() { Id = 12, Name = "史诗防具",     Type = RewardType.Normal,    Icon = "icon_armor" },
            new() { Id = 13, Name = "幸运符",       Type = RewardType.Normal,    Icon = "icon_luck" },
            new() { Id = 14, Name = "强化石 x5",    Type = RewardType.Normal,    Icon = "icon_stone" },
            new() { Id = 15, Name = "技能点 x10",   Type = RewardType.Normal,    Icon = "icon_skill" },
            new() { Id = 16, Name = "宠物饲料",     Type = RewardType.Normal,    Icon = "icon_food" },
            new() { Id = 17, Name = "复活石",       Type = RewardType.Normal,    Icon = "icon_revive" },
            new() { Id = 18, Name = "时装碎片",     Type = RewardType.Normal,    Icon = "icon_fashion" },
            new() { Id = 19, Name = "▶ 下一层",     Type = RewardType.NextLayer, Icon = "icon_next" },
            new() { Id = 20, Name = "▶ 下一层",     Type = RewardType.NextLayer, Icon = "icon_next" },
        };

        /// <summary>第一层：从所有物品中选 8 个（至少 1 个下一层）。</summary>
        public static List<RouletteRewardData> GetLayerRewards(int layer, int slotCount)
        {
            var pool = new List<RouletteRewardData>(AllRewards);

            // 确保每层至少有 1 个 NextLayer（第 3 层不需要）
            var result = new List<RouletteRewardData>();
            if (layer < 3)
            {
                var nextItems = pool.FindAll(r => r.Type == RewardType.NextLayer);
                if (nextItems.Count > 0)
                {
                    int idx = UnityEngine.Random.Range(0, nextItems.Count);
                    result.Add(nextItems[idx]);
                    pool.Remove(nextItems[idx]);
                }
            }

            // 剩余槽位随机填充普通物品
            while (result.Count < slotCount && pool.Count > 0)
            {
                var normals = pool.FindAll(r => r.Type == RewardType.Normal);
                if (normals.Count == 0) break;
                int idx = UnityEngine.Random.Range(0, normals.Count);
                result.Add(normals[idx]);
                pool.Remove(normals[idx]);
            }

            // 不够数就从整个池子补
            while (result.Count < slotCount && pool.Count > 0)
            {
                int idx = UnityEngine.Random.Range(0, pool.Count);
                result.Add(pool[idx]);
                pool.RemoveAt(idx);
            }

            // 打乱顺序
            for (int i = result.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (result[i], result[j]) = (result[j], result[i]);
            }

            return result;
        }
    }
}
