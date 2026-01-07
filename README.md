# Unity2D 物品系统架构文档

## 概述

本系统为横版回合制RPG设计，采用"类型(Type) + 功能标记(Flags/Interfaces)"的混合模型，支持装备驱动、武器合成/重铸（类似泰拉瑞亚）以及双库存系统（主库存 + 战斗背包）。

## 文件结构

```
ItemSystem/
├── Core/
│   ├── ItemEnums.cs          # 枚举定义
│   ├── ItemInterfaces.cs     # 接口定义
│   ├── Item.cs               # 物品基类和ItemInstance
│   └── Database.cs           # 数据库和支持类
├── Equipment/
│   └── Equipment.cs          # 装备类（武器、护甲、饰品）
├── Items/
│   └── ItemTypes.cs          # 消耗品、工具、材料、服装、剧情道具
├── Modifiers/
│   └── PrefixSystem.cs       # 前缀/修饰语系统（重铸）
├── Crafting/
│   └── CraftingSystem.cs     # 合成系统
└── Inventory/
    └── InventorySystem.cs    # 双库存系统
```

## 核心设计原则

### 1. 静态模板 + 动态实例

```
Item (ScriptableObject)     →  静态数据模板，在Inspector中编辑
        ↓
ItemInstance (Serializable) →  运行时动态数据（堆叠数、前缀、耐久等）
```

这个设计类似泰拉瑞亚：`Item = { type, prefix, stack }`

### 2. 类型 + 功能标记混合模型

- **大类（ItemType）**：决定基础行为
  - 能否装备？→ Equipment
  - 能否使用？→ Consumable
  - 能否堆叠？→ Material

- **功能标记（ItemFlags）**：决定高级行为（可组合）
  - `Stackable` - 可堆叠
  - `Craftable` - 可合成
  - `Reforgeable` - 可重铸
  - `UsableInCombat` - 战斗中可使用
  - `CanCarryToBattle` - 可携带进入战斗

### 3. 接口组合

```csharp
// 武器同时实现多个接口
public class Weapon : EquipmentBase, IReforgeable, ISocketable, IDurable
{
    // IReforgeable - 支持前缀重铸
    // ISocketable - 支持配件镶嵌
    // IDurable - 有耐久度
}
```

---

## 物品类型层次

```
Item (基类)
├── Equipment (装备类)
│   ├── Weapon          // 武器 - 支持重铸、镶嵌、耐久
│   ├── Armor           // 护甲 - 支持镶嵌、耐久
│   └── Accessory       // 饰品 - 特殊效果、被动技能
├── Consumable (消耗品类)
│   ├── HealingItem     // 回复类 - HP/SP/压力/疲劳
│   └── BuffItem        // 属性提升类 - 临时Buff
├── Tool                // 工具类 - 钥匙、炸弹等
├── Material            // 材料类 - 用于合成
├── Cosmetic            // 服装类 - 纯外观或带属性
└── QuestItem           // 剧情道具 - 不可丢弃
```

---

## 关键系统详解

### 1. 前缀/重铸系统

类似泰拉瑞亚，每件武器可以有一个前缀（修饰语）：

```csharp
// 前缀影响武器属性
public class PrefixData : ScriptableObject
{
    public float damageModifier;      // +15% = 0.15
    public float criticalModifier;    // 暴击加成
    public float speedModifier;       // 攻击速度
    public PrefixTier tier;           // 负面/中性/良好/优秀/最佳
}

// 重铸流程
var reforgeSystem = new ReforgeSystem();
int cost = reforgeSystem.CalculateReforgeCost(weaponInstance);
ReforgeResult result = reforgeSystem.Reforge(weaponInstance);

if (result.Success)
{
    Debug.Log($"重铸成功: {result.OldPrefix?.DisplayName} → {result.NewPrefix.DisplayName}");
    if (result.IsUpgrade)
        Debug.Log("品质提升！");
}
```

**武器类型限制前缀**：
- 近战武器 → 只能获得近战前缀
- 远程武器 → 只能获得远程前缀
- 魔法武器 → 只能获得魔法前缀
- 通用前缀 → 所有武器都可以

### 2. 合成系统

配方只关心输入和输出，不关心物品类别：

```csharp
[CreateAssetMenu(fileName = "NewRecipe", menuName = "ItemSystem/Recipe")]
public class Recipe : ScriptableObject
{
    public RecipeIngredient[] ingredients;  // 输入：物品ID + 数量
    public int outputItemId;                // 输出：物品ID
    public int outputCount;                 // 输出数量
    public CraftingStation requiredStation; // 所需工作台
}

// 使用示例
var craftingManager = new CraftingManager(inventory);
craftingManager.SetStation(CraftingStation.Anvil);

var result = craftingManager.TryCraft(recipe);
if (result.Success)
{
    Debug.Log($"成功合成: {result.CraftedItem.GetDisplayName()} x{result.CraftedCount}");
}
```

### 3. 双库存系统

```
主库存 (MainInventory)
    ├── 存储所有物品
    ├── 容量大（100+）
    └── 在主世界访问
    
战斗背包 (CombatInventory)
    ├── 临时携带物品
    ├── 容量小（10左右）
    ├── 只能在战斗中使用
    └── 战斗结束后清空
```

**流程**：
```csharp
var inventoryManager = new InventoryManager();

// 1. 设置携带规则
inventoryManager.SetCarryRule(normalBattleRule);

// 2. 准备战斗 - 选择携带物品
var prepResult = inventoryManager.PrepareBattle(selectedItemIds);

// 3. 战斗中使用物品
inventoryManager.UseItemInCombat(itemId, currentCharacter, target);

// 4. 战斗结束 - 合并战利品
var endResult = inventoryManager.EndBattle(victory: true);
foreach (var loot in endResult.AcquiredLoot)
{
    Debug.Log($"获得: {loot.GetDisplayName()}");
}
```

**携带规则配置**：
```csharp
[CreateAssetMenu(fileName = "NormalBattleRule", menuName = "ItemSystem/CarryRule")]
public class CarryRuleConfig : ScriptableObject
{
    public int maxTotalItems = 10;
    public int maxConsumables = 5;
    public int maxHealingItems = 3;
    public ItemType[] allowedTypes = { ItemType.Consumable };
    public ItemType[] forbiddenTypes = { ItemType.Material, ItemType.QuestItem };
}
```

---

## 与现有系统集成

### 与战斗系统集成

```csharp
// ATBCombatManager.cs 中
public void OnCharacterAction(Character character, int itemId)
{
    if (itemId > 0)
    {
        // 使用物品
        var item = combatInventory.GetItem(itemId);
        if (item?.Template is ICombatUsable usable)
        {
            int atbCost = usable.ATBCost;
            if (character.CurrentATB >= atbCost)
            {
                usable.Use(character, selectedTarget);
                character.CurrentATB -= atbCost;
                inventoryManager.UseItemInCombat(itemId, character, selectedTarget);
            }
        }
    }
}
```

### 与角色属性系统集成

```csharp
// Character.cs 中
public void EquipItem(ItemInstance item)
{
    if (item.Template is IEquippable equippable)
    {
        equippable.OnEquip(this);
        
        // 应用武器前缀加成
        if (item.Template is Weapon weapon && item.Prefix != null)
        {
            Stats.AddModifier(new StatModifier(
                StatType.PhysicalAttack, 
                ModifierType.PercentMult, 
                item.Prefix.DamageModifier,
                item
            ));
        }
    }
}
```

### 与伤害计算系统集成

```csharp
// DamageCalculator.cs 中
public int CalculateWeaponDamage(Character attacker, ItemInstance weaponInstance)
{
    var weapon = weaponInstance.Template as Weapon;
    
    // 基础伤害
    int baseDamage = weapon.GetFinalDamage(weaponInstance);
    
    // 应用镶嵌宝石加成
    if (weapon.InstalledGems != null)
    {
        foreach (var gem in weapon.InstalledGems)
        {
            if (gem != null)
            {
                foreach (var bonus in gem.Bonuses)
                {
                    // 应用宝石属性
                }
            }
        }
    }
    
    return baseDamage;
}
```

---

## Unity 编辑器设置

### 1. 创建数据库资源

```
Assets/
├── Resources/
│   └── Databases/
│       ├── ItemDatabase.asset
│       ├── PrefixDatabase.asset
│       └── RecipeDatabase.asset
├── ScriptableObjects/
│   ├── Items/
│   │   ├── Weapons/
│   │   ├── Armors/
│   │   ├── Consumables/
│   │   └── Materials/
│   ├── Prefixes/
│   └── Recipes/
```

### 2. 创建物品示例

在Unity中右键 → Create → ItemSystem → Equipment → Weapon

```csharp
// Inspector 中设置：
// - Item ID: 1001
// - Item Name: 铁剑
// - Rarity: Uncommon
// - Flags: Reforgeable | Socketable | HasDurability | Sellable
// - Weapon Category: Sharp
// - Base Damage: 15
// - Attack Range: Melee, positions [0, 1]
```

---

## 示例前缀配置

| 前缀名 | 类别 | 伤害 | 暴击 | 速度 | 等级 |
|--------|------|------|------|------|------|
| 残破的 | 通用 | -15% | -5% | -10% | Negative |
| 普通的 | 通用 | 0% | 0% | 0% | Neutral |
| 锋利的 | 近战 | +10% | +3% | 0% | Good |
| 迅捷的 | 通用 | +5% | 0% | +15% | Good |
| 致命的 | 通用 | +15% | +8% | 0% | Great |
| 传奇的 | 通用 | +20% | +10% | +10% | Best |

---

## 扩展建议

1. **掉落表系统**：创建 `LootTable` ScriptableObject 管理怪物掉落
2. **商店系统**：基于 `IInventory` 接口实现商店库存
3. **存档系统**：`ItemInstance` 已支持序列化，可直接保存
4. **物品强化**：扩展 `ItemInstance` 添加强化等级
5. **套装效果**：创建 `EquipmentSet` 检测完整套装并应用奖励

---

## 性能优化建议

1. **缓存模板引用**：`ItemInstance.Template` 使用延迟加载
2. **分帧加载**：大型数据库使用异步加载
3. **对象池**：频繁创建的 `ItemInstance` 使用对象池
4. **字典索引**：数据库使用 Dictionary 而非 List 查找