using System.Text.Json;
using System.Text.Json.Serialization;
using API.Dto;
using API.Genetic;
using API.Ingredients;
using API.MealPlans;
using API.Recipes;
using API.Users;
using Domain.DatabaseInitialization;
using Domain.Models;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using NutrientRetrieval.AbridgedRetrieval;
using NutrientRetrieval.NutrientCalculation;
using RecipeAndMesuris.Normalization;
using RecipeAndMesuris.Recipe_insert;
using RecipeInsertion;
using Swashbuckle.AspNetCore.Swagger;



DatabaseInitialization.Initialize();
AbridgedRetrieval.RetrieveFromApi();
Recipes.RecipeInsert();
Recipes.RecipeMeasures();
Recipes.InsertionOfRecipeData();
//NutrientCalculation.Calculate();

/*
DatabaseInitialization.Initialize();
AbridgedRetrieval.RetrieveFromApi();
Connect.InsertMeasuris();
Connect.InsertRecipe();
Connect.InsertRecipeIngredient();
NutrientCalculation.Calculate();
*/
/*
var builder = WebApplication.CreateBuilder(args);


// Add services to the container.

builder.Services.AddFluentValidation();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<NutrifoodsDbContext>(optionsBuilder =>
    {
        if (!optionsBuilder.IsConfigured)
            optionsBuilder.UseNpgsql(builder.Configuration.GetConnectionString("DatabaseConnection"),
                opt => opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery));
    }
);

builder.Services
    .AddScoped<IValidator<UserDto>, UserValidator>()
    .AddScoped<IValidator<UserBodyMetricDto>, UserBodyMetricValidator>()
    .AddScoped<IIngredientRepository, IngredientRepository>()
    .AddScoped<IRecipeRepository, RecipeRepository>()
    .AddScoped<IUserRepository, UserRepository>()
    .AddScoped<IMealPlanService, MealPlanService>();

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

builder.Services.AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme).AddCertificate();

builder.Services.AddFluentValidationRulesToSwagger();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "NutriFoods",
        Description = "The official API for the NutriFoods project"
    });
    options.AddFluentValidationRulesScoped();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(config =>
    {
        config.ConfigObject.AdditionalItems["syntaxHighlight"] = new Dictionary<string, object>
        {
            ["activated"] = false
        };
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
*/