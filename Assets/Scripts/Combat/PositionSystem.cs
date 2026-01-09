using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CombatSystem
{
    /// <summary>
    /// 战斗站位系统
    /// 
    /// 布局示意：
    /// 敌方:  [4] [3] [2] [1] [0]  ← 前排（位置0）
    ///              ↕ 战场 ↕
    /// 友方:  [0] [1] [2] [3] [4]  ← 前排（位置0）
    /// 
    /// 位置0最靠近敌人（前排），位置4最远（后排）
    /// </summary>
    public class FormationManager
    {
        public const int MAX_POSITIONS = 5;
        
        private readonly Character[] _playerSlots = new Character[MAX_POSITIONS];
        private readonly Character[] _enemySlots = new Character[MAX_POSITIONS];
        
        public event Action<TeamSide, int, Character> OnPositionChanged;
        public event Action<TeamSide> OnFormationShifted;
        
        #region 位置管理
        
        /// <summary>
        /// 设置角色到指定位置
        /// </summary>
        public bool SetCharacterPosition(Character character, TeamSide side, int position)
        {
            if (position < 0 || position >= MAX_POSITIONS) return false;
            
            var slots = GetSlots(side);
            
            // 如果该位置已有角色，返回失败
            if (slots[position] != null && slots[position] != character)
                return false;
            
            // 移除角色原位置
            RemoveCharacterFromFormation(character, side);
            
            // 设置新位置
            slots[position] = character;
            OnPositionChanged?.Invoke(side, position, character);
            
            return true;
        }
        
        /// <summary>
        /// 获取角色当前位置
        /// </summary>
        public int GetCharacterPosition(Character character, TeamSide side)
        {
            var slots = GetSlots(side);
            for (int i = 0; i < MAX_POSITIONS; i++)
            {
                if (slots[i] == character) return i;
            }
            return -1;
        }
        
        /// <summary>
        /// 获取指定位置的角色
        /// </summary>
        public Character GetCharacterAtPosition(TeamSide side, int position)
        {
            if (position < 0 || position >= MAX_POSITIONS) return null;
            return GetSlots(side)[position];
        }
        
        /// <summary>
        /// 从阵型中移除角色
        /// </summary>
        public void RemoveCharacterFromFormation(Character character, TeamSide side)
        {
            var slots = GetSlots(side);
            for (int i = 0; i < MAX_POSITIONS; i++)
            {
                if (slots[i] == character)
                {
                    slots[i] = null;
                    OnPositionChanged?.Invoke(side, i, null);
                    break;
                }
            }
        }
        
        /// <summary>
        /// 初始化队伍阵型
        /// </summary>
        public void InitializeFormation(TeamSide side, List<Character> characters)
        {
            var slots = GetSlots(side);
            Array.Clear(slots, 0, MAX_POSITIONS);
            
            for (int i = 0; i < Mathf.Min(characters.Count, MAX_POSITIONS); i++)
            {
                slots[i] = characters[i];
            }
        }
        
        #endregion
        
        #region 自动补位
        
        /// <summary>
        /// 角色倒下时自动补位
        /// 后排角色向前移动填补空位
        /// </summary>
        public void OnCharacterDowned(Character character, TeamSide side)
        {
            int position = GetCharacterPosition(character, side);
            if (position < 0) return;
            
            // 不立即移除，等待补位逻辑
            ShiftFormationForward(side);
        }
        
        /// <summary>
        /// 向前补位 - 后排填补前排空位
        /// </summary>
        public void ShiftFormationForward(TeamSide side)
        {
            var slots = GetSlots(side);
            bool shifted = false;
            
            // 从前往后检查空位
            for (int i = 0; i < MAX_POSITIONS - 1; i++)
            {
                // 如果当前位置空着或角色倒下
                if (slots[i] == null || slots[i].IsDowned)
                {
                    // 找后面第一个存活的角色
                    for (int j = i + 1; j < MAX_POSITIONS; j++)
                    {
                        if (slots[j] != null && !slots[j].IsDowned)
                        {
                            // 移动到前面
                            if (slots[i] == null || slots[i].IsDowned)
                            {
                                var downedChar = slots[i];
                                slots[i] = slots[j];
                                slots[j] = downedChar; // 倒下的角色移到后面
                                shifted = true;
                                OnPositionChanged?.Invoke(side, i, slots[i]);
                                OnPositionChanged?.Invoke(side, j, slots[j]);
                            }
                            break;
                        }
                    }
                }
            }
            
            if (shifted)
            {
                OnFormationShifted?.Invoke(side);
            }
        }
        
        #endregion
        
        #region 位置查询
        
        /// <summary>
        /// 获取所有存活角色
        /// </summary>
        public List<Character> GetAliveCharacters(TeamSide side)
        {
            return GetSlots(side)
                .Where(c => c != null && !c.IsDowned)
                .ToList();
        }
        
        /// <summary>
        /// 获取前排角色（最前面的存活角色）
        /// </summary>
        public Character GetFrontCharacter(TeamSide side)
        {
            var slots = GetSlots(side);
            for (int i = 0; i < MAX_POSITIONS; i++)
            {
                if (slots[i] != null && !slots[i].IsDowned)
                    return slots[i];
            }
            return null;
        }
        
        /// <summary>
        /// 获取前排位置索引
        /// </summary>
        public int GetFrontPosition(TeamSide side)
        {
            var slots = GetSlots(side);
            for (int i = 0; i < MAX_POSITIONS; i++)
            {
                if (slots[i] != null && !slots[i].IsDowned)
                    return i;
            }
            return -1;
        }
        
        /// <summary>
        /// 获取相邻位置的角色
        /// </summary>
        public List<Character> GetAdjacentCharacters(Character character, TeamSide side, bool includeDownedCharacters = false)
        {
            var result = new List<Character>();
            int position = GetCharacterPosition(character, side);
            if (position < 0) return result;
            
            var slots = GetSlots(side);
            
            // 左边相邻
            if (position > 0)
            {
                var adjacent = slots[position - 1];
                if (adjacent != null && (includeDownedCharacters || !adjacent.IsDowned))
                    result.Add(adjacent);
            }
            
            // 右边相邻
            if (position < MAX_POSITIONS - 1)
            {
                var adjacent = slots[position + 1];
                if (adjacent != null && (includeDownedCharacters || !adjacent.IsDowned))
                    result.Add(adjacent);
            }
            
            return result;
        }
        
        /// <summary>
        /// 计算两个角色之间的距离
        /// </summary>
        public int GetDistance(Character a, Character b, TeamSide sideA, TeamSide sideB)
        {
            int posA = GetCharacterPosition(a, sideA);
            int posB = GetCharacterPosition(b, sideB);
            
            if (posA < 0 || posB < 0) return -1;
            
            if (sideA == sideB)
            {
                // 同侧，直接计算位置差
                return Mathf.Abs(posA - posB);
            }
            else
            {
                // 对侧，需要计算跨越战场的距离
                // 距离 = 攻击者到前排 + 目标到前排 + 1（战场间隙）
                return posA + posB + 1;
            }
        }
        
        private Character[] GetSlots(TeamSide side)
        {
            return side == TeamSide.Player ? _playerSlots : _enemySlots;
        }
        
        #endregion
        
        #region 调试
        
        public string GetFormationDebugInfo(TeamSide side)
        {
            var slots = GetSlots(side);
            var parts = new string[MAX_POSITIONS];
            
            for (int i = 0; i < MAX_POSITIONS; i++)
            {
                if (slots[i] == null)
                    parts[i] = "[空]";
                else if (slots[i].IsDowned)
                    parts[i] = $"[{slots[i].CharacterName}☠]";
                else
                    parts[i] = $"[{slots[i].CharacterName}]";
            }
            
            return string.Join(" ", parts);
        }
        
        #endregion
    }
    
    public enum TeamSide
    {
        Player,
        Enemy
    }
    
    /// <summary>
    /// 攻击范围配置 - 支持多种范围类型
    /// </summary>
    [Serializable]
    public class AttackRangeDefinition
    {
        [Header("范围类型")]
        public RangeType rangeType = RangeType.Fixed;
        
        [Header("固定范围 - 可攻击的敌方位置")]
        [Tooltip("0=前排, 4=后排")]
        public int[] fixedTargetPositions = { 0, 1 };
        
        [Header("相对范围")]
        [Tooltip("相对于攻击者位置的偏移，正数=向后，负数=向前")]
        public int[] relativeOffsets = { -1, 0, 1 };
        
        [Header("距离范围")]
        public int minDistance = 0;
        public int maxDistance = 2;
        
        [Header("特殊选项")]
        public bool canTargetSelf = false;
        public bool canTargetAllies = false;
        public bool canTargetEnemies = true;
        public bool requiresLineOfSight = false;  // 是否需要前排无人才能打后排
        
        /// <summary>
        /// 检查是否可以攻击目标
        /// </summary>
        public bool CanTarget(
            FormationManager formation,
            Character attacker, TeamSide attackerSide,
            Character target, TeamSide targetSide)
        {
            // 检查是否可以攻击该阵营
            bool isAlly = attackerSide == targetSide;
            if (isAlly && !canTargetAllies) return false;
            if (!isAlly && !canTargetEnemies) return false;
            if (attacker == target && !canTargetSelf) return false;
            
            int attackerPos = formation.GetCharacterPosition(attacker, attackerSide);
            int targetPos = formation.GetCharacterPosition(target, targetSide);
            
            if (attackerPos < 0 || targetPos < 0) return false;
            
            switch (rangeType)
            {
                case RangeType.Fixed:
                    return CheckFixedRange(targetPos, isAlly);
                    
                case RangeType.Relative:
                    return CheckRelativeRange(attackerPos, targetPos, isAlly);
                    
                case RangeType.Distance:
                    int distance = formation.GetDistance(attacker, target, attackerSide, targetSide);
                    return distance >= minDistance && distance <= maxDistance;
                    
                case RangeType.Adjacent:
                    return Mathf.Abs(attackerPos - targetPos) <= 1 && isAlly;
                    
                case RangeType.All:
                    return true;
                    
                case RangeType.Self:
                    return attacker == target;
                    
                case RangeType.FrontLine:
                    // 只能攻击前排（最前面的存活角色所在位置）
                    int frontPos = formation.GetFrontPosition(targetSide);
                    return targetPos == frontPos;
                    
                default:
                    return false;
            }
        }
        
        private bool CheckFixedRange(int targetPos, bool isAlly)
        {
            // 固定范围：检查目标位置是否在允许的位置列表中
            return fixedTargetPositions.Contains(targetPos);
        }
        
        private bool CheckRelativeRange(int attackerPos, int targetPos, bool isAlly)
        {
            // 相对范围：检查目标位置相对于攻击者的偏移
            int offset = targetPos - attackerPos;
            return relativeOffsets.Contains(offset);
        }
        
        /// <summary>
        /// 获取所有可攻击的目标
        /// </summary>
        public List<Character> GetValidTargets(
            FormationManager formation,
            Character attacker, TeamSide attackerSide)
        {
            var targets = new List<Character>();
            
            // 检查友方
            if (canTargetAllies || canTargetSelf)
            {
                foreach (var ally in formation.GetAliveCharacters(attackerSide))
                {
                    if (CanTarget(formation, attacker, attackerSide, ally, attackerSide))
                        targets.Add(ally);
                }
            }
            
            // 检查敌方
            if (canTargetEnemies)
            {
                var enemySide = attackerSide == TeamSide.Player ? TeamSide.Enemy : TeamSide.Player;
                foreach (var enemy in formation.GetAliveCharacters(enemySide))
                {
                    if (CanTarget(formation, attacker, attackerSide, enemy, enemySide))
                        targets.Add(enemy);
                }
            }
            
            return targets;
        }
    }
    
    public enum RangeType
    {
        Fixed,      // 固定位置（如：弓只能打位置2,3,4）
        Relative,   // 相对攻击者位置（如：治疗相邻队友）
        Distance,   // 基于距离
        Adjacent,   // 仅相邻（自己±1位置）
        All,        // 全体
        Self,       // 仅自己
        FrontLine   // 仅前排
    }
    
    /// <summary>
    /// 预设攻击范围
    /// </summary>
    public static class AttackRangePresets
    {
        /// <summary>近战 - 只能打前排</summary>
        public static AttackRangeDefinition Melee => new()
        {
            rangeType = RangeType.Fixed,
            fixedTargetPositions = new[] { 0, 1 }
        };
        
        /// <summary>长柄 - 可打前两排</summary>
        public static AttackRangeDefinition Polearm => new()
        {
            rangeType = RangeType.Fixed,
            fixedTargetPositions = new[] { 0, 1, 2 }
        };
        
        /// <summary>弓箭 - 只能打后排</summary>
        public static AttackRangeDefinition Bow => new()
        {
            rangeType = RangeType.Fixed,
            fixedTargetPositions = new[] { 2, 3, 4 }
        };
        
        /// <summary>枪械 - 全距离</summary>
        public static AttackRangeDefinition Gun => new()
        {
            rangeType = RangeType.All
        };
        
        /// <summary>魔法 - 全体敌人</summary>
        public static AttackRangeDefinition MagicAll => new()
        {
            rangeType = RangeType.All,
            canTargetEnemies = true,
            canTargetAllies = false
        };
        
        /// <summary>治疗相邻 - 自己和相邻队友</summary>
        public static AttackRangeDefinition HealAdjacent => new()
        {
            rangeType = RangeType.Adjacent,
            canTargetEnemies = false,
            canTargetAllies = true,
            canTargetSelf = true
        };
        
        /// <summary>单体治疗 - 任意队友</summary>
        public static AttackRangeDefinition HealSingle => new()
        {
            rangeType = RangeType.All,
            canTargetEnemies = false,
            canTargetAllies = true,
            canTargetSelf = true
        };
        
        /// <summary>自我增益</summary>
        public static AttackRangeDefinition SelfOnly => new()
        {
            rangeType = RangeType.Self,
            canTargetSelf = true,
            canTargetAllies = false,
            canTargetEnemies = false
        };
    }
}
