using Boodschap.Features.Nutrition.Domain;
using Boodschap.Features.Nutrition.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Nutrition.Tests;

public sealed class FoodRepositoryTests
{
	[Fact]
	public async Task GetFoodsAsync_ReturnsFoodsOrderedByName()
	{
		await using var harness = await NutritionSqliteTestHarness.CreateAsync();
		await harness.AddFoodsAsync(
			CreateFood("Volkorenbrood"),
			CreateFood("Appel"));

		var repository = new FoodRepository(harness.DbContextFactory);

		var foods = await repository.GetFoodsAsync();

		Assert.Collection(
			foods,
			food => Assert.Equal("Appel", food.Name),
			food => Assert.Equal("Volkorenbrood", food.Name));
	}

	[Fact]
	public async Task SearchFoodsAsync_FiltersByName()
	{
		await using var harness = await NutritionSqliteTestHarness.CreateAsync();
		await harness.AddFoodsAsync(
			CreateFood("Magere yoghurt"),
			CreateFood("Havermout"));

		var repository = new FoodRepository(harness.DbContextFactory);

		var foods = await repository.SearchFoodsAsync("yoghurt");

		var food = Assert.Single(foods);
		Assert.Equal("Magere yoghurt", food.Name);
	}

	[Fact]
	public async Task NutritionDbContext_PersistsNutrientDetailsWithFood()
	{
		await using var harness = await NutritionSqliteTestHarness.CreateAsync();
		await harness.AddFoodsAsync(new Food
		{
			NevoVersion = "NEVO-Online 2025 9.0",
			FoodGroup = "Aardappelen en knolgewassen",
			EnglishFoodGroup = "Potatoes and tubers",
			NevoCode = "1",
			Name = "Aardappelen rauw",
			EnglishName = "Potatoes raw",
			Quantity = "per 100g",
			EnergyKcal = 88m,
			Protein = 2m,
			Carbohydrates = 19m,
			Fat = 0m,
			Fiber = 1.8m,
			NutrientDetails =
			[
				new FoodNutrientDetail
				{
					NutrientGroup = "Energie en macronutriënten",
					ComponentGroup = "Energy and macronutrients",
					NutrientCode = "ENERCC",
					NutrientName = "Energie kcal",
					Component = "Energy kcal",
					RawValue = "88",
					Value = 88m,
					Unit = "kcal",
					SourceCode = "MI0115",
					Reference = "Berekend"
				}
			]
		});

		await using var dbContext = harness.DbContextFactory.CreateDbContext();
		var persistedFood = await dbContext.Foods
			.Include(food => food.NutrientDetails)
			.SingleAsync(food => food.NevoCode == "1");

		Assert.Equal("NEVO-Online 2025 9.0", persistedFood.NevoVersion);
		var detail = Assert.Single(persistedFood.NutrientDetails);
		Assert.Equal("ENERCC", detail.NutrientCode);
		Assert.Equal(88m, detail.Value);
	}

	private static Food CreateFood(string name)
	{
		return new Food
		{
			NevoCode = name,
			Name = name,
			EnergyKcal = 100m,
			Protein = 1m,
			Carbohydrates = 2m,
			Fat = 3m,
			Fiber = 4m
		};
	}
}

file sealed class NutritionSqliteTestHarness : IAsyncDisposable
{
	private readonly SqliteConnection connection;

	private NutritionSqliteTestHarness(SqliteConnection connection, IDbContextFactory<NutritionDbContext> dbContextFactory)
	{
		this.connection = connection;
		DbContextFactory = dbContextFactory;
	}

	public IDbContextFactory<NutritionDbContext> DbContextFactory { get; }

	public static async Task<NutritionSqliteTestHarness> CreateAsync()
	{
		var connection = new SqliteConnection("Data Source=:memory:");
		await connection.OpenAsync();

		var options = new DbContextOptionsBuilder<NutritionDbContext>()
			.UseSqlite(connection)
			.Options;
		var dbContextFactory = new TestNutritionDbContextFactory(options);

		await using var dbContext = dbContextFactory.CreateDbContext();
		await dbContext.Database.EnsureCreatedAsync();

		return new NutritionSqliteTestHarness(connection, dbContextFactory);
	}

	public async Task AddFoodsAsync(params Food[] foods)
	{
		await using var dbContext = DbContextFactory.CreateDbContext();
		dbContext.Foods.AddRange(foods);
		await dbContext.SaveChangesAsync();
	}

	public async ValueTask DisposeAsync()
	{
		await connection.DisposeAsync();
	}

	private sealed class TestNutritionDbContextFactory(DbContextOptions<NutritionDbContext> options) : IDbContextFactory<NutritionDbContext>
	{
		public NutritionDbContext CreateDbContext()
		{
			return new NutritionDbContext(options);
		}
	}
}