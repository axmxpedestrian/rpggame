using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ItemSystem.Crafting
{
    using Core;

    /// <summary>
    /// 合成配方
    /// </summary>
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "ItemSystem/Recipe")]
    public class Recipe : ScriptableObject
    {
        [Header("配方信息")]
        [SerializeField] private int recipeId;
        [SerializeField] private string recipeName;

        [Header("输入材料")]
        [SerializeField] private RecipeIngredient[] ingredients;

        [Header("输出物品")]
        [SerializeField] private int outputItemId;
        [SerializeField] private int outputCount = 1;

        [Header("合成条件")]
        [SerializeField] private CraftingStation requiredStation;
        [SerializeField] private int requiredPlayerLevel;
        [SerializeField] private string[] requiredSkills;  // 需要的专业技能
        [SerializeField] private int requiredSkillLevel;

        [Header("合成时间")]
        [SerializeField] private float craftingTime = 0f;  // 0表示即时

        // 属性访问
        public int RecipeId => recipeId;
        public string RecipeName => recipeName;
        public RecipeIngredient[] Ingredients => ingredients;
        public int OutputItemId => outputItemId;
        public int OutputCount => outputCount;
        public CraftingStation RequiredStation => requiredStation;
        public float CraftingTime => craftingTime;

        /// <summary>
        /// 检查是否可以合成
        /// </summary>
        public bool CanCraft(IInventory inventory, CraftingContext context)
        {
            // 检查工作台
            if (requiredStation != CraftingStation.None &&
                context.CurrentStation != requiredStation)
                return false;

            // 检查等级要求
            if (context.PlayerLevel < requiredPlayerLevel)
                return false;

            // 检查技能要求
            if (requiredSkills != null)
            {
                foreach (var skill in requiredSkills)
                {
                    if (context.GetSkillLevel(skill) < requiredSkillLevel)
                        return false;
                }
            }

            // 检查材料
            foreach (var ingredient in ingredients)
            {
                if (!inventory.HasItem(ingredient.ItemId, ingredient.Count))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// 获取缺失的材料列表
        /// </summary>
        public List<RecipeIngredient> GetMissingIngredients(IInventory inventory)
        {
            var missing = new List<RecipeIngredient>();

            foreach (var ingredient in ingredients)
            {
                int have = inventory.GetItemCount(ingredient.ItemId);
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
    }

    /// <summary>
    /// 配方材料
    /// </summary>
    [Serializable]
    public struct RecipeIngredient
    {
        public int ItemId;
        public int Count;

        // 运行时缓存
        [NonSerialized] private Item _template;
        public Item Template
        {
            get
            {
                if (_template == null)
                    _template = ItemDatabase.Instance.GetItem(ItemId);
                return _template;
            }
        }
    }

    /// <summary>
    /// 工作台类型
    /// </summary>
    public enum CraftingStation
    {
        None,           // 无需工作台
        Workbench,      // 工作台
        Anvil,          // 铁砧
        MythrilAnvil,   // 秘银砧
        Furnace,        // 熔炉
        AlchemyTable,   // 炼金台
        Loom,           // 织布机
        Sawmill,        // 锯木厂
        CookingPot,     // 烹饪锅
        TinkerTable     // 工匠台
    }

    /// <summary>
    /// 合成上下文
    /// </summary>
    public class CraftingContext
    {
        public CraftingStation CurrentStation;
        public int PlayerLevel;
        public Dictionary<string, int> SkillLevels;

        public int GetSkillLevel(string skillName)
        {
            return SkillLevels?.TryGetValue(skillName, out int level) == true ? level : 0;
        }
    }

    /// <summary>
    /// 配方数据库
    /// </summary>
    public class RecipeDatabase : ScriptableObject
    {
        private static RecipeDatabase _instance;
        public static RecipeDatabase Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Resources.Load<RecipeDatabase>("Databases/RecipeDatabase");
                return _instance;
            }
        }

        [SerializeField] private Recipe[] allRecipes;

        private Dictionary<int, Recipe> _recipeLookup;
        private Dictionary<int, List<Recipe>> _recipesByOutput;
        private Dictionary<int, List<Recipe>> _recipesByIngredient;

        private void OnEnable()
        {
            BuildLookups();
        }

        private void BuildLookups()
        {
            _recipeLookup = new Dictionary<int, Recipe>();
            _recipesByOutput = new Dictionary<int, List<Recipe>>();
            _recipesByIngredient = new Dictionary<int, List<Recipe>>();

            foreach (var recipe in allRecipes)
            {
                _recipeLookup[recipe.RecipeId] = recipe;

                // 按输出物品索引
                if (!_recipesByOutput.ContainsKey(recipe.OutputItemId))
                    _recipesByOutput[recipe.OutputItemId] = new List<Recipe>();
                _recipesByOutput[recipe.OutputItemId].Add(recipe);

                // 按材料索引
                foreach (var ingredient in recipe.Ingredients)
                {
                    if (!_recipesByIngredient.ContainsKey(ingredient.ItemId))
                        _recipesByIngredient[ingredient.ItemId] = new List<Recipe>();
                    _recipesByIngredient[ingredient.ItemId].Add(recipe);
                }
            }
        }

        public Recipe GetRecipe(int recipeId)
        {
            if (_recipeLookup == null) BuildLookups();
            return _recipeLookup.TryGetValue(recipeId, out var recipe) ? recipe : null;
        }

        /// <summary>
        /// 获取可制作某物品的所有配方
        /// </summary>
        public List<Recipe> GetRecipesForItem(int outputItemId)
        {
            if (_recipesByOutput == null) BuildLookups();
            return _recipesByOutput.TryGetValue(outputItemId, out var recipes)
                ? recipes : new List<Recipe>();
        }

        /// <summary>
        /// 获取使用某材料的所有配方
        /// </summary>
        public List<Recipe> GetRecipesUsingMaterial(int materialId)
        {
            if (_recipesByIngredient == null) BuildLookups();
            return _recipesByIngredient.TryGetValue(materialId, out var recipes)
                ? recipes : new List<Recipe>();
        }

        /// <summary>
        /// 获取当前工作台可用的所有配方
        /// </summary>
        public List<Recipe> GetAvailableRecipes(CraftingStation station, IInventory inventory)
        {
            if (_recipeLookup == null) BuildLookups();

            return allRecipes
                .Where(r => r.RequiredStation == station || r.RequiredStation == CraftingStation.None)
                .Where(r => r.Ingredients.Any(i => inventory.HasItem(i.ItemId, 1)))
                .ToList();
        }
    }

    /// <summary>
    /// 合成管理器
    /// </summary>
    public class CraftingManager
    {
        public event Action<CraftingResult> OnCraftComplete;
        public event Action<CraftingResult> OnCraftFailed;

        private readonly IInventory _inventory;
        private readonly CraftingContext _context;

        public CraftingManager(IInventory inventory)
        {
            _inventory = inventory;
            _context = new CraftingContext();
        }

        public void SetStation(CraftingStation station)
        {
            _context.CurrentStation = station;
        }

        public void UpdateContext(int playerLevel, Dictionary<string, int> skills)
        {
            _context.PlayerLevel = playerLevel;
            _context.SkillLevels = skills;
        }

        /// <summary>
        /// 尝试合成
        /// </summary>
        public CraftingResult TryCraft(Recipe recipe, int count = 1)
        {
            // 验证配方
            if (!recipe.CanCraft(_inventory, _context))
            {
                var result = new CraftingResult
                {
                    Success = false,
                    Recipe = recipe,
                    MissingIngredients = recipe.GetMissingIngredients(_inventory)
                };
                OnCraftFailed?.Invoke(result);
                return result;
            }

            // 消耗材料
            foreach (var ingredient in recipe.Ingredients)
            {
                _inventory.RemoveItem(ingredient.ItemId, ingredient.Count * count);
            }

            // 生成产出物品
            var outputItem = ItemDatabase.Instance.GetItem(recipe.OutputItemId);
            var outputInstance = outputItem.CreateInstance(recipe.OutputCount * count);
            _inventory.AddItem(outputInstance);

            var successResult = new CraftingResult
            {
                Success = true,
                Recipe = recipe,
                CraftedItem = outputInstance,
                CraftedCount = recipe.OutputCount * count
            };

            OnCraftComplete?.Invoke(successResult);
            return successResult;
        }

        /// <summary>
        /// 获取当前可合成的配方
        /// </summary>
        public List<Recipe> GetCraftableRecipes()
        {
            return RecipeDatabase.Instance.GetAvailableRecipes(_context.CurrentStation, _inventory)
                .Where(r => r.CanCraft(_inventory, _context))
                .ToList();
        }
    }

    public struct CraftingResult
    {
        public bool Success;
        public Recipe Recipe;
        public ItemInstance CraftedItem;
        public int CraftedCount;
        public List<RecipeIngredient> MissingIngredients;
        public string ErrorMessage;
    }
}