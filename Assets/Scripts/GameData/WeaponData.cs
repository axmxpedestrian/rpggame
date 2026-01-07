// ============================================
// WeaponData.cs - 武器数据 ScriptableObject
// ============================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 武器数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Weapon", menuName = "Game Data/Equipment/Weapon")]
public class WeaponData : EquipmentData
{
    [Header("武器信息")]
    [Tooltip("武器类型")]
    public WeaponType weaponType;

    [Header("基础伤害")]
    [Tooltip("基础伤害值")]
    public int baseDamage;

    [Tooltip("伤害类别")]
    public DamageCategory damageCategory = DamageCategory.Physical;

    [Tooltip("元素类型")]
    public ElementType elementType = ElementType.None;

    [Header("攻击属性")]
    [Tooltip("攻击速度（1.0为标准）")]
    public float attackSpeed = 1f;

    [Tooltip("攻击范围")]
    public TargetRangeType attackRange = TargetRangeType.Front;

    [Tooltip("可攻击的位置（1-5）")]
    public List<int> targetablePositions = new List<int> { 1, 2 };

    [Header("增伤系数")]
    [Tooltip("增伤系数（1.0为无加成）")]
    public float damageMultiplier = 1.0f;

    [Header("附加效果")]
    [Tooltip("攻击附带的负面效果")]
    public List<WeaponDebuffEffect> debuffEffects = new List<WeaponDebuffEffect>();

    [Header("技能")]
    [Tooltip("武器特殊技能ID列表")]
    public List<string> weaponSkillIDs = new List<string>();

    [Header("熟练度")]
    [Tooltip("初始熟练度经验需求")]
    public int baseProficiencyExp = 100;

    /// <summary>
    /// 获取武器类型标志
    /// </summary>
    public WeaponTypeFlag GetWeaponTypeFlag()
    {
        return weaponType switch
        {
            WeaponType.Sharp => WeaponTypeFlag.Sharp,
            WeaponType.Blunt => WeaponTypeFlag.Blunt,
            WeaponType.Staff => WeaponTypeFlag.Staff,
            WeaponType.Bow => WeaponTypeFlag.Bow,
            WeaponType.Crossbow => WeaponTypeFlag.Crossbow,
            WeaponType.Gun => WeaponTypeFlag.Gun,
            WeaponType.Explosive => WeaponTypeFlag.Explosive,
            WeaponType.Throwing => WeaponTypeFlag.Throwing,
            WeaponType.Polearm => WeaponTypeFlag.Polearm,
            _ => WeaponTypeFlag.None
        };
    }

    /// <summary>
    /// 检查是否可以攻击指定位置
    /// </summary>
    public bool CanTargetPosition(int position)
    {
        return targetablePositions.Contains(position);
    }

    /// <summary>
    /// 创建运行时武器实例
    /// </summary>
    public Weapon CreateInstance()
    {
        return new Weapon(this);
    }

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(base.GenerateTooltip());

        sb.AppendLine();
        sb.AppendLine($"<b>武器类型:</b> {GetWeaponTypeName()}");
        sb.AppendLine($"<b>基础伤害:</b> {baseDamage}");
        sb.AppendLine($"<b>伤害类型:</b> {GetDamageCategoryName()}");

        if (elementType != ElementType.None)
        {
            sb.AppendLine($"<b>元素:</b> {GetElementName()}");
        }

        sb.AppendLine($"<b>攻击速度:</b> {attackSpeed:F2}");
        sb.AppendLine($"<b>攻击范围:</b> {GetRangeName()}");

        if (damageMultiplier != 1.0f)
        {
            sb.AppendLine($"<b>增伤系数:</b> {damageMultiplier * 100:F0}%");
        }

        if (debuffEffects.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>附加效果:</b>");
            foreach (var effect in debuffEffects)
            {
                sb.AppendLine($"  {effect.triggerChance * 100:F0}% {effect.debuffType}");
            }
        }

        return sb.ToString();
    }

    private string GetWeaponTypeName()
    {
        return weaponType switch
        {
            WeaponType.Blunt => "钝器",
            WeaponType.Sharp => "锐器",
            WeaponType.Bow => "弓",
            WeaponType.Crossbow => "弩",
            WeaponType.Gun => "枪",
            WeaponType.Explosive => "炸药",
            WeaponType.Staff => "法杖",
            WeaponType.Polearm => "长柄",
            WeaponType.Throwing => "投掷",
            _ => weaponType.ToString()
        };
    }

    private string GetDamageCategoryName()
    {
        return damageCategory switch
        {
            DamageCategory.Physical => "物理",
            DamageCategory.Magic => "魔法",
            DamageCategory.True => "真实",
            _ => damageCategory.ToString()
        };
    }

    private string GetElementName()
    {
        return elementType switch
        {
            ElementType.Fire => "火焰",
            ElementType.Ice => "冰霜",
            ElementType.Lightning => "雷电",
            ElementType.Poison => "毒素",
            ElementType.Holy => "神圣",
            ElementType.Dark => "黑暗",
            _ => "无"
        };
    }

    private string GetRangeName()
    {
        return attackRange switch
        {
            TargetRangeType.Front => "前排",
            TargetRangeType.Back => "后排",
            TargetRangeType.All => "全体",
            TargetRangeType.Single => "单体",
            TargetRangeType.Adjacent => "相邻",
            _ => attackRange.ToString()
        };
    }

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Weapon;
        isStackable = false;
        maxStackSize = 1;

        // 根据武器类型设置默认攻击范围
        if (targetablePositions.Count == 0)
        {
            targetablePositions = GetDefaultTargetPositions();
        }
    }

    private List<int> GetDefaultTargetPositions()
    {
        return weaponType switch
        {
            WeaponType.Sharp => new List<int> { 1, 2 },         // 前排
            WeaponType.Blunt => new List<int> { 1, 2 },         // 前排
            WeaponType.Bow => new List<int> { 3, 4, 5 },        // 后排
            WeaponType.Crossbow => new List<int> { 2, 3, 4 },   // 中后排
            WeaponType.Gun => new List<int> { 1, 2, 3, 4, 5 },  // 全体
            WeaponType.Staff => new List<int> { 1, 2, 3, 4, 5 },// 全体
            WeaponType.Polearm => new List<int> { 1, 2, 3 },    // 前中排
            _ => new List<int> { 1, 2 }
        };
    }
#endif
}

/// <summary>
/// 武器附带的负面效果
/// </summary>
[System.Serializable]
public class WeaponDebuffEffect
{
    public DebuffType debuffType;

    [Range(0f, 1f)]
    public float triggerChance = 0.1f;

    public float value;
    public float duration;
}