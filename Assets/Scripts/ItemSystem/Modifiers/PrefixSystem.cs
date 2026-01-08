using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemSystem.Modifiers
{
    using Core;

    /// <summary>
    /// 前缀数据 - 类似泰拉瑞亚的修饰语系统
    /// </summary>
    [CreateAssetMenu(fileName = "NewPrefix", menuName = "ItemSystem/Prefix")]
    public class PrefixData : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField] private int prefixId;
        [SerializeField] private string displayName;
        [SerializeField] private string description;
        [SerializeField] private PrefixCategory category;
        [SerializeField] private PrefixTier tier;

        [Header("属性修改")]
        [SerializeField] private float damageModifier;      // 伤害倍率 (+0.15 = +15%)
        [SerializeField] private float criticalModifier;    // 暴击修改
        [SerializeField] private float speedModifier;       // 攻击速度
        [SerializeField] private float knockbackModifier;   // 击退
        [SerializeField] private int flatDamageBonus;       // 固定伤害加成

        [Header("特殊效果")]
        [SerializeField] private PrefixSpecialEffect[] specialEffects;

        [Header("价格修正")]
        [SerializeField] private float priceMultiplier = 1f;

        [Header("重铸权重")]
        [SerializeField] private int reforgeWeight = 100;  // 抽取权重

        // 属性访问
        public int PrefixId => prefixId;
        public string DisplayName => displayName;
        public string Description => description;
        public PrefixCategory Category => category;
        public PrefixTier Tier => tier;
        public float DamageModifier => damageModifier;
        public float CriticalModifier => criticalModifier;
        public float SpeedModifier => speedModifier;
        public float KnockbackModifier => knockbackModifier;
        public int FlatDamageBonus => flatDamageBonus;
        public float PriceMultiplier => priceMultiplier;
        public int ReforgeWeight => reforgeWeight;
        public PrefixSpecialEffect[] SpecialEffects => specialEffects;

        /// <summary>
        /// 获取前缀颜色（用于UI显示）
        /// </summary>
        public Color GetDisplayColor()
        {
            return tier switch
            {
                PrefixTier.Negative => new Color(0.7f, 0.3f, 0.3f),  // 红色
                PrefixTier.Neutral => Color.white,
                PrefixTier.Good => new Color(0.4f, 0.8f, 0.4f),      // 绿色
                PrefixTier.Great => new Color(0.4f, 0.6f, 1f),       // 蓝色
                PrefixTier.Best => new Color(1f, 0.8f, 0.2f),        // 金色
                _ => Color.white
            };
        }

        /// <summary>
        /// 生成属性描述文本
        /// </summary>
        public string GetModifierText()
        {
            var lines = new List<string>();

            if (damageModifier != 0)
                lines.Add($"伤害 {FormatPercent(damageModifier)}");
            if (flatDamageBonus != 0)
                lines.Add($"伤害 {FormatFlat(flatDamageBonus)}");
            if (criticalModifier != 0)
                lines.Add($"暴击率 {FormatPercent(criticalModifier)}");
            if (speedModifier != 0)
                lines.Add($"攻击速度 {FormatPercent(speedModifier)}");
            if (knockbackModifier != 0)
                lines.Add($"击退 {FormatPercent(knockbackModifier)}");

            return string.Join("\n", lines);
        }

        private string FormatPercent(float value)
        {
            string sign = value >= 0 ? "+" : "";
            return $"{sign}{value * 100:F0}%";
        }

        private string FormatFlat(int value)
        {
            string sign = value >= 0 ? "+" : "";
            return $"{sign}{value}";
        }
    }

    /// <summary>
    /// 前缀特殊效果
    /// </summary>
    [Serializable]
    public class PrefixSpecialEffect
    {
        public PrefixEffectType effectType;
        public float value;
        public ElementType element;
    }

    public enum PrefixEffectType
    {
        None,
        LifeSteal,          // 生命窃取
        ManaSteal,          // 法力窃取
        ElementalDamage,    // 元素伤害加成
        ArmorPenetration,   // 护甲穿透
        Multishot,          // 多重攻击几率
        ChainAttack         // 连锁攻击
    }

    // 注意: PrefixTier 和 PrefixCategory 已移至 ItemSystem.Core.ItemEnums.cs

    /// <summary>
    /// 前缀数据库 - 管理所有前缀
    /// </summary>
    [CreateAssetMenu(fileName = "PrefixDatabase", menuName = "ItemSystem/Databases/PrefixDatabase")]
    public class PrefixDatabase : ScriptableObject
    {
        private static PrefixDatabase _instance;
        public static PrefixDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<PrefixDatabase>("Databases/PrefixDatabase");
                    _instance?.Initialize();
                }
                return _instance;
            }
        }

        /// <summary>
        /// 手动设置实例
        /// </summary>
        public static void SetInstance(PrefixDatabase database)
        {
            _instance = database;
            _instance?.Initialize();
        }

        [SerializeField] private PrefixData[] allPrefixes;

        private Dictionary<int, PrefixData> _prefixLookup;
        private Dictionary<PrefixCategory, List<PrefixData>> _categoryLookup;
        private bool _initialized;

        private void OnEnable()
        {
            Initialize();
        }

        public void Initialize()
        {
            if (_initialized) return;

            BuildLookups();

            // 注册前缀名称提供者到 PrefixRegistry
            PrefixRegistry.RegisterPrefixNameProvider(GetPrefixName);

            _initialized = true;
        }

        private void BuildLookups()
        {
            _prefixLookup = new Dictionary<int, PrefixData>();
            _categoryLookup = new Dictionary<PrefixCategory, List<PrefixData>>();

            if (allPrefixes == null) return;

            foreach (var prefix in allPrefixes)
            {
                if (prefix == null) continue;

                _prefixLookup[prefix.PrefixId] = prefix;

                if (!_categoryLookup.ContainsKey(prefix.Category))
                    _categoryLookup[prefix.Category] = new List<PrefixData>();
                _categoryLookup[prefix.Category].Add(prefix);
            }
        }

        public PrefixData GetPrefix(int prefixId)
        {
            if (_prefixLookup == null) BuildLookups();
            return _prefixLookup.TryGetValue(prefixId, out var prefix) ? prefix : null;
        }

        /// <summary>
        /// 获取前缀显示名称
        /// </summary>
        public string GetPrefixName(int prefixId)
        {
            var prefix = GetPrefix(prefixId);
            return prefix?.DisplayName;
        }

        public int[] GetPrefixesByCategory(PrefixCategory category)
        {
            if (_categoryLookup == null) BuildLookups();

            var prefixes = new List<int>();

            // 添加该类别专属前缀
            if (_categoryLookup.TryGetValue(category, out var categoryPrefixes))
            {
                prefixes.AddRange(categoryPrefixes.Select(p => p.PrefixId));
            }

            // 添加通用前缀
            if (category != PrefixCategory.Universal &&
                _categoryLookup.TryGetValue(PrefixCategory.Universal, out var universalPrefixes))
            {
                prefixes.AddRange(universalPrefixes.Select(p => p.PrefixId));
            }

            return prefixes.ToArray();
        }

        public List<PrefixData> GetAllPrefixes()
        {
            return allPrefixes != null ? new List<PrefixData>(allPrefixes) : new List<PrefixData>();
        }
    }

    /// <summary>
    /// 重铸系统
    /// </summary>
    public class ReforgeSystem
    {
        private readonly System.Random _random;

        public ReforgeSystem(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>
        /// 计算重铸费用
        /// </summary>
        public int CalculateReforgeCost(ItemInstance item)
        {
            var template = item.Template;
            if (template == null) return 0;

            int baseCost = template.BuyPrice;

            // 基于品质的倍率
            float rarityMultiplier = template.Rarity switch
            {
                ItemRarity.Common => 1f,
                ItemRarity.Uncommon => 1.5f,
                ItemRarity.Rare => 2f,
                ItemRarity.Epic => 3f,
                ItemRarity.Legendary => 5f,
                _ => 1f
            };

            return Mathf.RoundToInt(baseCost * rarityMultiplier * 0.3f);
        }

        /// <summary>
        /// 执行重铸
        /// </summary>
        public ReforgeResult Reforge(ItemInstance item, PrefixCategory category)
        {
            if (item?.Template == null || !item.Template.IsReforgeable)
            {
                return new ReforgeResult
                {
                    Success = false,
                    ErrorMessage = "此物品无法重铸"
                };
            }

            int oldPrefixId = item.PrefixId;
            int[] allowedPrefixes = PrefixDatabase.Instance?.GetPrefixesByCategory(category);

            if (allowedPrefixes == null || allowedPrefixes.Length == 0)
            {
                return new ReforgeResult
                {
                    Success = false,
                    ErrorMessage = "没有可用的前缀"
                };
            }

            // 加权随机选择新前缀
            int newPrefixId = SelectWeightedPrefix(allowedPrefixes);

            // 确保不会得到相同前缀（最多尝试3次）
            int attempts = 0;
            while (newPrefixId == oldPrefixId && attempts < 3)
            {
                newPrefixId = SelectWeightedPrefix(allowedPrefixes);
                attempts++;
            }

            item.SetPrefix(newPrefixId);

            var oldPrefix = PrefixDatabase.Instance?.GetPrefix(oldPrefixId);
            var newPrefix = PrefixDatabase.Instance?.GetPrefix(newPrefixId);

            return new ReforgeResult
            {
                Success = true,
                OldPrefixId = oldPrefixId,
                NewPrefixId = newPrefixId,
                OldPrefix = oldPrefix,
                NewPrefix = newPrefix,
                IsUpgrade = (newPrefix?.Tier ?? PrefixTier.Neutral) > (oldPrefix?.Tier ?? PrefixTier.Neutral),
                IsDowngrade = (newPrefix?.Tier ?? PrefixTier.Neutral) < (oldPrefix?.Tier ?? PrefixTier.Neutral)
            };
        }

        private int SelectWeightedPrefix(int[] prefixIds)
        {
            var prefixes = prefixIds
                .Select(id => PrefixDatabase.Instance?.GetPrefix(id))
                .Where(p => p != null)
                .ToList();

            if (prefixes.Count == 0)
                return 0;

            int totalWeight = prefixes.Sum(p => p.ReforgeWeight);
            int roll = _random.Next(totalWeight);

            int cumulative = 0;
            foreach (var prefix in prefixes)
            {
                cumulative += prefix.ReforgeWeight;
                if (roll < cumulative)
                    return prefix.PrefixId;
            }

            return prefixes.Last().PrefixId;
        }
    }

    public struct ReforgeResult
    {
        public bool Success;
        public string ErrorMessage;
        public int OldPrefixId;
        public int NewPrefixId;
        public PrefixData OldPrefix;
        public PrefixData NewPrefix;
        public bool IsUpgrade;
        public bool IsDowngrade;
    }
}