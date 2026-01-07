# CSV字段快速参考

## 所有物品通用字段（前9列）

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| ItemId | int | ✓ | 唯一ID |
| ItemName | string | ✓ | 名称 |
| Description | string | ✓ | 描述 |
| IconPath | string | | 图标路径 |
| Rarity | enum | ✓ | Common/Uncommon/Rare/Epic/Legendary |
| Flags | flags | | 用\|分隔的功能标记 |
| BuyPrice | int | ✓ | 购买价格 |
| SellPrice | int | ✓ | 出售价格 |

---

## 各类型专属字段

### 武器 (额外10列)
```
LevelRequirement, WeaponCategory, BaseDamage, CriticalChance, CriticalMultiplier, 
Element, DamageCategory, MaxDurability, RangeType, TargetPositions, SkillIds
```

### 护甲 (额外6列)
```
LevelRequirement, ArmorSlot, PhysicalDefense, MagicDefense, Resistances, MaxDurability
```

### 饰品 (额外4列)
```
LevelRequirement, StatModifiers, SpecialEffects, PassiveSkillId
```

### 回复品 (额外14列)
```
MaxStackSize, HealingType, HealAmount, HealPercent, HealOverTime, Duration, TickCount,
StressReduction, FatigueReduction, RemoveDebuffs, DebuffsToRemove, ATBCost, TargetType, Cooldown
```

### 增益品 (额外7列)
```
MaxStackSize, BuffEffects, Duration, IsPermanent, ATBCost, TargetType, Cooldown
```

### 工具 (额外5列)
```
MaxStackSize, ToolType, UseCount, Cooldown, EffectData
```

### 材料 (额外3列)
```
MaxStackSize, MaterialCategory, Tier
```

### 服装 (额外5列)
```
CosmeticSlot, HasStats, StatModifiers, TintColor, VisualSprites
```

### 剧情道具 (额外4列)
```
QuestId, QuestItemType, AutoRemoveOnQuestComplete, LoreText
```

---

## 枚举值速查

### Rarity（品质）
Common | Uncommon | Rare | Epic | Legendary

### ItemFlags（功能标记）
Stackable | Craftable | Reforgeable | HasDurability | Sellable | Droppable | UsableInCombat | CanCarryToBattle | Socketable | Tradeable

### WeaponCategory（武器类别）
Blunt | Sharp | Bow | Explosive | Gun | Magic

### Element（元素）
None | Fire | Ice | Lightning | Poison | Holy | Dark

### DamageCategory（伤害类型）
Physical | Magic | True

### ArmorSlot（护甲部位）
Head | Body | Legs

### HealingType（回复类型）
Health | SkillPoints | Stress | Fatigue | Revive | All

### BuffType（增益类型）
Attack | Defense | MagicAttack | MagicDefense | Speed | CriticalChance | CriticalDamage | Accuracy | Evasion | AllStats

### TargetType（目标类型）
Self | SingleAlly | AllAllies | SingleEnemy | AllEnemies | Position | All

### ToolType（工具类型）
Key | Bomb | Torch | Rope | Pickaxe | Shovel | FishingRod | Compass | Map | Teleporter

### MaterialCategory（材料类别）
Ore | Wood | Herb | Gem | Fabric | Leather | Bone | Monster | Essence | Other

### CosmeticSlot（装饰槽位）
Hat | Hair | Face | Back | Outfit | Pet

### QuestItemType（剧情道具类型）
KeyItem | Clue | Letter | Photo | Artifact | Memory

### StatType（属性类型）
Constitution | Strength | Perception | Reaction | Wisdom | Luck | MaxHealth | PhysicalAttack | MagicAttack | PhysicalDefense | MagicDefense | Resistance | CriticalRate | Speed | Accuracy | Evasion

### ModifierType（修改器类型）
Flat | PercentAdd | PercentMult

---

## 特殊格式字段

### StatModifiers
格式: `StatType:ModifierType:Value|StatType:ModifierType:Value`
示例: `Strength:Flat:10|CriticalRate:PercentAdd:0.05`

### BuffEffects
格式: `BuffType:Value:IsPercentage|...`
示例: `Attack:15:false|Speed:0.2:true`

### SpecialEffects
格式: `EffectType:Value:Element|...`
示例: `DamageBonus:0.1:None|ElementalDamageBonus:0.2:Fire`

### Resistances (护甲)
顺序: None|Fire|Ice|Lightning|Poison|Holy|Dark
示例: `0|0.3|0|0|0|0|0` (火抗30%)

### TargetPositions (武器)
位置索引0-4，用|分隔
示例: `0|1` (前两个位置)
