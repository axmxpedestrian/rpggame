using System;
using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem.Equipment
{
    using Core;
    using Modifiers;
    
    /// <summary>
    /// 装备基类
    /// </summary>
    public abstract class EquipmentBase : Item, IEquippable
    {
        [Header("装备属性")]
        [SerializeField] protected EquipmentSubType equipSubType;
        [SerializeField] protected StatModifier[] baseStatModifiers;
        [SerializeField] protected int levelRequirement;
        
        public EquipmentSubType EquipSubType => equipSubType;
        public int LevelRequirement => levelRequirement;
        
        public virtual void OnEquip(ICharacter character)
        {
            foreach (var mod in GetStatModifiers())
            {
                character.Stats?.AddModifier(mod);
            }
        }
        
        public virtual void OnUnequip(ICharacter character)
        {
            foreach (var mod in GetStatModifiers())
            {
                character.Stats?.RemoveModifier(mod);
            }
        }
        
        public virtual StatModifier[] GetStatModifiers()
        {
            return baseStatModifiers ?? Array.Empty<StatModifier>();
        }
    }
    
    /// <summary>
    /// 武器类
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeapon", menuName = "ItemSystem/Equipment/Weapon")]
    public class Weapon : EquipmentBase, IReforgeable, ISocketable, IDurable
    {
        [Header("武器属性")]
        [SerializeField] private WeaponCategory weaponCategory;
        [SerializeField] private int baseDamage;
        [SerializeField] private float criticalChance;
        [SerializeField] private float criticalMultiplier = 1.5f;
        [SerializeField] private ElementType element = ElementType.None;
        [SerializeField] private DamageCategory damageCategory = DamageCategory.Physical;
        
        [Header("攻击范围")]
        [SerializeField] private AttackRangeConfig attackRange;
        
        [Header("耐久度")]
        [SerializeField] private int maxDurability = 100;
        
        [Header("技能")]
        [SerializeField] private SkillData[] weaponSkills;
        
        // 属性访问
        public WeaponCategory WeaponCategory => weaponCategory;
        public int BaseDamage => baseDamage;
        public float CriticalChance => criticalChance;
        public float CriticalMultiplier => criticalMultiplier;
        public ElementType Element => element;
        public DamageCategory DamageCategory => damageCategory;
        public AttackRangeConfig AttackRange => attackRange;
        public SkillData[] WeaponSkills => weaponSkills;
        
        // IDurable 实现
        public int MaxDurability => maxDurability;
        public int CurrentDurability { get; set; }
        public bool IsBroken => CurrentDurability <= 0;
        
        public void ReduceDurability(int amount)
        {
            CurrentDurability = Mathf.Max(0, CurrentDurability - amount);
        }
        
        public void Repair(int amount)
        {
            CurrentDurability = Mathf.Min(maxDurability, CurrentDurability + amount);
        }
        
        // IReforgeable 实现
        public int CurrentPrefixId { get; set; }
        
        public PrefixCategory PrefixCategory
        {
            get
            {
                return weaponCategory switch
                {
                    WeaponCategory.Blunt or WeaponCategory.Sharp => PrefixCategory.Melee,
                    WeaponCategory.Bow or WeaponCategory.Gun or WeaponCategory.Explosive => PrefixCategory.Ranged,
                    WeaponCategory.Magic => PrefixCategory.Magic,
                    _ => PrefixCategory.Universal
                };
            }
        }
        
        public void ApplyPrefix(int prefixId)
        {
            CurrentPrefixId = prefixId;
        }
        
        public int[] GetAllowedPrefixes()
        {
            return PrefixDatabase.Instance.GetPrefixesByCategory(PrefixCategory);
        }
        
        // ISocketable 实现
        public int SocketCount => GetSocketCount();
        public SocketGem[] InstalledGems { get; private set; }
        
        public bool CanInsertGem(SocketGem gem, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SocketCount) return false;
            if (InstalledGems == null) InstalledGems = new SocketGem[SocketCount];
            return InstalledGems[slotIndex] == null && gem.CanInsertInto(this);
        }
        
        public void InsertGem(SocketGem gem, int slotIndex)
        {
            if (CanInsertGem(gem, slotIndex))
            {
                InstalledGems[slotIndex] = gem;
            }
        }
        
        public SocketGem RemoveGem(int slotIndex)
        {
            if (InstalledGems == null || slotIndex < 0 || slotIndex >= SocketCount)
                return null;
            
            var gem = InstalledGems[slotIndex];
            InstalledGems[slotIndex] = null;
            return gem;
        }
        
        /// <summary>
        /// 计算带前缀的最终伤害
        /// </summary>
        public int GetFinalDamage(ItemInstance instance)
        {
            int damage = baseDamage;
            
            // 应用前缀加成
            if (instance != null && instance.PrefixId > 0)
            {
                var prefix = PrefixDatabase.Instance?.GetPrefix(instance.PrefixId);
                if (prefix != null)
                {
                    damage = Mathf.RoundToInt(damage * (1f + prefix.DamageModifier));
                    damage += prefix.FlatDamageBonus;
                }
            }
            
            return damage;
        }
        
        /// <summary>
        /// 获取带前缀的暴击率
        /// </summary>
        public float GetFinalCriticalChance(ItemInstance instance)
        {
            float crit = criticalChance;
            
            if (instance != null && instance.PrefixId > 0)
            {
                var prefix = PrefixDatabase.Instance?.GetPrefix(instance.PrefixId);
                if (prefix != null)
                {
                    crit += prefix.CriticalModifier;
                }
            }
            
            return crit;
        }
    }
    
    /// <summary>
    /// 攻击范围配置
    /// </summary>
    [Serializable]
    public class AttackRangeConfig
    {
        public AttackRangeType rangeType;
        public int[] targetablePositions;  // 可攻击的位置索引（0-4）
        public bool isPositionRelative;    // 是否基于攻击者位置
        public int relativeRange;          // 相对范围（如"相邻1格"）
        
        /// <summary>
        /// 检查能否攻击目标位置
        /// </summary>
        public bool CanTarget(int attackerPosition, int targetPosition)
        {
            if (isPositionRelative)
            {
                return Mathf.Abs(attackerPosition - targetPosition) <= relativeRange;
            }
            
            return Array.Exists(targetablePositions, p => p == targetPosition);
        }
    }
    
    public enum AttackRangeType
    {
        Melee,          // 近战 - 只能打前排
        Ranged,         // 远程 - 只能打后排
        All,            // 全范围
        Adjacent,       // 相邻
        Self            // 自身
    }
    
    /// <summary>
    /// 护甲类
    /// </summary>
    [CreateAssetMenu(fileName = "NewArmor", menuName = "ItemSystem/Equipment/Armor")]
    public class Armor : EquipmentBase, ISocketable, IDurable
    {
        [Header("护甲属性")]
        [SerializeField] private ArmorSlot armorSlot;
        [SerializeField] private int physicalDefense;
        [SerializeField] private int magicDefense;
        [SerializeField] private float[] resistances; // 各元素抗性
        
        [Header("耐久度")]
        [SerializeField] private int maxDurability = 150;
        
        public ArmorSlot ArmorSlot => armorSlot;
        public int PhysicalDefense => physicalDefense;
        public int MagicDefense => magicDefense;
        
        // IDurable 实现
        public int MaxDurability => maxDurability;
        public int CurrentDurability { get; set; }
        public bool IsBroken => CurrentDurability <= 0;
        
        public void ReduceDurability(int amount) => CurrentDurability = Mathf.Max(0, CurrentDurability - amount);
        public void Repair(int amount) => CurrentDurability = Mathf.Min(maxDurability, CurrentDurability + amount);
        
        // ISocketable 实现
        public int SocketCount => GetSocketCount();
        public SocketGem[] InstalledGems { get; private set; }
        
        public bool CanInsertGem(SocketGem gem, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= SocketCount) return false;
            if (InstalledGems == null) InstalledGems = new SocketGem[SocketCount];
            return InstalledGems[slotIndex] == null;
        }
        
        public void InsertGem(SocketGem gem, int slotIndex)
        {
            if (CanInsertGem(gem, slotIndex))
                InstalledGems[slotIndex] = gem;
        }
        
        public SocketGem RemoveGem(int slotIndex)
        {
            if (InstalledGems == null || slotIndex < 0 || slotIndex >= SocketCount)
                return null;
            var gem = InstalledGems[slotIndex];
            InstalledGems[slotIndex] = null;
            return gem;
        }
        
        public float GetResistance(ElementType element)
        {
            int index = (int)element;
            if (resistances == null || index >= resistances.Length)
                return 0f;
            return resistances[index];
        }
    }
    
    /// <summary>
    /// 饰品类
    /// </summary>
    [CreateAssetMenu(fileName = "NewAccessory", menuName = "ItemSystem/Equipment/Accessory")]
    public class Accessory : EquipmentBase
    {
        [Header("饰品属性")]
        [SerializeField] private AccessoryEffect[] specialEffects;
        [SerializeField] private PassiveSkillData passiveSkill;
        
        public AccessoryEffect[] SpecialEffects => specialEffects;
        public PassiveSkillData PassiveSkill => passiveSkill;
        
        public override void OnEquip(ICharacter character)
        {
            base.OnEquip(character);
            
            // 应用特殊效果
            if (specialEffects != null)
            {
                foreach (var effect in specialEffects)
                {
                    effect.Apply(character);
                }
            }
            
            // 注册被动技能
            if (passiveSkill != null)
            {
                character.PassiveManager?.Register(passiveSkill);
            }
        }
        
        public override void OnUnequip(ICharacter character)
        {
            base.OnUnequip(character);
            
            if (specialEffects != null)
            {
                foreach (var effect in specialEffects)
                {
                    effect.Remove(character);
                }
            }
            
            if (passiveSkill != null)
            {
                character.PassiveManager?.Unregister(passiveSkill);
            }
        }
    }
    
    /// <summary>
    /// 饰品特殊效果
    /// </summary>
    [Serializable]
    public class AccessoryEffect
    {
        public AccessoryEffectType effectType;
        public float value;
        public ElementType element;
        
        public void Apply(ICharacter character)
        {
            // 根据效果类型应用
            switch (effectType)
            {
                case AccessoryEffectType.DamageBonus:
                    character.CombatStats?.AddDamageMultiplier(value);
                    break;
                case AccessoryEffectType.ElementalDamageBonus:
                    character.CombatStats?.AddElementalDamageBonus(element, value);
                    break;
                case AccessoryEffectType.CriticalChanceBonus:
                    character.CombatStats?.AddCriticalChance(value);
                    break;
                // ... 其他效果
            }
        }
        
        public void Remove(ICharacter character)
        {
            // 移除效果（逆向Apply）
        }
    }
    
    public enum AccessoryEffectType
    {
        DamageBonus,
        ElementalDamageBonus,
        CriticalChanceBonus,
        CriticalDamageBonus,
        ResistanceBonus,
        SkillPointRecovery,
        StressReduction,
        FatigueReduction
    }
}
