// ============================================
// GameEnums.cs - 游戏核心枚举定义
// ============================================
using System;

#region 物品相关枚举

/// <summary>
/// 物品稀有度
/// </summary>
public enum ItemRarity
{
    Common,     // 普通 - 白色
    Uncommon,   // 优秀 - 绿色
    Rare,       // 稀有 - 蓝色
    Epic,       // 史诗 - 紫色
    Legendary,  // 传说 - 橙色
    Mythic      // 神话 - 红色
}

/// <summary>
/// 物品类型
/// </summary>
public enum ItemType
{
    Weapon,         // 武器
    Armor,          // 护甲
    Accessory,      // 饰品
    Attachment,     // 配件（镶嵌物）
    Consumable,     // 消耗品
    Tool,           // 工具
    Material,       // 材料
    Cosmetic,       // 服装/时装
    QuestItem       // 剧情道具
}

/// <summary>
/// 武器类型
/// </summary>
public enum WeaponType
{
    Blunt,      // 钝器（锤、棍）
    Sharp,      // 锐器（剑、斧、匕首）
    Bow,        // 弓
    Crossbow,   // 弩
    Gun,        // 枪
    Explosive,  // 炸药
    Staff,      // 法杖
    Polearm,    // 长柄武器
    Throwing    // 投掷武器
}

/// <summary>
/// 护甲部位
/// </summary>
public enum ArmorSlot
{
    Head,       // 头部
    Body,       // 身体
    Legs        // 腿部
}

/// <summary>
/// 饰品类型
/// </summary>
public enum AccessoryType
{
    Ring,       // 戒指
    Necklace,   // 项链
    Earring,    // 耳环
    Bracelet    // 手镯
}

/// <summary>
/// 消耗品类型
/// </summary>
public enum ConsumableType
{
    Healing,        // 回复类
    Buff,           // 属性提升类
    Antidote,       // 解毒/解除异常
    Revival,        // 复活类
    SkillPoint      // 技能点恢复
}

/// <summary>
/// 工具类型
/// </summary>
public enum ToolType
{
    Key,            // 钥匙
    Bomb,           // 炸弹
    Rope,           // 绳索
    Torch,          // 火把
    Map,            // 地图
    Compass,        // 指南针
    Pickaxe,        // 镐
    Axe,            // 斧头
    FishingRod      // 钓竿
}

/// <summary>
/// 材料类型
/// </summary>
public enum MaterialType
{
    Ore,            // 矿石
    Wood,           // 木材
    Herb,           // 草药
    Cloth,          // 布料
    Leather,        // 皮革
    Gem,            // 宝石原石
    MonsterDrop,    // 怪物掉落
    Essence,        // 精华
    Ingredient      // 烹饪原料
}

/// <summary>
/// 配件类型
/// </summary>
public enum AttachmentType
{
    Gem,        // 宝石 - 法杖、魔法武器
    Spike,      // 钉子 - 钝器
    Rune,       // 符文 - 通用
    Crystal,    // 水晶 - 法杖
    Blade,      // 刀刃 - 利器
    Chain,      // 锁链 - 锤、连枷
    Poison,     // 毒素 - 匕首、箭矢
    Holy        // 圣物 - 法杖
}

/// <summary>
/// 配件适用的武器类型（标志位）
/// </summary>
[Flags]
public enum WeaponTypeFlag
{
    None = 0,
    Sharp = 1 << 0,
    Blunt = 1 << 1,
    Staff = 1 << 2,
    Bow = 1 << 3,
    Crossbow = 1 << 4,
    Throwing = 1 << 5,
    Polearm = 1 << 6,
    Gun = 1 << 7,
    Explosive = 1 << 8,

    Melee = Sharp | Blunt | Polearm,
    Ranged = Bow | Crossbow | Throwing | Gun,
    Magic = Staff,
    All = ~0
}

#endregion

#region 角色相关枚举

/// <summary>
/// 基础属性类型
/// </summary>
public enum BaseStatType
{
    Constitution,   // 体质
    Strength,       // 力量
    Perception,     // 感知
    Reaction,       // 反应
    Wisdom,         // 智慧
    Luck            // 幸运
}

/// <summary>
/// 战斗属性类型
/// </summary>
public enum CombatStatType
{
    MaxHealth,          // 生命值上限
    PhysicalAttack,     // 物理攻击力
    MagicAttack,        // 魔法攻击力
    PhysicalDefense,    // 物理防御力
    MagicDefense,       // 魔法防御力
    Resistance,         // 抗性
    CritRate,           // 暴击率
    CritDamage,         // 暴击伤害
    Speed,              // 速度
    HitRate,            // 命中率
    DodgeRate,          // 闪避率
    PhysicalBlock,      // 物理格挡率
    MagicBlock,         // 魔法格挡率
    PhysicalSkillCap,   // 物理技能点上限
    MagicSkillCap       // 魔法技能点上限
}

/// <summary>
/// 专业技能类型
/// </summary>
public enum ProfessionType
{
    Trading,        // 交易
    Logging,        // 伐木
    Planting,       // 种植
    Mining,         // 采矿
    Building,       // 建造
    Cooking,        // 烹饪
    Management,     // 经营
    Archaeology,    // 考古
    Astronomy       // 天文学
}

#endregion

#region 战斗相关枚举

/// <summary>
/// 伤害类别
/// </summary>
public enum DamageCategory
{
    Physical,   // 物理伤害
    Magic,      // 魔法伤害
    True        // 真实伤害（无视防御）
}

/// <summary>
/// 元素类型
/// </summary>
public enum ElementType
{
    None,       // 无元素
    Fire,       // 火
    Ice,        // 冰
    Lightning,  // 雷电
    Poison,     // 毒
    Holy,       // 神圣
    Dark        // 黑暗
}

/// <summary>
/// 负面效果类型
/// </summary>
public enum DebuffType
{
    // 持续伤害类
    Bleeding,       // 流血
    Burning,        // 燃烧
    Poisoned,       // 中毒
    Frostbite,      // 冻伤

    // 控制类
    Stunned,        // 眩晕
    Frozen,         // 冰冻
    Paralyzed,      // 麻痹
    Silenced,       // 沉默
    Blinded,        // 致盲

    // 削弱类
    Weakened,       // 虚弱（降低攻击）
    Vulnerable,     // 脆弱（增加受到伤害）
    Cursed,         // 诅咒（降低防御）
    Slowed,         // 减速
    Marked          // 标记（受到额外伤害）
}

/// <summary>
/// 正面效果类型
/// </summary>
public enum BuffType
{
    // 属性提升类
    AttackUp,       // 攻击提升
    DefenseUp,      // 防御提升
    SpeedUp,        // 速度提升
    CritUp,         // 暴击提升

    // 防护类
    Shield,         // 护盾
    Immunity,       // 免疫
    DamageReduction,// 减伤
    Reflect,        // 反伤

    // 恢复类
    Regeneration,   // 生命回复
    ManaRegen,      // 技能点回复

    // 特殊类
    Invisible,      // 隐身
    Invincible,     // 无敌
    Taunt           // 嘲讽
}

/// <summary>
/// 攻击范围类型
/// </summary>
public enum TargetRangeType
{
    Front,          // 前排（位置1-2）
    Back,           // 后排（位置3-5）
    All,            // 全体
    Single,         // 单体
    Adjacent,       // 相邻
    Self,           // 自身
    Row,            // 整排
    Random          // 随机
}

/// <summary>
/// 敌人种族
/// </summary>
public enum EnemyRace
{
    Human,      // 人类
    Undead,     // 亡灵
    Beast,      // 野兽
    Demon,      // 恶魔
    Elemental,  // 元素生物
    Construct,  // 构造体
    Dragon,     // 龙
    Insect,     // 虫类
    Plant       // 植物
}

/// <summary>
/// 敌人变体
/// </summary>
public enum EnemyVariant
{
    Normal,     // 普通
    Elite,      // 精英
    Boss,       // Boss
    Miniboss    // 小Boss
}

#endregion

#region 配件效果类型

/// <summary>
/// 配件效果类型
/// </summary>
public enum AttachmentEffectType
{
    // 属性加成类
    PhysicalDamageBonus,
    MagicDamageBonus,
    CritRateBonus,
    CritDamageBonus,
    AttackSpeedBonus,
    LifeSteal,
    ManaSteal,
    ArmorPenetration,
    MagicPenetration,

    // 负面效果类
    Bleeding,
    Burning,
    Poisoned,
    Frozen,
    Stunned,
    Weakened,
    Cursed,
    Blinded,
    Slowed,
    Marked,

    // 特殊效果类
    BonusDamageToUndead,
    BonusDamageToBeast,
    BonusDamageToDemon,
    BonusDamageToHuman,
    ChanceToInstantKill
}

/// <summary>
/// 配件稀有度
/// </summary>
public enum AttachmentRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Mythic
}

#endregion