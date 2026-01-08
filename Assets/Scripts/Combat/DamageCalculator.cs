using System;
using UnityEngine;

namespace CombatSystem
{
    using ItemSystem.Core;
    using ItemSystem.Equipment;
    
    /// <summary>
    /// 伤害计算器 - 分离伤害类型与元素
    /// </summary>
    public static class DamageCalculator
    {
        #region 伤害计算流水线
        
        /// <summary>
        /// 计算最终伤害
        /// 流程：基础伤害 → 攻击力加成 → 伤害类型倍率 → 元素倍率 → 暴击 → 防御减免 → 元素抗性 → 最终伤害
        /// </summary>
        public static DamageResult CalculateDamage(DamageContext context)
        {
            var result = new DamageResult
            {
                Attacker = context.Attacker,
                Defender = context.Defender,
                DamageCategory = context.DamageCategory,
                Element = context.Element
            };
            
            // 1. 命中判定
            if (!RollHit(context, out float hitChance))
            {
                result.IsMiss = true;
                result.HitChance = hitChance;
                return result;
            }
            result.HitChance = hitChance;
            
            // 2. 格挡判定
            result.IsBlocked = RollBlock(context, out float blockChance);
            result.BlockChance = blockChance;
            
            // 3. 基础伤害计算
            float baseDamage = CalculateBaseDamage(context);
            result.BaseDamage = baseDamage;
            
            // 4. 攻击力加成
            float attackBonus = GetAttackBonus(context);
            float damageAfterAttack = baseDamage + attackBonus;
            
            // 5. 伤害类型倍率（Physical/Magic/True）
            float typeMultiplier = context.Attacker.CombatCache.GetDamageTypeMultiplier(context.DamageCategory);
            float damageAfterType = damageAfterAttack * typeMultiplier;
            result.DamageTypeMultiplier = typeMultiplier;
            
            // 6. 元素倍率
            float elementBonus = 1f + context.Attacker.CombatCache.GetElementalDamageBonus(context.Element);
            float damageAfterElement = damageAfterType * elementBonus;
            result.ElementalMultiplier = elementBonus;
            
            // 7. 暴击判定
            result.IsCritical = RollCritical(context, out float critChance);
            result.CriticalChance = critChance;
            
            float damageAfterCrit = damageAfterElement;
            if (result.IsCritical)
            {
                float critMultiplier = GetCriticalMultiplier(context);
                damageAfterCrit *= critMultiplier;
                result.CriticalMultiplier = critMultiplier;
            }
            
            // 8. 防御减免
            float defenseReduction = CalculateDefenseReduction(context, damageAfterCrit);
            float damageAfterDefense = damageAfterCrit - defenseReduction;
            result.DefenseReduction = defenseReduction;
            
            // 9. 元素抗性
            float elementResist = context.Defender.CombatCache.GetElementalResistance(context.Element);
            float damageAfterResist = damageAfterDefense * (1f - elementResist);
            result.ElementalResistance = elementResist;
            
            // 10. 格挡减伤
            if (result.IsBlocked)
            {
                damageAfterResist *= (1f - context.Defender.CombatCache.BlockDamageReduction);
            }
            
            // 11. 其他倍率（技能倍率等）
            float finalDamage = damageAfterResist * context.SkillMultiplier;
            
            // 12. 随机浮动 (±5%)
            if (context.ApplyRandomVariance)
            {
                float variance = UnityEngine.Random.Range(0.95f, 1.05f);
                finalDamage *= variance;
            }
            
            // 13. 最终伤害（最小为1）
            result.FinalDamage = Mathf.Max(1, Mathf.RoundToInt(finalDamage));
            
            return result;
        }
        
        #endregion
        
        #region 基础伤害计算
        
        /// <summary>
        /// 计算基础伤害
        /// </summary>
        private static float CalculateBaseDamage(DamageContext context)
        {
            // 如果有固定伤害值，直接使用
            if (context.FixedDamage > 0)
                return context.FixedDamage;
            
            // 否则从武器获取
            var weaponInstance = context.Attacker.EquippedWeapon;
            if (weaponInstance?.Template is Weapon weapon)
            {
                return weapon.GetFinalDamage(weaponInstance);
            }
            
            // 空手伤害
            return context.DamageCategory == DamageCategory.Magic 
                ? context.Attacker.Stats.MagicAttack * 0.5f 
                : context.Attacker.Stats.PhysicalAttack * 0.5f;
        }
        
        /// <summary>
        /// 获取攻击力加成
        /// </summary>
        private static float GetAttackBonus(DamageContext context)
        {
            var cache = context.Attacker.CombatCache;
            
            return context.DamageCategory switch
            {
                DamageCategory.Physical => cache.PhysicalAttack * 0.5f,
                DamageCategory.Magic => cache.MagicAttack * 0.5f,
                DamageCategory.True => 0f, // 真实伤害不受攻击力影响
                _ => 0f
            };
        }
        
        #endregion
        
        #region 命中判定
        
        /// <summary>
        /// 命中判定
        /// 命中率 = 基础命中 × (攻击方命中 / 防守方闪避)
        /// </summary>
        private static bool RollHit(DamageContext context, out float hitChance)
        {
            // True伤害必中
            if (context.DamageCategory == DamageCategory.True || context.GuaranteedHit)
            {
                hitChance = 1f;
                return true;
            }
            
            float attackerAcc = context.Attacker.CombatCache.Accuracy;
            float defenderEva = context.Defender.CombatCache.Evasion;
            
            // 防止除零
            defenderEva = Mathf.Max(0.01f, defenderEva);
            
            hitChance = Mathf.Clamp(attackerAcc - defenderEva + 0.9f, 0.05f, 0.99f);
            
            return UnityEngine.Random.value <= hitChance;
        }
        
        #endregion
        
        #region 格挡判定
        
        /// <summary>
        /// 格挡判定
        /// </summary>
        private static bool RollBlock(DamageContext context, out float blockChance)
        {
            // True伤害无法格挡
            if (context.DamageCategory == DamageCategory.True)
            {
                blockChance = 0f;
                return false;
            }
            
            var cache = context.Defender.CombatCache;
            
            blockChance = context.DamageCategory == DamageCategory.Physical 
                ? cache.PhysicalBlockRate 
                : cache.MagicBlockRate;
            
            blockChance = Mathf.Clamp(blockChance, 0f, 0.75f); // 上限75%
            
            return UnityEngine.Random.value <= blockChance;
        }
        
        #endregion
        
        #region 暴击判定
        
        /// <summary>
        /// 暴击判定
        /// </summary>
        private static bool RollCritical(DamageContext context, out float critChance)
        {
            var cache = context.Attacker.CombatCache;
            
            // 基础暴击率 + 武器暴击率
            critChance = cache.CriticalRate + cache.WeaponCritChance;
            critChance = Mathf.Clamp(critChance, 0f, 0.9f); // 上限90%
            
            return UnityEngine.Random.value <= critChance;
        }
        
        /// <summary>
        /// 获取暴击倍率
        /// </summary>
        private static float GetCriticalMultiplier(DamageContext context)
        {
            var cache = context.Attacker.CombatCache;
            return cache.CriticalDamage + cache.WeaponCritMultiplier - 1f; // 防止重复计算基础1.5倍
        }
        
        #endregion
        
        #region 防御减免
        
        /// <summary>
        /// 计算防御减免
        /// 公式：防御 / (防御 + 100 + 攻击者等级 × 5)
        /// </summary>
        private static float CalculateDefenseReduction(DamageContext context, float damage)
        {
            // True伤害忽略防御
            if (context.DamageCategory == DamageCategory.True)
                return 0f;
            
            var cache = context.Defender.CombatCache;
            
            float defense = context.DamageCategory == DamageCategory.Physical 
                ? cache.PhysicalDefense 
                : cache.MagicDefense;
            
            // 应用穿透
            defense *= (1f - context.ArmorPenetration);
            defense = Mathf.Max(0, defense);
            
            // 防御公式：减免比例 = 防御 / (防御 + K)
            float k = 100f + context.Attacker.Level * 5f;
            float reductionPercent = defense / (defense + k);
            reductionPercent = Mathf.Clamp(reductionPercent, 0f, 0.8f); // 上限80%减免
            
            return damage * reductionPercent;
        }
        
        #endregion
        
        #region 便捷方法
        
        /// <summary>
        /// 快速计算普通攻击伤害
        /// </summary>
        public static DamageResult CalculateNormalAttack(Character attacker, Character defender)
        {
            var context = new DamageContext
            {
                Attacker = attacker,
                Defender = defender,
                DamageCategory = attacker.CombatCache.WeaponDamageCategory,
                Element = attacker.CombatCache.WeaponElement,
                SkillMultiplier = 1f,
                ApplyRandomVariance = true
            };
            
            return CalculateDamage(context);
        }
        
        /// <summary>
        /// 计算技能伤害
        /// </summary>
        public static DamageResult CalculateSkillDamage(
            Character attacker, 
            Character defender,
            float skillMultiplier,
            DamageCategory damageCategory,
            ElementType element,
            float armorPenetration = 0f)
        {
            var context = new DamageContext
            {
                Attacker = attacker,
                Defender = defender,
                DamageCategory = damageCategory,
                Element = element,
                SkillMultiplier = skillMultiplier,
                ArmorPenetration = armorPenetration,
                ApplyRandomVariance = true
            };
            
            return CalculateDamage(context);
        }
        
        /// <summary>
        /// 计算固定伤害（如中毒、燃烧）
        /// </summary>
        public static DamageResult CalculateFixedDamage(
            Character attacker,
            Character defender,
            int fixedDamage,
            DamageCategory damageCategory,
            ElementType element,
            bool ignoreDef = false)
        {
            var context = new DamageContext
            {
                Attacker = attacker,
                Defender = defender,
                FixedDamage = fixedDamage,
                DamageCategory = ignoreDef ? DamageCategory.True : damageCategory,
                Element = element,
                SkillMultiplier = 1f,
                GuaranteedHit = true,
                ApplyRandomVariance = false
            };
            
            return CalculateDamage(context);
        }
        
        #endregion
    }
    
    #region 数据结构
    
    /// <summary>
    /// 伤害计算上下文
    /// </summary>
    public struct DamageContext
    {
        public Character Attacker;
        public Character Defender;
        
        // 伤害类型与元素（分离）
        public DamageCategory DamageCategory;
        public ElementType Element;
        
        // 技能相关
        public float SkillMultiplier;
        public float ArmorPenetration;  // 护甲穿透 (0-1)
        
        // 固定伤害（用于DOT等）
        public int FixedDamage;
        
        // 选项
        public bool GuaranteedHit;
        public bool ApplyRandomVariance;
    }
    
    /// <summary>
    /// 伤害计算结果
    /// </summary>
    public struct DamageResult
    {
        public Character Attacker;
        public Character Defender;
        
        // 伤害信息
        public DamageCategory DamageCategory;
        public ElementType Element;
        public int FinalDamage;
        
        // 判定结果
        public bool IsMiss;
        public bool IsCritical;
        public bool IsBlocked;
        
        // 概率信息（用于UI显示）
        public float HitChance;
        public float CriticalChance;
        public float BlockChance;
        
        // 倍率信息（用于伤害统计）
        public float BaseDamage;
        public float DamageTypeMultiplier;
        public float ElementalMultiplier;
        public float CriticalMultiplier;
        public float DefenseReduction;
        public float ElementalResistance;
        
        /// <summary>
        /// 转换为DamageInfo（用于事件）
        /// </summary>
        public DamageInfo ToDamageInfo()
        {
            return new DamageInfo
            {
                FinalDamage = FinalDamage,
                DamageCategory = DamageCategory,
                Element = Element,
                IsCritical = IsCritical,
                IsBlocked = IsBlocked,
                Attacker = Attacker
            };
        }
    }
    
    /// <summary>
    /// 伤害信息（简化版，用于事件传递）
    /// </summary>
    public struct DamageInfo
    {
        public int FinalDamage;
        public DamageCategory DamageCategory;
        public ElementType Element;
        public bool IsCritical;
        public bool IsBlocked;
        public Character Attacker;
    }
    
    #endregion
}
