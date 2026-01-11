using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CombatSystem
{
    using ItemSystem.Core;
    
    // ============================================================
    // 天赋树系统 - 角色成长与专精
    // 
    // 设计思路：
    // 1. 多棵天赋树（战斗系、辅助系、生活系）
    // 2. 层级解锁（需要在前置层投入足够点数）
    // 3. 前置天赋要求
    // 4. 多等级天赋（可多次升级）
    // 5. 天赋效果（属性加成、技能解锁、配方解锁）
    // ============================================================

    /// <summary>
    /// 天赋节点数据 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewTalent", menuName = "CombatSystem/Talent")]
    public class TalentData : ScriptableObject
    {
        [Header("基础信息")]
        [SerializeField] private string talentId;
        [SerializeField] private string talentName;
        [TextArea(3, 5)]
        [SerializeField] private string description;
        [SerializeField] private Sprite icon;
        
        [Header("天赋类型")]
        [SerializeField] private TalentType talentType;
        [SerializeField] private TalentTree belongsToTree;
        [SerializeField] private int tier;  // 层级（0=起始，越高越深）
        
        [Header("解锁条件")]
        [SerializeField] private int pointCost = 1;
        [SerializeField] private int requiredLevel = 1;
        [SerializeField] private int maxRanks = 1;  // 最大等级
        [SerializeField] private string[] prerequisiteTalentIds;  // 前置天赋
        [SerializeField] private int requiredPointsInTree;  // 需要在本树投入的总点数
        
        [Header("效果")]
        [SerializeField] private TalentEffect[] effects;
        
        [Header("解锁内容")]
        [SerializeField] private int unlockedSkillId;  // 解锁的技能ID
        [SerializeField] private int[] unlockedRecipeIds;  // 解锁的配方ID
        
        // 属性访问器
        public string TalentId => talentId;
        public string TalentName => talentName;
        public string Description => description;
        public Sprite Icon => icon;
        public TalentType TalentType => talentType;
        public TalentTree BelongsToTree => belongsToTree;
        public int Tier => tier;
        public int PointCost => pointCost;
        public int RequiredLevel => requiredLevel;
        public int MaxRanks => maxRanks;
        public string[] PrerequisiteTalentIds => prerequisiteTalentIds;
        public int RequiredPointsInTree => requiredPointsInTree;
        public TalentEffect[] Effects => effects;
        public int UnlockedSkillId => unlockedSkillId;
        public int[] UnlockedRecipeIds => unlockedRecipeIds;
        
        /// <summary>
        /// 获取指定等级的效果描述
        /// </summary>
        public string GetEffectDescription(int rank)
        {
            if (effects == null || effects.Length == 0)
                return description;
            
            var lines = new List<string>();
            foreach (var effect in effects)
            {
                float value = effect.GetValueAtRank(rank);
                lines.Add(effect.FormatDescription(value));
            }
            
            return string.Join("\n", lines);
        }
        
        /// <summary>
        /// 获取下一级预览
        /// </summary>
        public string GetNextRankPreview(int currentRank)
        {
            if (currentRank >= maxRanks) return "已满级";
            return GetEffectDescription(currentRank + 1);
        }
    }
    
    /// <summary>
    /// 天赋类型
    /// </summary>
    public enum TalentType
    {
        Passive,        // 被动效果
        Active,         // 主动技能
        Modifier,       // 技能增强
        Keystone,       // 核心天赋
        Utility         // 工具类
    }
    
    /// <summary>
    /// 天赋树类型
    /// </summary>
    public enum TalentTree
    {
        // 战斗系
        Warrior,        // 战士 - 近战、防御
        Ranger,         // 游侠 - 远程、敏捷
        Mage,           // 法师 - 魔法、元素
        
        // 辅助系
        Support,        // 辅助 - 治疗、增益
        Debuffer,       // 削弱 - 减益、控制
        
        // 生活系
        Crafting,       // 制作 - 锻造、炼金
        Gathering,      // 采集
        Trading,        // 贸易
        
        // 通用
        General
    }
    
    /// <summary>
    /// 天赋效果
    /// </summary>
    [Serializable]
    public class TalentEffect
    {
        [Header("效果类型")]
        public TalentEffectType effectType;
        
        [Header("数值")]
        public float baseValue;
        public float valuePerRank;
        
        [Header("目标")]
        public StatType targetStat;
        public ElementType targetElement;
        public WeaponCategory targetWeapon;
        
        [Header("条件")]
        public TalentCondition condition;
        public float conditionValue;
        
        [Header("描述模板")]
        [Tooltip("使用 {0} 作为数值占位符")]
        public string descriptionTemplate = "效果 +{0}";
        
        /// <summary>
        /// 获取指定等级的效果值
        /// </summary>
        public float GetValueAtRank(int rank)
        {
            return baseValue + valuePerRank * (rank - 1);
        }
        
        /// <summary>
        /// 格式化描述
        /// </summary>
        public string FormatDescription(float value)
        {
            string valueStr = effectType switch
            {
                TalentEffectType.StatPercent => $"{value * 100:F0}%",
                TalentEffectType.CriticalChance => $"{value * 100:F1}%",
                TalentEffectType.DamageReduction => $"{value * 100:F0}%",
                _ => value.ToString("F0")
            };
            
            return string.Format(descriptionTemplate, valueStr);
        }
    }
    
    public enum TalentEffectType
    {
        // 属性增强
        StatFlat,
        StatPercent,
        
        // 战斗增强
        DamageFlat,
        DamagePercent,
        CriticalChance,
        CriticalDamage,
        AttackSpeed,
        
        // 防御增强
        DamageReduction,
        BlockChance,
        DodgeChance,
        
        // 元素增强
        ElementalDamage,
        ElementalResist,
        
        // 武器专精
        WeaponDamage,
        WeaponProficiency,
        
        // 资源增强
        HealthRegen,
        ManaRegen,
        SkillPointRegen,
        
        // 经济增强
        GoldFind,
        ItemFind,
        CraftingBonus,
        ReforgeCostReduce,
        
        // 特殊
        UnlockSkill,
        UnlockRecipe,
        SpecialAbility
    }
    
    public enum TalentCondition
    {
        None,
        HealthAbove,
        HealthBelow,
        InCombat,
        OutOfCombat,
        HasBuff,
        HasDebuff,
        EnemyCount,
        ConsecutiveHits,
        KillStreak
    }
    
    // ============================================================
    // 角色天赋管理器
    // ============================================================
    
    /// <summary>
    /// 角色天赋管理器
    /// </summary>
    [Serializable]
    public class CharacterTalentManager
    {
        [SerializeField] private int availablePoints;
        [SerializeField] private List<TalentAllocation> allocations = new();
        
        // 运行时缓存
        private Dictionary<string, int> _talentRanks;
        private Dictionary<TalentTree, int> _pointsPerTree;
        private List<TalentEffect> _activeEffects;
        private bool _isDirty = true;
        
        public int AvailablePoints => availablePoints;
        public IReadOnlyList<TalentAllocation> Allocations => allocations;
        
        public event Action<TalentData, int> OnTalentLearned;
        public event Action<TalentData> OnTalentUnlearned;
        public event Action OnTalentsReset;
        public event Action OnTalentsChanged;
        
        #region 初始化
        
        public void Initialize()
        {
            RebuildCache();
        }
        
        private void RebuildCache()
        {
            _talentRanks = new Dictionary<string, int>();
            _pointsPerTree = new Dictionary<TalentTree, int>();
            _activeEffects = new List<TalentEffect>();
            
            // 初始化所有树的点数为0
            foreach (TalentTree tree in Enum.GetValues(typeof(TalentTree)))
            {
                _pointsPerTree[tree] = 0;
            }
            
            // 遍历所有分配
            foreach (var allocation in allocations)
            {
                if (allocation.TalentData == null) continue;
                
                _talentRanks[allocation.TalentData.TalentId] = allocation.Rank;
                _pointsPerTree[allocation.TalentData.BelongsToTree] += 
                    allocation.TalentData.PointCost * allocation.Rank;
                
                // 收集激活的效果
                if (allocation.TalentData.Effects != null)
                {
                    foreach (var effect in allocation.TalentData.Effects)
                    {
                        var clone = new TalentEffect
                        {
                            effectType = effect.effectType,
                            baseValue = effect.GetValueAtRank(allocation.Rank),
                            valuePerRank = 0,
                            targetStat = effect.targetStat,
                            targetElement = effect.targetElement,
                            targetWeapon = effect.targetWeapon,
                            condition = effect.condition,
                            conditionValue = effect.conditionValue
                        };
                        _activeEffects.Add(clone);
                    }
                }
            }
            
            _isDirty = false;
        }
        
        private void EnsureCache()
        {
            if (_isDirty || _talentRanks == null)
                RebuildCache();
        }
        
        #endregion
        
        #region 天赋点管理
        
        /// <summary>
        /// 添加天赋点（升级时调用）
        /// </summary>
        public void AddTalentPoints(int points)
        {
            availablePoints += points;
            OnTalentsChanged?.Invoke();
        }
        
        /// <summary>
        /// 获取特定天赋树已投入的点数
        /// </summary>
        public int GetPointsInTree(TalentTree tree)
        {
            EnsureCache();
            return _pointsPerTree.TryGetValue(tree, out int points) ? points : 0;
        }
        
        /// <summary>
        /// 获取天赋的当前等级
        /// </summary>
        public int GetTalentRank(string talentId)
        {
            EnsureCache();
            return _talentRanks.TryGetValue(talentId, out int rank) ? rank : 0;
        }
        
        /// <summary>
        /// 检查是否已学习天赋
        /// </summary>
        public bool HasTalent(string talentId)
        {
            return GetTalentRank(talentId) > 0;
        }
        
        #endregion
        
        #region 学习天赋
        
        /// <summary>
        /// 检查是否可以学习天赋
        /// </summary>
        public TalentLearnResult CanLearnTalent(TalentData talent, int characterLevel)
        {
            EnsureCache();
            
            if (talent == null)
                return new TalentLearnResult { CanLearn = false, Reason = "无效的天赋" };
            
            // 检查是否已满级
            int currentRank = GetTalentRank(talent.TalentId);
            if (currentRank >= talent.MaxRanks)
                return new TalentLearnResult { CanLearn = false, Reason = "天赋已满级" };
            
            // 检查天赋点
            if (availablePoints < talent.PointCost)
                return new TalentLearnResult { CanLearn = false, Reason = $"天赋点不足（需要{talent.PointCost}点）" };
            
            // 检查等级要求
            if (characterLevel < talent.RequiredLevel)
                return new TalentLearnResult { CanLearn = false, Reason = $"需要角色等级{talent.RequiredLevel}" };
            
            // 检查天赋树点数要求
            int pointsInTree = GetPointsInTree(talent.BelongsToTree);
            if (pointsInTree < talent.RequiredPointsInTree)
                return new TalentLearnResult 
                { 
                    CanLearn = false, 
                    Reason = $"需要在{GetTreeName(talent.BelongsToTree)}投入{talent.RequiredPointsInTree}点" 
                };
            
            // 检查前置天赋
            if (talent.PrerequisiteTalentIds != null)
            {
                foreach (var prereqId in talent.PrerequisiteTalentIds)
                {
                    if (!HasTalent(prereqId))
                    {
                        var prereqTalent = TalentDatabase.Instance?.GetTalent(prereqId);
                        string prereqName = prereqTalent?.TalentName ?? prereqId;
                        return new TalentLearnResult 
                        { 
                            CanLearn = false, 
                            Reason = $"需要先学习：{prereqName}" 
                        };
                    }
                }
            }
            
            return new TalentLearnResult { CanLearn = true };
        }
        
        /// <summary>
        /// 学习天赋
        /// </summary>
        public TalentLearnResult LearnTalent(TalentData talent, int characterLevel)
        {
            var check = CanLearnTalent(talent, characterLevel);
            if (!check.CanLearn)
                return check;
            
            // 扣除天赋点
            availablePoints -= talent.PointCost;
            
            // 更新或添加分配
            var existing = allocations.Find(a => a.TalentData == talent);
            if (existing != null)
            {
                existing.Rank++;
            }
            else
            {
                allocations.Add(new TalentAllocation
                {
                    TalentData = talent,
                    Rank = 1
                });
            }
            
            _isDirty = true;
            RebuildCache();
            
            int newRank = GetTalentRank(talent.TalentId);
            OnTalentLearned?.Invoke(talent, newRank);
            OnTalentsChanged?.Invoke();
            
            return new TalentLearnResult 
            { 
                CanLearn = true, 
                Success = true, 
                NewRank = newRank 
            };
        }
        
        private string GetTreeName(TalentTree tree)
        {
            return tree switch
            {
                TalentTree.Warrior => "战士",
                TalentTree.Ranger => "游侠",
                TalentTree.Mage => "法师",
                TalentTree.Support => "辅助",
                TalentTree.Debuffer => "削弱",
                TalentTree.Crafting => "制作",
                TalentTree.Gathering => "采集",
                TalentTree.Trading => "贸易",
                TalentTree.General => "通用",
                _ => tree.ToString()
            };
        }
        
        #endregion
        
        #region 重置天赋
        
        /// <summary>
        /// 重置所有天赋
        /// </summary>
        public void ResetAllTalents()
        {
            foreach (var allocation in allocations)
            {
                if (allocation.TalentData != null)
                    availablePoints += allocation.TalentData.PointCost * allocation.Rank;
            }
            
            allocations.Clear();
            _isDirty = true;
            RebuildCache();
            
            OnTalentsReset?.Invoke();
            OnTalentsChanged?.Invoke();
        }
        
        /// <summary>
        /// 重置特定天赋树
        /// </summary>
        public void ResetTree(TalentTree tree)
        {
            var toRemove = allocations.Where(a => a.TalentData?.BelongsToTree == tree).ToList();
            
            foreach (var allocation in toRemove)
            {
                availablePoints += allocation.TalentData.PointCost * allocation.Rank;
                allocations.Remove(allocation);
            }
            
            _isDirty = true;
            RebuildCache();
            OnTalentsChanged?.Invoke();
        }
        
        #endregion
        
        #region 效果查询
        
        /// <summary>
        /// 获取所有激活的天赋效果
        /// </summary>
        public List<TalentEffect> GetActiveEffects()
        {
            EnsureCache();
            return new List<TalentEffect>(_activeEffects);
        }
        
        /// <summary>
        /// 获取特定类型的效果总值
        /// </summary>
        public float GetEffectValue(TalentEffectType effectType, 
            StatType stat = StatType.Strength,
            ElementType element = ElementType.None,
            WeaponCategory weapon = WeaponCategory.None)
        {
            EnsureCache();
            
            float total = 0f;
            foreach (var effect in _activeEffects)
            {
                if (effect.effectType != effectType) continue;
                
                bool matches = true;
                if (effectType == TalentEffectType.StatFlat || 
                    effectType == TalentEffectType.StatPercent)
                {
                    matches = effect.targetStat == stat;
                }
                else if (effectType == TalentEffectType.ElementalDamage ||
                         effectType == TalentEffectType.ElementalResist)
                {
                    matches = effect.targetElement == element || 
                              effect.targetElement == ElementType.None;
                }
                else if (effectType == TalentEffectType.WeaponDamage)
                {
                    matches = effect.targetWeapon == weapon ||
                              effect.targetWeapon == WeaponCategory.None;
                }
                
                if (matches)
                    total += effect.baseValue;
            }
            
            return total;
        }
        
        /// <summary>
        /// 获取解锁的技能ID列表
        /// </summary>
        public List<int> GetUnlockedSkillIds()
        {
            var skills = new List<int>();
            foreach (var allocation in allocations)
            {
                if (allocation.TalentData?.UnlockedSkillId > 0)
                    skills.Add(allocation.TalentData.UnlockedSkillId);
            }
            return skills;
        }
        
        /// <summary>
        /// 获取解锁的配方ID列表
        /// </summary>
        public List<int> GetUnlockedRecipeIds()
        {
            var recipes = new List<int>();
            foreach (var allocation in allocations)
            {
                if (allocation.TalentData?.UnlockedRecipeIds != null)
                    recipes.AddRange(allocation.TalentData.UnlockedRecipeIds);
            }
            return recipes.Distinct().ToList();
        }
        
        #endregion
        
        #region 应用效果
        
        /// <summary>
        /// 应用天赋效果到角色属性
        /// </summary>
        public void ApplyToCharacter(Character character)
        {
            EnsureCache();
            
            foreach (var effect in _activeEffects)
            {
                ApplyEffect(character, effect);
            }
        }
        
        private void ApplyEffect(Character character, TalentEffect effect)
        {
            switch (effect.effectType)
            {
                case TalentEffectType.StatFlat:
                    character.Stats.AddModifier(new StatModifier(
                        effect.targetStat, ModifierType.Flat, effect.baseValue, "Talent"));
                    break;
                    
                case TalentEffectType.StatPercent:
                    character.Stats.AddModifier(new StatModifier(
                        effect.targetStat, ModifierType.PercentAdd, effect.baseValue, "Talent"));
                    break;
                    
                case TalentEffectType.CriticalChance:
                    character.Stats.AddModifier(new StatModifier(
                        StatType.CriticalRate, ModifierType.Flat, effect.baseValue, "Talent"));
                    break;
                    
                case TalentEffectType.CriticalDamage:
                    character.Stats.AddModifier(new StatModifier(
                        StatType.CriticalDamage, ModifierType.Flat, effect.baseValue, "Talent"));
                    break;
                    
                case TalentEffectType.ElementalDamage:
                    character.CombatCache.AddElementalDamageBonus(
                        effect.targetElement, effect.baseValue);
                    break;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// 天赋分配记录
    /// </summary>
    [Serializable]
    public class TalentAllocation
    {
        public TalentData TalentData;
        public int Rank;
    }
    
    /// <summary>
    /// 天赋学习结果
    /// </summary>
    public struct TalentLearnResult
    {
        public bool CanLearn;
        public bool Success;
        public string Reason;
        public int NewRank;
    }
    
    // ============================================================
    // 天赋数据库
    // ============================================================
    
    /// <summary>
    /// 天赋数据库
    /// </summary>
    [CreateAssetMenu(fileName = "TalentDatabase", menuName = "CombatSystem/Databases/TalentDatabase")]
    public class TalentDatabase : ScriptableObject
    {
        private static TalentDatabase _instance;
        public static TalentDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<TalentDatabase>("Databases/TalentDatabase");
                    _instance?.Initialize();
                }
                return _instance;
            }
        }
        
        [SerializeField] private TalentData[] allTalents;
        
        [Header("天赋点配置")]
        [SerializeField] private int basePointsPerLevel = 1;
        [SerializeField] private int[] bonusPointsAtLevels;
        
        private Dictionary<string, TalentData> _talentLookup;
        private Dictionary<TalentTree, List<TalentData>> _treeLookup;
        private Dictionary<int, List<TalentData>> _tierLookup;
        private bool _initialized;
        
        public int BasePointsPerLevel => basePointsPerLevel;
        
        private void OnEnable() => Initialize();
        
        public void Initialize()
        {
            if (_initialized) return;
            
            _talentLookup = new Dictionary<string, TalentData>();
            _treeLookup = new Dictionary<TalentTree, List<TalentData>>();
            _tierLookup = new Dictionary<int, List<TalentData>>();
            
            if (allTalents == null)
            {
                _initialized = true;
                return;
            }
            
            foreach (var talent in allTalents)
            {
                if (talent == null) continue;
                
                _talentLookup[talent.TalentId] = talent;
                
                if (!_treeLookup.ContainsKey(talent.BelongsToTree))
                    _treeLookup[talent.BelongsToTree] = new List<TalentData>();
                _treeLookup[talent.BelongsToTree].Add(talent);
                
                if (!_tierLookup.ContainsKey(talent.Tier))
                    _tierLookup[talent.Tier] = new List<TalentData>();
                _tierLookup[talent.Tier].Add(talent);
            }
            
            _initialized = true;
        }
        
        public TalentData GetTalent(string talentId)
        {
            if (_talentLookup == null) Initialize();
            return _talentLookup.TryGetValue(talentId, out var talent) ? talent : null;
        }
        
        public List<TalentData> GetTalentsByTree(TalentTree tree)
        {
            if (_treeLookup == null) Initialize();
            return _treeLookup.TryGetValue(tree, out var talents) 
                ? new List<TalentData>(talents) 
                : new List<TalentData>();
        }
        
        public List<TalentData> GetTalentsByTier(int tier)
        {
            if (_tierLookup == null) Initialize();
            return _tierLookup.TryGetValue(tier, out var talents)
                ? new List<TalentData>(talents)
                : new List<TalentData>();
        }
        
        /// <summary>
        /// 计算指定等级应获得的总天赋点
        /// </summary>
        public int CalculateTotalPointsAtLevel(int level)
        {
            int total = level * basePointsPerLevel;
            
            if (bonusPointsAtLevels != null)
            {
                for (int i = 0; i < bonusPointsAtLevels.Length && i < level; i++)
                {
                    total += bonusPointsAtLevels[i];
                }
            }
            
            return total;
        }
        
        /// <summary>
        /// 获取天赋树结构（用于UI）
        /// </summary>
        public TalentTreeStructure GetTreeStructure(TalentTree tree)
        {
            var talents = GetTalentsByTree(tree);
            var structure = new TalentTreeStructure
            {
                Tree = tree,
                Tiers = new List<TalentTreeTier>()
            };
            
            var tierGroups = talents.GroupBy(t => t.Tier).OrderBy(g => g.Key);
            foreach (var group in tierGroups)
            {
                structure.Tiers.Add(new TalentTreeTier
                {
                    TierIndex = group.Key,
                    Talents = group.ToList()
                });
            }
            
            return structure;
        }
    }
    
    public class TalentTreeStructure
    {
        public TalentTree Tree;
        public List<TalentTreeTier> Tiers;
    }
    
    public class TalentTreeTier
    {
        public int TierIndex;
        public List<TalentData> Talents;
    }
    
    // ============================================================
    // 预设天赋效果辅助类
    // ============================================================
    
    /// <summary>
    /// 常用天赋效果预设
    /// </summary>
    public static class TalentPresets
    {
        public static TalentEffect CreateStatBonus(StatType stat, float flatValue, float perRank = 0)
        {
            return new TalentEffect
            {
                effectType = TalentEffectType.StatFlat,
                targetStat = stat,
                baseValue = flatValue,
                valuePerRank = perRank,
                descriptionTemplate = $"{GetStatName(stat)} +{{0}}"
            };
        }
        
        public static TalentEffect CreateStatPercentBonus(StatType stat, float percent, float perRank = 0)
        {
            return new TalentEffect
            {
                effectType = TalentEffectType.StatPercent,
                targetStat = stat,
                baseValue = percent,
                valuePerRank = perRank,
                descriptionTemplate = $"{GetStatName(stat)} +{{0}}"
            };
        }
        
        public static TalentEffect CreateCritChance(float value, float perRank = 0)
        {
            return new TalentEffect
            {
                effectType = TalentEffectType.CriticalChance,
                baseValue = value,
                valuePerRank = perRank,
                descriptionTemplate = "暴击率 +{0}"
            };
        }
        
        public static TalentEffect CreateElementalDamage(ElementType element, float value)
        {
            string elementName = element switch
            {
                ElementType.Fire => "火焰",
                ElementType.Ice => "冰霜",
                ElementType.Lightning => "雷电",
                ElementType.Poison => "毒素",
                ElementType.Holy => "神圣",
                ElementType.Dark => "暗影",
                _ => element.ToString()
            };
            
            return new TalentEffect
            {
                effectType = TalentEffectType.ElementalDamage,
                targetElement = element,
                baseValue = value,
                descriptionTemplate = $"{elementName}伤害 +{{0}}"
            };
        }
        
        public static TalentEffect CreateWeaponMastery(WeaponCategory weapon, float damageBonus)
        {
            string weaponName = weapon switch
            {
                WeaponCategory.Blunt => "钝器",
                WeaponCategory.Sharp => "利器",
                WeaponCategory.Bow => "弓箭",
                WeaponCategory.Gun => "枪械",
                WeaponCategory.Magic => "法杖",
                WeaponCategory.Explosive => "爆炸物",
                _ => weapon.ToString()
            };
            
            return new TalentEffect
            {
                effectType = TalentEffectType.WeaponDamage,
                targetWeapon = weapon,
                baseValue = damageBonus,
                descriptionTemplate = $"{weaponName}伤害 +{{0}}"
            };
        }
        
        private static string GetStatName(StatType stat)
        {
            return stat switch
            {
                StatType.Strength => "力量",
                StatType.Reaction => "敏捷",
                StatType.Wisdom => "智力",
                StatType.Constitution => "体质",
                StatType.Perception => "精神",
                StatType.Luck => "幸运",
                StatType.PhysicalAttack => "物理攻击",
                StatType.MagicAttack => "魔法攻击",
                StatType.PhysicalDefense => "物理防御",
                StatType.MagicDefense => "魔法防御",
                StatType.MaxHealth => "最大生命",
                _ => stat.ToString()
            };
        }
    }
}
