namespace Castara.Application.UseCases.SaveCompositionProfile;

using Castara.Application.Abstractions;
using Castara.Application.Abstractions.Repositories;
using Castara.Application.Abstractions.Services;
using Castara.Application.DTOs;
using Castara.Application.Validation;

public sealed class SaveCompositionProfileHandler
{
    private readonly ICompositionProfileRepository _repo;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public SaveCompositionProfileHandler(
        ICompositionProfileRepository repo,
        IUnitOfWork uow,
        IClock clock)
    {
        _repo = repo;
        _uow = uow;
        _clock = clock;
    }

    public async Task<Guid> HandleAsync(SaveCompositionProfileRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Profile name is required.", nameof(request.Name));

        CompositionValidator.ValidateOrThrow(request.Composition);

        var id = Guid.NewGuid();
        var profile = new CompositionProfileDto(
            Id: id,
            Name: request.Name.Trim(),
            Composition: request.Composition,
            CreatedAt: _clock.UtcNow
        );

        await _repo.SaveAsync(profile, ct);
        await _uow.CommitAsync(ct);

        return id;
    }
}