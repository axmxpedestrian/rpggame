// ============================================
// GameDataID.cs - ID命名规范
// ============================================

/// <summary>
/// ID命名规范：
/// 
/// 角色：    HERO_[职业]_[编号]        例：HERO_WARRIOR_001
/// 敌人：    ENEMY_[种族]_[变体]_[编号] 例：ENEMY_GOBLIN_ELITE_001
/// 武器：    WEAPON_[类型]_[编号]       例：WEAPON_SWORD_001
/// 护甲：    ARMOR_[部位]_[编号]        例：ARMOR_HEAD_001
/// 饰品：    ACCESSORY_[类型]_[编号]    例：ACCESSORY_RING_001
/// 技能：    SKILL_[来源]_[编号]        例：SKILL_WARRIOR_001
/// 状态效果：STATUS_[类型]_[编号]       例：STATUS_BLEED_001
/// </summary>
public static class GameDataID
{
    // 前缀常量
    public const string HERO_PREFIX = "HERO";
    public const string ENEMY_PREFIX = "ENEMY";
    public const string WEAPON_PREFIX = "WEAPON";
    public const string ARMOR_PREFIX = "ARMOR";
    public const string ACCESSORY_PREFIX = "ACCESSORY";
    public const string SKILL_PREFIX = "SKILL";

    /// <summary>
    /// 生成英雄ID
    /// </summary>
    public static string GenerateHeroID(string className, int index)
    {
        return $"{HERO_PREFIX}_{className.ToUpper()}_{index:D3}";
    }

    /// <summary>
    /// 生成敌人ID
    /// </summary>
    public static string GenerateEnemyID(string race, string variant, int index)
    {
        if (string.IsNullOrEmpty(variant))
            return $"{ENEMY_PREFIX}_{race.ToUpper()}_{index:D3}";
        return $"{ENEMY_PREFIX}_{race.ToUpper()}_{variant.ToUpper()}_{index:D3}";
    }

    /// <summary>
    /// 生成武器ID
    /// </summary>
    public static string GenerateWeaponID(WeaponType type, int index)
    {
        return $"{WEAPON_PREFIX}_{type.ToString().ToUpper()}_{index:D3}";
    }
}