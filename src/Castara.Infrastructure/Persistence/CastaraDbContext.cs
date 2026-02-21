using Castara.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Castara.Infrastructure.Persistence;

public sealed class CastaraDbContext : DbContext
{
    public CastaraDbContext(DbContextOptions<CastaraDbContext> options) : base(options) { }

    public DbSet<CompositionProfileEntity> CompositionProfiles => Set<CompositionProfileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<CompositionProfileEntity>();

        e.ToTable("composition_profiles");
        e.HasKey(x => x.Id);

        e.Property(x => x.Name).HasMaxLength(200).IsRequired();
        e.HasIndex(x => x.Name);

        // SQLite stores DateTimeOffset as TEXT by default; that’s okay.
        e.Property(x => x.CreatedAt).IsRequired();
    }
}