// ============================================
// CombatStats.cs - 战斗属性系统
// 由基础属性计算得出的战斗相关属性
// ============================================
using System;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// 战斗属性
    /// 由多个基础属性共同影响计算得出
    /// </summary>
    [Serializable]
    public class CombatStats
    {
        [Header("生命与资源")]
        public int maxHealth;           // 生命值上限
        public int currentHealth;       // 当前生命值
        public int maxSkillPoints;      // 技能点上限
        public int currentSkillPoints;  // 当前技能点
        public int skillPointRegen = 1; // 每回合技能点恢复量

        [Header("攻击属性")]
        public int physicalAttack;      // 物理攻击力
        public int magicAttack;         // 魔法攻击力

        [Header("防御属性")]
        public int physicalDefense;     // 物理防御力
        public int magicDefense;        // 魔法防御力
        public float resistance;        // 抗性（状态效果抵抗）

        [Header("暴击属性")]
        public float critRate;          // 暴击率 (0-1)
        public float critDamage = 1.5f; // 暴击伤害倍率

        [Header("速度与行动")]
        public int speed;               // 速度（影响ATB填充）

        [Header("命中与闪避")]
        public float hitRate = 1.0f;    // 命中率 (0-1)
        public float dodgeRate;         // 闪避率 (0-1)

        [Header("格挡")]
        public float physicalBlockRate; // 物理格挡率 (0-1)
        public float magicBlockRate;    // 魔法格挡率 (0-1)
        public float blockReduction = 0.5f; // 格挡减伤比例

        [Header("技能点上限")]
        public int physicalSkillCap;    // 物理技能点上限
        public int magicSkillCap;       // 魔法技能点上限

        /// <summary>
        /// 从基础属性计算战斗属性
        /// </summary>
        public void CalculateFromBase(BaseAttributes baseAttr, int level = 1)
        {
            // 生命值 = 体质 * 10 + 等级 * 5
            maxHealth = baseAttr.constitution * 10 + level * 5;

            // 物理攻击 = 力量 * 2
            physicalAttack = baseAttr.strength * 2;

            // 魔法攻击 = 智慧 * 2
            magicAttack = baseAttr.wisdom * 2;

            // 物理防御 = 体质 + 力量 / 2
            physicalDefense = baseAttr.constitution + baseAttr.strength / 2;

            // 魔法防御 = 体质 + 感知 / 2
            magicDefense = baseAttr.constitution + baseAttr.perception / 2;

            // 抗性 = (体质 + 感知) / 100
            resistance = (baseAttr.constitution + baseAttr.perception) / 100f;

            // 暴击率 = (感知 + 智慧 + 幸运) / 300
            critRate = (baseAttr.perception + baseAttr.wisdom + baseAttr.luck) / 300f;
            critRate = Mathf.Clamp(critRate, 0f, 0.75f); // 上限75%

            // 速度 = 反应
            speed = baseAttr.reaction;

            // 命中率 = 0.9 + (感知 + 反应) / 200
            hitRate = 0.9f + (baseAttr.perception + baseAttr.reaction) / 200f;
            hitRate = Mathf.Clamp(hitRate, 0.5f, 1f);

            // 闪避率 = (反应 + 智慧 + 幸运) / 400
            dodgeRate = (baseAttr.reaction + baseAttr.wisdom + baseAttr.luck) / 400f;
            dodgeRate = Mathf.Clamp(dodgeRate, 0f, 0.5f); // 上限50%

            // 物理格挡率 = (力量 + 反应) / 200
            physicalBlockRate = (baseAttr.strength + baseAttr.reaction) / 200f;
            physicalBlockRate = Mathf.Clamp(physicalBlockRate, 0f, 0.4f);

            // 魔法格挡率 = (感知 + 反应) / 200
            magicBlockRate = (baseAttr.perception + baseAttr.reaction) / 200f;
            magicBlockRate = Mathf.Clamp(magicBlockRate, 0f, 0.4f);

            // 物理技能点上限 = (力量 + 感知) / 4
            physicalSkillCap = (baseAttr.strength + baseAttr.perception) / 4;

            // 魔法技能点上限 = (智慧 + 感知) / 4
            magicSkillCap = (baseAttr.wisdom + baseAttr.perception) / 4;

            // 总技能点上限
            maxSkillPoints = physicalSkillCap + magicSkillCap;
        }

        /// <summary>
        /// 应用装备加成
        /// </summary>
        public void ApplyEquipmentBonus(CombatStatModifiers modifiers)
        {
            // 固定值加成
            maxHealth += modifiers.flatMaxHealth;
            physicalAttack += modifiers.flatPhysicalAttack;
            magicAttack += modifiers.flatMagicAttack;
            physicalDefense += modifiers.flatPhysicalDefense;
            magicDefense += modifiers.flatMagicDefense;
            speed += modifiers.flatSpeed;

            // 百分比加成
            maxHealth = Mathf.RoundToInt(maxHealth * (1 + modifiers.percentMaxHealth));
            physicalAttack = Mathf.RoundToInt(physicalAttack * (1 + modifiers.percentPhysicalAttack));
            magicAttack = Mathf.RoundToInt(magicAttack * (1 + modifiers.percentMagicAttack));

            // 直接加成
            critRate += modifiers.flatCritRate;
            critDamage += modifiers.flatCritDamage;
            dodgeRate += modifiers.flatDodgeRate;
            hitRate += modifiers.flatHitRate;

            // 限制范围
            ClampValues();
        }

        /// <summary>
        /// 限制属性值范围
        /// </summary>
        public void ClampValues()
        {
            critRate = Mathf.Clamp(critRate, 0f, 0.95f);
            critDamage = Mathf.Max(critDamage, 1f);
            dodgeRate = Mathf.Clamp(dodgeRate, 0f, 0.75f);
            hitRate = Mathf.Clamp(hitRate, 0.1f, 1f);
            physicalBlockRate = Mathf.Clamp(physicalBlockRate, 0f, 0.6f);
            magicBlockRate = Mathf.Clamp(magicBlockRate, 0f, 0.6f);
            resistance = Mathf.Clamp(resistance, 0f, 0.9f);
        }

        /// <summary>
        /// 重置为满状态
        /// </summary>
        public void ResetToFull()
        {
            currentHealth = maxHealth;
            currentSkillPoints = maxSkillPoints;
        }

        /// <summary>
        /// 是否存活
        /// </summary>
        public bool IsAlive => currentHealth > 0;

        /// <summary>
        /// 生命百分比
        /// </summary>
        public float HealthPercent => maxHealth > 0 ? (float)currentHealth / maxHealth : 0;

        /// <summary>
        /// 技能点百分比
        /// </summary>
        public float SkillPointPercent => maxSkillPoints > 0 ? (float)currentSkillPoints / maxSkillPoints : 0;

        /// <summary>
        /// 回合开始时恢复技能点
        /// </summary>
        public void RegenerateSkillPoints()
        {
            currentSkillPoints = Mathf.Min(currentSkillPoints + skillPointRegen, maxSkillPoints);
        }

        /// <summary>
        /// 消耗技能点
        /// </summary>
        public bool ConsumeSkillPoints(int amount)
        {
            if (currentSkillPoints >= amount)
            {
                currentSkillPoints -= amount;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 受到伤害
        /// </summary>
        public int TakeDamage(int damage)
        {
            int actualDamage = Mathf.Min(damage, currentHealth);
            currentHealth -= actualDamage;
            return actualDamage;
        }

        /// <summary>
        /// 治疗
        /// </summary>
        public int Heal(int amount)
        {
            int actualHeal = Mathf.Min(amount, maxHealth - currentHealth);
            currentHealth += actualHeal;
            return actualHeal;
        }

        /// <summary>
        /// 复制
        /// </summary>
        public CombatStats Clone()
        {
            return (CombatStats)MemberwiseClone();
        }
    }

    /// <summary>
    /// 战斗属性修正器（装备/Buff提供）
    /// </summary>
    [Serializable]
    public class CombatStatModifiers
    {
        [Header("固定值加成")]
        public int flatMaxHealth;
        public int flatPhysicalAttack;
        public int flatMagicAttack;
        public int flatPhysicalDefense;
        public int flatMagicDefense;
        public int flatSpeed;
        public float flatCritRate;
        public float flatCritDamage;
        public float flatDodgeRate;
        public float flatHitRate;

        [Header("百分比加成")]
        public float percentMaxHealth;
        public float percentPhysicalAttack;
        public float percentMagicAttack;
        public float percentPhysicalDefense;
        public float percentMagicDefense;

        /// <summary>
        /// 合并修正器
        /// </summary>
        public static CombatStatModifiers Combine(params CombatStatModifiers[] modifiers)
        {
            var result = new CombatStatModifiers();
            foreach (var mod in modifiers)
            {
                if (mod == null) continue;
                result.flatMaxHealth += mod.flatMaxHealth;
                result.flatPhysicalAttack += mod.flatPhysicalAttack;
                result.flatMagicAttack += mod.flatMagicAttack;
                result.flatPhysicalDefense += mod.flatPhysicalDefense;
                result.flatMagicDefense += mod.flatMagicDefense;
                result.flatSpeed += mod.flatSpeed;
                result.flatCritRate += mod.flatCritRate;
                result.flatCritDamage += mod.flatCritDamage;
                result.flatDodgeRate += mod.flatDodgeRate;
                result.flatHitRate += mod.flatHitRate;
                result.percentMaxHealth += mod.percentMaxHealth;
                result.percentPhysicalAttack += mod.percentPhysicalAttack;
                result.percentMagicAttack += mod.percentMagicAttack;
                result.percentPhysicalDefense += mod.percentPhysicalDefense;
                result.percentMagicDefense += mod.percentMagicDefense;
            }
            return result;
        }
    }
}