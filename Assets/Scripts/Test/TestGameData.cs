// ============================================
// 使用示例
// ============================================
using UnityEngine;

public class GameExample : MonoBehaviour
{
    void Start()
    {
        // 获取数据管理器
        var dataManager = GameDataManager.Instance;

        // 创建玩家角色
        var warrior = dataManager.CreateCharacter("HERO_WARRIOR_001", 5);
        var mage = dataManager.CreateCharacter("HERO_MAGE_001", 5);

        // 创建敌人
        var goblin = dataManager.CreateEnemy("ENEMY_GOBLIN_001", 3);
        var boss = dataManager.CreateEnemy("ENEMY_GOBLIN_BOSS_001", 10);

        // 获取随机敌人
        var randomEnemy = dataManager.GetRandomEnemyForLevel(5);

        // 创建装备
        var sword = dataManager.CreateWeapon("WEAPON_SWORD_001");
        warrior.Equip(sword);

        // 获取特定类型的武器列表
        var allBows = dataManager.GetWeaponsByType(WeaponType.Bow);
        var legendaryWeapons = dataManager.GetWeaponsByRarity(ItemRarity.Legendary);

        // 获取特定种族的敌人
        var allGoblins = dataManager.GetEnemiesByRace("Goblin");
        var allBosses = dataManager.GetBossEnemies();
    }
}