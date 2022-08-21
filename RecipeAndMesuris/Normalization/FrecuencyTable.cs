using System.Globalization;
using System.Text;

namespace RecipeAndMesuris.Normalization;

public class FrecuencyTable
{
    private static List<ValuesIngredientMeasures> _ingredient;
    private static List<ValuesIngredientMeasures> _measures;

    static FrecuencyTable()
    {
        _ingredient = new List<ValuesIngredientMeasures>();
        _measures = new List<ValuesIngredientMeasures>();
    }
    
    public static string RemoveAccentsWithNormalization(string inputString)
    {
        var normalizedString = inputString.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var t in from t in normalizedString let uc = CharUnicodeInfo.GetUnicodeCategory(t) where uc != UnicodeCategory.NonSpacingMark select t)
        {
            sb.Append(t);
        }
        return (sb.ToString().Normalize(NormalizationForm.FormC));
    }

    public static void GetTableFrecuencyMeasuresIngredient()
    {

        string[] category = { "Ensaladas", "Entradas", "PlatosFondo", "Postres", "Vegano" };
        foreach (var nameCategory in category)
        {
            var nameRecipe = File.ReadAllLines($"Recipe_insert/Recipe/gourmet/recetas_{nameCategory}.txt");
            foreach (var x in nameRecipe)
            {
                var name = x.Split(";")[0];
                var fileRecipeIngredientMeasures = File.ReadAllLines($"Recipe_insert/ingredient/parcerIngredientes/" +
                                                                     $"ingredientes_Gourmet/{nameCategory}/ingre_{name}.txt");

                foreach (var dataRecipe in fileRecipeIngredientMeasures)
                {
                    var ingredient = RemoveAccentsWithNormalization(dataRecipe.Split(",")[2].ToLower());
                    var measures = RemoveAccentsWithNormalization(dataRecipe.Split(",")[1].ToLower());
                    var resultIngredient = _ingredient.SingleOrDefault(e => e.Name.Equals(ingredient));
                    if (resultIngredient is null) InsertIngredient(ingredient);
                    
                    var resultMeasures = _measures.SingleOrDefault(e => e.Name.Equals(measures));
                    if (resultMeasures is null) InsertMeasures(measures);
                }
            }
        }
        WriteFiles();
    }

    private static void InsertIngredient(string name)
    {
        _ingredient.Add(new ValuesIngredientMeasures(name));
    }

    private static void InsertMeasures(string name)
    {
        _measures.Add(new ValuesIngredientMeasures(name));
    }

    private static void WriteFiles()
    {
        var ingredientOrder = _ingredient.OrderBy(x => x.Name).ToList();
        var measuresOrder = _measures.OrderBy(x => x.Name).ToList();
        var nameIngredient = ingredientOrder.Select(x => x.Name).ToList();
        File.WriteAllLines("C:/Users/Eduardo/RiderProjects/NutriFoods-Latest/RecipeAndMesuris/Normalization/ingredient.csv",ingredientOrder.Select(x => x.Name));
        File.WriteAllLines("C:/Users/Eduardo/RiderProjects/NutriFoods-Latest/RecipeAndMesuris/Normalization//measures.csv",measuresOrder.Select(x => x.Name));
        
    }


}