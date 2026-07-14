using Boodschap.Features.Nutrition.Domain;
using Boodschap.Features.Nutrition.Infrastructure.Import;
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

	[Fact]
	public async Task UpsertFoodsAsync_InsertsNewFoodsAndUpdatesExistingFoods()
	{
		await using var harness = await NutritionSqliteTestHarness.CreateAsync();
		await harness.AddFoodsAsync(new Food
		{
			NevoCode = "1",
			Name = "Oude aardappel",
			NutrientDetails =
			[
				new FoodNutrientDetail
				{
					NutrientCode = "OLD",
					NutrientName = "Old nutrient",
					Component = "Old nutrient",
					RawValue = "1",
					Value = 1m,
					Unit = "g"
				}
			]
		});
		var repository = new FoodRepository(harness.DbContextFactory);

		await repository.UpsertFoodsAsync(
		[
			new Food
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
				NutrientDetails = [CreateDetail("ENERCC", "88", 88m, "kcal")]
			},
			new Food
			{
				NevoCode = "2",
				Name = "Banaan",
				EnergyKcal = 89m,
				Protein = 1.1m,
				Carbohydrates = 23m,
				Fat = 0.3m,
				Fiber = 2.6m,
				NutrientDetails = [CreateDetail("FIBT", "2,6", 2.6m, "g")]
			}
		]);

		await using var dbContext = harness.DbContextFactory.CreateDbContext();
		var foods = await dbContext.Foods
			.Include(food => food.NutrientDetails)
			.OrderBy(food => food.NevoCode)
			.ToListAsync();

		Assert.Collection(
			foods,
			food =>
			{
				Assert.Equal("Aardappelen rauw", food.Name);
				Assert.Equal(88m, food.EnergyKcal);
				var detail = Assert.Single(food.NutrientDetails);
				Assert.Equal("ENERCC", detail.NutrientCode);
			},
			food =>
			{
				Assert.Equal("Banaan", food.Name);
				var detail = Assert.Single(food.NutrientDetails);
				Assert.Equal("FIBT", detail.NutrientCode);
			});
	}

	[Fact]
	public async Task UpsertFoodsAsync_AllowsDuplicateNutrientCodesInDifferentGroups()
	{
		await using var harness = await NutritionSqliteTestHarness.CreateAsync();
		await harness.AddFoodsAsync(new Food
		{
			NevoCode = "1",
			Name = "Oude aardappel"
		});
		var repository = new FoodRepository(harness.DbContextFactory);

		await repository.UpsertFoodsAsync(
		[
			new Food
			{
				NevoCode = "1",
				Name = "Aardappelen rauw",
				NutrientDetails =
				[
					CreateDetail("Energie en macronutrienten", "Energy and macronutrients", "PROT", "2", 2m, "g"),
					CreateDetail("Eiwitten", "Protein", "PROT", "2", 2m, "g")
				]
			}
		]);

		await using var dbContext = harness.DbContextFactory.CreateDbContext();
		var persistedFood = await dbContext.Foods
			.Include(food => food.NutrientDetails)
			.SingleAsync(food => food.NevoCode == "1");

		Assert.Equal("Aardappelen rauw", persistedFood.Name);
		var proteinDetails = persistedFood.NutrientDetails
			.Where(detail => detail.NutrientCode == "PROT")
			.OrderBy(detail => detail.NutrientGroup)
			.ToList();
		Assert.Collection(
			proteinDetails,
			detail => Assert.Equal("Eiwitten", detail.NutrientGroup),
			detail => Assert.Equal("Energie en macronutrienten", detail.NutrientGroup));
	}

	[Fact]
	public async Task UpsertFoodsAsync_PersistsImportedNevoFoodWithDuplicateNutrientCodes()
	{
		await using var harness = await NutritionSqliteTestHarness.CreateAsync();
		var importer = new NevoDetailsCsvImporter();
		var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "NEVO2025_v9.0_Details.csv");
		var potatoes = importer.ReadFoods(fixturePath).Single(food => food.NevoCode == "1");
		var repository = new FoodRepository(harness.DbContextFactory);

		await repository.UpsertFoodsAsync([potatoes]);

		await using var dbContext = harness.DbContextFactory.CreateDbContext();
		var persistedFood = await dbContext.Foods
			.Include(food => food.NutrientDetails)
			.SingleAsync(food => food.NevoCode == "1");

		Assert.Equal("Aardappelen rauw", persistedFood.Name);
		Assert.True(persistedFood.NutrientDetails.Count > 100);
		Assert.Equal(2, persistedFood.NutrientDetails.Count(detail => detail.NutrientCode == "PROT"));
		Assert.Equal(2, persistedFood.NutrientDetails.Count(detail => detail.NutrientCode == "CHO"));
		Assert.Equal(2, persistedFood.NutrientDetails.Count(detail => detail.NutrientCode == "FIBT"));
	}

	[Fact]
	public async Task UpsertFoodsAsync_PersistsReportedSmallNevoDetailsCsv()
	{
		await using var harness = await NutritionSqliteTestHarness.CreateAsync();
		var importer = new NevoDetailsCsvImporter();
		var fixturePath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "NEVO2025_v9.0_Details_SMALL.csv");
		var foods = importer.ReadFoods(fixturePath);
		var repository = new FoodRepository(harness.DbContextFactory);

		await repository.UpsertFoodsAsync(foods);

		await using var dbContext = harness.DbContextFactory.CreateDbContext();
		var persistedFood = await dbContext.Foods
			.Include(food => food.NutrientDetails)
			.SingleAsync(food => food.NevoCode == "1");

		Assert.Equal("Aardappelen rauw", persistedFood.Name);
		Assert.Equal(88m, persistedFood.EnergyKcal);
		Assert.True(persistedFood.NutrientDetails.Count > 100);
		Assert.Equal(2, persistedFood.NutrientDetails.Count(detail => detail.NutrientCode == "PROT"));
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

	private static FoodNutrientDetail CreateDetail(string nutrientCode, string rawValue, decimal value, string unit)
	{
		return new FoodNutrientDetail
		{
			NutrientCode = nutrientCode,
			NutrientName = nutrientCode,
			Component = nutrientCode,
			RawValue = rawValue,
			Value = value,
			Unit = unit
		};
	}

	private static FoodNutrientDetail CreateDetail(
		string nutrientGroup,
		string componentGroup,
		string nutrientCode,
		string rawValue,
		decimal value,
		string unit)
	{
		return new FoodNutrientDetail
		{
			NutrientGroup = nutrientGroup,
			ComponentGroup = componentGroup,
			NutrientCode = nutrientCode,
			NutrientName = nutrientCode,
			Component = nutrientCode,
			RawValue = rawValue,
			Value = value,
			Unit = unit
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