using System;
using System.Collections.Generic;
using UnityEngine;

namespace CombatSystem
{
    using ItemSystem.Core;
    using ItemSystem.Equipment;
    using ItemSystem.Modifiers;
    
    /// <summary>
    /// 战斗属性缓存 - 分离伤害类型与元素
    /// 提供高频访问的属性缓存，避免重复计算
    /// </summary>
    [Serializable]
    public class CombatAttributeCache
    {
        private readonly Character _owner;
        
        #region 缓存标记
        
        private bool _isDirty = true;
        private int _lastUpdateFrame = -1;
        
        #endregion
        
        #region 基础战斗属性缓存
        
        // 攻击属性
        private int _cachedPhysicalAttack;
        private int _cachedMagicAttack;
        private float _cachedAttackSpeed;
        
        // 防御属性
        private int _cachedPhysicalDefense;
        private int _cachedMagicDefense;
        
        // 命中/回避
        private float _cachedAccuracy;
        private float _cachedEvasion;
        
        // 暴击
        private float _cachedCriticalRate;
        private float _cachedCriticalDamage;
        
        // 格挡
        private float _cachedPhysicalBlockRate;
        private float _cachedMagicBlockRate;
        private float _cachedBlockDamageReduction;
        
        #endregion
        
        #region 伤害类型加成（Physical/Magic/True 分离）
        
        // 伤害类型乘数
        private readonly Dictionary<DamageCategory, float> _damageTypeMultipliers = new()
        {
            { DamageCategory.Physical, 1f },
            { DamageCategory.Magic, 1f },
            { DamageCategory.True, 1f }
        };
        
        // 伤害类型抗性
        private readonly Dictionary<DamageCategory, float> _damageTypeResistances = new()
        {
            { DamageCategory.Physical, 0f },
            { DamageCategory.Magic, 0f },
            { DamageCategory.True, 0f }  // True伤害通常无法抵抗
        };
        
        #endregion
        
        #region 元素加成（与伤害类型独立）
        
        // 元素伤害加成
        private readonly Dictionary<ElementType, float> _elementalDamageBonuses = new();
        
        // 元素抗性（-100% ~ +100%，负值表示弱点）
        private readonly Dictionary<ElementType, float> _elementalResistances = new();
        
        #endregion
        
        #region 临时Buff
        
        private readonly Dictionary<BuffType, float> _temporaryBuffs = new();
        
        #endregion
        
        #region 武器相关缓存
        
        private WeaponCategory _cachedWeaponCategory;
        private ElementType _cachedWeaponElement;
        private DamageCategory _cachedWeaponDamageCategory;
        private int _cachedWeaponBaseDamage;
        private float _cachedWeaponCritChance;
        private float _cachedWeaponCritMultiplier;
        
        #endregion
        
        #region 构造函数
        
        public CombatAttributeCache(Character owner)
        {
            _owner = owner;
            InitializeDefaults();
        }
        
        private void InitializeDefaults()
        {
            // 初始化元素加成/抗性
            foreach (ElementType element in Enum.GetValues(typeof(ElementType)))
            {
                _elementalDamageBonuses[element] = 0f;
                _elementalResistances[element] = 0f;
            }
        }
        
        #endregion
        
        #region 公开属性（自动刷新缓存）
        
        public int PhysicalAttack { get { EnsureCache(); return _cachedPhysicalAttack; } }
        public int MagicAttack { get { EnsureCache(); return _cachedMagicAttack; } }
        public float AttackSpeed { get { EnsureCache(); return _cachedAttackSpeed; } }
        public int PhysicalDefense { get { EnsureCache(); return _cachedPhysicalDefense; } }
        public int MagicDefense { get { EnsureCache(); return _cachedMagicDefense; } }
        public float Accuracy { get { EnsureCache(); return _cachedAccuracy; } }
        public float Evasion { get { EnsureCache(); return _cachedEvasion; } }
        public float CriticalRate { get { EnsureCache(); return _cachedCriticalRate; } }
        public float CriticalDamage { get { EnsureCache(); return _cachedCriticalDamage; } }
        public float PhysicalBlockRate { get { EnsureCache(); return _cachedPhysicalBlockRate; } }
        public float MagicBlockRate { get { EnsureCache(); return _cachedMagicBlockRate; } }
        public float BlockDamageReduction { get { EnsureCache(); return _cachedBlockDamageReduction; } }
        
        // 武器属性
        public WeaponCategory WeaponCategory { get { EnsureCache(); return _cachedWeaponCategory; } }
        public ElementType WeaponElement { get { EnsureCache(); return _cachedWeaponElement; } }
        public DamageCategory WeaponDamageCategory { get { EnsureCache(); return _cachedWeaponDamageCategory; } }
        public int WeaponBaseDamage { get { EnsureCache(); return _cachedWeaponBaseDamage; } }
        public float WeaponCritChance { get { EnsureCache(); return _cachedWeaponCritChance; } }
        public float WeaponCritMultiplier { get { EnsureCache(); return _cachedWeaponCritMultiplier; } }
        
        #endregion
        
        #region 伤害类型查询
        
        /// <summary>
        /// 获取伤害类型乘数
        /// </summary>
        public float GetDamageTypeMultiplier(DamageCategory category)
        {
            float baseMultiplier = _damageTypeMultipliers.TryGetValue(category, out float m) ? m : 1f;
            
            // 应用临时Buff
            float buffBonus = 0f;
            switch (category)
            {
                case DamageCategory.Physical:
                    buffBonus = GetTempBuff(BuffType.Attack);
                    break;
                case DamageCategory.Magic:
                    buffBonus = GetTempBuff(BuffType.MagicAttack);
                    break;
            }
            
            return baseMultiplier + buffBonus;
        }
        
        /// <summary>
        /// 获取伤害类型抗性
        /// </summary>
        public float GetDamageTypeResistance(DamageCategory category)
        {
            float baseResist = _damageTypeResistances.TryGetValue(category, out float r) ? r : 0f;
            
            // 应用临时Buff
            float buffBonus = 0f;
            switch (category)
            {
                case DamageCategory.Physical:
                    buffBonus = GetTempBuff(BuffType.Defense);
                    break;
                case DamageCategory.Magic:
                    buffBonus = GetTempBuff(BuffType.MagicDefense);
                    break;
            }
            
            return baseResist + buffBonus * 0.01f; // 转换为百分比
        }
        
        /// <summary>
        /// 添加伤害类型乘数
        /// </summary>
        public void AddDamageMultiplier(float value, DamageCategory category = DamageCategory.Physical)
        {
            if (_damageTypeMultipliers.ContainsKey(category))
                _damageTypeMultipliers[category] += value;
        }
        
        /// <summary>
        /// 添加伤害乘数（所有类型）
        /// </summary>
        public void AddDamageMultiplier(float value)
        {
            _damageTypeMultipliers[DamageCategory.Physical] += value;
            _damageTypeMultipliers[DamageCategory.Magic] += value;
        }
        
        #endregion
        
        #region 元素查询
        
        /// <summary>
        /// 获取元素伤害加成
        /// </summary>
        public float GetElementalDamageBonus(ElementType element)
        {
            return _elementalDamageBonuses.TryGetValue(element, out float bonus) ? bonus : 0f;
        }
        
        /// <summary>
        /// 获取元素抗性
        /// </summary>
        public float GetElementalResistance(ElementType element)
        {
            float baseResist = _elementalResistances.TryGetValue(element, out float r) ? r : 0f;
            
            // 从角色获取装备提供的抗性
            float equipResist = _owner.GetElementResistance(element);
            
            return Mathf.Clamp(baseResist + equipResist, -1f, 0.9f); // 上限90%抗性
        }
        
        /// <summary>
        /// 添加元素伤害加成
        /// </summary>
        public void AddElementalDamageBonus(ElementType element, float value)
        {
            if (!_elementalDamageBonuses.ContainsKey(element))
                _elementalDamageBonuses[element] = 0f;
            _elementalDamageBonuses[element] += value;
        }
        
        /// <summary>
        /// 设置元素抗性
        /// </summary>
        public void SetElementalResistance(ElementType element, float value)
        {
            _elementalResistances[element] = Mathf.Clamp(value, -1f, 1f);
        }
        
        #endregion
        
        #region 临时Buff管理
        
        public void AddTemporaryBuff(BuffType type, float value)
        {
            if (!_temporaryBuffs.ContainsKey(type))
                _temporaryBuffs[type] = 0f;
            _temporaryBuffs[type] += value;
        }
        
        public void RemoveTemporaryBuff(BuffType type, float value)
        {
            if (_temporaryBuffs.ContainsKey(type))
            {
                _temporaryBuffs[type] -= value;
                if (Mathf.Approximately(_temporaryBuffs[type], 0f))
                    _temporaryBuffs.Remove(type);
            }
        }
        
        public float GetTempBuff(BuffType type)
        {
            return _temporaryBuffs.TryGetValue(type, out float v) ? v : 0f;
        }
        
        public void ClearAllTemporaryBuffs()
        {
            _temporaryBuffs.Clear();
        }
        
        #endregion
        
        #region 缓存管理
        
        /// <summary>
        /// 确保缓存有效
        /// </summary>
        private void EnsureCache()
        {
            // 每帧只计算一次
            if (_isDirty || _lastUpdateFrame != Time.frameCount)
            {
                RecalculateAll();
                _isDirty = false;
                _lastUpdateFrame = Time.frameCount;
            }
        }
        
        /// <summary>
        /// 标记缓存失效
        /// </summary>
        public void MarkAllDirty()
        {
            _isDirty = true;
        }
        
        /// <summary>
        /// 重新计算所有缓存值
        /// </summary>
        private void RecalculateAll()
        {
            var stats = _owner.Stats;
            
            // 基础战斗属性
            _cachedPhysicalAttack = stats.PhysicalAttack + Mathf.RoundToInt(GetTempBuff(BuffType.Attack));
            _cachedMagicAttack = stats.MagicAttack + Mathf.RoundToInt(GetTempBuff(BuffType.MagicAttack));
            _cachedAttackSpeed = 1f + GetTempBuff(BuffType.Speed);
            
            _cachedPhysicalDefense = stats.PhysicalDefense + Mathf.RoundToInt(GetTempBuff(BuffType.Defense));
            _cachedMagicDefense = stats.MagicDefense + Mathf.RoundToInt(GetTempBuff(BuffType.MagicDefense));
            
            _cachedAccuracy = stats.Accuracy + GetTempBuff(BuffType.Accuracy);
            _cachedEvasion = stats.Evasion + GetTempBuff(BuffType.Evasion);
            
            _cachedCriticalRate = stats.CriticalRate + GetTempBuff(BuffType.CriticalChance);
            _cachedCriticalDamage = stats.CriticalDamage + GetTempBuff(BuffType.CriticalDamage);
            
            _cachedPhysicalBlockRate = stats.PhysicalBlockRate;
            _cachedMagicBlockRate = stats.MagicBlockRate;
            _cachedBlockDamageReduction = 0.5f; // 格挡减少50%伤害
            
            // 武器属性
            RecalculateWeaponCache();
        }
        
        private void RecalculateWeaponCache()
        {
            var weaponInstance = _owner.EquippedWeapon;
            
            if (weaponInstance?.Template is Weapon weapon)
            {
                _cachedWeaponCategory = weapon.WeaponCategory;
                _cachedWeaponElement = weapon.Element;
                _cachedWeaponDamageCategory = weapon.DamageCategory;
                _cachedWeaponBaseDamage = weapon.GetFinalDamage(weaponInstance);
                _cachedWeaponCritChance = weapon.GetFinalCriticalChance(weaponInstance);
                _cachedWeaponCritMultiplier = weapon.CriticalMultiplier;
            }
            else
            {
                // 无武器时的默认值（空手）
                _cachedWeaponCategory = WeaponCategory.Blunt;
                _cachedWeaponElement = ElementType.None;
                _cachedWeaponDamageCategory = DamageCategory.Physical;
                _cachedWeaponBaseDamage = 1;
                _cachedWeaponCritChance = 0.05f;
                _cachedWeaponCritMultiplier = 1.5f;
            }
        }
        
        #endregion
        
        #region 综合伤害计算辅助
        
        /// <summary>
        /// 计算最终输出伤害倍率
        /// 分离：伤害类型倍率 × 元素倍率
        /// </summary>
        public float CalculateOutgoingDamageMultiplier(DamageCategory damageCategory, ElementType element)
        {
            float typeMultiplier = GetDamageTypeMultiplier(damageCategory);
            float elementBonus = 1f + GetElementalDamageBonus(element);
            
            return typeMultiplier * elementBonus;
        }
        
        /// <summary>
        /// 计算最终受到伤害倍率
        /// 分离：伤害类型减免 × 元素减免
        /// </summary>
        public float CalculateIncomingDamageMultiplier(DamageCategory damageCategory, ElementType element)
        {
            float typeReduction = 1f - GetDamageTypeResistance(damageCategory);
            float elementReduction = 1f - GetElementalResistance(element);
            
            // True伤害忽略伤害类型抗性
            if (damageCategory == DamageCategory.True)
                typeReduction = 1f;
            
            return Mathf.Max(0.1f, typeReduction * elementReduction); // 最少受到10%伤害
        }
        
        #endregion
        
        #region 调试
        
        public string GetDebugInfo()
        {
            EnsureCache();
            return $"[Combat Cache]\n" +
                   $"ATK: P{_cachedPhysicalAttack}/M{_cachedMagicAttack} | DEF: P{_cachedPhysicalDefense}/M{_cachedMagicDefense}\n" +
                   $"ACC:{_cachedAccuracy:P0} EVA:{_cachedEvasion:P0} CRIT:{_cachedCriticalRate:P0}x{_cachedCriticalDamage:F1}\n" +
                   $"Weapon: {_cachedWeaponCategory} {_cachedWeaponElement} DMG:{_cachedWeaponBaseDamage}";
        }
        
        #endregion
    }
}
