using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

namespace ItemSystem.Core
{
    /// <summary>
    /// 可使用接口 - 消耗品、工具等
    /// </summary>
    public interface IUsable
    {
        bool CanUse(Character user, Character target = null);
        void Use(Character user, Character target = null);
        float Cooldown { get; }
    }

    /// <summary>
    /// 可装备接口
    /// </summary>
    public interface IEquippable
    {
        EquipmentSubType EquipSubType { get; }
        void OnEquip(Character character);
        void OnUnequip(Character character);
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
    /// </summary>
    public interface ICraftable
    {
        Recipe GetRecipe();
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
    /// 有耐久度接口
    /// </summary>
    public interface IDurable
    {
        int CurrentDurability { get; set; }
        int MaxDurability { get; }
        void ReduceDurability(int amount);
        void Repair(int amount);
        bool IsBroken { get; }
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

    /// <summary>
    /// 前缀类别 - 用于限制武器可用的修饰语
    /// </summary>
    public enum PrefixCategory
    {
        Melee,          // 近战
        Ranged,         // 远程
        Magic,          // 魔法
        Universal       // 通用
    }
}