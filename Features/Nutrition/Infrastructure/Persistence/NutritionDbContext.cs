using Boodschap.Features.Nutrition.Domain;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Nutrition.Infrastructure.Persistence;

public sealed class NutritionDbContext(DbContextOptions<NutritionDbContext> options) : DbContext(options)
{
	public const string MigrationsHistoryTableName = "__NutritionMigrationsHistory";

	public DbSet<Food> Foods => Set<Food>();
	public DbSet<FoodNutrientDetail> FoodNutrientDetails => Set<FoodNutrientDetail>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<Food>(entity =>
		{
			entity.HasKey(food => food.Id);
			entity.Property(food => food.NevoVersion).HasMaxLength(100);
			entity.Property(food => food.FoodGroup).HasMaxLength(250);
			entity.Property(food => food.EnglishFoodGroup).HasMaxLength(250);
			entity.Property(food => food.NevoCode).HasMaxLength(50);
			entity.HasIndex(food => food.NevoCode).IsUnique();
			entity.Property(food => food.Name).HasMaxLength(250);
			entity.HasIndex(food => food.Name);
			entity.Property(food => food.EnglishName).HasMaxLength(250);
			entity.Property(food => food.Quantity).HasMaxLength(100);
			entity.Property(food => food.EnergyKcal).HasPrecision(10, 2);
			entity.Property(food => food.Protein).HasPrecision(10, 2);
			entity.Property(food => food.Carbohydrates).HasPrecision(10, 2);
			entity.Property(food => food.Fat).HasPrecision(10, 2);
			entity.Property(food => food.Fiber).HasPrecision(10, 2);
			entity.HasMany(food => food.NutrientDetails)
				.WithOne(detail => detail.Food)
				.HasForeignKey(detail => detail.FoodId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<FoodNutrientDetail>(entity =>
		{
			entity.HasKey(detail => detail.Id);
			entity.Property(detail => detail.NutrientGroup).HasMaxLength(250);
			entity.Property(detail => detail.ComponentGroup).HasMaxLength(250);
			entity.Property(detail => detail.NutrientCode).HasMaxLength(50);
			entity.Property(detail => detail.NutrientName).HasMaxLength(250);
			entity.Property(detail => detail.Component).HasMaxLength(250);
			entity.Property(detail => detail.RawValue).HasMaxLength(100);
			entity.Property(detail => detail.Value).HasPrecision(18, 6);
			entity.Property(detail => detail.Unit).HasMaxLength(50);
			entity.Property(detail => detail.TraceFortified).HasMaxLength(100);
			entity.Property(detail => detail.SourceCode).HasMaxLength(100);
			entity.Property(detail => detail.Reference).HasMaxLength(2000);
			entity.HasIndex(detail => new { detail.FoodId, detail.NutrientCode }).IsUnique();
		});
	}
}