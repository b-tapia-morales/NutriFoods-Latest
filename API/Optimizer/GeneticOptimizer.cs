// ReSharper disable ClassNeverInstantiated.Global

using API.Dto;
using static API.Optimizer.IEvolutionaryOptimizer<API.Optimizer.GeneticOptimizer>;

namespace API.Optimizer;

public class GeneticOptimizer : IEvolutionaryOptimizer<GeneticOptimizer>
{
    public static IList<RecipeDto> GenerateSolution(IReadOnlyList<RecipeDto> universe,
        IReadOnlyList<NutritionalTargetDto> targets,
        Selection selection, Crossover crossover, Mutation mutation,
        double errorMargin = ErrorMargin, int chromosomeSize = ChromosomeSize,
        int populationSize = PopulationSize, int maxIterations = MaxIterations)
    {
        var maxFitness = CalculateMaximumFitness(targets);
        var population = GenerateInitialPopulation(universe, chromosomeSize, populationSize);
        var winners = new List<Chromosome>();
        CalculatePopulationFitness(population, targets, errorMargin);
        for (var i = 0; i < maxIterations || !SolutionExists(population, maxFitness); i++)
        {
            selection.Method(population, winners);
            crossover.Method(population, winners, chromosomeSize, populationSize);
            mutation.Method(population, universe, chromosomeSize, populationSize);
            CalculatePopulationFitness(population, targets, errorMargin);
        }

        return population.OrderByDescending(e => e.Fitness).First().Recipes;
    }
}