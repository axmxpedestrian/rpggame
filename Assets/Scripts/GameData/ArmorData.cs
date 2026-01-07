// ============================================
// ArmorData.cs - 护甲数据 ScriptableObject
// ============================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 护甲数据定义
/// </summary>
[CreateAssetMenu(fileName = "New Armor", menuName = "Game Data/Equipment/Armor")]
public class ArmorData : EquipmentData
{
    [Header("护甲信息")]
    [Tooltip("护甲部位")]
    public ArmorSlot armorSlot;

    [Header("防御属性")]
    [Tooltip("基础物理防御")]
    public int basePhysicalDefense;

    [Tooltip("基础魔法防御")]
    public int baseMagicDefense;

    [Header("抗性")]
    [Tooltip("元素抗性列表")]
    public List<ElementResistance> elementResistances = new List<ElementResistance>();

    [Tooltip("负面效果抗性列表")]
    public List<DebuffResistance> debuffResistances = new List<DebuffResistance>();

    [Header("减伤")]
    [Tooltip("减伤系数（0-1，0.1表示减少10%伤害）")]
    [Range(0f, 0.5f)]
    public float damageReduction = 0f;

    [Header("套装")]
    [Tooltip("所属套装ID（可选）")]
    public string setID;

    /// <summary>
    /// 获取指定元素的抗性值
    /// </summary>
    public float GetElementResistance(ElementType element)
    {
        foreach (var res in elementResistances)
        {
            if (res.elementType == element)
                return res.resistanceValue;
        }
        return 0f;
    }

    /// <summary>
    /// 获取指定负面效果的抗性值
    /// </summary>
    public float GetDebuffResistance(DebuffType debuff)
    {
        foreach (var res in debuffResistances)
        {
            if (res.debuffType == debuff)
                return res.resistanceValue;
        }
        return 0f;
    }

    /// <summary>
    /// 创建运行时护甲实例
    /// </summary>
    public Armor CreateInstance()
    {
        return new Armor(this);
    }

    public override string GenerateTooltip()
    {
        var sb = new System.Text.StringBuilder();
        sb.Append(base.GenerateTooltip());

        sb.AppendLine();
        sb.AppendLine($"<b>部位:</b> {GetSlotName()}");
        sb.AppendLine($"<b>物理防御:</b> +{basePhysicalDefense}");
        sb.AppendLine($"<b>魔法防御:</b> +{baseMagicDefense}");

        if (damageReduction > 0)
        {
            sb.AppendLine($"<b>减伤:</b> {damageReduction * 100:F0}%");
        }

        if (elementResistances.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>元素抗性:</b>");
            foreach (var res in elementResistances)
            {
                sb.AppendLine($"  {GetElementName(res.elementType)}: {res.resistanceValue * 100:F0}%");
            }
        }

        if (debuffResistances.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("<b>异常抗性:</b>");
            foreach (var res in debuffResistances)
            {
                sb.AppendLine($"  {res.debuffType}: {res.resistanceValue * 100:F0}%");
            }
        }

        if (!string.IsNullOrEmpty(setID))
        {
            sb.AppendLine();
            sb.AppendLine($"<color=green>套装: {setID}</color>");
        }

        return sb.ToString();
    }

    private string GetSlotName()
    {
        return armorSlot switch
        {
            ArmorSlot.Head => "头部",
            ArmorSlot.Body => "身体",
            ArmorSlot.Legs => "腿部",
            _ => armorSlot.ToString()
        };
    }

    private string GetElementName(ElementType element)
    {
        return element switch
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

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        itemType = ItemType.Armor;
        isStackable = false;
        maxStackSize = 1;
    }
#endif
}

/// <summary>
/// 元素抗性
/// </summary>
[System.Serializable]
public class ElementResistance
{
    public ElementType elementType;

    [Range(-1f, 1f)]
    [Tooltip("抗性值（正数减少伤害，负数增加伤害）")]
    public float resistanceValue;
}

/// <summary>
/// 负面效果抗性
/// </summary>
[System.Serializable]
public class DebuffResistance
{
    public DebuffType debuffType;

    [Range(0f, 1f)]
    [Tooltip("抗性值（降低触发几率）")]
    public float resistanceValue;
}