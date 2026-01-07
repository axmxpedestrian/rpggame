using System;

namespace ItemSystem.Core
{
    /// <summary>
    /// 物品大类 - 决定基础行为
    /// </summary>
    public enum ItemType
    {
        Equipment,      // 装备类
        Consumable,     // 消耗品类
        Tool,           // 工具类
        Material,       // 材料类
        Cosmetic,       // 服装/时装类
        QuestItem       // 剧情道具
    }

    /// <summary>
    /// 装备子类型
    /// </summary>
    public enum EquipmentSubType
    {
        Weapon,
        Armor,
        Accessory
    }

    /// <summary>
    /// 消耗品子类型
    /// </summary>
    public enum ConsumableSubType
    {
        Healing,        // 回复类
        Buff,           // 属性提升类
        Utility         // 功能类（如传送卷轴）
    }

    /// <summary>
    /// 武器类型 - 影响攻击范围和熟练度
    /// </summary>
    public enum WeaponCategory
    {
        Blunt,          // 钝器
        Sharp,          // 锐器
        Bow,            // 弓
        Explosive,      // 炸药
        Gun,            // 枪
        Magic           // 法术
    }

    /// <summary>
    /// 护甲部位
    /// </summary>
    public enum ArmorSlot
    {
        Head,
        Body,
        Legs
    }

    /// <summary>
    /// 物品品质 - 影响基础数值和配件槽数量
    /// </summary>
    public enum ItemRarity
    {
        Common,         // 普通 - 0个配件槽
        Uncommon,       // 稀有 - 1个配件槽
        Rare,           // 精良 - 2个配件槽
        Epic,           // 史诗 - 3个配件槽
        Legendary       // 传说 - 4个配件槽
    }

    /// <summary>
    /// 元素类型
    /// </summary>
    public enum ElementType
    {
        None,
        Fire,
        Ice,
        Lightning,
        Poison,
        Holy,
        Dark
    }

    /// <summary>
    /// 伤害类型
    /// </summary>
    public enum DamageCategory
    {
        Physical,
        Magic,
        True            // 真实伤害
    }

    /// <summary>
    /// 功能标记 - 决定高级行为（可组合）
    /// </summary>
    [Flags]
    public enum ItemFlags
    {
        None = 0,
        Stackable = 1 << 0,         // 可堆叠
        Craftable = 1 << 1,         // 可合成
        Reforgeable = 1 << 2,       // 可重铸
        HasDurability = 1 << 3,     // 有耐久度
        Sellable = 1 << 4,          // 可出售
        Droppable = 1 << 5,         // 可丢弃
        UsableInCombat = 1 << 6,    // 战斗中可使用
        CanCarryToBattle = 1 << 7,  // 可携带进入战斗
        Socketable = 1 << 8,        // 可镶嵌配件
        Tradeable = 1 << 9          // 可交易
    }
}