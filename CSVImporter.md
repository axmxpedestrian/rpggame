# 物品CSV导入格式说明

## 使用方法

1. 在Unity中打开菜单: `Tools → Item System → CSV Importer`
2. 点击"生成CSV模板"按钮，会在指定文件夹生成所有类型的模板文件
3. 用Excel或其他工具编辑CSV文件
4. 选择CSV文件路径，点击"导入"

## 通用规则

- 以 `#` 开头的行为注释，会被跳过
- 第一行（非注释）为表头，会被跳过
- 空行会被跳过
- 字段之间用逗号 `,` 分隔
- 多值字段用竖线 `|` 分隔

---

## 各类物品字段说明

### 1. 武器 (Weapons)

| 列号 | 字段名 | 类型 | 说明 | 示例 |
|------|--------|------|------|------|
| 1 | ItemId | int | 唯一物品ID | 1001 |
| 2 | ItemName | string | 物品名称 | 铁剑 |
| 3 | Description | string | 物品描述 | 一把普通的铁剑 |
| 4 | IconPath | string | 图标路径(相对Assets) | Icons/Weapons/iron_sword |
| 5 | Rarity | enum | 品质 | Common/Uncommon/Rare/Epic/Legendary |
| 6 | Flags | flags | 功能标记，用\|分隔 | Reforgeable\|Sellable\|HasDurability |
| 7 | BuyPrice | int | 购买价格 | 100 |
| 8 | SellPrice | int | 出售价格 | 50 |
| 9 | LevelRequirement | int | 等级要求 | 1 |
| 10 | WeaponCategory | enum | 武器类别 | Blunt/Sharp/Bow/Explosive/Gun/Magic |
| 11 | BaseDamage | int | 基础伤害 | 15 |
| 12 | CriticalChance | float | 暴击率(0-1) | 0.05 |
| 13 | CriticalMultiplier | float | 暴击倍率 | 1.5 |
| 14 | Element | enum | 元素类型 | None/Fire/Ice/Lightning/Poison/Holy/Dark |
| 15 | DamageCategory | enum | 伤害类型 | Physical/Magic/True |
| 16 | MaxDurability | int | 最大耐久 | 100 |
| 17 | RangeType | enum | 攻击范围类型 | Melee/Ranged/All/Adjacent/Self |
| 18 | TargetPositions | int[] | 可攻击位置(0-4) | 0\|1 |
| 19 | SkillIds | int[] | 武器技能ID | 101\|102 |

**武器类别说明：**
- `Blunt` - 钝器（近战）
- `Sharp` - 锐器（近战）
- `Bow` - 弓（远程）
- `Explosive` - 炸药（远程）
- `Gun` - 枪（远程）
- `Magic` - 法术（魔法）

---

### 2. 护甲 (Armors)

| 列号 | 字段名 | 类型 | 说明 | 示例 |
|------|--------|------|------|------|
| 1 | ItemId | int | 唯一物品ID | 2001 |
| 2 | ItemName | string | 物品名称 | 铁头盔 |
| 3 | Description | string | 物品描述 | 基础的铁制头盔 |
| 4 | IconPath | string | 图标路径 | Icons/Armors/iron_helmet |
| 5 | Rarity | enum | 品质 | Common |
| 6 | Flags | flags | 功能标记 | Socketable\|Sellable\|HasDurability |
| 7 | BuyPrice | int | 购买价格 | 80 |
| 8 | SellPrice | int | 出售价格 | 40 |
| 9 | LevelRequirement | int | 等级要求 | 1 |
| 10 | ArmorSlot | enum | 护甲部位 | Head/Body/Legs |
| 11 | PhysicalDefense | int | 物理防御 | 5 |
| 12 | MagicDefense | int | 魔法防御 | 2 |
| 13 | Resistances | float[] | 元素抗性(7个值) | 0\|0.3\|0\|0\|0\|0\|0 |
| 14 | MaxDurability | int | 最大耐久 | 120 |

**抗性顺序：** None | Fire | Ice | Lightning | Poison | Holy | Dark

---

### 3. 饰品 (Accessories)

| 列号 | 字段名 | 类型 | 说明 | 示例 |
|------|--------|------|------|------|
| 1 | ItemId | int | 唯一物品ID | 3001 |
| 2 | ItemName | string | 物品名称 | 力量戒指 |
| 3 | Description | string | 物品描述 | 增加力量的戒指 |
| 4 | IconPath | string | 图标路径 | Icons/Accessories/str_ring |
| 5 | Rarity | enum | 品质 | Uncommon |
| 6 | Flags | flags | 功能标记 | Sellable |
| 7 | BuyPrice | int | 购买价格 | 200 |
| 8 | SellPrice | int | 出售价格 | 100 |
| 9 | LevelRequirement | int | 等级要求 | 5 |
| 10 | StatModifiers | special | 属性修改 | Strength:Flat:5 |
| 11 | SpecialEffects | special | 特殊效果 | CriticalChanceBonus:0.05:None |
| 12 | PassiveSkillId | int | 被动技能ID | 201 |

**StatModifiers格式：** `StatType:ModifierType:Value|StatType:ModifierType:Value`

StatType可选值：
- Constitution, Strength, Perception, Reaction, Wisdom, Luck
- MaxHealth, PhysicalAttack, MagicAttack, PhysicalDefense, MagicDefense
- Resistance, CriticalRate, Speed, Accuracy, Evasion

ModifierType可选值：
- `Flat` - 固定值加成
- `PercentAdd` - 百分比加成（加法）
- `PercentMult` - 百分比乘数（乘法）

**SpecialEffects格式：** `EffectType:Value:Element`

EffectType可选值：
- DamageBonus, ElementalDamageBonus, CriticalChanceBonus
- CriticalDamageBonus, ResistanceBonus, SkillPointRecovery
- StressReduction, FatigueReduction

---

### 4. 回复类消耗品 (HealingItems)

| 列号 | 字段名 | 类型 | 说明 | 示例 |
|------|--------|------|------|------|
| 1 | ItemId | int | 唯一物品ID | 4001 |
| 2 | ItemName | string | 物品名称 | 小型生命药水 |
| 3 | Description | string | 物品描述 | 恢复少量生命 |
| 4 | IconPath | string | 图标路径 | Icons/Consumables/hp_potion_s |
| 5 | Rarity | enum | 品质 | Common |
| 6 | Flags | flags | 功能标记 | Stackable\|UsableInCombat\|CanCarryToBattle\|Sellable |
| 7 | BuyPrice | int | 购买价格 | 50 |
| 8 | SellPrice | int | 出售价格 | 25 |
| 9 | MaxStackSize | int | 最大堆叠数 | 20 |
| 10 | HealingType | enum | 回复类型 | Health/SkillPoints/Stress/Fatigue/Revive/All |
| 11 | HealAmount | int | 固定回复量 | 50 |
| 12 | HealPercent | float | 百分比回复(0-1) | 0.3 |
| 13 | HealOverTime | bool | 是否持续回复 | false |
| 14 | Duration | float | 持续时间(秒) | 0 |
| 15 | TickCount | int | 回复次数 | 0 |
| 16 | StressReduction | int | 减少压力值 | 0 |
| 17 | FatigueReduction | int | 减少疲劳值 | 0 |
| 18 | RemoveDebuffs | bool | 是否移除负面效果 | false |
| 19 | DebuffsToRemove | enum[] | 要移除的负面效果 | Poisoned\|Bleeding |
| 20 | ATBCost | int | ATB消耗 | 50 |
| 21 | TargetType | enum | 目标类型 | SingleAlly |
| 22 | Cooldown | float | 冷却时间 | 0 |

**TargetType可选值：**
- `Self` - 自身
- `SingleAlly` - 单个友方
- `AllAllies` - 所有友方
- `SingleEnemy` - 单个敌人
- `AllEnemies` - 所有敌人
- `Position` - 基于位置
- `All` - 全体

---

### 5. 增益类消耗品 (BuffItems)

| 列号 | 字段名 | 类型 | 说明 | 示例 |
|------|--------|------|------|------|
| 1 | ItemId | int | 唯一物品ID | 5001 |
| 2 | ItemName | string | 物品名称 | 力量药剂 |
| 3 | Description | string | 物品描述 | 暂时提升攻击力 |
| 4 | IconPath | string | 图标路径 | Icons/Consumables/str_potion |
| 5 | Rarity | enum | 品质 | Common |
| 6 | Flags | flags | 功能标记 | Stackable\|UsableInCombat\|CanCarryToBattle\|Sellable |
| 7 | BuyPrice | int | 购买价格 | 100 |
| 8 | SellPrice | int | 出售价格 | 50 |
| 9 | MaxStackSize | int | 最大堆叠数 | 10 |
| 10 | BuffEffects | special | 增益效果 | Attack:15:false\|Speed:0.2:true |
| 11 | Duration | float | 持续时间(秒) | 60 |
| 12 | IsPermanent | bool | 战斗内永久 | false |
| 13 | ATBCost | int | ATB消耗 | 50 |
| 14 | TargetType | enum | 目标类型 | SingleAlly |
| 15 | Cooldown | float | 冷却时间 | 0 |

**BuffEffects格式：** `BuffType:Value:IsPercentage`

BuffType可选值：
- Attack, Defense, MagicAttack, MagicDefense
- Speed, CriticalChance, CriticalDamage
- Accuracy, Evasion, AllStats

IsPercentage: `true`=百分比, `false`=固定值

---

### 6. 工具 (Tools)

| 列号 | 字段名 | 类型 | 说明 | 示例 |
|------|--------|------|------|------|
| 1 | ItemId | int | 唯一物品ID | 6001 |
| 2 | ItemName | string | 物品名称 | 铁钥匙 |
| 3 | Description | string | 物品描述 | 打开铁门的钥匙 |
| 4 | IconPath | string | 图标路径 | Icons/Tools/iron_key |
| 5 | Rarity | enum | 品质 | Common |
| 6 | Flags | flags | 功能标记 | Stackable\|Sellable |
| 7 | BuyPrice | int | 购买价格 | 50 |
| 8 | SellPrice | int | 出售价格 | 25 |
| 9 | MaxStackSize | int | 最大堆叠数 | 10 |
| 10 | ToolType | enum | 工具类型 | Key/Bomb/Torch/Rope/Pickaxe/Shovel/FishingRod/Compass/Map/Teleporter |
| 11 | UseCount | int | 使用次数(-1=无限) | 1 |
| 12 | Cooldown | float | 冷却时间 | 0 |
| 13 | EffectData | string | 效果数据(自定义) | door_iron |

---

### 7. 材料 (Materials)

| 列号 | 字段名 | 类型 | 说明 | 示例 |
|------|--------|------|------|------|
| 1 | ItemId | int | 唯一物品ID | 7001 |
| 2 | ItemName | string | 物品名称 | 铁矿石 |
| 3 | Description | string | 物品描述 | 普通的铁矿石 |
| 4 | IconPath | string | 图标路径 | Icons/Materials/iron_ore |
| 5 | Rarity | enum | 品质 | Common |
| 6 | Flags | flags | 功能标记 | Stackable\|Sellable\|Craftable |
| 7 | BuyPrice | int | 购买价格 | 20 |
| 8 | SellPrice | int | 出售价格 | 10 |
| 9 | MaxStackSize | int | 最大堆叠数 | 99 |
| 10 | MaterialCategory | enum | 材料类别 | Ore/Wood/Herb/Gem/Fabric/Leather/Bone/Monster/Essence/Other |
| 11 | Tier | int | 材料等级 | 1 |

---

### 8. 服装/时装 (Cosmetics)

| 列号 | 字段名 | 类型 | 说明 | 示例 |
|------|--------|------|------|------|
| 1 | ItemId | int | 唯一物品ID | 8001 |
| 2 | ItemName | string | 物品名称 | 红色披风 |
| 3 | Description | string | 物品描述 | 一件漂亮的红色披风 |
| 4 | IconPath | string | 图标路径 | Icons/Cosmetics/red_cape |
| 5 | Rarity | enum | 品质 | Uncommon |
| 6 | Flags | flags | 功能标记 | Sellable |
| 7 | BuyPrice | int | 购买价格 | 300 |
| 8 | SellPrice | int | 出售价格 | 150 |
| 9 | CosmeticSlot | enum | 装饰槽位 | Hat/Hair/Face/Back/Outfit/Pet |
| 10 | HasStats | bool | 是否有属性 | false |
| 11 | StatModifiers | special | 属性修改 | Strength:Flat:3 |
| 12 | TintColor | hex | 着色(RRGGBB) | FF0000 |
| 13 | VisualSprites | string[] | 视觉精灵路径 | Sprites/cape_0\|Sprites/cape_1 |

---

### 9. 剧情道具 (QuestItems)

| 列号 | 字段名 | 类型 | 说明 | 示例 |
|------|--------|------|------|------|
| 1 | ItemId | int | 唯一物品ID | 9001 |
| 2 | ItemName | string | 物品名称 | 神秘钥匙 |
| 3 | Description | string | 物品描述 | 打开古代遗迹的钥匙 |
| 4 | IconPath | string | 图标路径 | Icons/Quest/mystery_key |
| 5 | Rarity | enum | 品质 | Epic |
| 6 | QuestId | string | 关联任务ID | quest_ancient_ruins |
| 7 | QuestItemType | enum | 道具类型 | KeyItem/Clue/Letter/Photo/Artifact/Memory |
| 8 | AutoRemoveOnQuestComplete | bool | 任务完成后自动移除 | true |
| 9 | LoreText | string | 剧情文本 | 这把钥匙上刻着古老的符文... |

**注意：** 剧情道具自动设置为不可丢弃、不可出售

---

## 功能标记 (Flags) 完整列表

| 标记名 | 说明 |
|--------|------|
| Stackable | 可堆叠 |
| Craftable | 可合成 |
| Reforgeable | 可重铸 |
| HasDurability | 有耐久度 |
| Sellable | 可出售 |
| Droppable | 可丢弃 |
| UsableInCombat | 战斗中可使用 |
| CanCarryToBattle | 可携带进入战斗 |
| Socketable | 可镶嵌配件 |
| Tradeable | 可交易 |

---

## ID规划建议

| 物品类型 | ID范围 |
|----------|--------|
| 武器 | 1001 - 1999 |
| 护甲 | 2001 - 2999 |
| 饰品 | 3001 - 3999 |
| 回复品 | 4001 - 4999 |
| 增益品 | 5001 - 5999 |
| 工具 | 6001 - 6999 |
| 材料 | 7001 - 7999 |
| 服装 | 8001 - 8999 |
| 剧情道具 | 9001 - 9999 |

---

## CSV示例

### 武器示例
```csv
ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,LevelRequirement,WeaponCategory,BaseDamage,CriticalChance,CriticalMultiplier,Element,DamageCategory,MaxDurability,RangeType,TargetPositions,SkillIds
1001,铁剑,一把普通的铁剑,Icons/Weapons/iron_sword,Common,Reforgeable|Sellable|HasDurability,100,50,1,Sharp,15,0.05,1.5,None,Physical,100,Melee,0|1,
1002,火焰法杖,蕴含火焰之力的法杖,Icons/Weapons/fire_staff,Rare,Reforgeable|Socketable|Sellable,500,250,10,Magic,25,0.08,1.8,Fire,Magic,80,All,0|1|2|3|4,101|102
1003,猎弓,轻便的猎弓,Icons/Weapons/hunting_bow,Common,Reforgeable|Sellable|HasDurability,120,60,3,Bow,12,0.06,1.6,None,Physical,90,Ranged,2|3|4,
```

### 回复品示例
```csv
ItemId,ItemName,Description,IconPath,Rarity,Flags,BuyPrice,SellPrice,MaxStackSize,HealingType,HealAmount,HealPercent,HealOverTime,Duration,TickCount,StressReduction,FatigueReduction,RemoveDebuffs,DebuffsToRemove,ATBCost,TargetType,Cooldown
4001,小型生命药水,恢复少量生命,Icons/Consumables/hp_potion_s,Common,Stackable|UsableInCombat|CanCarryToBattle|Sellable,50,25,20,Health,50,0,false,0,0,0,0,false,,50,SingleAlly,0
4002,全能药剂,恢复生命和技能点,Icons/Consumables/all_potion,Rare,Stackable|UsableInCombat|CanCarryToBattle|Sellable,300,150,5,All,100,0.2,false,0,0,10,10,false,,60,SingleAlly,0
```