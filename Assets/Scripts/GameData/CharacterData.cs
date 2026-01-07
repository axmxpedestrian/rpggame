// ============================================
// CharacterData.cs - 角色数据 ScriptableObject
// ============================================
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色基础属性
/// </summary>
[Serializable]
public class BaseStats
{
    [Tooltip("体质")]
    public int constitution = 10;

    [Tooltip("力量")]
    public int strength = 10;

    [Tooltip("感知")]
    public int perception = 10;

    [Tooltip("反应")]
    public int reaction = 10;

    [Tooltip("智慧")]
    public int wisdom = 10;

    [Tooltip("幸运")]
    public int luck = 10;

    public int GetStat(BaseStatType type)
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
}

/// <summary>
/// 武器熟练度
/// </summary>
[Serializable]
public class WeaponProficiency
{
    public WeaponType weaponType;
    public int level = 1;
    public int currentExp = 0;

    /// <summary>
    /// 获取熟练度加成
    /// </summary>
    public float GetDamageBonus()
    {
        return 1.0f + (level - 1) * 0.05f; // 每级+5%伤害
    }

    public float GetCritBonus()
    {
        return (level - 1) * 0.01f; // 每级+1%暴击率
    }
}

/// <summary>
/// 专业技能
/// </summary>
[Serializable]
public class ProfessionSkill
{
    public ProfessionType professionType;
    public int level = 1;
    public int currentExp = 0;

    public int GetExpToNextLevel()
    {
        return level * 100; // 简单公式，可自定义
    }
}

/// <summary>
/// 角色数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Character", menuName = "Game Data/Character")]
public class CharacterData : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("角色唯一ID")]
    public string characterID;

    [Tooltip("显示名称")]
    public string displayName;

    [Tooltip("角色描述")]
    [TextArea(2, 4)]
    public string description;

    [Tooltip("角色头像")]
    public Sprite portrait;

    [Tooltip("角色立绘")]
    public Sprite fullBodyImage;

    [Header("基础属性")]
    [Tooltip("初始基础属性")]
    public BaseStats baseStats = new BaseStats();

    [Tooltip("每级属性成长")]
    public BaseStats growthStats = new BaseStats();

    [Header("武器熟练度")]
    [Tooltip("初始武器熟练度")]
    public List<WeaponProficiency> weaponProficiencies = new List<WeaponProficiency>();

    [Header("专业技能")]
    [Tooltip("专业技能列表")]
    public List<ProfessionSkill> professionSkills = new List<ProfessionSkill>();

    [Header("默认装备")]
    [Tooltip("初始武器")]
    public WeaponData defaultWeapon;

    [Tooltip("初始护甲（头）")]
    public ArmorData defaultHelmet;

    [Tooltip("初始护甲（身）")]
    public ArmorData defaultBodyArmor;

    [Tooltip("初始护甲（腿）")]
    public ArmorData defaultLegs;

    [Header("技能")]
    [Tooltip("角色固有技能ID列表")]
    public List<string> innateSkillIDs = new List<string>();

    [Header("角色特性")]
    [Tooltip("角色标签（用于特殊效果判定）")]
    public List<string> tags = new List<string>();

    /// <summary>
    /// 获取指定武器的熟练度等级
    /// </summary>
    public int GetWeaponProficiencyLevel(WeaponType weaponType)
    {
        foreach (var prof in weaponProficiencies)
        {
            if (prof.weaponType == weaponType)
                return prof.level;
        }
        return 1;
    }

    /// <summary>
    /// 获取指定专业技能等级
    /// </summary>
    public int GetProfessionLevel(ProfessionType profession)
    {
        foreach (var skill in professionSkills)
        {
            if (skill.professionType == profession)
                return skill.level;
        }
        return 0;
    }

    /// <summary>
    /// 创建运行时角色实例
    /// </summary>
    public Character CreateInstance(int level = 1)
    {
        return new Character(this, level);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrEmpty(characterID))
        {
            characterID = name.ToLower().Replace(" ", "_");
        }
    }
#endif
}