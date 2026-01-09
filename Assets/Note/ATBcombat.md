# Unity2D ATB战斗系统

## 概述

本系统为横版回合制RPG设计，采用ATB（Active Time Battle）动态时间战斗模式，与物品系统完全兼容。

**核心特性：**
- 伤害类型(Physical/Magic/True)与元素(Fire/Ice等)分离
- 站位系统（5位置阵型、自动补位）
- 基于位置的攻击范围判定
- 武器熟练度系统
- 完整技能系统（武器技能/角色技能）
- 技能点回合恢复机制
- 高频属性访问缓存机制

## 文件结构

```
CombatSystem/
├── CharacterStats.cs         # 属性系统（6维基础+派生）
├── Character.cs              # 角色类（装备/状态/ATB/熟练度/技能）
├── CombatAttributeCache.cs   # 战斗属性缓存
├── DamageCalculator.cs       # 伤害计算器
├── ATBCombatManager.cs       # ATB战斗管理器
├── StatusEffectSystem.cs     # 状态效果系统
├── PositionSystem.cs         # 站位与攻击范围系统 [新增]
├── WeaponProficiencySystem.cs# 武器熟练度系统 [新增]
└── SkillSystem.cs            # 技能系统 [新增]
```

---

## 站位系统

### 阵型布局

```
敌方队伍:  [4] [3] [2] [1] [0]  ← 前排（位置0）
                 ↕ 战场 ↕
友方队伍:  [0] [1] [2] [3] [4]  ← 前排（位置0）

位置0 = 前排（最靠近敌人）
位置4 = 后排（最远离敌人）
```

### 自动补位

```csharp
// 当前排角色倒下时，后排自动向前补位
_formation.OnCharacterDowned(character, side);

// 补位前: [倒下] [B] [C] [空] [空]
// 补位后: [B] [C] [倒下] [空] [空]
```

### 攻击范围类型

| 类型 | 说明 | 示例 |
|------|------|------|
| `Fixed` | 固定位置 | 弓只能打位置[2,3,4] |
| `Relative` | 相对攻击者 | 治疗自己±1位置 |
| `Distance` | 基于距离 | 距离≤2的目标 |
| `Adjacent` | 仅相邻 | 自己和左右队友 |
| `FrontLine` | 仅前排 | 只能打最前面的敌人 |
| `All` | 全体 | 群体魔法 |

```csharp
// 近战武器 - 只能打前两排
var meleeRange = new AttackRangeDefinition
{
    rangeType = RangeType.Fixed,
    fixedTargetPositions = new[] { 0, 1 }
};

// 治疗相邻 - 自己和左右队友
var healAdjacent = new AttackRangeDefinition
{
    rangeType = RangeType.Adjacent,
    canTargetAllies = true,
    canTargetSelf = true,
    canTargetEnemies = false
};
```

---

## 武器熟练度系统

### 熟练度等级

| 等级 | 名称 | 伤害加成 | 暴击加成 | 速度加成 |
|------|------|----------|----------|----------|
| 0 | 生疏 | +0% | +0% | +0% |
| 1 | 入门 | +5% | +2% | +3% |
| 2 | 熟练 | +10% | +4% | +6% |
| 3 | 精通 | +15% | +6% | +9% |
| 4 | 大师 | +20% | +8% | +12% |
| 5+ | 宗师 | +25%+ | +10%+ | +15%+ |

### 经验获取

```csharp
// 使用武器攻击获得经验
proficiency.OnWeaponUsed(WeaponCategory.Sharp, isSkill: false, hitTarget: true);
// 命中: +2 exp, 未命中: +1 exp, 技能: +1 额外

// 击杀敌人获得额外经验
proficiency.OnEnemyKilled(WeaponCategory.Sharp, enemyLevel: 5);
// +5 + 敌人等级 exp
```

### 技能解锁

```csharp
// 熟练度等级解锁技能槽
// 1级: 1个武器技能槽
// 3级: 2个武器技能槽
// 5级: 3个武器技能槽

// 高熟练度减少技能点消耗
// 3级起每级减少1点消耗
int reduction = proficiency.GetSkillCostReduction(category);
```

---

## 技能系统

### 技能类型

| 类型 | 说明 | 限制 |
|------|------|------|
| `WeaponSkill` | 武器技能 | 需装备对应武器+熟练度 |
| `CharacterSkill` | 角色技能 | 角色专属 |
| `ItemSkill` | 物品技能 | 来自装备 |
| `CommonSkill` | 通用技能 | 无限制 |

### 技能消耗

```csharp
public enum SkillCostType
{
    None,       // 普通攻击：无消耗
    PhysicalSP, // 物理技能：消耗体力点
    MagicSP,    // 魔法技能：消耗魔力点
    Health      // 特殊技能：消耗生命值
}
```

### 技能点恢复

```csharp
// 每回合结束时自动恢复
// 基础: 物理+1, 魔法+1
// 装备可增加恢复量
SkillPointManager.OnTurnStart(character);
```

### 技能定义示例

```csharp
[CreateAssetMenu(fileName = "斩击", menuName = "CombatSystem/Skill")]
public class SlashSkill : SkillDefinition
{
    // 基础信息
    skillName = "斩击";
    skillType = SkillType.WeaponSkill;
    requiredWeaponCategory = WeaponCategory.Sharp;
    requiredProficiencyLevel = 1;
    
    // 消耗
    costType = SkillCostType.PhysicalSP;
    baseCost = 2;
    atbCost = 100;
    
    // 效果
    damageMultiplier = 1.5f;
    damageCategory = DamageCategory.Physical;
    
    // 范围（只能打前排）
    attackRange = AttackRangePresets.Melee;
}
```

---

## 核心设计

### 1. 伤害类型与元素分离

```
伤害 = 基础伤害 
     × 伤害类型倍率(Physical/Magic/True) 
     × 元素倍率(Fire/Ice/Lightning...)
     - 防御减免(基于伤害类型)
     × (1 - 元素抗性)
```

**优势：**
- 物理火焰剑：Physical + Fire
- 魔法冰霜箭：Magic + Ice
- 真实毒素：True + Poison（无视防御，但受毒抗影响）

```csharp
// 伤害类型
public enum DamageCategory
{
    Physical,   // 物理 - 受物防减免
    Magic,      // 魔法 - 受魔防减免
    True        // 真实 - 无视防御
}

// 元素类型（独立于伤害类型）
public enum ElementType
{
    None, Fire, Ice, Lightning, Poison, Holy, Dark
}
```

### 2. 属性缓存系统

```csharp
public class CombatAttributeCache
{
    // 每帧只计算一次
    private void EnsureCache()
    {
        if (_isDirty || _lastUpdateFrame != Time.frameCount)
        {
            RecalculateAll();
            _isDirty = false;
            _lastUpdateFrame = Time.frameCount;
        }
    }
    
    // 分离存储
    Dictionary<DamageCategory, float> _damageTypeMultipliers;  // 伤害类型
    Dictionary<ElementType, float> _elementalDamageBonuses;    // 元素加成
    Dictionary<ElementType, float> _elementalResistances;      // 元素抗性
    Dictionary<BuffType, float> _temporaryBuffs;               // 临时Buff
}
```

### 3. 修饰器优先级

```
最终值 = (基础值 + Flat总和) × (1 + PercentAdd总和) × PercentMult乘积

示例：
基础攻击力 = 100
+20 Flat (装备)
+10% PercentAdd (Buff)
+15% PercentMult (前缀)

最终 = (100 + 20) × (1 + 0.1) × 1.15 = 151.8
```

---

## 属性系统

### 基础属性（6维）

| 属性 | 影响 |
|------|------|
| 体质(CON) | 生命值、物理格挡 |
| 力量(STR) | 物理攻击、物理技能点 |
| 感知(PER) | 命中、暴击率、暴击伤害 |
| 反应(REA) | 速度、闪避 |
| 智慧(WIS) | 魔法攻击、魔法防御、魔法技能点 |
| 幸运(LUK) | 暴击、闪避、掉落 |

### 派生公式

```csharp
MaxHealth = 100 + CON × 15
PhysicalAttack = STR × 2
MagicAttack = WIS × 2
PhysicalDefense = CON × 0.5
MagicDefense = WIS × 0.8
Speed = 50 + REA × 2
CriticalRate = 5% + PER × 0.3% + LUK × 0.2%
Accuracy = 90% + PER × 0.5%
Evasion = REA × 0.3% + LUK × 0.1%
```

---

## 伤害计算流程

```
1. 命中判定
   命中率 = 攻击方命中 - 防守方闪避 + 90%
   
2. 格挡判定（Physical/Magic独立）
   格挡减伤 = 50%
   
3. 基础伤害
   = 武器伤害 + 前缀加成
   
4. 攻击力加成
   Physical: +物攻×0.5
   Magic: +魔攻×0.5
   True: 无加成
   
5. 伤害类型倍率
   = 基础1.0 + 装备加成 + Buff加成
   
6. 元素倍率
   = 1.0 + 元素伤害加成
   
7. 暴击判定
   暴击率 = 角色暴击 + 武器暴击
   暴击倍率 = 角色暴伤 + 武器暴伤
   
8. 防御减免
   减免率 = 防御 / (防御 + 100 + 等级×5)
   上限80%
   
9. 元素抗性
   = 装备抗性 + Buff抗性
   范围: -100% ~ +90%
   
10. 最终伤害
    = Max(1, 计算结果)
```

---

## ATB战斗系统

### 状态流程

```
Idle → Preparing → Running ↔ ActionSelect → Executing
                      ↓              ↓
                   Victory        Defeat
```

### ATB增长

```csharp
ATBGain = Speed × deltaTime × 0.5

// 压力/疲劳惩罚
if (IsStressed) ATBGain *= 0.8f;
if (IsFatigued) ATBGain *= 0.9f;
```

### 与物品系统集成

```csharp
// 战斗中使用物品
public void UseItem(Character user, int itemId, Character target)
{
    // 通过库存管理器使用
    _inventoryManager.UseItemInCombat(itemId, user, target);
    
    // 消耗ATB（物品可能有不同ATB消耗）
    var item = ItemDatabase.Instance.GetItem(itemId);
    if (item is ICombatUsable usable)
    {
        user.ConsumeATB(usable.ATBCost);
    }
}
```

---

## 状态效果系统

### 效果类型

| 类型 | 说明 | 示例 |
|------|------|------|
| DOT | 持续伤害 | 中毒、燃烧、流血 |
| HOT | 持续治疗 | 再生 |
| Buff | 属性提升 | 强化、加速 |
| Debuff | 属性降低 | 虚弱、减速 |
| 控制 | 阻止行动 | 眩晕、冰冻、沉默 |

### DOT伤害计算

```csharp
// DOT也支持伤害类型×元素分离
StatusEffectData:
  - DamageCategory: Physical/Magic/True
  - Element: Fire/Ice/Poison...

// 火焰燃烧：Physical + Fire（受物防和火抗影响）
// 毒素DOT：True + Poison（无视防御，受毒抗影响）
```

### 效果叠加

```csharp
// 可叠加效果
if (IsStackable && CurrentStacks < MaxStacks)
{
    CurrentStacks++;
    // 属性修改器数值×层数
}

// 刷新持续时间
if (RefreshOnReapply)
{
    RemainingTime = Max(RemainingTime, NewDuration);
}
```

---

## 使用示例

### 初始化战斗

```csharp
// 设置队伍
var players = new List<Character> { player1, player2, player3 };
var enemies = new List<Character> { enemy1, enemy2 };

// 设置库存管理器
ATBCombatManager.Instance.SetInventoryManager(inventoryManager);

// 开始战斗
ATBCombatManager.Instance.StartCombat(players, enemies);

// 准备战斗物品后
ATBCombatManager.Instance.BeginBattle();
```

### 执行攻击

```csharp
// 普通攻击
ATBCombatManager.Instance.ExecuteNormalAttack(attacker, target);

// 技能攻击
var result = DamageCalculator.CalculateSkillDamage(
    attacker: player,
    defender: enemy,
    skillMultiplier: 1.5f,
    damageCategory: DamageCategory.Magic,
    element: ElementType.Fire,
    armorPenetration: 0.2f  // 20%穿透
);

enemy.TakeDamage(result.ToDamageInfo());
```

### 装备武器

```csharp
var weaponInstance = ironSword.CreateInstance();
weaponInstance.SetPrefix(legendaryPrefixId);  // 传说前缀

character.Equip(weaponInstance);

// 装备后属性自动更新
Debug.Log($"攻击力: {character.CombatCache.PhysicalAttack}");
Debug.Log($"武器伤害: {character.CombatCache.WeaponBaseDamage}");
```

### 状态效果

```csharp
// 施加中毒
character.AddStatusEffect(poisonEffect, source: attacker, duration: 10f);

// 检查状态
if (character.HasStatusEffect(StatusEffectType.Stunned))
{
    // 无法行动
}

// 移除状态
character.RemoveStatusEffect(StatusEffectType.Poisoned);
```

---

## 性能优化

1. **属性缓存**：`CombatAttributeCache` 每帧只计算一次
2. **脏标记**：只在属性变化时标记重算
3. **延迟计算**：属性访问时才触发计算
4. **事件驱动**：使用事件而非轮询检查

---

## 与物品系统的对应关系

| 物品系统类型 | 战斗系统对应 |
|-------------|-------------|
| `ICharacter` (接口) | `CombatSystem.Character` 实现 |
| `ICharacterStats` (接口) | `CharacterStatsAdapter` 适配 |
| `ICombatStats` (接口) | `CombatStatsAdapter` 适配 |
| `IStatusEffect` (接口) | `StatusEffectInstance` 实现 |
| `BuffEffect` | 直接使用 |

---

## 完整使用示例

```csharp
// 1. 初始化战斗
var players = new List<Character> { player1, player2, player3 };
var enemies = new List<Character> { enemy1, enemy2 };

ATBCombatManager.Instance.StartCombat(players, enemies);
ATBCombatManager.Instance.BeginBattle();

// 2. 检查技能可用目标
var skill = player1.SkillManager.GetEquippedSkill(0);
var targets = ATBCombatManager.Instance.GetValidTargetsForSkill(player1, skill);

// 3. 执行技能
ATBCombatManager.Instance.ExecuteSkill(player1, skill, targets);

// 4. 查询站位信息
var frontEnemy = ATBCombatManager.Instance.Formation.GetFrontCharacter(TeamSide.Enemy);
int myPosition = ATBCombatManager.Instance.Formation.GetCharacterPosition(player1, TeamSide.Player);
var adjacentAllies = ATBCombatManager.Instance.Formation.GetAdjacentCharacters(player1, TeamSide.Player);

// 5. 武器熟练度
int sharpLevel = player1.WeaponProficiency.GetProficiencyLevel(WeaponCategory.Sharp);
float damageBonus = player1.WeaponProficiency.GetDamageBonus(WeaponCategory.Sharp);
```

---

## 扩展建议

1. **AI系统**：实现更复杂的敌人行为树，考虑站位和技能选择
2. **连击系统**：添加连击计数和倍率
3. **弱点系统**：元素克制关系（火克冰等）
4. **装备套装**：套装效果影响技能
5. **天赋树**：角色专属天赋解锁技能
