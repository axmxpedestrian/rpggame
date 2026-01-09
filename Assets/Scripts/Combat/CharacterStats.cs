using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CombatSystem
{
    using ItemSystem.Core;
    
    /// <summary>
    /// 角色属性系统 - 支持修饰器和缓存
    /// 基础属性 → 战斗属性 → 最终属性
    /// </summary>
    [Serializable]
    public class CharacterStats
    {
        #region 基础属性（6维）
        
        [Header("基础属性")]
        [SerializeField] private int baseConstitution = 10;  // 体质 - 影响生命、物理格挡
        [SerializeField] private int baseStrength = 10;      // 力量 - 影响物攻、负重
        [SerializeField] private int basePerception = 10;    // 感知 - 影响命中、暴击
        [SerializeField] private int baseReaction = 10;      // 反应 - 影响速度、闪避
        [SerializeField] private int baseWisdom = 10;        // 智慧 - 影响魔攻、魔防、技能点
        [SerializeField] private int baseLuck = 10;          // 幸运 - 影响暴击、掉落、重铸
        
        #endregion
        
        #region 修饰器系统
        
        private readonly Dictionary<StatType, List<StatModifier>> _modifiers = new();
        private readonly Dictionary<StatType, float> _cachedValues = new();
        private readonly HashSet<StatType> _dirtyFlags = new();
        private bool _allDirty = true;
        
        #endregion
        
        #region 属性访问器（带缓存）
        
        // 基础属性（计算后）
        public int Constitution => Mathf.RoundToInt(GetStat(StatType.Constitution));
        public int Strength => Mathf.RoundToInt(GetStat(StatType.Strength));
        public int Perception => Mathf.RoundToInt(GetStat(StatType.Perception));
        public int Reaction => Mathf.RoundToInt(GetStat(StatType.Reaction));
        public int Wisdom => Mathf.RoundToInt(GetStat(StatType.Wisdom));
        public int Luck => Mathf.RoundToInt(GetStat(StatType.Luck));
        
        // 战斗属性（从基础属性派生）
        public int MaxHealth => CalculateMaxHealth();
        public int MaxPhysicalSP => CalculateMaxPhysicalSP();
        public int MaxMagicSP => CalculateMaxMagicSP();
        public int PhysicalAttack => CalculatePhysicalAttack();
        public int MagicAttack => CalculateMagicAttack();
        public int PhysicalDefense => CalculatePhysicalDefense();
        public int MagicDefense => CalculateMagicDefense();
        public float Speed => CalculateSpeed();
        public float CriticalRate => CalculateCriticalRate();
        public float CriticalDamage => CalculateCriticalDamage();
        public float Accuracy => CalculateAccuracy();
        public float Evasion => CalculateEvasion();
        public float PhysicalBlockRate => CalculatePhysicalBlockRate();
        public float MagicBlockRate => CalculateMagicBlockRate();
        
        #endregion
        
        #region 初始化
        
        public CharacterStats() { }
        
        public CharacterStats(int con, int str, int per, int rea, int wis, int luk)
        {
            baseConstitution = con;
            baseStrength = str;
            basePerception = per;
            baseReaction = rea;
            baseWisdom = wis;
            baseLuck = luk;
            MarkAllDirty();
        }
        
        /// <summary>
        /// 从配置初始化
        /// </summary>
        public void Initialize(CharacterStatsConfig config)
        {
            if (config == null) return;
            
            baseConstitution = config.Constitution;
            baseStrength = config.Strength;
            basePerception = config.Perception;
            baseReaction = config.Reaction;
            baseWisdom = config.Wisdom;
            baseLuck = config.Luck;
            MarkAllDirty();
        }
        
        #endregion
        
        #region 修饰器管理
        
        /// <summary>
        /// 添加属性修饰器
        /// </summary>
        public void AddModifier(StatModifier modifier)
        {
            if (modifier == null) return;
            
            if (!_modifiers.ContainsKey(modifier.StatType))
                _modifiers[modifier.StatType] = new List<StatModifier>();
            
            _modifiers[modifier.StatType].Add(modifier);
            MarkDirty(modifier.StatType);
        }
        
        /// <summary>
        /// 移除属性修饰器
        /// </summary>
        public void RemoveModifier(StatModifier modifier)
        {
            if (modifier == null) return;
            
            if (_modifiers.TryGetValue(modifier.StatType, out var list))
            {
                list.Remove(modifier);
                MarkDirty(modifier.StatType);
            }
        }
        
        /// <summary>
        /// 移除指定来源的所有修饰器
        /// </summary>
        public void RemoveModifiersFromSource(object source)
        {
            foreach (var kvp in _modifiers)
            {
                int removed = kvp.Value.RemoveAll(m => m.Source == source);
                if (removed > 0)
                    MarkDirty(kvp.Key);
            }
        }
        
        /// <summary>
        /// 清除所有修饰器
        /// </summary>
        public void ClearAllModifiers()
        {
            _modifiers.Clear();
            MarkAllDirty();
        }
        
        #endregion
        
        #region 属性计算
        
        /// <summary>
        /// 获取属性最终值（带缓存）
        /// </summary>
        public float GetStat(StatType statType)
        {
            // 检查缓存
            if (!_allDirty && !_dirtyFlags.Contains(statType))
            {
                if (_cachedValues.TryGetValue(statType, out float cached))
                    return cached;
            }
            
            // 计算新值
            float baseValue = GetBaseValue(statType);
            float finalValue = CalculateFinalValue(statType, baseValue);
            
            // 更新缓存
            _cachedValues[statType] = finalValue;
            _dirtyFlags.Remove(statType);
            
            return finalValue;
        }
        
        private float GetBaseValue(StatType statType)
        {
            return statType switch
            {
                StatType.Constitution => baseConstitution,
                StatType.Strength => baseStrength,
                StatType.Perception => basePerception,
                StatType.Reaction => baseReaction,
                StatType.Wisdom => baseWisdom,
                StatType.Luck => baseLuck,
                _ => 0f
            };
        }
        
        /// <summary>
        /// 计算最终属性值
        /// 公式：(Base + FlatSum) * (1 + PercentAddSum) * PercentMultProduct
        /// </summary>
        private float CalculateFinalValue(StatType statType, float baseValue)
        {
            if (!_modifiers.TryGetValue(statType, out var modifiers) || modifiers.Count == 0)
                return baseValue;
            
            float flatSum = 0f;
            float percentAddSum = 0f;
            float percentMultProduct = 1f;
            
            foreach (var mod in modifiers)
            {
                switch (mod.ModifierType)
                {
                    case ModifierType.Flat:
                        flatSum += mod.Value;
                        break;
                    case ModifierType.PercentAdd:
                        percentAddSum += mod.Value;
                        break;
                    case ModifierType.PercentMult:
                        percentMultProduct *= (1f + mod.Value);
                        break;
                }
            }
            
            return (baseValue + flatSum) * (1f + percentAddSum) * percentMultProduct;
        }
        
        #endregion
        
        #region 战斗属性计算公式
        
        // 生命值 = 100 + 体质 * 15 + 等级 * 10
        private int CalculateMaxHealth()
        {
            float bonus = GetStat(StatType.MaxHealth);
            return Mathf.RoundToInt(100 + Constitution * 15 + bonus);
        }
        
        // 物理技能点 = 5 + 力量 / 5
        private int CalculateMaxPhysicalSP()
        {
            float bonus = GetStat(StatType.MaxPhysicalSkillPoints);
            return Mathf.RoundToInt(5 + Strength / 5f + bonus);
        }
        
        // 魔法技能点 = 5 + 智慧 / 5
        private int CalculateMaxMagicSP()
        {
            float bonus = GetStat(StatType.MaxMagicSkillPoints);
            return Mathf.RoundToInt(5 + Wisdom / 5f + bonus);
        }
        
        // 物理攻击 = 力量 * 2 + 装备加成
        private int CalculatePhysicalAttack()
        {
            float bonus = GetStat(StatType.PhysicalAttack);
            return Mathf.RoundToInt(Strength * 2 + bonus);
        }
        
        // 魔法攻击 = 智慧 * 2 + 装备加成
        private int CalculateMagicAttack()
        {
            float bonus = GetStat(StatType.MagicAttack);
            return Mathf.RoundToInt(Wisdom * 2 + bonus);
        }
        
        // 物理防御 = 体质 * 0.5 + 装备加成
        private int CalculatePhysicalDefense()
        {
            float bonus = GetStat(StatType.PhysicalDefense);
            return Mathf.RoundToInt(Constitution * 0.5f + bonus);
        }
        
        // 魔法防御 = 智慧 * 0.8 + 装备加成
        private int CalculateMagicDefense()
        {
            float bonus = GetStat(StatType.MagicDefense);
            return Mathf.RoundToInt(Wisdom * 0.8f + bonus);
        }
        
        // 速度 = 50 + 反应 * 2
        private float CalculateSpeed()
        {
            float bonus = GetStat(StatType.Speed);
            return 50f + Reaction * 2f + bonus;
        }
        
        // 暴击率 = 5% + 感知 * 0.3% + 幸运 * 0.2%
        private float CalculateCriticalRate()
        {
            float bonus = GetStat(StatType.CriticalRate);
            return 0.05f + Perception * 0.003f + Luck * 0.002f + bonus;
        }
        
        // 暴击伤害 = 150% + 感知 * 1%
        private float CalculateCriticalDamage()
        {
            return 1.5f + Perception * 0.01f;
        }
        
        // 命中率 = 90% + 感知 * 0.5%
        private float CalculateAccuracy()
        {
            float bonus = GetStat(StatType.Accuracy);
            return 0.9f + Perception * 0.005f + bonus;
        }
        
        // 闪避率 = 反应 * 0.3% + 幸运 * 0.1%
        private float CalculateEvasion()
        {
            float bonus = GetStat(StatType.Evasion);
            return Reaction * 0.003f + Luck * 0.001f + bonus;
        }
        
        // 物理格挡率 = 体质 * 0.2%
        private float CalculatePhysicalBlockRate()
        {
            float bonus = GetStat(StatType.PhysicalBlockRate);
            return Constitution * 0.002f + bonus;
        }
        
        // 魔法格挡率 = 智慧 * 0.15%
        private float CalculateMagicBlockRate()
        {
            float bonus = GetStat(StatType.MagicBlockRate);
            return Wisdom * 0.0015f + bonus;
        }
        
        #endregion
        
        #region 缓存管理
        
        private void MarkDirty(StatType statType)
        {
            _dirtyFlags.Add(statType);
            
            // 标记依赖此属性的派生属性也为脏
            MarkDependentsDirty(statType);
        }
        
        private void MarkAllDirty()
        {
            _allDirty = true;
            _cachedValues.Clear();
            _dirtyFlags.Clear();
        }
        
        private void MarkDependentsDirty(StatType statType)
        {
            // 基础属性改变会影响多个战斗属性
            switch (statType)
            {
                case StatType.Constitution:
                    _dirtyFlags.Add(StatType.MaxHealth);
                    _dirtyFlags.Add(StatType.PhysicalDefense);
                    _dirtyFlags.Add(StatType.PhysicalBlockRate);
                    break;
                case StatType.Strength:
                    _dirtyFlags.Add(StatType.PhysicalAttack);
                    _dirtyFlags.Add(StatType.MaxPhysicalSkillPoints);
                    break;
                case StatType.Perception:
                    _dirtyFlags.Add(StatType.Accuracy);
                    _dirtyFlags.Add(StatType.CriticalRate);
                    break;
                case StatType.Reaction:
                    _dirtyFlags.Add(StatType.Speed);
                    _dirtyFlags.Add(StatType.Evasion);
                    break;
                case StatType.Wisdom:
                    _dirtyFlags.Add(StatType.MagicAttack);
                    _dirtyFlags.Add(StatType.MagicDefense);
                    _dirtyFlags.Add(StatType.MaxMagicSkillPoints);
                    _dirtyFlags.Add(StatType.MagicBlockRate);
                    break;
                case StatType.Luck:
                    _dirtyFlags.Add(StatType.CriticalRate);
                    _dirtyFlags.Add(StatType.Evasion);
                    break;
            }
        }
        
        /// <summary>
        /// 强制刷新所有缓存
        /// </summary>
        public void RefreshAllCache()
        {
            _allDirty = false;
            _dirtyFlags.Clear();
            _cachedValues.Clear();
        }
        
        #endregion
        
        #region 调试
        
        public string GetDebugInfo()
        {
            return $"CON:{Constitution} STR:{Strength} PER:{Perception} " +
                   $"REA:{Reaction} WIS:{Wisdom} LUK:{Luck}\n" +
                   $"HP:{MaxHealth} PATK:{PhysicalAttack} MATK:{MagicAttack} " +
                   $"PDEF:{PhysicalDefense} MDEF:{MagicDefense}\n" +
                   $"SPD:{Speed:F1} CRIT:{CriticalRate:P1} ACC:{Accuracy:P1} EVA:{Evasion:P1}";
        }
        
        #endregion
    }
    
    /// <summary>
    /// 角色属性配置 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewCharacterStats", menuName = "CombatSystem/CharacterStatsConfig")]
    public class CharacterStatsConfig : ScriptableObject
    {
        [Header("基础属性")]
        public int Constitution = 10;
        public int Strength = 10;
        public int Perception = 10;
        public int Reaction = 10;
        public int Wisdom = 10;
        public int Luck = 10;
        
        [Header("成长率（每级）")]
        public float ConstitutionGrowth = 1f;
        public float StrengthGrowth = 1f;
        public float PerceptionGrowth = 1f;
        public float ReactionGrowth = 1f;
        public float WisdomGrowth = 1f;
        public float LuckGrowth = 0.5f;
    }
}
