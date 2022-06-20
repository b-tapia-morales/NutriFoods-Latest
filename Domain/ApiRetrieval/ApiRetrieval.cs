using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Domain.Models;

namespace Domain.ApiRetrieval;

public class ApiRetrieval
{
    private readonly NutrifoodsDbContext _context;

    public ApiRetrieval(NutrifoodsDbContext context)
    {
        _context = context;
    }

    public static Dictionary<string, int> CreateDictionaryIds()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var path = Path.Combine(currentDirectory, "Schema", "MapeoNutrientsID.out.csv");
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Encoding = Encoding.UTF8,
            Delimiter = ";",
            HasHeaderRecord = true
        };

        using var textReader = new StreamReader(path, Encoding.UTF8);
        using var csv = new CsvReader(textReader, configuration);
        csv.Context.RegisterClassMap<RowMapping>();
        return csv.GetRecords<CsvRow>().ToDictionary(record => record.FoodDataCentralId, record => record.NutriFoodsId);
    }

    public sealed class CsvRow
    {
        public string FoodDataCentralName { get; set; }
        public string FoodDataCentralId { get; set; }
        public string NutriFoodsName { get; set; }
        public int NutriFoodsId { get; set; }
    }
    
    public sealed class RowMapping: ClassMap<CsvRow>
    {
        public RowMapping()
        {
            Map(p => p.FoodDataCentralName).Index(0);
            Map(p => p.FoodDataCentralId).Index(1);
            Map(p => p.NutriFoodsName).Index(2).Optional();
            Map(p => p.NutriFoodsId).Index(3).Optional();
        }
    }
}