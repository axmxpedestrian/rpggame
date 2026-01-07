// ============================================
// ItemInstances.cs - 运行时物品实例类
// ============================================
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Progress;

#region 物品实例基类

/// <summary>
/// 运行时物品实例基类
/// </summary>
[Serializable]
public abstract class ItemInstance
{
    public string itemID;
    public int count;

    public abstract ItemData GetData();
    public abstract ItemType GetItemType();

    public virtual bool CanStack => GetData()?.isStackable ?? false;
    public virtual int MaxStack => GetData()?.maxStackSize ?? 1;
}

#endregion

#region 装备实例

/// <summary>
/// 配件槽位
/// </summary>
[Serializable]
public class AttachmentSlot
{
    public int slotIndex;
    public bool isUnlocked;
    public AttachmentData currentAttachment;

    public bool IsEmpty => currentAttachment == null;

    public bool CanSocket(AttachmentData attachment)
    {
        return isUnlocked && IsEmpty && attachment != null;
    }
}

/// <summary>
/// 武器运行时实例
/// </summary>
[Serializable]
public class Weapon : ItemInstance
{
    public WeaponData data;
    public int enhanceLevel;
    public int currentDurability;
    public int maxDurability = 100;
    public bool isEquipped;
    public List<AttachmentSlot> attachmentSlots = new List<AttachmentSlot>();

    public Weapon(WeaponData weaponData)
    {
        data = weaponData;
        itemID = data.itemID;
        count = 1;
        enhanceLevel = 0;
        currentDurability = maxDurability;
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        attachmentSlots.Clear();
        int slotCount = data.attachmentSlotCount;
        int unlockedCount = data.rarity switch
        {
            ItemRarity.Common => 0,
            ItemRarity.Uncommon => 1,
            ItemRarity.Rare => 1,
            ItemRarity.Epic => 2,
            ItemRarity.Legendary => 3,
            ItemRarity.Mythic => slotCount,
            _ => 0
        };

        for (int i = 0; i < slotCount; i++)
        {
            attachmentSlots.Add(new AttachmentSlot
            {
                slotIndex = i,
                isUnlocked = i < unlockedCount
            });
        }
    }

    public override ItemData GetData() => data;
    public override ItemType GetItemType() => ItemType.Weapon;

    public int GetFinalPhysicalAttack()
    {
        float baseValue = data.baseDamage + data.GetStatBonus(CombatStatType.PhysicalAttack);
        float enhanceBonus = baseValue * (enhanceLevel * 0.05f);
        float attachmentBonus = GetAttachmentBonus(AttachmentEffectType.PhysicalDamageBonus);
        return Mathf.RoundToInt(baseValue + enhanceBonus + attachmentBonus);
    }

    public int GetFinalMagicAttack()
    {
        float baseValue = data.GetStatBonus(CombatStatType.MagicAttack);
        float enhanceBonus = baseValue * (enhanceLevel * 0.05f);
        float attachmentBonus = GetAttachmentBonus(AttachmentEffectType.MagicDamageBonus);
        return Mathf.RoundToInt(baseValue + enhanceBonus + attachmentBonus);
    }

    private float GetAttachmentBonus(AttachmentEffectType effectType)
    {
        float total = 0;
        foreach (var slot in attachmentSlots)
        {
            if (slot.currentAttachment != null)
            {
                foreach (var effect in slot.currentAttachment.effects)
                {
                    if (effect.effectType == effectType)
                        total += effect.value;
                }
            }
        }
        return total;
    }

    public bool SocketAttachment(AttachmentData attachment)
    {
        if (!attachment.IsCompatibleWith(data.GetWeaponTypeFlag()))
            return false;

        foreach (var slot in attachmentSlots)
        {
            if (slot.CanSocket(attachment))
            {
                slot.currentAttachment = attachment;
                return true;
            }
        }
        return false;
    }
}

/// <summary>
/// 护甲运行时实例
/// </summary>
[Serializable]
public class Armor : ItemInstance
{
    public ArmorData data;
    public int enhanceLevel;
    public int currentDurability;
    public int maxDurability = 100;
    public bool isEquipped;
    public List<AttachmentSlot> attachmentSlots = new List<AttachmentSlot>();

    public Armor(ArmorData armorData)
    {
        data = armorData;
        itemID = data.itemID;
        count = 1;
        enhanceLevel = 0;
        currentDurability = maxDurability;
    }

    public override ItemData GetData() => data;
    public override ItemType GetItemType() => ItemType.Armor;

    public int GetFinalPhysicalDefense()
    {
        float baseValue = data.basePhysicalDefense;
        float enhanceBonus = baseValue * (enhanceLevel * 0.05f);
        return Mathf.RoundToInt(baseValue + enhanceBonus);
    }

    public int GetFinalMagicDefense()
    {
        float baseValue = data.baseMagicDefense;
        float enhanceBonus = baseValue * (enhanceLevel * 0.05f);
        return Mathf.RoundToInt(baseValue + enhanceBonus);
    }
}

/// <summary>
/// 饰品运行时实例
/// </summary>
[Serializable]
public class Accessory : ItemInstance
{
    public AccessoryData data;
    public bool isEquipped;

    public Accessory(AccessoryData accessoryData)
    {
        data = accessoryData;
        itemID = data.itemID;
        count = 1;
    }

    public override ItemData GetData() => data;
    public override ItemType GetItemType() => ItemType.Accessory;
}

#endregion

#region 消耗品/工具/材料实例

/// <summary>
/// 消耗品运行时实例
/// </summary>
[Serializable]
public class ConsumableItem : ItemInstance
{
    public ConsumableData data;
    public int usesThisBattle;

    public ConsumableItem(ConsumableData consumableData, int amount = 1)
    {
        data = consumableData;
        itemID = data.itemID;
        count = amount;
        usesThisBattle = 0;
    }

    public override ItemData GetData() => data;
    public override ItemType GetItemType() => ItemType.Consumable;

    public bool CanUseInBattle()
    {
        if (!data.usableInBattle) return false;
        if (data.maxUsesPerBattle > 0 && usesThisBattle >= data.maxUsesPerBattle) return false;
        return count > 0;
    }

    public void Use()
    {
        count--;
        usesThisBattle++;
    }

    public void ResetBattleUses()
    {
        usesThisBattle = 0;
    }
}

/// <summary>
/// 工具运行时实例
/// </summary>
[Serializable]
public class ToolItem : ItemInstance
{
    public ToolData data;
    public int remainingUses;

    public ToolItem(ToolData toolData, int amount = 1)
    {
        data = toolData;
        itemID = data.itemID;
        count = amount;
        remainingUses = data.maxUses;
    }

    public override ItemData GetData() => data;
    public override ItemType GetItemType() => ItemType.Tool;
}

/// <summary>
/// 材料运行时实例
/// </summary>
[Serializable]
public class MaterialItem : ItemInstance
{
    public MaterialData data;

    public MaterialItem(MaterialData materialData, int amount = 1)
    {
        data = materialData;
        itemID = data.itemID;
        count = amount;
    }

    public override ItemData GetData() => data;
    public override ItemType GetItemType() => ItemType.Material;
}

#endregion

#region 角色/敌人实例

/// <summary>
/// 角色运行时实例
/// </summary>
[Serializable]
public class Character
{
    public CharacterData data;
    public int level;
    public int currentExp;
    public int currentHealth;
    public int maxHealth;
    public int currentSkillPoints;
    public int maxSkillPoints;

    // 当前属性点分配
    public BaseStats allocatedStats = new BaseStats();

    // 装备
    public Weapon equippedWeapon;
    public Armor equippedHelmet;
    public Armor equippedBody;
    public Armor equippedLegs;
    public Accessory[] equippedAccessories = new Accessory[4];

    // 武器熟练度
    public List<WeaponProficiency> weaponProficiencies = new List<WeaponProficiency>();

    // 专业技能
    public List<ProfessionSkill> professionSkills = new List<ProfessionSkill>();

    // 战斗状态
    public int position;
    public bool isAlive = true;
    public float atbValue;

    public Character(CharacterData characterData, int startLevel = 1)
    {
        data = characterData;
        level = startLevel;
        currentExp = 0;

        // 复制初始熟练度和技能
        weaponProficiencies = new List<WeaponProficiency>(data.weaponProficiencies);
        professionSkills = new List<ProfessionSkill>(data.professionSkills);

        // 计算初始属性
        CalculateStats();
        currentHealth = maxHealth;
        currentSkillPoints = maxSkillPoints;

        // 装备默认装备
        if (data.defaultWeapon != null)
            equippedWeapon = data.defaultWeapon.CreateInstance();
    }

    public void CalculateStats()
    {
        // 基础属性 + 等级成长 + 分配点数
        var totalStats = new BaseStats
        {
            constitution = data.baseStats.constitution + (level - 1) * data.growthStats.constitution + allocatedStats.constitution,
            strength = data.baseStats.strength + (level - 1) * data.growthStats.strength + allocatedStats.strength,
            perception = data.baseStats.perception + (level - 1) * data.growthStats.perception + allocatedStats.perception,
            reaction = data.baseStats.reaction + (level - 1) * data.growthStats.reaction + allocatedStats.reaction,
            wisdom = data.baseStats.wisdom + (level - 1) * data.growthStats.wisdom + allocatedStats.wisdom,
            luck = data.baseStats.luck + (level - 1) * data.growthStats.luck + allocatedStats.luck
        };

        // 计算战斗属性
        maxHealth = totalStats.constitution * 10 + level * 5;
        maxSkillPoints = (totalStats.strength + totalStats.wisdom) / 2;
    }

    public int GetSpeed()
    {
        return data.baseStats.reaction + (level - 1) * data.growthStats.reaction + allocatedStats.reaction;
    }
}

/// <summary>
/// 敌人运行时实例
/// </summary>
[Serializable]
public class Enemy
{
    public EnemyData data;
    public int level;
    public int currentHealth;
    public int maxHealth;
    public Weapon equippedWeapon;

    // 战斗状态
    public int position;
    public bool isAlive = true;
    public float atbValue;

    public Enemy(EnemyData enemyData, int spawnLevel = -1)
    {
        data = enemyData;

        if (spawnLevel < 0)
            level = UnityEngine.Random.Range(data.minLevel, data.maxLevel + 1);
        else
            level = Mathf.Clamp(spawnLevel, data.minLevel, data.maxLevel);

        maxHealth = data.CalculateHealth(level);
        currentHealth = maxHealth;

        if (data.defaultWeapon != null)
            equippedWeapon = data.defaultWeapon.CreateInstance();
    }

    public int GetExpReward()
    {
        return data.CalculateExp(level);
    }

    public int GetSpeed()
    {
        return data.baseStats.reaction + (level - data.minLevel) * data.growthStats.reaction;
    }

    public void TakeDamage(int damage)
    {
        currentHealth = Mathf.Max(0, currentHealth - damage);
        if (currentHealth <= 0)
            isAlive = false;
    }
}

#endregion