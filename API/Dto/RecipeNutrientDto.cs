namespace API.Dto;

public class RecipeNutrientDto
{
    public string Nutrient { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
}