using Castara.Domain.Casting;

namespace Castara.Application.Abstractions.Repositories;

public sealed class CastingProfileRepositoryOptions
{
    public string FilePath { get; init; } = "";
}

public interface ICastingProfileRespository
{
    Task<IReadOnlyList<CastingProfileDefinition>> GetAllAsync(
     CancellationToken cancellationToken = default);

    Task<CastingProfileDefinition?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
}