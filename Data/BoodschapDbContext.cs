using Microsoft.EntityFrameworkCore;

namespace Boodschap.Data;

public sealed class BoodschapDbContext(DbContextOptions<BoodschapDbContext> options) : DbContext(options)
{
	public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
	public DbSet<ShoppingListItem> ShoppingListItems => Set<ShoppingListItem>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<ShoppingList>(entity =>
		{
			entity.HasKey(list => list.Id);
			entity.Property(list => list.Name).HasMaxLength(200);
			entity.Property(list => list.Description).HasMaxLength(500);
			entity.HasMany(list => list.Items)
				.WithOne(item => item.ShoppingList)
				.HasForeignKey(item => item.ShoppingListId)
				.OnDelete(DeleteBehavior.Cascade);
		});

		modelBuilder.Entity<ShoppingListItem>(entity =>
		{
			entity.HasKey(item => item.Id);
			entity.Property(item => item.Name).HasMaxLength(200);
		});
	}
}