// ============================================
// AttachmentSystemExample.cs - 配件系统使用示例
// ============================================
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 配件系统使用示例
/// 展示如何在游戏中使用配件系统
/// </summary>
public class AttachmentSystemExample : MonoBehaviour
{
    [Header("测试数据")]
    public WeaponData testWeapon;
    public AttachmentData testAttachment;

    [Header("运行时测试")]
    [SerializeField] private List<AttachmentData> inventory = new List<AttachmentData>();

    private void Start()
    {
        if (testWeapon != null)
        {
            // 初始化武器的配件槽位
            testWeapon.Initialize();

            Debug.Log($"武器: {testWeapon.displayName}");
            Debug.Log($"品质: {testWeapon.rarity}");
            Debug.Log($"最大槽位: {testWeapon.attachmentSlots.Slots.Count}");
            Debug.Log($"已解锁槽位: {testWeapon.attachmentSlots.UnlockedSlotCount}");
        }
    }

    /// <summary>
    /// 尝试镶嵌配件到武器
    /// </summary>
    public bool TrySocketAttachment(WeaponData weapon, AttachmentData attachment)
    {
        if (weapon == null || attachment == null)
        {
            Debug.LogWarning("武器或配件为空");
            return false;
        }

        // 检查是否可以镶嵌
        if (!weapon.CanSocketAttachment(attachment))
        {
            Debug.LogWarning($"无法将 {attachment.displayName} 镶嵌到 {weapon.displayName}");
            return false;
        }

        // 执行镶嵌
        bool success = weapon.attachmentSlots.SocketAttachment(attachment);

        if (success)
        {
            Debug.Log($"成功将 {attachment.displayName} 镶嵌到 {weapon.displayName}");

            // 从背包移除
            inventory.Remove(attachment);
        }

        return success;
    }

    /// <summary>
    /// 从武器上移除配件
    /// </summary>
    public AttachmentData RemoveAttachment(WeaponData weapon, int slotIndex)
    {
        if (weapon == null) return null;

        var removed = weapon.attachmentSlots.RemoveAttachment(slotIndex);

        if (removed != null)
        {
            Debug.Log($"从 {weapon.displayName} 移除了 {removed.displayName}");

            // 添加到背包
            inventory.Add(removed);
        }

        return removed;
    }

    /// <summary>
    /// 解锁武器的下一个配件槽位
    /// </summary>
    public bool UnlockNextSlot(WeaponData weapon, int playerGold)
    {
        if (weapon == null) return false;

        int cost = weapon.attachmentSlots.GetUnlockCost();

        if (playerGold < cost)
        {
            Debug.LogWarning($"金币不足，需要 {cost} 金币");
            return false;
        }

        bool success = weapon.attachmentSlots.UnlockNextSlot();

        if (success)
        {
            Debug.Log($"解锁了新的配件槽位，花费 {cost} 金币");
            // playerGold -= cost; // 实际项目中扣除金币
        }

        return success;
    }

    /// <summary>
    /// 计算武器的最终属性（包含配件加成）
    /// </summary>
    public void PrintWeaponStats(WeaponData weapon)
    {
        if (weapon == null) return;

        Debug.Log("========== 武器属性 ==========");
        Debug.Log($"名称: {weapon.displayName}");
        Debug.Log($"基础伤害: {weapon.baseDamage}");
        Debug.Log($"最终物理攻击: {weapon.GetFinalPhysicalAttack()}");
        Debug.Log($"最终魔法攻击: {weapon.GetFinalMagicAttack()}");
        Debug.Log($"最终暴击率: {weapon.GetFinalCritRate() * 100:F1}%");

        // 打印所有配件效果
        var bonuses = weapon.attachmentSlots.CalculateTotalBonuses();
        if (bonuses.Count > 0)
        {
            Debug.Log("--- 配件加成 ---");
            foreach (var bonus in bonuses)
            {
                Debug.Log($"  {bonus.Key}: +{bonus.Value}");
            }
        }

        // 打印所有负面效果
        var debuffs = weapon.attachmentSlots.GetAllDebuffs();
        if (debuffs.Count > 0)
        {
            Debug.Log("--- 攻击附带效果 ---");
            foreach (var debuff in debuffs)
            {
                Debug.Log($"  {debuff.GenerateDescription()}");
            }
        }
    }

    /// <summary>
    /// 攻击时触发配件的负面效果
    /// </summary>
    public void OnAttackHit(WeaponData weapon, GameObject target)
    {
        if (weapon == null || target == null) return;

        var debuffs = weapon.attachmentSlots.GetAllDebuffs();

        foreach (var debuff in debuffs)
        {
            // 根据触发几率判断是否触发
            if (Random.value <= debuff.triggerChance)
            {
                ApplyDebuffToTarget(target, debuff);
            }
        }
    }

    /// <summary>
    /// 将负面效果应用到目标
    /// </summary>
    private void ApplyDebuffToTarget(GameObject target, AttachmentEffect debuff)
    {
        Debug.Log($"对 {target.name} 施加了 {debuff.effectType} 效果");

        // 这里根据实际项目实现具体的效果逻辑
        // 例如：
        // var statusManager = target.GetComponent<StatusEffectManager>();
        // statusManager?.AddEffect(debuff.effectType, debuff.value, debuff.duration);

        switch (debuff.effectType)
        {
            case AttachmentEffectType.Bleeding:
                // 添加流血效果
                break;
            case AttachmentEffectType.Burning:
                // 添加燃烧效果
                break;
            case AttachmentEffectType.Poisoned:
                // 添加中毒效果
                break;
            case AttachmentEffectType.Stunned:
                // 添加眩晕效果
                break;
                // ... 其他效果
        }
    }

#if UNITY_EDITOR
    [ContextMenu("测试镶嵌配件")]
    private void TestSocketAttachment()
    {
        if (testWeapon != null && testAttachment != null)
        {
            TrySocketAttachment(testWeapon, testAttachment);
            PrintWeaponStats(testWeapon);
        }
    }

    [ContextMenu("打印武器属性")]
    private void TestPrintStats()
    {
        PrintWeaponStats(testWeapon);
    }

    [ContextMenu("解锁下一个槽位")]
    private void TestUnlockSlot()
    {
        UnlockNextSlot(testWeapon, 999999);
    }
#endif
}