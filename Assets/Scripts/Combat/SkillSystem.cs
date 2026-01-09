using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CombatSystem
{
    using ItemSystem.Core;
    using ItemSystem.Equipment;
    
    /// <summary>
    /// 技能数据 - ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "NewSkill", menuName = "CombatSystem/Skill")]
    public class SkillDefinition : ScriptableObject
    {
        [Header("基础信息")]
        public int skillId;
        public string skillName;
        [TextArea] public string description;
        public Sprite icon;
        
        [Header("技能类型")]
        public SkillType skillType;
        public SkillCategory category;
        
        [Header("来源限制")]
        [Tooltip("武器技能：需要装备此类型武器")]
        public WeaponCategory requiredWeaponCategory;
        [Tooltip("武器熟练度要求")]
        public int requiredProficiencyLevel;
        [Tooltip("角色专属技能ID（0=无限制）")]
        public int exclusiveCharacterId;
        
        [Header("消耗")]
        public SkillCostType costType;
        public int baseCost = 1;
        public int atbCost = 100;  // ATB消耗（100=满条）
        
        [Header("目标与范围")]
        public AttackRangeDefinition attackRange;
        public bool isMultiTarget;  // 是否群体技能
        public int maxTargets = 1;   // 最大目标数
        
        [Header("伤害/效果")]
        public float damageMultiplier = 1f;
        public DamageCategory damageCategory;
        public ElementType element;
        public float armorPenetration;
        
        [Header("附加效果")]
        public SkillEffect[] additionalEffects;
        
        [Header("冷却")]
        public int cooldownTurns;
        
        [Header("动画")]
        public string animationTrigger;
        public float castTime;
        
        /// <summary>
        /// 计算实际消耗（考虑熟练度减免）
        /// </summary>
        public int GetActualCost(Character caster)
        {
            int cost = baseCost;
            
            // 武器熟练度减免
            if (skillType == SkillType.WeaponSkill && caster.WeaponProficiency != null)
            {
                int reduction = caster.WeaponProficiency.GetSkillCostReduction(requiredWeaponCategory);
                cost = Mathf.Max(1, cost - reduction);
            }
            
            return cost;
        }
        
        /// <summary>
        /// 检查是否可以使用
        /// </summary>
        public SkillUsabilityResult CanUse(Character caster, FormationManager formation, TeamSide casterSide)
        {
            var result = new SkillUsabilityResult { CanUse = true };
            
            // 检查武器要求
            if (skillType == SkillType.WeaponSkill)
            {
                var weapon = caster.EquippedWeapon?.Template as Weapon;
                if (weapon == null || weapon.WeaponCategory != requiredWeaponCategory)
                {
                    result.CanUse = false;
                    result.FailReason = $"需要装备{requiredWeaponCategory}类武器";
                    return result;
                }
                
                // 检查熟练度
                if (caster.WeaponProficiency != null)
                {
                    int profLevel = caster.WeaponProficiency.GetProficiencyLevel(requiredWeaponCategory);
                    if (profLevel < requiredProficiencyLevel)
                    {
                        result.CanUse = false;
                        result.FailReason = $"需要{requiredWeaponCategory}熟练度{requiredProficiencyLevel}级";
                        return result;
                    }
                }
            }
            
            // 检查角色专属
            if (exclusiveCharacterId > 0)
            {
                // 这里需要角色ID匹配逻辑
            }
            
            // 检查消耗
            int actualCost = GetActualCost(caster);
            bool hasEnoughResource = costType switch
            {
                SkillCostType.PhysicalSP => caster.CurrentPhysicalSP >= actualCost,
                SkillCostType.MagicSP => caster.CurrentMagicSP >= actualCost,
                SkillCostType.Health => caster.CurrentHealth > actualCost,
                SkillCostType.None => true,
                _ => true
            };
            
            if (!hasEnoughResource)
            {
                result.CanUse = false;
                result.FailReason = $"资源不足（需要{actualCost}{GetCostTypeName(costType)}）";
                return result;
            }
            
            // 检查ATB
            if (caster.CurrentATB < atbCost)
            {
                result.CanUse = false;
                result.FailReason = "ATB不足";
                return result;
            }
            
            // 检查是否有可用目标
            var validTargets = attackRange.GetValidTargets(formation, caster, casterSide);
            if (validTargets.Count == 0)
            {
                result.CanUse = false;
                result.FailReason = "没有有效目标";
                return result;
            }
            
            result.ValidTargets = validTargets;
            return result;
        }
        
        private string GetCostTypeName(SkillCostType type)
        {
            return type switch
            {
                SkillCostType.PhysicalSP => "体力",
                SkillCostType.MagicSP => "魔力",
                SkillCostType.Health => "生命",
                _ => ""
            };
        }
    }
    
    public enum SkillType
    {
        WeaponSkill,    // 武器技能 - 需要特定武器
        CharacterSkill, // 角色技能 - 角色专属
        ItemSkill,      // 物品技能 - 来自装备
        CommonSkill     // 通用技能 - 所有人可用
    }
    
    public enum SkillCategory
    {
        Attack,     // 攻击技能
        Defense,    // 防御技能
        Support,    // 辅助技能
        Heal,       // 治疗技能
        Buff,       // 增益技能
        Debuff,     // 减益技能
        Movement    // 位移技能
    }
    
    public enum SkillCostType
    {
        None,       // 无消耗（普通攻击）
        PhysicalSP, // 消耗体力技能点
        MagicSP,    // 消耗魔力技能点
        Health      // 消耗生命值
    }
    
    /// <summary>
    /// 技能附加效果
    /// </summary>
    [Serializable]
    public class SkillEffect
    {
        public SkillEffectType effectType;
        public float value;
        public float chance = 1f;  // 触发概率
        public float duration;
        public StatusEffectData statusEffect;  // 如果是施加状态
    }
    
    public enum SkillEffectType
    {
        None,
        ApplyStatus,        // 施加状态效果
        Knockback,          // 击退
        Pull,               // 拉近
        Lifesteal,          // 吸血
        ShieldBreak,        // 破盾
        IgnoreDefense,      // 无视防御
        BonusDamageVsStatus,// 对特定状态额外伤害
        ChainAttack,        // 连锁攻击
        AreaOfEffect        // 范围伤害
    }
    
    /// <summary>
    /// 技能可用性检查结果
    /// </summary>
    public struct SkillUsabilityResult
    {
        public bool CanUse;
        public string FailReason;
        public List<Character> ValidTargets;
    }
    
    /// <summary>
    /// 角色技能管理器
    /// </summary>
    [Serializable]
    public class CharacterSkillManager
    {
        [SerializeField] private List<SkillDefinition> learnedSkills = new();
        [SerializeField] private List<SkillDefinition> equippedSkills = new();  // 当前装备的技能
        
        private Dictionary<int, int> _skillCooldowns = new();  // 技能ID -> 剩余冷却回合
        
        public const int MaxEquippedSkills = 4;
        
        public IReadOnlyList<SkillDefinition> LearnedSkills => learnedSkills;
        public IReadOnlyList<SkillDefinition> EquippedSkills => equippedSkills;
        
        public event Action<SkillDefinition> OnSkillLearned;
        public event Action<SkillDefinition> OnSkillUsed;
        
        #region 技能学习
        
        /// <summary>
        /// 学习技能
        /// </summary>
        public bool LearnSkill(SkillDefinition skill)
        {
            if (skill == null || learnedSkills.Contains(skill))
                return false;
            
            learnedSkills.Add(skill);
            OnSkillLearned?.Invoke(skill);
            return true;
        }
        
        /// <summary>
        /// 是否已学习技能
        /// </summary>
        public bool HasLearnedSkill(SkillDefinition skill)
        {
            return learnedSkills.Contains(skill);
        }
        
        #endregion
        
        #region 技能装备
        
        /// <summary>
        /// 装备技能到技能栏
        /// </summary>
        public bool EquipSkill(SkillDefinition skill, int slot)
        {
            if (skill == null || slot < 0 || slot >= MaxEquippedSkills)
                return false;
            
            if (!learnedSkills.Contains(skill))
                return false;
            
            // 确保列表足够大
            while (equippedSkills.Count <= slot)
                equippedSkills.Add(null);
            
            equippedSkills[slot] = skill;
            return true;
        }
        
        /// <summary>
        /// 卸下技能
        /// </summary>
        public void UnequipSkill(int slot)
        {
            if (slot >= 0 && slot < equippedSkills.Count)
                equippedSkills[slot] = null;
        }
        
        /// <summary>
        /// 获取槽位的技能
        /// </summary>
        public SkillDefinition GetEquippedSkill(int slot)
        {
            if (slot >= 0 && slot < equippedSkills.Count)
                return equippedSkills[slot];
            return null;
        }
        
        #endregion
        
        #region 冷却管理
        
        /// <summary>
        /// 检查技能是否在冷却中
        /// </summary>
        public bool IsOnCooldown(SkillDefinition skill)
        {
            return _skillCooldowns.TryGetValue(skill.skillId, out int cd) && cd > 0;
        }
        
        /// <summary>
        /// 获取剩余冷却回合
        /// </summary>
        public int GetCooldownRemaining(SkillDefinition skill)
        {
            return _skillCooldowns.TryGetValue(skill.skillId, out int cd) ? cd : 0;
        }
        
        /// <summary>
        /// 设置技能冷却
        /// </summary>
        public void SetCooldown(SkillDefinition skill)
        {
            if (skill.cooldownTurns > 0)
                _skillCooldowns[skill.skillId] = skill.cooldownTurns;
        }
        
        /// <summary>
        /// 回合结束时减少冷却
        /// </summary>
        public void TickCooldowns()
        {
            var keys = _skillCooldowns.Keys.ToList();
            foreach (var key in keys)
            {
                if (_skillCooldowns[key] > 0)
                    _skillCooldowns[key]--;
            }
        }
        
        /// <summary>
        /// 重置所有冷却
        /// </summary>
        public void ResetAllCooldowns()
        {
            _skillCooldowns.Clear();
        }
        
        #endregion
        
        #region 技能使用
        
        /// <summary>
        /// 使用技能
        /// </summary>
        public void OnSkillUsedInternal(SkillDefinition skill)
        {
            SetCooldown(skill);
            OnSkillUsed?.Invoke(skill);
        }
        
        /// <summary>
        /// 获取所有可用技能（考虑武器、冷却等）
        /// </summary>
        public List<SkillDefinition> GetAvailableSkills(Character character, FormationManager formation, TeamSide side)
        {
            var available = new List<SkillDefinition>();
            
            foreach (var skill in equippedSkills)
            {
                if (skill == null) continue;
                if (IsOnCooldown(skill)) continue;
                
                var result = skill.CanUse(character, formation, side);
                if (result.CanUse)
                    available.Add(skill);
            }
            
            return available;
        }
        
        #endregion
        
        #region 武器技能同步
        
        /// <summary>
        /// 当武器改变时，更新可用的武器技能
        /// </summary>
        public void OnWeaponChanged(Weapon oldWeapon, Weapon newWeapon)
        {
            // 移除旧武器的技能
            if (oldWeapon != null)
            {
                for (int i = equippedSkills.Count - 1; i >= 0; i--)
                {
                    var skill = equippedSkills[i];
                    if (skill != null && 
                        skill.skillType == SkillType.WeaponSkill &&
                        skill.requiredWeaponCategory == oldWeapon.WeaponCategory)
                    {
                        equippedSkills[i] = null;
                    }
                }
            }
            
            // 注意：新武器的技能需要手动装备，或者实现自动装备逻辑
        }
        
        #endregion
    }
    
    /// <summary>
    /// 技能点管理（回合恢复）
    /// </summary>
    public static class SkillPointManager
    {
        /// <summary>
        /// 回合开始时恢复技能点
        /// </summary>
        public static void OnTurnStart(Character character)
        {
            // 基础恢复量
            int physicalRecovery = 1;
            int magicRecovery = 1;
            
            // 装备加成
            // TODO: 从装备获取额外恢复量
            
            // 应用恢复
            character.RestoreSkillPoints(physicalRecovery, magicRecovery);
        }
        
        /// <summary>
        /// 消耗技能点
        /// </summary>
        public static bool ConsumeSkillPoints(Character character, SkillCostType costType, int amount)
        {
            switch (costType)
            {
                case SkillCostType.PhysicalSP:
                    if (character.CurrentPhysicalSP < amount) return false;
                    character.CurrentPhysicalSP -= amount;
                    return true;
                    
                case SkillCostType.MagicSP:
                    if (character.CurrentMagicSP < amount) return false;
                    character.CurrentMagicSP -= amount;
                    return true;
                    
                case SkillCostType.Health:
                    if (character.CurrentHealth <= amount) return false;
                    character.CurrentHealth -= amount;
                    return true;
                    
                case SkillCostType.None:
                    return true;
                    
                default:
                    return true;
            }
        }
    }
    
    /// <summary>
    /// 技能执行器
    /// </summary>
    public class SkillExecutor
    {
        private readonly FormationManager _formation;
        
        public event Action<Character, SkillDefinition, List<Character>> OnSkillExecuted;
        public event Action<Character, Character, DamageResult> OnSkillDamageDealt;
        
        public SkillExecutor(FormationManager formation)
        {
            _formation = formation;
        }
        
        /// <summary>
        /// 执行技能
        /// </summary>
        public SkillExecutionResult Execute(
            Character caster, 
            TeamSide casterSide,
            SkillDefinition skill, 
            List<Character> targets)
        {
            var result = new SkillExecutionResult { Success = true, Skill = skill };
            
            // 1. 再次验证
            var usability = skill.CanUse(caster, _formation, casterSide);
            if (!usability.CanUse)
            {
                result.Success = false;
                result.FailReason = usability.FailReason;
                return result;
            }
            
            // 2. 消耗资源
            int actualCost = skill.GetActualCost(caster);
            if (!SkillPointManager.ConsumeSkillPoints(caster, skill.costType, actualCost))
            {
                result.Success = false;
                result.FailReason = "资源不足";
                return result;
            }
            
            // 3. 消耗ATB
            caster.ConsumeATB(skill.atbCost);
            
            // 4. 对每个目标执行效果
            foreach (var target in targets)
            {
                if (target == null || target.IsDowned) continue;
                
                var targetResult = ExecuteOnTarget(caster, casterSide, skill, target);
                result.TargetResults.Add(targetResult);
            }
            
            // 5. 设置冷却
            caster.SkillManager?.OnSkillUsedInternal(skill);
            
            // 6. 武器熟练度经验
            if (skill.skillType == SkillType.WeaponSkill)
            {
                caster.WeaponProficiency?.OnWeaponUsed(skill.requiredWeaponCategory, true, result.TargetResults.Any(r => r.Hit));
            }
            
            OnSkillExecuted?.Invoke(caster, skill, targets);
            
            return result;
        }
        
        private SkillTargetResult ExecuteOnTarget(Character caster, TeamSide casterSide, SkillDefinition skill, Character target)
        {
            var result = new SkillTargetResult { Target = target };
            
            // 根据技能类别执行不同逻辑
            switch (skill.category)
            {
                case SkillCategory.Attack:
                case SkillCategory.Debuff:
                    ExecuteAttackSkill(caster, casterSide, skill, target, result);
                    break;
                    
                case SkillCategory.Heal:
                    ExecuteHealSkill(caster, skill, target, result);
                    break;
                    
                case SkillCategory.Buff:
                    ExecuteBuffSkill(caster, skill, target, result);
                    break;
                    
                case SkillCategory.Support:
                    ExecuteSupportSkill(caster, skill, target, result);
                    break;
            }
            
            return result;
        }
        
        private void ExecuteAttackSkill(Character caster, TeamSide casterSide, SkillDefinition skill, Character target, SkillTargetResult result)
        {
            // 计算伤害
            var damageContext = new DamageContext
            {
                Attacker = caster,
                Defender = target,
                DamageCategory = skill.damageCategory,
                Element = skill.element,
                SkillMultiplier = skill.damageMultiplier,
                ArmorPenetration = skill.armorPenetration,
                ApplyRandomVariance = true
            };
            
            var damageResult = DamageCalculator.CalculateDamage(damageContext);
            result.Hit = !damageResult.IsMiss;
            result.Damage = damageResult.FinalDamage;
            result.Critical = damageResult.IsCritical;
            
            if (result.Hit)
            {
                target.TakeDamage(damageResult.ToDamageInfo());
                OnSkillDamageDealt?.Invoke(caster, target, damageResult);
                
                // 应用附加效果
                ApplyAdditionalEffects(caster, skill, target, result);
            }
        }
        
        private void ExecuteHealSkill(Character caster, SkillDefinition skill, Character target, SkillTargetResult result)
        {
            // 治疗量基于魔法攻击
            int healAmount = Mathf.RoundToInt(caster.CombatCache.MagicAttack * skill.damageMultiplier);
            target.Heal(healAmount);
            
            result.Hit = true;
            result.Heal = healAmount;
            
            ApplyAdditionalEffects(caster, skill, target, result);
        }
        
        private void ExecuteBuffSkill(Character caster, SkillDefinition skill, Character target, SkillTargetResult result)
        {
            result.Hit = true;
            ApplyAdditionalEffects(caster, skill, target, result);
        }
        
        private void ExecuteSupportSkill(Character caster, SkillDefinition skill, Character target, SkillTargetResult result)
        {
            result.Hit = true;
            ApplyAdditionalEffects(caster, skill, target, result);
        }
        
        private void ApplyAdditionalEffects(Character caster, SkillDefinition skill, Character target, SkillTargetResult result)
        {
            if (skill.additionalEffects == null) return;
            
            foreach (var effect in skill.additionalEffects)
            {
                // 概率检查
                if (UnityEngine.Random.value > effect.chance) continue;
                
                switch (effect.effectType)
                {
                    case SkillEffectType.ApplyStatus:
                        if (effect.statusEffect != null)
                        {
                            target.AddStatusEffect(effect.statusEffect, caster, effect.duration);
                            result.AppliedEffects.Add(effect.statusEffect.EffectName);
                        }
                        break;
                        
                    case SkillEffectType.Lifesteal:
                        int lifeSteal = Mathf.RoundToInt(result.Damage * effect.value);
                        caster.Heal(lifeSteal);
                        break;
                        
                    // TODO: 实现其他效果
                }
            }
        }
    }
    
    /// <summary>
    /// 技能执行结果
    /// </summary>
    public class SkillExecutionResult
    {
        public bool Success;
        public string FailReason;
        public SkillDefinition Skill;
        public List<SkillTargetResult> TargetResults = new();
    }
    
    /// <summary>
    /// 单目标技能结果
    /// </summary>
    public class SkillTargetResult
    {
        public Character Target;
        public bool Hit;
        public int Damage;
        public int Heal;
        public bool Critical;
        public List<string> AppliedEffects = new();
    }
}
