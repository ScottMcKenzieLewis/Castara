namespace Castara.Application.Abstractions.Repositories;

using Castara.Application.DTOs;

public interface ICompositionProfileRepository
{
    Task<CompositionProfileDto?> GetAsync(Guid id, CancellationToken ct);
    Task<IReadOnlyList<CompositionProfileDto>> SearchByNameAsync(string nameLike, CancellationToken ct);
    Task SaveAsync(CompositionProfileDto profile, CancellationToken ct);
}