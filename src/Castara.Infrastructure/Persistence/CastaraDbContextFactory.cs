using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Castara.Infrastructure.Persistence;

public sealed class CastaraDbContextFactory : IDesignTimeDbContextFactory<CastaraDbContext>
{
    public CastaraDbContext CreateDbContext(string[] args)
    {
        // Use a stable path for design-time runs (solution root / local file)
        var dbPath = Path.Combine(AppContext.BaseDirectory, "castara.design.db");

        var options = new DbContextOptionsBuilder<CastaraDbContext>()
            .UseSqlite($"Data Source={dbPath}")
            .Options;

        return new CastaraDbContext(options);
    }
}