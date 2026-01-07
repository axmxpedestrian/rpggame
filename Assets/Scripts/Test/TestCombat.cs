// ============================================
// 完整使用示例
// ============================================
using UnityEngine;
using System.Collections.Generic;

public class CombatExample : MonoBehaviour
{
    void Start()
    {
        // 设置可复现的随机数
        CombatRandom.SetProvider(new SeededRandomProvider(12345));

        // 创建玩家队伍
        var tank = new Character("骑士", "Knight", new BaseAttributes(15, 12, 8, 8, 6, 6));
        var healer = new Character("牧师", "Priest", new BaseAttributes(8, 5, 12, 6, 15, 8));
        var archer = new Character("弓手", "Archer", new BaseAttributes(8, 14, 14, 12, 6, 10));

        var playerTeam = new List<Character> { tank, healer, archer };

        // 创建敌人队伍
        var goblin1 = new Character("哥布林战士", "Goblin", new BaseAttributes(8, 10, 6, 10, 4, 5), false);
        var goblin2 = new Character("哥布林弓手", "Goblin", new BaseAttributes(6, 8, 10, 12, 4, 6), false);
        var boss = new Character("哥布林首领", "GoblinBoss", new BaseAttributes(12, 14, 8, 8, 6, 4), false);

        var enemyTeam = new List<Character> { goblin1, goblin2, boss };

        // 创建位置管理器
        var playerPositions = new PositionManager(playerTeam, true);
        var enemyPositions = new PositionManager(enemyTeam, false);

        // 装备武器
        var sword = new Weapon("骑士剑", WeaponType.Sharp, ItemRarity.Rare)
        {
            baseDamage = 15,
            elementType = ElementType.None
        };
        tank.Equip(sword);

        var staff = new Weapon("治愈法杖", WeaponType.Staff, ItemRarity.Rare)
        {
            baseDamage = 8,
            elementType = ElementType.Holy
        };
        healer.Equip(staff);

        var bow = new Weapon("精灵弓", WeaponType.Bow, ItemRarity.Epic)
        {
            baseDamage = 12,
            elementType = ElementType.None
        };
        archer.Equip(bow);

        // 初始化战斗属性
        foreach (var c in playerTeam) c.combatStats.InitializeCurrentValues();
        foreach (var c in enemyTeam) c.combatStats.InitializeCurrentValues();

        Debug.Log("=== 战斗开始 ===");
        LogPositions(playerTeam, "玩家队伍");
        LogPositions(enemyTeam, "敌人队伍");

        // 测试1：牧师使用相邻治疗
        Debug.Log("\n--- 测试：相邻治疗 ---");
        var healSkill = SkillFactory.CreateAdjacentHeal();
        var healTargets = TargetSelector.GetValidTargets(healer, healSkill, playerPositions, enemyPositions);
        Debug.Log($"牧师(位置{healer.position})的治疗目标：");
        foreach (var t in healTargets.ValidTargets)
        {
            Debug.Log($"  - {t.characterName} (位置{t.position})");
        }

        // 测试2：弓手使用狙击
        Debug.Log("\n--- 测试：狙击技能 ---");
        var snipeSkill = SkillFactory.CreateSnipe();
        var snipeTargets = TargetSelector.GetValidTargets(archer, snipeSkill, playerPositions, enemyPositions);

        if (snipeSkill.CanUse(archer))
        {
            Debug.Log($"弓手(位置{archer.position})可以狙击的目标：");
            foreach (var t in snipeTargets.ValidTargets)
            {
                Debug.Log($"  - {t.characterName} (位置{t.position})");
            }

            // 执行狙击
            if (snipeTargets.ValidTargets.Count > 0)
            {
                var target = snipeTargets.ValidTargets[0];
                var context = new DamageCalculationContext(archer, target, snipeSkill);
                var result = DamageCalculator.CalculateAndApply(context);
                Debug.Log(result.GetDetailedLog());
            }
        }
        else
        {
            Debug.Log($"弓手无法使用狙击：{snipeSkill.GetCannotUseReason(archer)}");
        }

        // 测试3：骑士从位置0使用冲锋（应该失败，因为需要后排）
        Debug.Log("\n--- 测试：冲锋技能（位置限制）---");
        var chargeSkill = SkillFactory.CreateFrontCharge();
        if (chargeSkill.CanUse(tank))
        {
            Debug.Log("骑士可以使用冲锋");
        }
        else
        {
            Debug.Log($"骑士无法使用冲锋：{chargeSkill.GetCannotUseReason(tank)}");
        }

        // 测试4：火焰风暴全体攻击
        Debug.Log("\n--- 测试：火焰风暴（全体攻击）---");
        var fireStorm = SkillFactory.CreateFireStorm();
        healer.combatStats.currentMagicSP = 10; // 确保有足够SP
        var fireTargets = TargetSelector.GetValidTargets(healer, fireStorm, playerPositions, enemyPositions);

        Debug.Log($"火焰风暴目标数量：{fireTargets.ValidTargets.Count}");
        Debug.Log($"需要玩家选择：{fireTargets.RequiresPlayerSelection}");

        foreach (var target in fireTargets.SelectedTargets)
        {
            var context = new DamageCalculationContext(healer, target, fireStorm);
            var result = DamageCalculator.Calculate(context);
            Debug.Log($"  对 {target.characterName}: {result.FinalDamage} 伤害");
        }
    }

    void LogPositions(List<Character> team, string teamName)
    {
        Debug.Log($"\n{teamName}站位：");
        foreach (var c in team)
        {
            Debug.Log($"  [{c.position}] {c.characterName} - HP:{c.combatStats.currentHealth}");
        }
    }
}