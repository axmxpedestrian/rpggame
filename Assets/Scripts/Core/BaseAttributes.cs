// ============================================
// BaseAttributes.cs - 基础属性系统
// ============================================
using System;
using UnityEngine;

namespace Combat.Core
{
    /// <summary>
    /// 基础属性数据
    /// 角色升级自由加点的6种属性
    /// </summary>
    [Serializable]
    public class BaseAttributes
    {
        [Tooltip("体质 - 影响生命值、物理防御、魔法防御、抗性")]
        public int constitution = 10;

        [Tooltip("力量 - 影响物理攻击、物理防御、物理格挡、物理技能点")]
        public int strength = 10;

        [Tooltip("感知 - 影响魔法防御、抗性、暴击率、命中率、魔法格挡、技能点")]
        public int perception = 10;

        [Tooltip("反应 - 影响速度、命中率、闪避率、格挡率")]
        public int reaction = 10;

        [Tooltip("智慧 - 影响魔法攻击、暴击率、闪避率、魔法技能点")]
        public int wisdom = 10;

        [Tooltip("幸运 - 影响暴击率、闪避率、掉落率")]
        public int luck = 10;

        public BaseAttributes() { }

        public BaseAttributes(int con, int str, int per, int rea, int wis, int luc)
        {
            constitution = con;
            strength = str;
            perception = per;
            reaction = rea;
            wisdom = wis;
            luck = luc;
        }

        /// <summary>
        /// 复制属性
        /// </summary>
        public BaseAttributes Clone()
        {
            return new BaseAttributes(constitution, strength, perception, reaction, wisdom, luck);
        }

        /// <summary>
        /// 属性相加
        /// </summary>
        public static BaseAttributes operator +(BaseAttributes a, BaseAttributes b)
        {
            return new BaseAttributes(
                a.constitution + b.constitution,
                a.strength + b.strength,
                a.perception + b.perception,
                a.reaction + b.reaction,
                a.wisdom + b.wisdom,
                a.luck + b.luck
            );
        }

        /// <summary>
        /// 属性乘以等级成长
        /// </summary>
        public static BaseAttributes operator *(BaseAttributes a, int level)
        {
            return new BaseAttributes(
                a.constitution * level,
                a.strength * level,
                a.perception * level,
                a.reaction * level,
                a.wisdom * level,
                a.luck * level
            );
        }

        /// <summary>
        /// 获取指定属性值
        /// </summary>
        public int GetAttribute(BaseStatType type)
        {
            return type switch
            {
                BaseStatType.Constitution => constitution,
                BaseStatType.Strength => strength,
                BaseStatType.Perception => perception,
                BaseStatType.Reaction => reaction,
                BaseStatType.Wisdom => wisdom,
                BaseStatType.Luck => luck,
                _ => 0
            };
        }

        /// <summary>
        /// 设置指定属性值
        /// </summary>
        public void SetAttribute(BaseStatType type, int value)
        {
            switch (type)
            {
                case BaseStatType.Constitution: constitution = value; break;
                case BaseStatType.Strength: strength = value; break;
                case BaseStatType.Perception: perception = value; break;
                case BaseStatType.Reaction: reaction = value; break;
                case BaseStatType.Wisdom: wisdom = value; break;
                case BaseStatType.Luck: luck = value; break;
            }
        }

        /// <summary>
        /// 增加指定属性
        /// </summary>
        public void AddAttribute(BaseStatType type, int amount)
        {
            SetAttribute(type, GetAttribute(type) + amount);
        }

        /// <summary>
        /// 获取总属性点数
        /// </summary>
        public int GetTotalPoints()
        {
            return constitution + strength + perception + reaction + wisdom + luck;
        }

        public override string ToString()
        {
            return $"CON:{constitution} STR:{strength} PER:{perception} REA:{reaction} WIS:{wisdom} LUC:{luck}";
        }
    }

    /// <summary>
    /// 属性成长配置
    /// </summary>
    [Serializable]
    public class AttributeGrowth
    {
        public BaseAttributes baseStats = new BaseAttributes();
        public BaseAttributes growthPerLevel = new BaseAttributes(1, 1, 1, 1, 1, 1);

        /// <summary>
        /// 计算指定等级的属性
        /// </summary>
        public BaseAttributes CalculateAtLevel(int level)
        {
            return baseStats + (growthPerLevel * (level - 1));
        }
    }
}