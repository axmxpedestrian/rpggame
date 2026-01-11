using System;

namespace ItemSystem.Core
{
    /// <summary>
    /// 角色接口 - 物品系统与战斗系统共享
    /// 物品系统中的所有方法使用此接口，战斗系统的 Character 类实现此接口
    /// </summary>
    public interface ICharacter
    {
        // 基础信息
        string CharacterName { get; }
        int Level { get; }
        
        // 生命值
        int CurrentHealth { get; set; }
        int MaxHealth { get; }
        bool IsDowned { get; }
        
        // 技能点
        int CurrentSkillPoints { get; set; }
        int MaxSkillPoints { get; }
        
        // 压力与疲劳
        int CurrentStress { get; set; }
        int CurrentFatigue { get; set; }
        
        // ATB
        float CurrentATB { get; set; }
        
        // 基础操作
        void Heal(int amount);
        void RestoreSkillPoints(int amount);
        void ReduceStress(int amount);
        void ReduceFatigue(int amount);
        void Revive(int healthAmount);
        
        // 属性系统
        ICharacterStats Stats { get; }
        ICombatStats CombatStats { get; }
        
        // 状态效果管理器
        IStatusEffectManager StatusEffectManager { get; }
        
        // 便捷方法（兼容旧代码）
        void AddStatusEffect(IStatusEffect effect);
        void RemoveStatusEffect(StatusEffectType effectType);
        bool HasStatusEffect(StatusEffectType effectType);
        
        // 获取基础属性值（用于百分比计算）
        float GetBaseStat(BuffType buffType);
        
        // 被动技能
        IPassiveManager PassiveManager { get; }
        
        // 外观
        IVisualManager VisualManager { get; }
    }
    
    /// <summary>
    /// 角色属性接口
    /// </summary>
    public interface ICharacterStats
    {
        void AddModifier(StatModifier modifier);
        void RemoveModifier(StatModifier modifier);
        void RemoveModifiersFromSource(object source);
    }
    
    /// <summary>
    /// 战斗属性接口
    /// </summary>
    public interface ICombatStats
    {
        void AddDamageMultiplier(float value);
        void AddElementalDamageBonus(ElementType element, float value);
        void AddCriticalChance(float value);
        void AddTemporaryBonus(BuffType type, float value);
        void RemoveTemporaryBonus(BuffType type, float value);
    }
    
    /// <summary>
    /// 状态效果管理器接口
    /// </summary>
    public interface IStatusEffectManager
    {
        void AddEffect(IStatusEffect effect);
        void RemoveEffect(StatusEffectType effectType);
        bool HasEffect(StatusEffectType effectType);
        void ClearAllEffects();
        void Tick(float deltaTime);
    }
    
    /// <summary>
    /// 状态效果接口
    /// </summary>
    public interface IStatusEffect
    {
        StatusEffectType EffectType { get; }
        void OnApply(ICharacter target);
        void OnRemove(ICharacter target);
        void OnTick(ICharacter target, float deltaTime);
        bool IsExpired { get; }
    }
    
    /// <summary>
    /// 被动技能管理器接口
    /// </summary>
    public interface IPassiveManager
    {
        void Register(object skill);
        void Unregister(object skill);
    }
    
    /// <summary>
    /// 视觉管理器接口
    /// </summary>
    public interface IVisualManager
    {
        void SetCosmetic(Cosmetics.CosmeticSlot slot, Cosmetics.Cosmetic cosmetic);
        void RemoveCosmetic(Cosmetics.CosmeticSlot slot);
    }
    
    // 注意: BuffType 已移至 ItemSystem.Core.ItemEnums.cs
}
