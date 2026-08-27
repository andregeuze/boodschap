using Boodschap.Features.Authentication.Domain;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Boodschap.Features.Authentication.Infrastructure.Persistence;

public sealed class AuthenticationDbContext(DbContextOptions<AuthenticationDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
	public const string MigrationsHistoryTableName = "__AuthenticationMigrationsHistory";

	public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
	public DbSet<LocalUser> LocalUsers => Set<LocalUser>();

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.Entity<DataProtectionKey>(entity =>
		{
			entity.ToTable("DataProtectionKeys");
			entity.HasKey(key => key.Id);
		});

		modelBuilder.Entity<LocalUser>(entity =>
		{
			entity.ToTable("LocalUsers");
			entity.HasKey(user => user.Id);
			entity.Property(user => user.Username).HasMaxLength(200);
			entity.Property(user => user.NormalizedUsername).HasMaxLength(200);
			entity.Property(user => user.PasswordHash).HasMaxLength(1024);
			entity.HasIndex(user => user.NormalizedUsername).IsUnique();
		});
	}
}