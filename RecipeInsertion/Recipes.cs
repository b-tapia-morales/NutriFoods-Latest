using System.Collections.Immutable;
using Domain.Enum;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using RecipeInsertion.Mapping;
using Utils.Csv;
using Utils.Enumerable;
using Utils.String;
using static Domain.Enum.DishTypes;
using static Domain.Enum.MealTypes;

namespace RecipeInsertion;

public static class Recipes
{
    private const string ConnectionString =
        "Host=localhost;Database=nutrifoods_db;Username=nutrifoods_dev;Password=MVmYneLqe91$";

    private static readonly DbContextOptions<NutrifoodsDbContext> Options =
        new DbContextOptionsBuilder<NutrifoodsDbContext>()
            .UseNpgsql(ConnectionString,
                builder => builder.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery))
            .Options;

    private static readonly string BaseDirectory =
        Directory.GetParent(Directory.GetCurrentDirectory())!.FullName;

    private static readonly string ProjectDirectory = Path.Combine(BaseDirectory, "RecipeInsertion");
    private static readonly string RecipesPath = Path.Combine(ProjectDirectory, "Recipe", "recipe.csv");
    private static readonly string IngredientMeasuresPath = Path.Combine(ProjectDirectory, "Measures");
    private static readonly string RecipeMeasuresPath = Path.Combine(ProjectDirectory, "DataRecipes", "Ingredient");
    private static readonly string StepsPath = Path.Combine(ProjectDirectory, "DataRecipes", "Steps");

    private static readonly Dictionary<string, MealTypes> MealTypesDict = new()
    {
        ["Desayunos"] = Breakfast,
        ["Ensaladas"] = Lunch,
        ["Entradas"] = Lunch,
        ["PlatosFondo"] = Lunch,
        ["Cenas"] = Dinner,
    };

    private static readonly Dictionary<string, DishTypes> DishTypesDict = new()
    {
        ["Ensaladas"] = Salad,
        ["Entradas"] = Entree,
        ["PlatosFondo"] = MainDish,
        ["Postres"] = Dessert,
        ["Vegano"] = Vegan
    };

    public static void BatchInsert()
    {
        var mappings = RowRetrieval
            .RetrieveRows<Recipe, RecipeMapping>(RecipesPath, DelimiterToken.Semicolon, true)
            .DistinctBy(e => e.Url);
        using var context = new NutrifoodsDbContext(Options);
        InsertAllRecipes(context, mappings);
        var ingredients = IncludeSubfields(context.Ingredients).ToList();
        var recipes = IncludeSubfields(context.Recipes).ToList();
        var measures = IncludeSubfields(context.IngredientMeasures).ToList();
        var ingredientsDict = IngredientDictionary(ingredients).AsReadOnly();
        var recipesDict = RecipeDictionary(recipes).AsReadOnly();
        var measuresDict = MeasureDictionary(measures).AsReadOnly();
        InsertAllCategories(context, recipesDict);
        InsertAllSteps(context, recipesDict);
        InsertAllMeasures(context, recipesDict, ingredientsDict, measuresDict);
    }

    private static void InsertAllRecipes(DbContext context, IEnumerable<Recipe> mappings)
    {
        foreach (var recipe in mappings)
        {
            context.Add(new Recipe
            {
                Name = recipe.Name,
                Author = recipe.Author,
                Url = recipe.Url,
                Portions = recipe.Portions,
                Time = recipe.Time
            });
        }

        context.SaveChanges();
    }

    private static void InsertAllMeasures(DbContext context,
        IReadOnlyDictionary<string, Recipe> recipesDict, IDictionary<string, Ingredient> ingredientsDict,
        IDictionary<(string Measure, string IngredientName), IngredientMeasure> measuresDict)
    {
        var paths = Directory.GetFiles(RecipeMeasuresPath, "*.csv", SearchOption.AllDirectories);
        var usedRecipes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var path in paths)
        {
            var name = path.ExtractFileName().Standardize();

            if (!recipesDict.TryGetValue(name, out var recipe) || usedRecipes.Contains(name))
                continue;

            var recipeId = recipe.Id;

            var recipeIngredients = RowRetrieval
                .RetrieveRows<RecipeIngredient, RecipeIngredientMapping>(path, DelimiterToken.Comma)
                .Where(x => !x.Quantity.Equals("x") && !x.IngredientName.Equals("agua"));
            foreach (var recipeIngredient in recipeIngredients)
                InsertRecipeMeasure(context, recipeIngredient, ingredientsDict, measuresDict, recipeId);

            usedRecipes.Add(name);
        }

        context.SaveChanges();
    }

    private static void InsertRecipeMeasure(DbContext context, RecipeIngredient data,
        IDictionary<string, Ingredient> ingredientsDict,
        IDictionary<(string Measure, string IngredientName), IngredientMeasure> measuresDict, int recipeId)
    {
        if (!ingredientsDict.TryGetValue(data.IngredientName, out var ingredient))
            return;

        var ingredientId = ingredient.Id;
        if (data.MeasureName.Equals("g") || data.MeasureName.Equals("ml") || data.MeasureName.Equals("cc"))
        {
            InsertQuantity(context, recipeId, ingredientId, data.Quantity);
            return;
        }

        if (!measuresDict.TryGetValue((data.MeasureName.Format().Standardize(), data.IngredientName.Standardize()),
                out var measure))
            return;

        ParseMeasure(context, data.Quantity, recipeId, measure.Id);
    }

    private static void InsertAllCategories(DbContext context, IReadOnlyDictionary<string, Recipe> recipesDict)
    {
        var paths = Directory.GetFiles(RecipeMeasuresPath, "*.csv", SearchOption.AllDirectories);
        foreach (var file in paths)
        {
            var name = file.ExtractFileName().Standardize();
            if (!recipesDict.TryGetValue(name, out var recipe))
                continue;

            var mealTypes = MealTypesDict.Where(e => file.Contains(e.Key)).Select(e => e.Value);
            foreach (var mealType in mealTypes)
                recipe.MealTypes.Add(mealType);

            var dishTypes = DishTypesDict.Where(e => file.Contains(e.Key)).Select(e => e.Value);
            foreach (var dishType in dishTypes)
                recipe.DishTypes.Add(dishType);
        }

        context.SaveChanges();
    }

    private static void InsertAllSteps(DbContext context, IReadOnlyDictionary<string, Recipe> recipesDict)
    {
        var stepPaths = Directory.GetFiles(StepsPath, "*.csv", SearchOption.AllDirectories);
        var usedRecipes = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        foreach (var stepPath in stepPaths)
        {
            var recipeName = stepPath.ExtractFileName().Standardize();
            if (!recipesDict.TryGetValue(recipeName, out var recipe) || usedRecipes.Contains(recipeName))
                continue;

            var id = recipe.Id;
            var step = File.ReadAllLines(stepPath);

            for (var i = 0; i < step.Length; i++)
                context.Add(new RecipeStep
                {
                    RecipeId = id,
                    Number = i + 1,
                    Description = step[i]
                });

            usedRecipes.Add(recipeName);
        }

        context.SaveChanges();
    }

    private static void ParseMeasure(DbContext context, string rawQuantity, int recipeId, int measureId)
    {
        rawQuantity = rawQuantity.Trim();
        if (rawQuantity.Contains(' ') && rawQuantity.Contains('/'))
        {
            var mixedFraction = rawQuantity.Split(' ');
            var integerPart = int.Parse(mixedFraction[0]);
            var fractionalPart = mixedFraction[1].Split('/');
            var numerator = int.Parse(fractionalPart[0]);
            var denominator = int.Parse(fractionalPart[1]);
            InsertMeasure(context, recipeId, measureId, integerPart, numerator, denominator);
            return;
        }

        if (rawQuantity.Contains('/'))
        {
            var fractionalPart = rawQuantity.Split('/');
            var numerator = int.Parse(fractionalPart[0]);
            var denominator = int.Parse(fractionalPart[1]);
            InsertMeasure(context, recipeId, measureId, 0, numerator, denominator);
            return;
        }

        InsertMeasure(context, recipeId, measureId, int.Parse(rawQuantity), 1, 1);
    }

    private static void InsertQuantity(DbContext context, int recipeId, int ingredientId, string quantity) =>
        context.Add(new RecipeQuantity
        {
            RecipeId = recipeId,
            IngredientId = ingredientId,
            Grams = double.Parse(quantity)
        });

    private static void InsertMeasure(DbContext context, int recipeId, int measureId, int integerPart,
        int numerator, int denominator)
    {
        context.Add(new RecipeMeasure
        {
            RecipeId = recipeId,
            IngredientMeasureId = measureId,
            IntegerPart = integerPart,
            Numerator = numerator,
            Denominator = denominator
        });
    }

    private static IDictionary<string, Ingredient> IngredientDictionary(IList<Ingredient> ingredients)
    {
        var ingredientsDict = ingredients
            .GroupBy(e => e.Name.Standardize(), StringComparer.InvariantCultureIgnoreCase)
            .ToDictionary(e => e.Key, e => e.First(), StringComparer.InvariantCultureIgnoreCase);
        var synonymsDict = ingredients
            .SelectMany(e => e.Synonyms.Select(x => (Synonym: x, Ingredient: e)))
            .GroupBy(e => e.Synonym.Standardize(), StringComparer.InvariantCultureIgnoreCase)
            .ToDictionary(e => e.Key, e => e.First().Ingredient, StringComparer.InvariantCultureIgnoreCase);
        return ingredientsDict.Merge(synonymsDict);
    }

    private static IDictionary<(string Measure, string IngredientName), IngredientMeasure> MeasureDictionary(
        IList<IngredientMeasure> measures) =>
        measures
            .GroupBy(e => (e.Name.Format().Standardize(), e.Ingredient.Name.Standardize()))
            .ToDictionary(e => e.Key, e => e.First());


    private static IDictionary<string, Recipe> RecipeDictionary(IList<Recipe> recipes) =>
        recipes
            .GroupBy(e => e.Name.Standardize(), StringComparer.InvariantCultureIgnoreCase)
            .ToDictionary(e => e.Key, e => e.First(), StringComparer.InvariantCultureIgnoreCase);

    private static IQueryable<Recipe> IncludeSubfields(this DbSet<Recipe> recipes) =>
        recipes
            .AsQueryable()
            .Include(e => e.NutritionalValues)
            .Include(e => e.RecipeMeasures)
            .ThenInclude(e => e.IngredientMeasure)
            .ThenInclude(e => e.Ingredient)
            .ThenInclude(e => e.NutritionalValues)
            .Include(e => e.RecipeQuantities)
            .ThenInclude(e => e.Ingredient)
            .ThenInclude(e => e.NutritionalValues)
            .Include(e => e.RecipeSteps);

    private static IQueryable<Ingredient> IncludeSubfields(this DbSet<Ingredient> dbSet) =>
        dbSet
            .AsQueryable()
            .Include(e => e.IngredientMeasures)
            .Include(e => e.NutritionalValues);

    private static IQueryable<IngredientMeasure> IncludeSubfields(this DbSet<IngredientMeasure> dbSet) =>
        dbSet
            .AsQueryable()
            .Include(e => e.Ingredient)
            .ThenInclude(e => e.NutritionalValues);

    private static string ExtractFileName(this string path) =>
        path.Split(@"\")[^1].Replace("_", " ").Replace(".csv", "");
}