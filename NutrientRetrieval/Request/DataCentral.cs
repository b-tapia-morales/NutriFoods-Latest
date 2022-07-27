using Newtonsoft.Json;
using NutrientRetrieval.Dictionaries;
using NutrientRetrieval.Food;
using Utils.Enumerable;

namespace NutrientRetrieval.Request;

public static class DataCentral
{
    private const int MaxItemsPerRequest = 20;
    private const string ApiKey = "aLGkW4nbdeEhoFefi68nOYLNPaSXhiSjO7bIBzQk";

    private static readonly HttpClient Client = new();

    public static async Task<(int Id, T? Food)> SingleRequest<T>(int nutriFoodsId, int foodDataCentralId, string format)
        where T : IFood
    {
        var path = $"https://api.nal.usda.gov/fdc/v1/food/{foodDataCentralId}?format={format}&api_key={ApiKey}";
        var uri = new Uri(path);

        var response = await Client.GetAsync(uri);
        var serialized = await response.Content.ReadAsStringAsync();
        var food = JsonConvert.DeserializeObject<T>(serialized);

        return (nutriFoodsId, food);
    }

    public static async Task<IEnumerable<(int Id, T Food)>> MultipleRequests<T>(
        Dictionary<int, int> dictionary, string format) where T : IFood
    {
        var ids = string.Join('&', dictionary.Select(e => $"fdcIds={e.Key}"));
        var path = $"https://api.nal.usda.gov/fdc/v1/food/{ids}?format={format}&api_key={ApiKey}";
        var uri = new Uri(path);

        var response = await Client.GetAsync(uri);
        var serialized = await response.Content.ReadAsStringAsync();
        var list = JsonConvert.DeserializeObject<List<T>>(serialized) ?? throw new InvalidOperationException();

        return list.Select(e => (dictionary[e.FdcId()], e));
    }

    public static async Task<Dictionary<int, T?>> SingleRetrieval<T>(string format) where T : IFood
    {
        var dictionary = IngredientRetrieval.RetrieveRows()
            .ToDictionary(e => e.NutriFoodsId, e => e.FoodDataCentralId);
        var tasks = dictionary.Select(e => SingleRequest<T>(e.Key, e.Value, format));
        var tuples = await Task.WhenAll(tasks);
        return tuples.ToDictionary(tuple => tuple.Id, tuple => tuple.Food);
    }

    public static async Task<Dictionary<int, T>> MultipleRetrievals<T>(string format) where T : IFood
    {
        var dictionary = IngredientRetrieval.RetrieveRows()
            .ToDictionary(e => e.FoodDataCentralId, e => e.NutriFoodsId);
        var dictionaryList = EnumerableUtils.Partition(dictionary, MaxItemsPerRequest);
        var tasks = dictionaryList.Select(e => MultipleRequests<T>(new Dictionary<int, int>(e), format));
        var tuplesList = await Task.WhenAll(tasks);
        return tuplesList.SelectMany(e => e).ToDictionary(e => e.Id, e => e.Food);
    }
}