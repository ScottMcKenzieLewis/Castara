using Castara.Application.Abstractions;

namespace Castara.Infrastructure.Persistence;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly CastaraDbContext _db;
    public EfUnitOfWork(CastaraDbContext db) => _db = db;

    public Task CommitAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}