using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemSystem.Modifiers
{
    using Core;

    // ============================================================
    // 泰拉瑞亚风格前缀/重铸系统
    // 
    // 特性：
    // 1. 权重随机抽取
    // 2. 保底机制（连续不出好前缀时提高概率）
    // 3. 费用计算（基于物品价值和品质）
    // 4. 重铸统计追踪
    // 5. 批量重铸支持
    // 6. 前缀评分系统
    // ============================================================

    /// <summary>
    /// 前缀数据 - 类似泰拉瑞亚的修饰语系统
    /// </summary>
    [CreateAssetMenu(fileName = "NewPrefix", menuName = "ItemSystem/Prefix")]
    public class PrefixData : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField] private int prefixId;
        [SerializeField] private string displayName;
        [TextArea(2, 3)]
        [SerializeField] private string description;
        [SerializeField] private PrefixCategory category;
        [SerializeField] private PrefixTier tier;

        [Header("属性修改")]
        [SerializeField] private float damageModifier;       // 伤害倍率 (+0.15 = +15%)
        [SerializeField] private float criticalModifier;     // 暴击修改
        [SerializeField] private float speedModifier;        // 攻击速度
        [SerializeField] private float knockbackModifier;    // 击退
        [SerializeField] private int flatDamageBonus;        // 固定伤害加成
        [SerializeField] private float sizeModifier;         // 武器大小
        [SerializeField] private float manaModifier;         // 魔力消耗修改

        [Header("特殊效果")]
        [SerializeField] private PrefixSpecialEffect[] specialEffects;

        [Header("价格与重铸")]
        [SerializeField] private float priceMultiplier = 1f;
        [SerializeField] private int reforgeWeight = 100;    // 抽取权重
        [SerializeField] private bool isLegendary = false;   // 是否传奇前缀

        // 属性访问器
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
        public float SizeModifier => sizeModifier;
        public float ManaModifier => manaModifier;
        public float PriceMultiplier => priceMultiplier;
        public int ReforgeWeight => reforgeWeight;
        public bool IsLegendary => isLegendary;
        public PrefixSpecialEffect[] SpecialEffects => specialEffects;

        /// <summary>
        /// 计算前缀总体评分（用于比较好坏）
        /// 评分越高代表前缀越好
        /// </summary>
        public float CalculateScore()
        {
            float score = 0;
            
            // 伤害相关加成权重最高
            score += damageModifier * 100;
            score += flatDamageBonus * 2;
            
            // 暴击和速度次之
            score += criticalModifier * 50;
            score += speedModifier * 80;
            
            // 击退和大小影响较小
            score += knockbackModifier * 30;
            score += sizeModifier * 20;
            
            // 法力消耗是负面的（增加消耗=负分）
            score -= manaModifier * 20;
            
            // 特殊效果额外加分
            if (specialEffects != null)
            {
                foreach (var effect in specialEffects)
                {
                    score += effect.value * 10;
                }
            }
            
            return score;
        }

        /// <summary>
        /// 获取前缀颜色（用于UI显示）
        /// </summary>
        public Color GetDisplayColor()
        {
            return tier switch
            {
                PrefixTier.Negative => new Color(0.7f, 0.3f, 0.3f),  // 红色
                PrefixTier.Neutral => Color.white,
                PrefixTier.Good => new Color(0.4f, 0.8f, 0.4f),       // 绿色
                PrefixTier.Great => new Color(0.4f, 0.6f, 1f),        // 蓝色
                PrefixTier.Best => new Color(1f, 0.8f, 0.2f),         // 金色
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
            if (sizeModifier != 0)
                lines.Add($"大小 {FormatPercent(sizeModifier)}");
            if (manaModifier != 0)
                lines.Add($"魔力消耗 {FormatPercent(manaModifier)}");

            // 特殊效果描述
            if (specialEffects != null)
            {
                foreach (var effect in specialEffects)
                {
                    lines.Add(effect.GetDescription());
                }
            }

            return string.Join("\n", lines);
        }

        /// <summary>
        /// 获取简短描述（一行）
        /// </summary>
        public string GetShortDescription()
        {
            var parts = new List<string>();
            
            if (damageModifier != 0)
                parts.Add($"伤害{FormatPercent(damageModifier)}");
            if (criticalModifier != 0)
                parts.Add($"暴击{FormatPercent(criticalModifier)}");
            if (speedModifier != 0)
                parts.Add($"速度{FormatPercent(speedModifier)}");
                
            return parts.Count > 0 ? string.Join(", ", parts) : "无特殊效果";
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

        /// <summary>
        /// 获取效果描述
        /// </summary>
        public string GetDescription()
        {
            return effectType switch
            {
                PrefixEffectType.LifeSteal => $"生命窃取 {value * 100:F0}%",
                PrefixEffectType.ManaSteal => $"法力窃取 {value * 100:F0}%",
                PrefixEffectType.ElementalDamage => $"{GetElementName(element)}伤害 +{value * 100:F0}%",
                PrefixEffectType.ArmorPenetration => $"护甲穿透 +{value:F0}",
                PrefixEffectType.Multishot => $"{value * 100:F0}%几率多重射击",
                PrefixEffectType.ChainAttack => $"连锁攻击 {value:F0}个目标",
                PrefixEffectType.OnHitEffect => "击中时触发特殊效果",
                _ => ""
            };
        }

        private string GetElementName(ElementType element)
        {
            return element switch
            {
                ElementType.Fire => "火焰",
                ElementType.Ice => "冰霜",
                ElementType.Lightning => "雷电",
                ElementType.Poison => "毒素",
                ElementType.Holy => "神圣",
                ElementType.Dark => "暗影",
                _ => element.ToString()
            };
        }
    }

    public enum PrefixEffectType
    {
        None,
        LifeSteal,          // 生命窃取
        ManaSteal,          // 法力窃取
        ElementalDamage,    // 元素伤害加成
        ArmorPenetration,   // 护甲穿透
        Multishot,          // 多重攻击几率
        ChainAttack,        // 连锁攻击
        OnHitEffect         // 击中效果
    }

    // ============================================================
    // 前缀数据库
    // ============================================================

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

        public static void SetInstance(PrefixDatabase database)
        {
            _instance = database;
            _instance?.Initialize();
        }

        [SerializeField] private PrefixData[] allPrefixes;

        private Dictionary<int, PrefixData> _prefixLookup;
        private Dictionary<PrefixCategory, List<PrefixData>> _categoryLookup;
        private Dictionary<PrefixTier, List<PrefixData>> _tierLookup;
        private bool _initialized;

        private void OnEnable() => Initialize();

        public void Initialize()
        {
            if (_initialized) return;
            BuildLookups();
            PrefixRegistry.RegisterPrefixNameProvider(GetPrefixName);
            _initialized = true;
        }

        private void BuildLookups()
        {
            _prefixLookup = new Dictionary<int, PrefixData>();
            _categoryLookup = new Dictionary<PrefixCategory, List<PrefixData>>();
            _tierLookup = new Dictionary<PrefixTier, List<PrefixData>>();

            if (allPrefixes == null) return;

            foreach (var prefix in allPrefixes)
            {
                if (prefix == null) continue;

                _prefixLookup[prefix.PrefixId] = prefix;

                if (!_categoryLookup.ContainsKey(prefix.Category))
                    _categoryLookup[prefix.Category] = new List<PrefixData>();
                _categoryLookup[prefix.Category].Add(prefix);

                if (!_tierLookup.ContainsKey(prefix.Tier))
                    _tierLookup[prefix.Tier] = new List<PrefixData>();
                _tierLookup[prefix.Tier].Add(prefix);
            }
        }

        public PrefixData GetPrefix(int prefixId)
        {
            if (_prefixLookup == null) Initialize();
            return _prefixLookup.TryGetValue(prefixId, out var prefix) ? prefix : null;
        }

        public string GetPrefixName(int prefixId)
        {
            return GetPrefix(prefixId)?.DisplayName;
        }

        public int[] GetPrefixesByCategory(PrefixCategory category)
        {
            if (_categoryLookup == null) Initialize();

            var prefixes = new List<int>();

            if (_categoryLookup.TryGetValue(category, out var categoryPrefixes))
                prefixes.AddRange(categoryPrefixes.Select(p => p.PrefixId));

            // 添加通用前缀
            if (category != PrefixCategory.Universal &&
                _categoryLookup.TryGetValue(PrefixCategory.Universal, out var universalPrefixes))
                prefixes.AddRange(universalPrefixes.Select(p => p.PrefixId));

            return prefixes.ToArray();
        }

        public List<PrefixData> GetPrefixesByTier(PrefixTier tier)
        {
            if (_tierLookup == null) Initialize();
            return _tierLookup.TryGetValue(tier, out var prefixes)
                ? new List<PrefixData>(prefixes)
                : new List<PrefixData>();
        }

        /// <summary>
        /// 获取该类别的最佳前缀
        /// </summary>
        public PrefixData GetBestPrefix(PrefixCategory category)
        {
            return GetPrefixesByCategory(category)
                .Select(id => GetPrefix(id))
                .Where(p => p != null)
                .OrderByDescending(p => p.CalculateScore())
                .FirstOrDefault();
        }

        public List<PrefixData> GetAllPrefixes()
        {
            return allPrefixes != null ? new List<PrefixData>(allPrefixes) : new List<PrefixData>();
        }
    }

    // ============================================================
    // 重铸系统（增强版）
    // ============================================================

    /// <summary>
    /// 重铸系统 - 类似泰拉瑞亚哥布林工匠
    /// 支持保底机制、统计追踪、批量重铸
    /// </summary>
    public class ReforgeSystem
    {
        private readonly System.Random _random;
        
        // 保底计数器（连续重铸未获得好前缀时增加好前缀概率）
        private Dictionary<int, int> _pityCounters = new();
        
        // 重铸统计
        private int _totalReforges;
        private int _totalGoldSpent;
        private Dictionary<PrefixTier, int> _tierCounts = new();

        public int TotalReforges => _totalReforges;
        public int TotalGoldSpent => _totalGoldSpent;

        public event Action<ReforgeResult> OnReforgeComplete;
        public event Action<ItemInstance, PrefixData> OnLegendaryObtained;

        public ReforgeSystem(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
            
            // 初始化tier统计
            foreach (PrefixTier tier in Enum.GetValues(typeof(PrefixTier)))
                _tierCounts[tier] = 0;
        }

        /// <summary>
        /// 计算重铸费用
        /// 费用 = 物品基础价格 × 品质倍率 × 当前前缀倍率 × 0.3
        /// </summary>
        public int CalculateReforgeCost(ItemInstance item)
        {
            var template = item?.Template;
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

            // 当前前缀影响价格
            var currentPrefix = PrefixDatabase.Instance?.GetPrefix(item.PrefixId);
            float prefixMultiplier = currentPrefix?.PriceMultiplier ?? 1f;

            return Mathf.RoundToInt(baseCost * rarityMultiplier * prefixMultiplier * 0.3f);
        }

        /// <summary>
        /// 计算折扣后的重铸费用
        /// </summary>
        public int CalculateReforgeCostWithDiscount(ItemInstance item, float discountPercent)
        {
            int baseCost = CalculateReforgeCost(item);
            return Mathf.RoundToInt(baseCost * (1f - discountPercent));
        }

        /// <summary>
        /// 检查物品是否可以重铸
        /// </summary>
        public bool CanReforge(ItemInstance item)
        {
            return item?.Template != null && item.Template.IsReforgeable;
        }

        /// <summary>
        /// 执行重铸
        /// </summary>
        public ReforgeResult Reforge(ItemInstance item, PrefixCategory category, int goldPaid = 0)
        {
            if (!CanReforge(item))
            {
                return new ReforgeResult
                {
                    Success = false,
                    ErrorMessage = "此物品无法重铸"
                };
            }

            int oldPrefixId = item.PrefixId;
            var oldPrefix = PrefixDatabase.Instance?.GetPrefix(oldPrefixId);
            
            int[] allowedPrefixes = PrefixDatabase.Instance?.GetPrefixesByCategory(category);

            if (allowedPrefixes == null || allowedPrefixes.Length == 0)
            {
                return new ReforgeResult
                {
                    Success = false,
                    ErrorMessage = "没有可用的前缀"
                };
            }

            // 获取或初始化保底计数器
            int itemHash = item.GetHashCode();
            if (!_pityCounters.ContainsKey(itemHash))
                _pityCounters[itemHash] = 0;

            // 选择新前缀（带保底机制）
            int newPrefixId = SelectWeightedPrefixWithPity(
                allowedPrefixes, oldPrefixId, _pityCounters[itemHash]);
            var newPrefix = PrefixDatabase.Instance?.GetPrefix(newPrefixId);

            // 更新保底计数器
            if (newPrefix != null)
            {
                if (newPrefix.Tier >= PrefixTier.Great)
                    _pityCounters[itemHash] = 0;  // 获得好前缀，重置保底
                else
                    _pityCounters[itemHash]++;    // 未获得好前缀，累加
            }

            // 应用新前缀
            item.SetPrefix(newPrefixId);

            // 更新统计
            _totalReforges++;
            _totalGoldSpent += goldPaid;
            if (newPrefix != null)
                _tierCounts[newPrefix.Tier]++;

            // 构建结果
            var result = new ReforgeResult
            {
                Success = true,
                Item = item,
                OldPrefixId = oldPrefixId,
                NewPrefixId = newPrefixId,
                OldPrefix = oldPrefix,
                NewPrefix = newPrefix,
                GoldSpent = goldPaid,
                IsUpgrade = ComparePrefix(newPrefix, oldPrefix) > 0,
                IsDowngrade = ComparePrefix(newPrefix, oldPrefix) < 0,
                IsSamePrefix = newPrefixId == oldPrefixId
            };

            OnReforgeComplete?.Invoke(result);
            
            // 传奇前缀特殊通知
            if (newPrefix?.IsLegendary == true)
                OnLegendaryObtained?.Invoke(item, newPrefix);

            return result;
        }

        /// <summary>
        /// 批量重铸直到获得目标前缀
        /// </summary>
        public ReforgeUntilResult ReforgeUntilPrefix(
            ItemInstance item, 
            PrefixCategory category,
            int targetPrefixId, 
            int maxAttempts,
            Func<int> getGold, 
            Action<int> spendGold)
        {
            var attempts = new List<ReforgeResult>();
            int totalCost = 0;
            bool found = false;

            for (int i = 0; i < maxAttempts; i++)
            {
                int cost = CalculateReforgeCost(item);
                if (getGold() < cost)
                {
                    return new ReforgeUntilResult
                    {
                        Success = false,
                        Attempts = attempts,
                        TotalGoldSpent = totalCost,
                        ErrorMessage = "金币不足"
                    };
                }

                spendGold(cost);
                totalCost += cost;

                var result = Reforge(item, category, cost);
                attempts.Add(result);

                if (result.NewPrefixId == targetPrefixId)
                {
                    found = true;
                    break;
                }
            }

            return new ReforgeUntilResult
            {
                Success = found,
                Attempts = attempts,
                TotalGoldSpent = totalCost,
                FinalPrefix = PrefixDatabase.Instance?.GetPrefix(item.PrefixId),
                ErrorMessage = found ? null : $"尝试{maxAttempts}次后未获得目标前缀"
            };
        }

        /// <summary>
        /// 批量重铸直到获得指定品质或更高
        /// </summary>
        public ReforgeUntilResult ReforgeUntilTier(
            ItemInstance item, 
            PrefixCategory category,
            PrefixTier minTier, 
            int maxAttempts,
            Func<int> getGold, 
            Action<int> spendGold)
        {
            var attempts = new List<ReforgeResult>();
            int totalCost = 0;
            bool found = false;

            for (int i = 0; i < maxAttempts; i++)
            {
                int cost = CalculateReforgeCost(item);
                if (getGold() < cost)
                {
                    return new ReforgeUntilResult
                    {
                        Success = false,
                        Attempts = attempts,
                        TotalGoldSpent = totalCost,
                        ErrorMessage = "金币不足"
                    };
                }

                spendGold(cost);
                totalCost += cost;

                var result = Reforge(item, category, cost);
                attempts.Add(result);

                if (result.NewPrefix?.Tier >= minTier)
                {
                    found = true;
                    break;
                }
            }

            return new ReforgeUntilResult
            {
                Success = found,
                Attempts = attempts,
                TotalGoldSpent = totalCost,
                FinalPrefix = PrefixDatabase.Instance?.GetPrefix(item.PrefixId),
                ErrorMessage = found ? null : $"尝试{maxAttempts}次后未达到目标品质"
            };
        }

        /// <summary>
        /// 带保底机制的加权随机选择
        /// 保底机制：连续5次未获得Great及以上前缀，提高好前缀概率
        /// 连续10次以上，大幅提升
        /// </summary>
        private int SelectWeightedPrefixWithPity(int[] prefixIds, int excludeId, int pityCount)
        {
            var prefixes = prefixIds
                .Select(id => PrefixDatabase.Instance?.GetPrefix(id))
                .Where(p => p != null)
                .ToList();

            if (prefixes.Count == 0)
                return 0;

            // 计算权重（应用保底机制）
            var weights = new List<(PrefixData prefix, int weight)>();
            foreach (var prefix in prefixes)
            {
                int weight = prefix.ReforgeWeight;
                
                // 保底：连续5次未获得Great或更好的前缀
                if (pityCount >= 5 && prefix.Tier >= PrefixTier.Great)
                    weight = (int)(weight * (1 + pityCount * 0.1f));
                
                // 保底：连续10次以上，大幅提升好前缀概率
                if (pityCount >= 10 && prefix.Tier >= PrefixTier.Good)
                    weight *= 2;

                weights.Add((prefix, weight));
            }

            int totalWeight = weights.Sum(w => w.weight);
            int roll = _random.Next(totalWeight);

            int cumulative = 0;
            foreach (var (prefix, weight) in weights)
            {
                cumulative += weight;
                if (roll < cumulative)
                    return prefix.PrefixId;
            }

            return prefixes.Last().PrefixId;
        }

        /// <summary>
        /// 比较两个前缀的好坏
        /// 返回: >0 表示a更好, <0 表示b更好, =0 表示相同
        /// </summary>
        private int ComparePrefix(PrefixData a, PrefixData b)
        {
            if (a == null && b == null) return 0;
            if (a == null) return -1;
            if (b == null) return 1;
            
            return a.CalculateScore().CompareTo(b.CalculateScore());
        }

        /// <summary>
        /// 获取重铸统计
        /// </summary>
        public ReforgeStatistics GetStatistics()
        {
            return new ReforgeStatistics
            {
                TotalReforges = _totalReforges,
                TotalGoldSpent = _totalGoldSpent,
                TierCounts = new Dictionary<PrefixTier, int>(_tierCounts),
                AverageCostPerReforge = _totalReforges > 0 ? _totalGoldSpent / _totalReforges : 0
            };
        }

        /// <summary>
        /// 重置统计
        /// </summary>
        public void ResetStatistics()
        {
            _totalReforges = 0;
            _totalGoldSpent = 0;
            foreach (var key in _tierCounts.Keys.ToList())
                _tierCounts[key] = 0;
        }
    }

    /// <summary>
    /// 重铸结果
    /// </summary>
    public struct ReforgeResult
    {
        public bool Success;
        public string ErrorMessage;
        public ItemInstance Item;
        public int OldPrefixId;
        public int NewPrefixId;
        public PrefixData OldPrefix;
        public PrefixData NewPrefix;
        public int GoldSpent;
        public bool IsUpgrade;
        public bool IsDowngrade;
        public bool IsSamePrefix;
    }

    /// <summary>
    /// 批量重铸结果
    /// </summary>
    public struct ReforgeUntilResult
    {
        public bool Success;
        public List<ReforgeResult> Attempts;
        public int TotalGoldSpent;
        public PrefixData FinalPrefix;
        public string ErrorMessage;
        
        public int AttemptCount => Attempts?.Count ?? 0;
    }

    /// <summary>
    /// 重铸统计
    /// </summary>
    public struct ReforgeStatistics
    {
        public int TotalReforges;
        public int TotalGoldSpent;
        public Dictionary<PrefixTier, int> TierCounts;
        public int AverageCostPerReforge;
    }

    // ============================================================
    // 重铸NPC组件
    // ============================================================

    /// <summary>
    /// 重铸NPC（如哥布林工匠）- 挂载在NPC物体上
    /// </summary>
    public class ReforgeNPC : MonoBehaviour
    {
        [Header("NPC设置")]
        [SerializeField] private string npcName = "哥布林工匠";
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private float discountPercent = 0f;

        [Header("对话")]
        [SerializeField] private string[] greetings;
        [SerializeField] private string[] reforgeSuccessLines;
        [SerializeField] private string[] legendaryLines;

        private ReforgeSystem _reforgeSystem;

        public string NpcName => npcName;
        public float DiscountPercent => discountPercent;

        public event Action<string> OnNpcSpeak;

        private void Awake()
        {
            _reforgeSystem = new ReforgeSystem();
            _reforgeSystem.OnLegendaryObtained += OnLegendary;
        }

        public string GetGreeting()
        {
            if (greetings == null || greetings.Length == 0)
                return $"你好！我是{npcName}。想要重铸你的武器吗？";
            return greetings[UnityEngine.Random.Range(0, greetings.Length)];
        }

        public ReforgeResult Reforge(ItemInstance item, int goldPaid)
        {
            if (item?.Template == null || !(item.Template is IReforgeable reforgeable))
            {
                return new ReforgeResult { Success = false, ErrorMessage = "此物品无法重铸" };
            }

            var result = _reforgeSystem.Reforge(item, reforgeable.PrefixCategory, goldPaid);
            
            if (result.Success)
            {
                string line = GetReforgeSuccessLine(result);
                OnNpcSpeak?.Invoke(line);
            }

            return result;
        }

        public int GetReforgeCost(ItemInstance item)
        {
            return _reforgeSystem.CalculateReforgeCostWithDiscount(item, discountPercent);
        }

        private string GetReforgeSuccessLine(ReforgeResult result)
        {
            if (result.NewPrefix?.IsLegendary == true && legendaryLines?.Length > 0)
                return legendaryLines[UnityEngine.Random.Range(0, legendaryLines.Length)];
            
            if (result.IsUpgrade)
                return "不错的结果！这下你的武器更强了。";
            if (result.IsDowngrade)
                return "嗯...下次运气会更好的。再试一次？";
            
            if (reforgeSuccessLines?.Length > 0)
                return reforgeSuccessLines[UnityEngine.Random.Range(0, reforgeSuccessLines.Length)];
            
            return "重铸完成！";
        }

        private void OnLegendary(ItemInstance item, PrefixData prefix)
        {
            Debug.Log($"[ReforgeNPC] 获得传奇前缀: {prefix.DisplayName}!");
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}
