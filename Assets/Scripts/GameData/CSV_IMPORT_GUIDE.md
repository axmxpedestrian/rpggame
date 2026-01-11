# CSV数据导入指南

## 概述

本系统允许你通过CSV文件批量导入游戏数据，自动生成对应的ScriptableObject资源。这种方式非常适合与策划人员协作，他们可以在Excel/Google Sheets中编辑数据，导出CSV后直接导入Unity。

## 使用方法

### 在Unity编辑器中使用

1. 菜单栏选择 `Tools > CSV Import > Import Window`
2. 选择导入类型（角色/敌人/技能/武器技能）
3. 选择CSV文件路径
4. 选择输出文件夹
5. 点击"开始导入"

### 生成CSV模板

菜单栏选择 `Tools > CSV Import > Generate CSV Templates`，系统会在 `Assets/Data/CSVTemplates/` 生成所有模板文件，可以直接参考格式填写数据。

---

## 角色数据 (CharacterData)

### 需要提供的字段

| 字段名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| characterId | string | ✓ | - | 唯一标识符，如 "hero_001" |
| characterName | string | ✓ | - | 显示名称 |
| description | string | | "" | 角色描述文本 |
| characterType | enum | | Player | Player/Companion/Enemy/Elite/Boss/Summon |
| baseLevel | int | | 1 | 初始等级 |

#### 基础属性（6维）

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| constitution | int | 10 | 体质 - 影响生命值、物理格挡 |
| strength | int | 10 | 力量 - 影响物理攻击、负重 |
| perception | int | 10 | 感知 - 影响命中率、暴击率 |
| reaction | int | 10 | 反应 - 影响速度、闪避 |
| wisdom | int | 10 | 智慧 - 影响魔法攻击、魔法防御 |
| luck | int | 10 | 幸运 - 影响暴击、掉落率 |

#### 成长率（每级增加）

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| constitutionGrowth | float | 1.0 | 体质成长率 |
| strengthGrowth | float | 1.0 | 力量成长率 |
| perceptionGrowth | float | 1.0 | 感知成长率 |
| reactionGrowth | float | 1.0 | 反应成长率 |
| wisdomGrowth | float | 1.0 | 智慧成长率 |
| luckGrowth | float | 0.5 | 幸运成长率 |

#### 战斗设置

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| baseATBSpeed | float | 100 | 基础ATB充能速度 |
| basePhysicalSP | int | 5 | 基础物理技能点 |
| baseMagicSP | int | 5 | 基础魔法技能点 |

#### 元素抗性（-1.0 到 1.0）

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| fireResistance | float | 0 | 火焰抗性，负数为弱点 |
| iceResistance | float | 0 | 冰霜抗性 |
| lightningResistance | float | 0 | 雷电抗性 |
| poisonResistance | float | 0 | 毒素抗性 |
| holyResistance | float | 0 | 神圣抗性 |
| darkResistance | float | 0 | 暗影抗性 |

#### 初始配置（ID列表，用分号分隔）

| 字段名 | 类型 | 说明 |
|--------|------|------|
| defaultWeaponIds | string[] | 默认武器ID，如 "weapon_001;weapon_002" |
| defaultArmorIds | string[] | 默认护甲ID |
| defaultSkillIds | string[] | 默认技能ID |

#### 资源路径

| 字段名 | 类型 | 说明 |
|--------|------|------|
| spriteAtlasPath | string | 精灵图集路径 |
| portraitPath | string | 头像路径 |
| animatorPath | string | 动画控制器路径 |

### CSV示例

```csv
characterId,characterName,description,characterType,baseLevel,constitution,strength,perception,reaction,wisdom,luck,defaultSkillIds
hero_001,勇者,冒险的主角,Player,1,12,14,10,11,8,10,skill_slash;skill_guard
```

---

## 敌人数据 (EnemyData)

敌人数据继承自角色数据，包含所有角色字段，并额外增加以下字段：

### 敌人特有字段

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| category | enum | Beast | Beast/Humanoid/Undead/Demon/Elemental/Mechanical/Dragon/Aberration |
| threatLevel | int | 1 | 威胁等级 (1-10) |
| isBoss | bool | false | 是否为Boss |

### AI设置

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| aiBehavior | enum | Balanced | Aggressive/Defensive/Support/Balanced/Berserker/Tactical/Coward |
| aggroRange | float | 5 | 仇恨触发范围 |
| deaggroRange | float | 15 | 脱离战斗范围 |

### 奖励设置

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| baseExpReward | int | 10 | 基础经验奖励 |
| baseGoldReward | int | 5 | 基础金币奖励 |

### 生成设置

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| spawnLocations | string[] | [] | 可出现的地点ID列表 |
| minSpawnLevel | int | 1 | 最小生成等级 |
| maxSpawnLevel | int | 99 | 最大生成等级 |
| spawnWeight | float | 1.0 | 生成权重 |

### CSV示例

```csv
characterId,characterName,category,threatLevel,isBoss,aiBehavior,constitution,strength,baseExpReward,baseGoldReward,defaultSkillIds
enemy_slime_001,史莱姆,Beast,1,false,Aggressive,8,6,10,5,skill_tackle
boss_dragon_001,火焰巨龙,Dragon,10,true,Berserker,50,45,500,200,skill_fire_breath;skill_tail_swipe
```

---

## 技能数据 (SkillDataConfig)

### 基础信息

| 字段名 | 类型 | 必填 | 默认值 | 说明 |
|--------|------|------|--------|------|
| skillId | string | ✓ | - | 唯一标识符 |
| skillName | string | ✓ | - | 技能名称 |
| description | string | | "" | 技能描述 |
| category | enum | | PhysicalSkill | WeaponSkill/MagicSkill/PhysicalSkill/SupportSkill/PassiveSkill/UltimateSkill |
| skillType | enum | | Active | Active/Passive/Toggle/Reaction |

### 消耗与冷却

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| spCost | int | 1 | 技能点消耗 |
| mpCost | int | 0 | 魔法值消耗 |
| hpCost | int | 0 | 生命值消耗 |
| atbCost | int | 100 | ATB消耗 |
| cooldown | int | 0 | 冷却回合数 |

### 伤害数值

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| basePower | float | 100 | 基础威力 |
| powerScaling | float | 1.0 | 属性加成系数 |
| scalingAttribute | enum | Strength | 加成属性来源 |
| damageType | enum | Physical | Physical/Magic/True |
| element | enum | None | None/Fire/Ice/Lightning/Poison/Holy/Dark |

### 目标设置

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| targetType | enum | SingleEnemy | Self/SingleAlly/SingleEnemy/AllAllies/AllEnemies/Area |
| maxTargets | int | 1 | 最大目标数 |
| range | float | 1.0 | 射程 |
| aoeRadius | float | 0 | AOE半径（0为单体） |

### 命中与暴击

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| accuracyModifier | float | 1.0 | 命中率修正 |
| critRateBonus | float | 0 | 额外暴击率 |
| critDamageBonus | float | 0 | 额外暴击伤害 |

### 连击设置

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| hitCount | int | 1 | 攻击次数 |
| hitInterval | float | 0.1 | 攻击间隔（秒） |

### 解锁条件

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| requiredLevel | int | 1 | 需求等级 |
| prerequisiteSkillIds | string[] | [] | 前置技能ID |

### CSV示例

```csv
skillId,skillName,category,spCost,atbCost,basePower,powerScaling,scalingAttribute,damageType,element,targetType,hitCount,requiredLevel
skill_slash,斩击,PhysicalSkill,1,100,120,1.0,Strength,Physical,None,SingleEnemy,1,1
skill_fireball,火球术,MagicSkill,0,100,150,1.2,Wisdom,Magic,Fire,SingleEnemy,1,5
```

---

## 武器技能数据 (WeaponSkillData)

继承自技能数据，额外增加以下字段：

### 武器技能特有字段

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| requiredWeaponCategory | enum | Blunt | Blunt/Sharp/Bow/Explosive/Gun/Magic |
| proficiencyRequired | int | 0 | 需要的熟练度等级 |
| proficiencyGainOnUse | int | 1 | 使用时获得的熟练度 |
| consumesDurability | bool | true | 是否消耗耐久 |
| durabilityConsumption | int | 1 | 耐久消耗量 |

### 连击设置

| 字段名 | 类型 | 默认值 | 说明 |
|--------|------|--------|------|
| isComboStarter | bool | false | 是否为连击起手 |
| isComboFinisher | bool | false | 是否为连击终结 |
| validComboPredecessors | string[] | [] | 有效的前置连击技能 |

---

## 关于技能的设计建议

### 适合CSV导入的内容

1. **数值参数** - 伤害、消耗、冷却、范围等
2. **分类信息** - 技能类型、目标类型、元素类型
3. **解锁条件** - 等级需求、前置技能
4. **资源路径** - 图标、音效、特效的路径

### 不适合CSV导入的内容

1. **复杂逻辑** - 条件判断、特殊效果计算
2. **Unity资源引用** - 需要在编辑器中手动关联
3. **动态行为** - AI逻辑、状态机

### 推荐的混合方案

```
CSV导入数值数据 → 生成SkillDataConfig → 在代码中实现SkillEffect → 通过skillId关联
```

例如，"火球术"的实现：

```csharp
// CSV中定义的数值
// skillId: skill_fireball
// basePower: 150
// element: Fire

// 代码中实现的效果
public class FireballEffect : SkillEffect
{
    public override void Execute(SkillDataConfig data, ICharacter caster, ICharacter target)
    {
        // 使用data中的数值进行伤害计算
        float damage = data.CalculatePower(caster.Stats.Wisdom);
        
        // 实现特殊效果（燃烧、爆炸等）
        if (Random.value < 0.3f)
        {
            target.StatusEffectManager.AddEffect(new BurningEffect());
        }
    }
}
```

---

## CSV格式注意事项

1. **编码** - 使用UTF-8编码保存CSV文件
2. **分隔符** - 字段间使用逗号分隔
3. **引号** - 如果字段值包含逗号，需要用双引号包围
4. **多值字段** - 使用分号分隔，如 `skill_01;skill_02;skill_03`
5. **布尔值** - 支持 true/false、1/0、yes/no、是/否
6. **枚举值** - 不区分大小写
7. **注释** - 以 # 开头的行会被忽略

---

## 常见问题

### Q: 如何更新已存在的数据？
A: 勾选"覆盖已存在文件"选项，导入时会更新现有的ScriptableObject。

### Q: 如何只导入部分数据？
A: 在CSV中只保留需要导入的行，或者使用#注释掉不需要的行。

### Q: 资源路径如何填写？
A: 填写相对于Assets文件夹的路径，如 `Sprites/Characters/Hero`。导入后需要在编辑器中手动关联实际资源。

### Q: 如何处理本地化？
A: 建议在CSV中只填写ID，实际显示文本通过本地化系统加载。例如 `characterName` 填写 `LOC_HERO_NAME`，运行时从本地化表查询实际文本。
