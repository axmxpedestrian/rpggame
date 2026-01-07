// ============================================
// EnemyData.cs - 敌人数据 ScriptableObject
// ============================================
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

/// <summary>
/// 敌人弱点
/// </summary>
[Serializable]
public class EnemyWeakness
{
    public ElementType elementType;

    [Tooltip("抗性系数（小于1为弱点，大于1为抗性）")]
    [Range(0f, 2f)]
    public float resistanceMultiplier = 1.2f; // 弱点默认受到120%伤害
}

/// <summary>
/// 敌人掉落物
/// </summary>
[Serializable]
public class EnemyDrop
{
    [Tooltip("掉落物品ID")]
    public string itemID;

    [Tooltip("掉落几率（0-1）")]
    [Range(0f, 1f)]
    public float dropChance = 0.1f;

    [Tooltip("最小数量")]
    public int minCount = 1;

    [Tooltip("最大数量")]
    public int maxCount = 1;
}

/// <summary>
/// 敌人数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Enemy", menuName = "Game Data/Enemy")]
public class EnemyData : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("敌人唯一ID")]
    public string enemyID;

    [Tooltip("显示名称")]
    public string displayName;

    [Tooltip("敌人描述")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("敌人图标")]
    public Sprite icon;

    [Header("分类")]
    [Tooltip("敌人种族")]
    public EnemyRace race;

    [Tooltip("敌人变体")]
    public EnemyVariant variant = EnemyVariant.Normal;

    [Header("等级范围")]
    [Tooltip("最低等级")]
    public int minLevel = 1;

    [Tooltip("最高等级")]
    public int maxLevel = 10;

    [Header("基础属性")]
    [Tooltip("基础属性")]
    public BaseStats baseStats = new BaseStats();

    [Tooltip("每级属性成长")]
    public BaseStats growthStats = new BaseStats();

    [Header("战斗属性")]
    [Tooltip("基础生命值")]
    public int baseHealth = 100;

    [Tooltip("每级生命成长")]
    public int healthGrowth = 10;

    [Tooltip("基础经验值")]
    public int baseExp = 10;

    [Tooltip("每级经验成长")]
    public int expGrowth = 5;

    [Header("弱点与抗性")]
    [Tooltip("弱点列表")]
    public List<EnemyWeakness> weaknesses = new List<EnemyWeakness>();

    [Tooltip("免疫的负面效果")]
    public List<DebuffType> debuffImmunities = new List<DebuffType>();

    [Header("装备")]
    [Tooltip("默认武器")]
    public WeaponData defaultWeapon;

    [Header("AI行为")]
    [Tooltip("AI行为ID")]
    public string aiBehaviorID;

    [Tooltip("技能ID列表")]
    public List<string> skillIDs = new List<string>();

    [Header("掉落")]
    [Tooltip("掉落物列表")]
    public List<EnemyDrop> drops = new List<EnemyDrop>();

    [Tooltip("金币掉落范围（最小）")]
    public int minGold = 0;

    [Tooltip("金币掉落范围（最大）")]
    public int maxGold = 10;

    [Header("挑战奖励")]
    [Tooltip("击杀奖励buff ID（击杀一定数量后触发）")]
    public string challengeBuffID;

    [Tooltip("触发挑战奖励需要的击杀数")]
    public int challengeKillCount = 10;

    /// <summary>
    /// 获取指定元素的抗性系数
    /// </summary>
    public float GetElementResistance(ElementType element)
    {
        foreach (var weakness in weaknesses)
        {
            if (weakness.elementType == element)
                return weakness.resistanceMultiplier;
        }
        return 1.0f; // 默认无抗性
    }

    /// <summary>
    /// 检查是否免疫指定负面效果
    /// </summary>
    public bool IsImmuneToDebuff(DebuffType debuff)
    {
        return debuffImmunities.Contains(debuff);
    }

    /// <summary>
    /// 计算指定等级的生命值
    /// </summary>
    public int CalculateHealth(int level)
    {
        return baseHealth + (level - minLevel) * healthGrowth;
    }

    /// <summary>
    /// 计算指定等级的经验值
    /// </summary>
    public int CalculateExp(int level)
    {
        return baseExp + (level - minLevel) * expGrowth;
    }

    /// <summary>
    /// 创建运行时敌人实例
    /// </summary>
    public Enemy CreateInstance(int level = -1)
    {
        return new Enemy(this, level);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(enemyID))
        {
            enemyID = name.ToLower().Replace(" ", "_");
        }

        if (maxLevel < minLevel) maxLevel = minLevel;
    }
#endif
}