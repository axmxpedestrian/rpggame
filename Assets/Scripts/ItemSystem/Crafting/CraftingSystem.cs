using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ItemSystem.Inventory;

namespace ItemSystem.Crafting
{
    using Core;

    // ============================================================
    // 泰拉瑞亚风格合成系统
    // 
    // 特性：
    // 1. 工作台邻近检测（多个工作台同时生效）
    // 2. 配方自动发现（有材料才显示配方）
    // 3. 引导查询（材料能合成什么、如何合成某物品）
    // 4. 批量合成
    // 5. 替代材料支持
    // 6. 额外产出概率
    // 7. 配方解锁系统
    // ============================================================

    [CreateAssetMenu(fileName = "NewRecipe", menuName = "ItemSystem/Recipe")]
    public class Recipe : ScriptableObject
    {
        [Header("配方信息")]
        [SerializeField] private int recipeId;
        [SerializeField] private string recipeName;
        [TextArea(2, 4)]
        [SerializeField] private string description;

        [Header("输入材料")]
        [SerializeField] private RecipeIngredient[] ingredients;

        [Header("输出物品")]
        [SerializeField] private int outputItemId;
        [SerializeField] private int outputCount = 1;

        [Header("合成条件")]
        [SerializeField] private CraftingStation[] requiredStations;
        [SerializeField] private CraftingStation[] nearbyStations;
        [SerializeField] private int requiredPlayerLevel;
        [SerializeField] private string[] requiredSkills;
        [SerializeField] private int requiredSkillLevel;
        [SerializeField] private bool requiresUnlock = false;

        [Header("合成属性")]
        [SerializeField] private float craftingTime = 0f;
        [SerializeField] private int experienceGain = 1;
        [SerializeField] private RecipeCategory category;

        [Header("特殊选项")]
        [SerializeField] private float successRate = 1f;
        [SerializeField] private BonusOutput[] bonusOutputs;

        // 属性访问器
        public int RecipeId => recipeId;
        public string RecipeName => recipeName;
        public string Description => description;
        public RecipeIngredient[] Ingredients => ingredients;
        public int OutputItemId => outputItemId;
        public int OutputCount => outputCount;
        public CraftingStation[] RequiredStations => requiredStations;
        public CraftingStation[] NearbyStations => nearbyStations;
        public float CraftingTime => craftingTime;
        public int ExperienceGain => experienceGain;
        public RecipeCategory Category => category;
        public float SuccessRate => successRate;
        public bool RequiresUnlock => requiresUnlock;
        public BonusOutput[] BonusOutputs => bonusOutputs;

        // 向后兼容
        public CraftingStation RequiredStation => 
            requiredStations != null && requiredStations.Length > 0 
                ? requiredStations[0] : CraftingStation.None;

        public bool CanCraft(IInventory inventory, CraftingContext context)
        {
            // 检查主工作台
            if (requiredStations != null && requiredStations.Length > 0)
            {
                bool hasStation = requiredStations.Any(s => 
                    s == CraftingStation.None || context.AvailableStations.Contains(s));
                if (!hasStation) return false;
            }

            // 检查邻近工作台
            if (nearbyStations != null && nearbyStations.Length > 0)
            {
                foreach (var station in nearbyStations)
                {
                    if (!context.NearbyStations.Contains(station))
                        return false;
                }
            }

            if (context.PlayerLevel < requiredPlayerLevel) return false;

            if (requiredSkills != null)
            {
                foreach (var skill in requiredSkills)
                {
                    if (context.GetSkillLevel(skill) < requiredSkillLevel)
                        return false;
                }
            }

            if (requiresUnlock && !context.UnlockedRecipes.Contains(recipeId))
                return false;

            foreach (var ingredient in ingredients)
            {
                if (!HasIngredient(inventory, ingredient))
                    return false;
            }

            return true;
        }

        private bool HasIngredient(IInventory inventory, RecipeIngredient ingredient)
        {
            int have = inventory.GetItemCount(ingredient.ItemId);
            
            if (ingredient.AcceptAlternatives && ingredient.AlternativeItemIds != null)
            {
                foreach (var altId in ingredient.AlternativeItemIds)
                    have += inventory.GetItemCount(altId);
            }
            
            return have >= ingredient.Count;
        }

        public List<RecipeIngredient> GetMissingIngredients(IInventory inventory)
        {
            var missing = new List<RecipeIngredient>();

            foreach (var ingredient in ingredients)
            {
                int have = inventory.GetItemCount(ingredient.ItemId);
                
                if (ingredient.AcceptAlternatives && ingredient.AlternativeItemIds != null)
                {
                    foreach (var altId in ingredient.AlternativeItemIds)
                        have += inventory.GetItemCount(altId);
                }
                
                if (have < ingredient.Count)
                {
                    missing.Add(new RecipeIngredient
                    {
                        ItemId = ingredient.ItemId,
                        Count = ingredient.Count - have
                    });
                }
            }

            return missing;
        }

        public int GetMaxCraftableCount(IInventory inventory)
        {
            int maxCount = int.MaxValue;

            foreach (var ingredient in ingredients)
            {
                int have = inventory.GetItemCount(ingredient.ItemId);
                
                if (ingredient.AcceptAlternatives && ingredient.AlternativeItemIds != null)
                {
                    foreach (var altId in ingredient.AlternativeItemIds)
                        have += inventory.GetItemCount(altId);
                }
                
                int canMake = have / ingredient.Count;
                maxCount = Mathf.Min(maxCount, canMake);
            }

            return maxCount == int.MaxValue ? 0 : maxCount;
        }

        public string GetStationRequirementText()
        {
            var parts = new List<string>();

            if (requiredStations != null && requiredStations.Length > 0)
            {
                var names = requiredStations
                    .Where(s => s != CraftingStation.None)
                    .Select(s => GetStationName(s));
                if (names.Any())
                    parts.Add("需要: " + string.Join(" 或 ", names));
            }

            if (nearbyStations != null && nearbyStations.Length > 0)
            {
                var names = nearbyStations.Select(s => GetStationName(s));
                parts.Add("邻近: " + string.Join(", ", names));
            }

            return parts.Count > 0 ? string.Join("\n", parts) : "徒手制作";
        }

        public static string GetStationName(CraftingStation station)
        {
            return station switch
            {
                CraftingStation.Workbench => "工作台",
                CraftingStation.Anvil => "铁砧",
                CraftingStation.MythrilAnvil => "秘银砧",
                CraftingStation.Furnace => "熔炉",
                CraftingStation.HellForge => "地狱熔炉",
                CraftingStation.AdamantiteForge => "精金熔炉",
                CraftingStation.AlchemyTable => "炼金台",
                CraftingStation.Loom => "织布机",
                CraftingStation.Sawmill => "锯木厂",
                CraftingStation.CookingPot => "烹饪锅",
                CraftingStation.TinkerTable => "工匠台",
                CraftingStation.BookCase => "书架",
                CraftingStation.CrystalBall => "水晶球",
                CraftingStation.DemonAltar => "恶魔祭坛",
                CraftingStation.WaterSource => "水源",
                CraftingStation.HoneySource => "蜂蜜",
                CraftingStation.LavaSource => "熔岩",
                _ => station.ToString()
            };
        }
    }

    [Serializable]
    public struct RecipeIngredient
    {
        public int ItemId;
        public int Count;
        public bool AcceptAlternatives;
        public int[] AlternativeItemIds;

        [NonSerialized] private Item _template;
        public Item Template
        {
            get
            {
                if (_template == null)
                    _template = ItemDatabase.Instance?.GetItem(ItemId);
                return _template;
            }
        }

        public bool Matches(int itemId)
        {
            if (ItemId == itemId) return true;
            if (AcceptAlternatives && AlternativeItemIds != null)
                return AlternativeItemIds.Contains(itemId);
            return false;
        }
    }

    [Serializable]
    public class BonusOutput
    {
        public int ItemId;
        public int Count = 1;
        [Range(0f, 1f)]
        public float Chance = 0.1f;
    }

    public enum RecipeCategory
    {
        All, Weapons, Armor, Accessories, Tools,
        Consumables, Materials, Furniture, Blocks,
        Lighting, Alchemy, Cooking, Miscellaneous
    }

    public enum CraftingStation
    {
        None, Workbench, Anvil, MythrilAnvil, Furnace,
        HellForge, AdamantiteForge, AlchemyTable, Loom,
        Sawmill, CookingPot, TinkerTable, BookCase,
        CrystalBall, DemonAltar, WaterSource, HoneySource, LavaSource
    }

    public class CraftingContext
    {
        public HashSet<CraftingStation> AvailableStations = new();
        public HashSet<CraftingStation> NearbyStations = new();
        public HashSet<int> UnlockedRecipes = new();
        public int PlayerLevel = 1;
        public Dictionary<string, int> SkillLevels = new();
        
        public CraftingStation CurrentStation
        {
            get => AvailableStations.FirstOrDefault(s => s != CraftingStation.None);
            set
            {
                AvailableStations.Clear();
                AvailableStations.Add(CraftingStation.None);
                AvailableStations.Add(value);
            }
        }

        public int GetSkillLevel(string skillName)
        {
            return SkillLevels?.TryGetValue(skillName, out int level) == true ? level : 0;
        }

        public void AddStation(CraftingStation station, bool isNearby = false)
        {
            AvailableStations.Add(station);
            if (isNearby) NearbyStations.Add(station);
        }

        public void ClearStations()
        {
            AvailableStations.Clear();
            NearbyStations.Clear();
            AvailableStations.Add(CraftingStation.None);
        }
    }

    [CreateAssetMenu(fileName = "RecipeDatabase", menuName = "ItemSystem/Databases/RecipeDatabase")]
    public class RecipeDatabase : ScriptableObject
    {
        private static RecipeDatabase _instance;
        public static RecipeDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<RecipeDatabase>("Databases/RecipeDatabase");
                    _instance?.Initialize();
                }
                return _instance;
            }
        }

        [SerializeField] private Recipe[] allRecipes;

        private Dictionary<int, Recipe> _recipeLookup;
        private Dictionary<int, List<Recipe>> _recipesByOutput;
        private Dictionary<int, List<Recipe>> _recipesByIngredient;
        private Dictionary<RecipeCategory, List<Recipe>> _recipesByCategory;
        private bool _initialized;

        private void OnEnable() => Initialize();

        public void Initialize()
        {
            if (_initialized) return;
            BuildLookups();
            _initialized = true;
        }

        private void BuildLookups()
        {
            _recipeLookup = new Dictionary<int, Recipe>();
            _recipesByOutput = new Dictionary<int, List<Recipe>>();
            _recipesByIngredient = new Dictionary<int, List<Recipe>>();
            _recipesByCategory = new Dictionary<RecipeCategory, List<Recipe>>();

            if (allRecipes == null) return;

            foreach (var recipe in allRecipes)
            {
                if (recipe == null) continue;

                _recipeLookup[recipe.RecipeId] = recipe;

                if (!_recipesByOutput.ContainsKey(recipe.OutputItemId))
                    _recipesByOutput[recipe.OutputItemId] = new List<Recipe>();
                _recipesByOutput[recipe.OutputItemId].Add(recipe);

                foreach (var ingredient in recipe.Ingredients)
                {
                    if (!_recipesByIngredient.ContainsKey(ingredient.ItemId))
                        _recipesByIngredient[ingredient.ItemId] = new List<Recipe>();
                    _recipesByIngredient[ingredient.ItemId].Add(recipe);
                }

                if (!_recipesByCategory.ContainsKey(recipe.Category))
                    _recipesByCategory[recipe.Category] = new List<Recipe>();
                _recipesByCategory[recipe.Category].Add(recipe);
            }
        }

        public Recipe GetRecipe(int recipeId)
        {
            if (_recipeLookup == null) Initialize();
            return _recipeLookup.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }

        public List<Recipe> GetRecipesForItem(int outputItemId)
        {
            if (_recipesByOutput == null) Initialize();
            return _recipesByOutput.TryGetValue(outputItemId, out var recipes)
                ? new List<Recipe>(recipes) : new List<Recipe>();
        }

        public List<Recipe> GetRecipesUsingMaterial(int materialId)
        {
            if (_recipesByIngredient == null) Initialize();
            return _recipesByIngredient.TryGetValue(materialId, out var recipes)
                ? new List<Recipe>(recipes) : new List<Recipe>();
        }

        public List<Recipe> GetRecipesByCategory(RecipeCategory category)
        {
            if (_recipesByCategory == null) Initialize();
            if (category == RecipeCategory.All)
                return allRecipes?.ToList() ?? new List<Recipe>();
            return _recipesByCategory.TryGetValue(category, out var recipes)
                ? new List<Recipe>(recipes) : new List<Recipe>();
        }

        public List<Recipe> GetAvailableRecipes(CraftingStation station, IInventory inventory)
        {
            if (_recipeLookup == null) Initialize();
            return allRecipes?
                .Where(r => r != null)
                .Where(r => r.RequiredStation == station || r.RequiredStation == CraftingStation.None)
                .Where(r => r.Ingredients.Any(i => inventory.HasItem(i.ItemId, 1)))
                .ToList() ?? new List<Recipe>();
        }

        public List<Recipe> GetAvailableRecipes(CraftingContext context, IInventory inventory)
        {
            if (_recipeLookup == null) Initialize();
            return allRecipes?
                .Where(r => r != null)
                .Where(r => {
                    if (r.RequiredStations == null || r.RequiredStations.Length == 0)
                        return true;
                    return r.RequiredStations.Any(s => 
                        s == CraftingStation.None || context.AvailableStations.Contains(s));
                })
                .Where(r => r.Ingredients.Any(i => inventory.HasItem(i.ItemId, 1)))
                .Where(r => !r.RequiresUnlock || context.UnlockedRecipes.Contains(r.RecipeId))
                .ToList() ?? new List<Recipe>();
        }

        public List<Recipe> SearchRecipes(string keyword)
        {
            if (string.IsNullOrEmpty(keyword) || allRecipes == null)
                return new List<Recipe>();
            keyword = keyword.ToLower();
            return allRecipes
                .Where(r => r != null && 
                           (r.RecipeName.ToLower().Contains(keyword) ||
                            r.Description?.ToLower().Contains(keyword) == true))
                .ToList();
        }
    }

    public class CraftingManager
    {
        public event Action<CraftingResult> OnCraftComplete;
        public event Action<CraftingResult> OnCraftFailed;
        public event Action<Recipe> OnRecipeUnlocked;
        public event Action<int> OnCraftingExpGained;

        private readonly IInventory _inventory;
        private readonly CraftingContext _context;
        private readonly System.Random _random;

        private int _craftingExp;
        private int _craftingLevel = 1;
        
        public int CraftingExp => _craftingExp;
        public int CraftingLevel => _craftingLevel;
        public CraftingContext Context => _context;

        public CraftingManager(IInventory inventory)
        {
            _inventory = inventory;
            _context = new CraftingContext();
            _random = new System.Random();
            _context.AvailableStations.Add(CraftingStation.None);
        }

        public void SetStation(CraftingStation station) => _context.CurrentStation = station;

        public void UpdateContext(int playerLevel, Dictionary<string, int> skills)
        {
            _context.PlayerLevel = playerLevel;
            _context.SkillLevels = skills ?? new Dictionary<string, int>();
        }

        public void UnlockRecipe(int recipeId)
        {
            if (_context.UnlockedRecipes.Add(recipeId))
            {
                var recipe = RecipeDatabase.Instance?.GetRecipe(recipeId);
                OnRecipeUnlocked?.Invoke(recipe);
            }
        }

        public CraftingResult TryCraft(Recipe recipe, int count = 1)
        {
            if (!recipe.CanCraft(_inventory, _context))
            {
                var result = new CraftingResult
                {
                    Success = false,
                    Recipe = recipe,
                    MissingIngredients = recipe.GetMissingIngredients(_inventory),
                    ErrorMessage = "无法合成"
                };
                OnCraftFailed?.Invoke(result);
                return result;
            }

            int maxCount = recipe.GetMaxCraftableCount(_inventory);
            count = Mathf.Min(count, maxCount);

            if (count <= 0)
                return new CraftingResult { Success = false, Recipe = recipe, ErrorMessage = "材料不足" };

            bool craftSuccess = recipe.SuccessRate >= 1f || _random.NextDouble() < recipe.SuccessRate;
            if (!craftSuccess)
            {
                var failResult = new CraftingResult { Success = false, Recipe = recipe, ErrorMessage = "合成失败！" };
                OnCraftFailed?.Invoke(failResult);
                return failResult;
            }

            ConsumeIngredients(recipe, count);

            var outputItem = ItemDatabase.Instance?.GetItem(recipe.OutputItemId);
            if (outputItem == null)
                return new CraftingResult { Success = false, Recipe = recipe, ErrorMessage = "产出物品不存在" };

            var outputInstance = outputItem.CreateInstance(recipe.OutputCount * count);
            _inventory.AddItem(outputInstance);

            var bonusItems = ProcessBonusOutputs(recipe, count);
            int expGain = recipe.ExperienceGain * count;
            GainCraftingExp(expGain);

            var successResult = new CraftingResult
            {
                Success = true, Recipe = recipe, CraftedItem = outputInstance,
                CraftedCount = recipe.OutputCount * count, BonusItems = bonusItems, ExpGained = expGain
            };
            OnCraftComplete?.Invoke(successResult);
            return successResult;
        }

        private void ConsumeIngredients(Recipe recipe, int count)
        {
            foreach (var ingredient in recipe.Ingredients)
            {
                int remaining = ingredient.Count * count;
                
                int mainHave = _inventory.GetItemCount(ingredient.ItemId);
                int mainConsume = Mathf.Min(mainHave, remaining);
                if (mainConsume > 0)
                {
                    _inventory.RemoveItem(ingredient.ItemId, mainConsume);
                    remaining -= mainConsume;
                }
                
                if (remaining > 0 && ingredient.AcceptAlternatives && ingredient.AlternativeItemIds != null)
                {
                    foreach (var altId in ingredient.AlternativeItemIds)
                    {
                        if (remaining <= 0) break;
                        int altHave = _inventory.GetItemCount(altId);
                        int altConsume = Mathf.Min(altHave, remaining);
                        if (altConsume > 0)
                        {
                            _inventory.RemoveItem(altId, altConsume);
                            remaining -= altConsume;
                        }
                    }
                }
            }
        }

        private List<ItemInstance> ProcessBonusOutputs(Recipe recipe, int count)
        {
            var bonusItems = new List<ItemInstance>();
            if (recipe.BonusOutputs == null) return bonusItems;

            foreach (var bonus in recipe.BonusOutputs)
            {
                for (int i = 0; i < count; i++)
                {
                    if (_random.NextDouble() < bonus.Chance)
                    {
                        var bonusItem = ItemDatabase.Instance?.GetItem(bonus.ItemId);
                        if (bonusItem != null)
                        {
                            var bonusInstance = bonusItem.CreateInstance(bonus.Count);
                            _inventory.AddItem(bonusInstance);
                            bonusItems.Add(bonusInstance);
                        }
                    }
                }
            }
            return bonusItems;
        }

        public CraftingResult CraftAll(Recipe recipe) => TryCraft(recipe, recipe.GetMaxCraftableCount(_inventory));

        public List<Recipe> GetCraftableRecipes()
        {
            return RecipeDatabase.Instance?.GetAvailableRecipes(_context, _inventory)
                .Where(r => r.CanCraft(_inventory, _context))
                .ToList() ?? new List<Recipe>();
        }

        /// <summary>引导查询：这个材料能合成什么？</summary>
        public List<Recipe> GuideQuery(int materialId) => RecipeDatabase.Instance?.GetRecipesUsingMaterial(materialId) ?? new List<Recipe>();

        /// <summary>反向查询：如何合成这个物品？</summary>
        public List<Recipe> ReverseQuery(int outputItemId) => RecipeDatabase.Instance?.GetRecipesForItem(outputItemId) ?? new List<Recipe>();

        private void GainCraftingExp(int exp)
        {
            _craftingExp += exp;
            OnCraftingExpGained?.Invoke(exp);
            int expForNextLevel = _craftingLevel * 100;
            while (_craftingExp >= expForNextLevel)
            {
                _craftingExp -= expForNextLevel;
                _craftingLevel++;
                expForNextLevel = _craftingLevel * 100;
            }
        }
    }

    public struct CraftingResult
    {
        public bool Success;
        public Recipe Recipe;
        public ItemInstance CraftedItem;
        public int CraftedCount;
        public List<ItemInstance> BonusItems;
        public List<RecipeIngredient> MissingIngredients;
        public string ErrorMessage;
        public int ExpGained;
    }

    public class CraftingStationComponent : MonoBehaviour
    {
        [SerializeField] private CraftingStation stationType;
        [SerializeField] private float interactionRange = 3f;
        [SerializeField] private float nearbyRange = 5f;

        public CraftingStation StationType => stationType;
        public float InteractionRange => interactionRange;
        public float NearbyRange => nearbyRange;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, interactionRange);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, nearbyRange);
        }
    }

    public struct CraftingStationInfo
    {
        public CraftingStation StationType;
        public Vector3 Position;
        public float Distance;
        public bool IsWithinRange;
    }
}
