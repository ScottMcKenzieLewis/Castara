namespace Castara.Application.Abstractions;

public interface IUnitOfWork
{
    Task CommitAsync(CancellationToken ct);
}