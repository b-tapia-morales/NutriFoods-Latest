using API.Dto;
using Utils.Nutrition;
using static Utils.Nutrition.Macronutrient;

namespace API.Genetic;

public interface IGeneticAlgorithm
{
    DailyMenuDto GenerateSolution(IEnumerable<RecipeDto> recipes, double energy, double carbohydrates, double lipids,
        double proteins, int chromosomeSize = 3, double marginOfError = 0.08, int populationSize = 20)
    {
        var population = new List<Chromosome>();
        var winners = new List<Chromosome>();
        var menus = GenerateUniverse(recipes);
        GenerateInitialPopulation(menus, population, chromosomeSize, populationSize);
        CalculatePopulationFitness(population, energy, carbohydrates, lipids, proteins, marginOfError);
        var i = 0;
        while (!SolutionExists(population))
        {
            Selection(population, winners);
            Crossover(population, winners, populationSize);
            Mutation(menus, population, chromosomeSize, populationSize);
            CalculatePopulationFitness(population, energy, carbohydrates, lipids, proteins, marginOfError);
            i++;
        }

        Console.WriteLine("Generaciones : " + i);
        ShowPopulation(population);

        return population.First(p => p.Fitness == 8).Recipes;
    }

    DailyMenuDto GenerateSolution(IEnumerable<RecipeDto> recipes, double energy, int chromosomeSize = 3,
        double marginOfError = 0.08, int populationSize = 20)
    {
        var (carbohydrates, lipids, proteins) = EnergyDistribution.Calculate(energy);
        return GenerateSolution(recipes, energy, carbohydrates, lipids, proteins, chromosomeSize, marginOfError,
            populationSize);
    }

    DailyMenuDto GenerateCustomSolution(IEnumerable<RecipeDto> recipes, double energy, double carbsPercent,
        double fatsPercent, double proteinsPercent, int chromosomeSize = 3, double marginOfError = 0.08,
        int populationSize = 20)
    {
        carbsPercent = Math.Round(carbsPercent, 2);
        fatsPercent = Math.Round(fatsPercent, 2);
        proteinsPercent = Math.Round(proteinsPercent, 2);
        if (carbsPercent < Carbohydrates.MinPercent || fatsPercent < Lipids.MinPercent ||
            proteinsPercent < Proteins.MinPercent)
            throw new ArgumentException("One of the percentages is below the threshold");
        if (carbsPercent > Carbohydrates.MaxPercent || fatsPercent > Lipids.MaxPercent ||
            proteinsPercent > Proteins.MaxPercent)
            throw new ArgumentException("One of the percentages is above the threshold");
        if (Math.Abs(1 - (carbsPercent + fatsPercent + proteinsPercent)) >= 1e-2)
            throw new ArgumentException("The sum of the percentages does not equal to one");
        var (carbohydrates, lipids, proteins) =
            EnergyDistribution.Calculate(energy, carbsPercent, fatsPercent, proteinsPercent);
        return GenerateSolution(recipes, energy, carbohydrates, lipids, proteins, chromosomeSize: chromosomeSize,
            marginOfError: marginOfError, populationSize: populationSize);
    }

    IList<MenuRecipeDto> GenerateUniverse(IEnumerable<RecipeDto> recipes);

    void GenerateInitialPopulation(IList<MenuRecipeDto> universe, IList<Chromosome> population, int chromosomeSize,
        int populationSize);

    void CalculatePopulationFitness(IList<Chromosome> population, double energy, double carbohydrates, double lipids,
        double proteins, double marginOfError);

    bool SolutionExists(IList<Chromosome> population);

    void Selection(IList<Chromosome> population, IList<Chromosome> winners);

    void Crossover(IList<Chromosome> population, IList<Chromosome> winners, int populationSize);

    void Mutation(IList<MenuRecipeDto> menus, IList<Chromosome> population, int chromosomeSize, int populationSize);

    void ShowPopulation(IList<Chromosome> population);
}