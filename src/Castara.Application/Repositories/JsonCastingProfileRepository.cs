using AutoMapper;
using Castara.Application.Abstractions.Repositories;
using Castara.Application.DTOs;
using Castara.Domain.Casting;
using Castara.Domain.Exceptions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Castara.Application.Repositories;

public sealed class JsonCastingProfileRepository : ICastingProfileRespository
{
    private readonly IMapper _mapper;

    private readonly string _filePath;
    private readonly JsonSerializerOptions _options =
        new() { PropertyNameCaseInsensitive = true };

    private IReadOnlyList<CastingProfileDefinition>? _cachedProfiles;

    public JsonCastingProfileRepository(IOptions<CastingProfileRepositoryOptions> options,
    IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(options.Value.FilePath);
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _filePath = options.Value.FilePath;
    }

    public async Task<IReadOnlyList<CastingProfileDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cachedProfiles is not null)
        {
            return _cachedProfiles;
        }

        await using var stream = File.OpenRead(_filePath);

        var config = await JsonSerializer.DeserializeAsync<CastingProfilesConfig>(
            stream,
            _options,
            cancellationToken);

        if (config is null)
        {
            throw new DomainException("Could not load casting profiles.");
        }

        var profiles = _mapper.Map<List<CastingProfileDefinition>>(config.Profiles);

        foreach (var profile in profiles)
        {
            profile.ValidateRanges();
        }

        _cachedProfiles = profiles;
        return _cachedProfiles;
    }

    public async Task<CastingProfileDefinition?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var profiles = await GetAllAsync(cancellationToken);

        return profiles.FirstOrDefault(x =>
            x.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
    }
}