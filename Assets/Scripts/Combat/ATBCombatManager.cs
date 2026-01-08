using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CombatSystem
{
    using ItemSystem.Core;
    using ItemSystem.Inventory;
    
    /// <summary>
    /// ATB战斗管理器 - 动态时间战斗系统
    /// </summary>
    public class ATBCombatManager : MonoBehaviour
    {
        #region 单例
        
        private static ATBCombatManager _instance;
        public static ATBCombatManager Instance => _instance;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
        
        #endregion
        
        #region 战斗配置
        
        [Header("战斗配置")]
        [SerializeField] private float atbTickRate = 60f;  // ATB每秒更新次数
        [SerializeField] private float baseATBGainMultiplier = 1f;
        [SerializeField] private bool pauseOnAction = true;  // 行动时暂停
        
        #endregion
        
        #region 战斗状态
        
        public enum CombatState
        {
            Idle,           // 未在战斗
            Preparing,      // 准备中（选择携带物品）
            Running,        // ATB运行中
            ActionSelect,   // 选择行动
            Executing,      // 执行行动动画
            Victory,        // 胜利
            Defeat          // 失败
        }
        
        [Header("当前状态")]
        [SerializeField] private CombatState currentState = CombatState.Idle;
        public CombatState CurrentState => currentState;
        
        private List<Character> _allCombatants = new();
        private List<Character> _playerTeam = new();
        private List<Character> _enemyTeam = new();
        private Character _activeCharacter;
        private float _atbTimer;
        
        public IReadOnlyList<Character> AllCombatants => _allCombatants;
        public IReadOnlyList<Character> PlayerTeam => _playerTeam;
        public IReadOnlyList<Character> EnemyTeam => _enemyTeam;
        public Character ActiveCharacter => _activeCharacter;
        
        #endregion
        
        #region 库存集成
        
        private InventoryManager _inventoryManager;
        public InventoryManager InventoryManager => _inventoryManager;
        
        public void SetInventoryManager(InventoryManager manager)
        {
            _inventoryManager = manager;
        }
        
        #endregion
        
        #region 事件
        
        public event Action OnCombatStarted;
        public event Action OnCombatEnded;
        public event Action<Character> OnCharacterTurnStart;
        public event Action<Character> OnCharacterTurnEnd;
        public event Action<Character, Character, DamageResult> OnDamageDealt;
        public event Action<CombatState> OnStateChanged;
        public event Action<int> OnTurnChanged;
        
        private int _turnCount;
        public int TurnCount => _turnCount;
        
        #endregion
        
        #region 战斗初始化
        
        /// <summary>
        /// 开始战斗
        /// </summary>
        public void StartCombat(List<Character> players, List<Character> enemies)
        {
            _playerTeam = new List<Character>(players);
            _enemyTeam = new List<Character>(enemies);
            _allCombatants = _playerTeam.Concat(_enemyTeam).ToList();
            
            _turnCount = 0;
            _atbTimer = 0f;
            
            // 初始化所有角色
            foreach (var character in _allCombatants)
            {
                character.CurrentATB = UnityEngine.Random.Range(0f, 30f); // 随机初始ATB
                character.OnATBFull += OnCharacterATBFull;
                character.OnDowned += OnCharacterDowned;
            }
            
            SetState(CombatState.Preparing);
            OnCombatStarted?.Invoke();
        }
        
        /// <summary>
        /// 准备完成，开始战斗
        /// </summary>
        public void BeginBattle()
        {
            if (currentState != CombatState.Preparing) return;
            SetState(CombatState.Running);
        }
        
        /// <summary>
        /// 结束战斗
        /// </summary>
        public void EndCombat(bool victory)
        {
            SetState(victory ? CombatState.Victory : CombatState.Defeat);
            
            // 清理事件
            foreach (var character in _allCombatants)
            {
                character.OnATBFull -= OnCharacterATBFull;
                character.OnDowned -= OnCharacterDowned;
            }
            
            // 通知库存管理器
            _inventoryManager?.EndBattle(victory);
            
            OnCombatEnded?.Invoke();
        }
        
        #endregion
        
        #region 战斗循环
        
        private void Update()
        {
            if (currentState != CombatState.Running) return;
            
            UpdateATB(Time.deltaTime);
            UpdateStatusEffects(Time.deltaTime);
            CheckBattleEnd();
        }
        
        /// <summary>
        /// 更新所有角色ATB
        /// </summary>
        private void UpdateATB(float deltaTime)
        {
            foreach (var character in _allCombatants)
            {
                if (!character.IsDowned)
                {
                    character.UpdateATB(deltaTime * baseATBGainMultiplier);
                }
            }
        }
        
        /// <summary>
        /// 更新状态效果
        /// </summary>
        private void UpdateStatusEffects(float deltaTime)
        {
            foreach (var character in _allCombatants)
            {
                character.UpdateStatusEffects(deltaTime);
            }
        }
        
        /// <summary>
        /// 角色ATB满触发
        /// </summary>
        private void OnCharacterATBFull(Character character)
        {
            if (currentState != CombatState.Running) return;
            
            // 暂停ATB
            if (pauseOnAction)
            {
                SetState(CombatState.ActionSelect);
            }
            
            _activeCharacter = character;
            _turnCount++;
            
            OnTurnChanged?.Invoke(_turnCount);
            OnCharacterTurnStart?.Invoke(character);
            
            // 如果是敌人，触发AI
            if (character.CharacterType == CharacterType.Enemy || 
                character.CharacterType == CharacterType.Boss)
            {
                ExecuteAIAction(character);
            }
        }
        
        #endregion
        
        #region 行动执行
        
        /// <summary>
        /// 执行普通攻击
        /// </summary>
        public void ExecuteNormalAttack(Character attacker, Character target)
        {
            if (_activeCharacter != attacker || currentState != CombatState.ActionSelect)
                return;
            
            SetState(CombatState.Executing);
            
            // 计算伤害
            var result = DamageCalculator.CalculateNormalAttack(attacker, target);
            
            // 应用伤害
            if (!result.IsMiss)
            {
                target.TakeDamage(result.ToDamageInfo());
            }
            
            // 武器耐久消耗
            if (attacker.EquippedWeapon?.Template is IDurable durable)
            {
                durable.ReduceDurability(1);
            }
            
            OnDamageDealt?.Invoke(attacker, target, result);
            
            EndTurn(attacker);
        }
        
        /// <summary>
        /// 执行技能
        /// </summary>
        public void ExecuteSkill(Character attacker, SkillData skill, Character target)
        {
            if (_activeCharacter != attacker || currentState != CombatState.ActionSelect)
                return;
            
            SetState(CombatState.Executing);
            
            // TODO: 实现技能系统
            // skill.Execute(attacker, target);
            
            EndTurn(attacker);
        }
        
        /// <summary>
        /// 使用物品
        /// </summary>
        public void UseItem(Character user, int itemId, Character target = null)
        {
            if (_activeCharacter != user || currentState != CombatState.ActionSelect)
                return;
            
            if (_inventoryManager == null) return;
            
            SetState(CombatState.Executing);
            
            // 通过库存管理器使用物品
            bool success = _inventoryManager.UseItemInCombat(itemId, user, target);
            
            if (success)
            {
                // 使用物品可能不消耗完整回合
                var item = ItemDatabase.Instance?.GetItem(itemId);
                if (item is ICombatUsable usable)
                {
                    user.ConsumeATB(usable.ATBCost);
                    
                    // 如果ATB还有剩余，不结束回合
                    if (user.CurrentATB >= Character.MaxATB)
                    {
                        SetState(CombatState.ActionSelect);
                        return;
                    }
                }
            }
            
            EndTurn(user);
        }
        
        /// <summary>
        /// 防御
        /// </summary>
        public void ExecuteDefend(Character character)
        {
            if (_activeCharacter != character || currentState != CombatState.ActionSelect)
                return;
            
            SetState(CombatState.Executing);
            
            // 添加防御状态
            // TODO: 实现防御buff
            
            // 防御只消耗一半ATB
            character.CurrentATB = Character.MaxATB * 0.5f;
            
            EndTurn(character);
        }
        
        /// <summary>
        /// 逃跑
        /// </summary>
        public bool TryEscape()
        {
            if (currentState != CombatState.ActionSelect) return false;
            
            // 计算逃跑成功率（基于速度差）
            float playerSpeedAvg = _playerTeam.Where(c => !c.IsDowned).Average(c => c.Stats.Speed);
            float enemySpeedAvg = _enemyTeam.Where(c => !c.IsDowned).Average(c => c.Stats.Speed);
            
            float escapeChance = 0.3f + (playerSpeedAvg - enemySpeedAvg) * 0.01f;
            escapeChance = Mathf.Clamp(escapeChance, 0.1f, 0.9f);
            
            if (UnityEngine.Random.value <= escapeChance)
            {
                EndCombat(false);
                return true;
            }
            
            // 逃跑失败，消耗回合
            EndTurn(_activeCharacter);
            return false;
        }
        
        /// <summary>
        /// 结束回合
        /// </summary>
        private void EndTurn(Character character)
        {
            character.ResetATB();
            
            OnCharacterTurnEnd?.Invoke(character);
            
            _activeCharacter = null;
            SetState(CombatState.Running);
        }
        
        #endregion
        
        #region AI行动
        
        /// <summary>
        /// 执行AI行动
        /// </summary>
        private void ExecuteAIAction(Character enemy)
        {
            // 简单AI：随机选择存活的玩家攻击
            var aliveTargets = _playerTeam.Where(c => !c.IsDowned).ToList();
            
            if (aliveTargets.Count == 0)
            {
                EndTurn(enemy);
                return;
            }
            
            var target = aliveTargets[UnityEngine.Random.Range(0, aliveTargets.Count)];
            
            // 延迟执行（给玩家反应时间）
            StartCoroutine(DelayedAIAttack(enemy, target, 0.5f));
        }
        
        private System.Collections.IEnumerator DelayedAIAttack(Character attacker, Character target, float delay)
        {
            yield return new WaitForSeconds(delay);
            ExecuteNormalAttack(attacker, target);
        }
        
        #endregion
        
        #region 战斗判定
        
        /// <summary>
        /// 检查战斗是否结束
        /// </summary>
        private void CheckBattleEnd()
        {
            bool allPlayersDowned = _playerTeam.All(c => c.IsDowned);
            bool allEnemiesDowned = _enemyTeam.All(c => c.IsDowned);
            
            if (allPlayersDowned)
            {
                EndCombat(false);
            }
            else if (allEnemiesDowned)
            {
                EndCombat(true);
            }
        }
        
        /// <summary>
        /// 角色倒下回调
        /// </summary>
        private void OnCharacterDowned(Character character)
        {
            // 检查战斗是否结束
            CheckBattleEnd();
        }
        
        #endregion
        
        #region 状态管理
        
        private void SetState(CombatState newState)
        {
            if (currentState == newState) return;
            
            currentState = newState;
            OnStateChanged?.Invoke(newState);
        }
        
        #endregion
        
        #region 查询方法
        
        /// <summary>
        /// 获取可选择的目标
        /// </summary>
        public List<Character> GetValidTargets(Character attacker, TargetType targetType)
        {
            bool isPlayer = attacker.CharacterType == CharacterType.Player || 
                           attacker.CharacterType == CharacterType.Ally;
            
            return targetType switch
            {
                TargetType.Self => new List<Character> { attacker },
                TargetType.SingleAlly => (isPlayer ? _playerTeam : _enemyTeam)
                    .Where(c => !c.IsDowned).ToList(),
                TargetType.AllAllies => (isPlayer ? _playerTeam : _enemyTeam)
                    .Where(c => !c.IsDowned).ToList(),
                TargetType.SingleEnemy => (isPlayer ? _enemyTeam : _playerTeam)
                    .Where(c => !c.IsDowned).ToList(),
                TargetType.AllEnemies => (isPlayer ? _enemyTeam : _playerTeam)
                    .Where(c => !c.IsDowned).ToList(),
                TargetType.All => _allCombatants.Where(c => !c.IsDowned).ToList(),
                _ => new List<Character>()
            };
        }
        
        /// <summary>
        /// 获取战斗背包中可用物品
        /// </summary>
        public List<ItemInstance> GetUsableCombatItems()
        {
            return _inventoryManager?.GetCombatInventory()?.GetUsableItems() ?? new List<ItemInstance>();
        }
        
        /// <summary>
        /// 获取ATB排序（下一个行动的角色）
        /// </summary>
        public List<Character> GetATBOrder()
        {
            return _allCombatants
                .Where(c => !c.IsDowned)
                .OrderByDescending(c => c.CurrentATB)
                .ToList();
        }
        
        #endregion
        
        #region 调试
        
        [Header("调试")]
        [SerializeField] private bool showDebugInfo = true;
        
        private void OnGUI()
        {
            if (!showDebugInfo || currentState == CombatState.Idle) return;
            
            GUILayout.BeginArea(new Rect(10, 10, 300, 500));
            GUILayout.Label($"战斗状态: {currentState}");
            GUILayout.Label($"回合数: {_turnCount}");
            GUILayout.Label($"当前行动: {_activeCharacter?.CharacterName ?? "无"}");
            
            GUILayout.Space(10);
            GUILayout.Label("=== 玩家队伍 ===");
            foreach (var c in _playerTeam)
            {
                GUILayout.Label($"{c.CharacterName}: HP {c.CurrentHealth}/{c.Stats.MaxHealth} ATB:{c.CurrentATB:F0}% {(c.IsDowned ? "[倒下]" : "")}");
            }
            
            GUILayout.Space(10);
            GUILayout.Label("=== 敌人队伍 ===");
            foreach (var c in _enemyTeam)
            {
                GUILayout.Label($"{c.CharacterName}: HP {c.CurrentHealth}/{c.Stats.MaxHealth} ATB:{c.CurrentATB:F0}% {(c.IsDowned ? "[倒下]" : "")}");
            }
            
            GUILayout.EndArea();
        }
        
        #endregion
    }
}
