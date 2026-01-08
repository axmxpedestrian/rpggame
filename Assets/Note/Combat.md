# Unity2D ATB战斗系统

## 概述

本系统为横版回合制RPG设计，采用ATB（Active Time Battle）动态时间战斗模式，与物品系统完全兼容。

**核心特性：**
- 伤害类型(Physical/Magic/True)与元素(Fire/Ice等)分离
- 高频属性访问缓存机制
- 修饰器系统支持装备/Buff叠加
- 双库存战斗集成

## 文件结构

```
CombatSystem/
├── CharacterStats.cs         # 属性系统（6维基础+派生）
├── Character.cs              # 角色类（装备/状态/ATB）
├── CombatAttributeCache.cs   # 战斗属性缓存
├── DamageCalculator.cs       # 伤害计算器
├── ATBCombatManager.cs       # ATB战斗管理器
└── StatusEffectSystem.cs     # 状态效果系统
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
| `ItemSystem.Character` (占位) | `CombatSystem.Character` |
| `ItemSystem.CharacterStats` (占位) | `CombatSystem.CharacterStats` |
| `ItemSystem.CombatStats` (占位) | `CombatSystem.CombatAttributeCache` |
| `ItemSystem.StatusEffect` (抽象) | `CombatSystem.StatusEffectInstance` |
| `ItemSystem.BuffEffect` | `CombatSystem.BuffEffect` |

---

## 扩展建议

1. **技能系统**：创建 `SkillData` ScriptableObject
2. **AI系统**：实现更复杂的敌人行为树
3. **连击系统**：添加连击计数和倍率
4. **弱点系统**：元素克制关系
5. **位置系统**：前排/后排影响攻击范围
