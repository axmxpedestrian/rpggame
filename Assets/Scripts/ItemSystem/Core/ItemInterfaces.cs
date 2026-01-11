using System.Collections.Generic;
using UnityEngine;

namespace ItemSystem.Core
{
    /// <summary>
    /// 可使用接口 - 消耗品、工具等
    /// </summary>
    public interface IUsable
    {
        bool CanUse(ICharacter user, ICharacter target = null);
        void Use(ICharacter user, ICharacter target = null);
        float Cooldown { get; }
    }

    /// <summary>
    /// 可装备接口
    /// </summary>
    public interface IEquippable
    {
        EquipmentSubType EquipSubType { get; }
        void OnEquip(ICharacter character);
        void OnUnequip(ICharacter character);
        StatModifier[] GetStatModifiers();
    }

    /// <summary>
    /// 可堆叠接口
    /// </summary>
    public interface IStackable
    {
        int StackCount { get; set; }
        int MaxStackSize { get; }
        bool CanStackWith(Item other);
    }

    /// <summary>
    /// 可合成接口
    /// 注意：Recipe 类定义在 ItemSystem.Crafting 命名空间
    /// 使用 RecipeId 避免跨命名空间引用问题
    /// </summary>
    public interface ICraftable
    {
        /// <summary>
        /// 获取合成此物品所需的配方ID
        /// 配方详情通过 RecipeDatabase.Instance.GetRecipe(id) 获取
        /// </summary>
        int GetRecipeId();
        
        /// <summary>
        /// 获取所有可合成此物品的配方ID列表（可选实现）
        /// </summary>
        int[] GetAllRecipeIds() => new[] { GetRecipeId() };
    }

    /// <summary>
    /// 可重铸接口 - 武器前缀系统
    /// </summary>
    public interface IReforgeable
    {
        int CurrentPrefixId { get; set; }
        PrefixCategory PrefixCategory { get; }
        void ApplyPrefix(int prefixId);
        int[] GetAllowedPrefixes();
    }

    /// <summary>
    /// 可镶嵌接口 - 配件系统
    /// </summary>
    public interface ISocketable
    {
        int SocketCount { get; }
        SocketGem[] InstalledGems { get; }
        bool CanInsertGem(SocketGem gem, int slotIndex);
        void InsertGem(SocketGem gem, int slotIndex);
        SocketGem RemoveGem(int slotIndex);
    }

    /// <summary>
    /// 战斗中可使用接口
    /// </summary>
    public interface ICombatUsable : IUsable
    {
        int ATBCost { get; }  // 使用消耗的ATB值
        TargetType TargetType { get; }  // 目标类型
        bool CanUseInCombat(CombatContext context);
    }

    /// <summary>
    /// 目标类型
    /// </summary>
    public enum TargetType
    {
        Self,
        SingleAlly,
        AllAllies,
        SingleEnemy,
        AllEnemies,
        Position,       // 基于位置（相邻等）
        All
    }
    
    // 注意：PrefixCategory 已移至 ItemEnums.cs
    // 注意：IDurable 已移至 Database.cs
    // 注意：ICharacter 定义在 ICharacter.cs
}
