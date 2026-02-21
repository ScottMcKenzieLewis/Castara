using Castara.Application.Abstractions.Repositories;
using Castara.Application.DTOs;
using Castara.Infrastructure.Persistence;
using Castara.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Castara.Infrastructure.Repositories;

public sealed class CompositionProfileRepository : ICompositionProfileRepository
{
    private readonly CastaraDbContext _db;

    public CompositionProfileRepository(CastaraDbContext db) => _db = db;

    public async Task<CompositionProfileDto?> GetAsync(Guid id, CancellationToken ct)
    {
        var e = await _db.CompositionProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return e is null ? null : MapToDto(e);
    }

    public async Task<IReadOnlyList<CompositionProfileDto>> SearchByNameAsync(string nameLike, CancellationToken ct)
    {
        nameLike = nameLike?.Trim() ?? "";

        var list = await _db.CompositionProfiles.AsNoTracking()
            .Where(x => x.Name.Contains(nameLike))
            .OrderBy(x => x.Name)
            .Take(100)
            .ToListAsync(ct);

        return list.Select(MapToDto).ToList();
    }

    public async Task SaveAsync(CompositionProfileDto profile, CancellationToken ct)
    {
        var e = new CompositionProfileEntity
        {
            Id = profile.Id,
            Name = profile.Name,
            CreatedAt = profile.CreatedAt,
            Carbon = profile.Composition.Carbon,
            Silicon = profile.Composition.Silicon,
            Manganese = profile.Composition.Manganese,
            Phosphorus = profile.Composition.Phosphorus,
            Sulfur = profile.Composition.Sulfur,
            Chromium = profile.Composition.Chromium,
            Nickel = profile.Composition.Nickel,
            Molybdenum = profile.Composition.Molybdenum
        };

        // Upsert-ish
        var exists = await _db.CompositionProfiles.AnyAsync(x => x.Id == e.Id, ct);
        if (!exists)
            await _db.CompositionProfiles.AddAsync(e, ct);
        else
            _db.CompositionProfiles.Update(e);
    }

    private static CompositionProfileDto MapToDto(CompositionProfileEntity e) =>
        new(
            e.Id,
            e.Name,
            new CompositionDto(e.Carbon, e.Silicon, e.Manganese, e.Phosphorus, e.Sulfur, e.Chromium, e.Nickel, e.Molybdenum),
            e.CreatedAt
        );
}