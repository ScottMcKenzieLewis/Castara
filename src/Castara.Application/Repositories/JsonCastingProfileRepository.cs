using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AutoMapper;
using Castara.Application.Abstractions.Repositories;
using Castara.Application.DTOs;
using Castara.Domain.Exceptions;
using Microsoft.Extensions.Options;
using Castara.Domain.Estimation.Models.Inputs;

namespace Castara.Application.Repositories;

/// <summary>
/// Repository implementation for loading casting profile definitions from a JSON configuration file.
/// </summary>
/// <remarks>
/// <para>
/// This repository provides read-only access to casting profiles stored in a JSON file,
/// with in-memory caching for performance. The profiles are loaded once and cached for the
/// lifetime of the repository instance.
/// </para>
/// <para>
/// <strong>Configuration:</strong> The JSON file path is configured via
/// <see cref="CastingProfileRepositoryOptions"/> in the application's dependency injection setup.
/// </para>
/// <para>
/// <strong>Data Flow:</strong>
/// <list type="number">
///   <item><description>Load JSON file from configured path</description></item>
///   <item><description>Deserialize to <see cref="CastingProfilesConfig"/> DTO structure</description></item>
///   <item><description>Map DTOs to <see cref="CastingProfileDefinition"/> domain models via AutoMapper</description></item>
///   <item><description>Validate all profile ranges to ensure data integrity</description></item>
///   <item><description>Cache profiles in memory for subsequent requests</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Caching Behavior:</strong> Profiles are loaded once on first access and cached for the
/// lifetime of the repository. This is appropriate for configuration data that doesn't change at runtime.
/// </para>
/// </remarks>
public sealed class JsonCastingProfileRepository : ICastingProfileRepository
{
    // ============================================================
    // Fields
    // ============================================================

    private readonly IMapper _mapper;
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// In-memory cache of loaded and validated casting profiles.
    /// Populated on first access and reused for subsequent requests.
    /// </summary>
    private IReadOnlyList<CastingProfileDefinition>? _cachedProfiles;

    // ============================================================
    // Constructor
    // ============================================================

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonCastingProfileRepository"/> class.
    /// </summary>
    /// <param name="options">
    /// Configuration options containing the JSON file path for casting profiles.
    /// Must contain a non-null <see cref="CastingProfileRepositoryOptions.FilePath"/>.
    /// </param>
    /// <param name="mapper">
    /// AutoMapper instance configured with <see cref="CastingProfileMappingProfile"/> for mapping
    /// <see cref="CastingProfileConfig"/> DTOs to <see cref="CastingProfileDefinition"/> domain models.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="options"/>.Value.FilePath is null or <paramref name="mapper"/> is null.
    /// </exception>
    /// <remarks>
    /// The constructor validates that the file path is configured but does not verify file existence.
    /// File access errors will be raised during the first call to <see cref="GetAllAsync"/>.
    /// </remarks>
    public JsonCastingProfileRepository(
        IOptions<CastingProfileRepositoryOptions> options,
        IMapper mapper)
    {
        ArgumentNullException.ThrowIfNull(options.Value.FilePath);
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _filePath = options.Value.FilePath;
    }

    // ============================================================
    // Public Methods
    // ============================================================

    /// <summary>
    /// Retrieves all available casting profile definitions from the configured JSON file.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the asynchronous operation. Default is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only list
    /// of all <see cref="CastingProfileDefinition"/> instances loaded from configuration.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the configured JSON file cannot be found at the specified path.
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the JSON file cannot be deserialized (invalid format, syntax errors, etc.).
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>The JSON deserializes to null (empty or invalid structure)</description></item>
    ///   <item><description>Any profile fails range validation (min > max, invalid thresholds, etc.)</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Caching:</strong> After the first successful load, profiles are cached in memory.
    /// Subsequent calls return the cached instance without re-reading the file or re-validating.
    /// </para>
    /// <para>
    /// <strong>Validation:</strong> All loaded profiles are validated via
    /// <see cref="CastingProfileDefinition.ValidateRanges"/> to ensure configuration integrity
    /// before being cached and returned.
    /// </para>
    /// <para>
    /// <strong>Thread Safety:</strong> This method is not thread-safe. If called concurrently before
    /// caching completes, multiple file reads may occur. Consider using a singleton lifetime for the
    /// repository in dependency injection configuration.
    /// </para>
    /// </remarks>
    public async Task<IReadOnlyList<CastingProfileDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        // Return cached profiles if already loaded
        if (_cachedProfiles is not null)
        {
            return _cachedProfiles;
        }

        // Load and deserialize JSON configuration
        await using var stream = File.OpenRead(_filePath);

        var config = await JsonSerializer.DeserializeAsync<CastingProfilesConfig>(
            stream,
            _options,
            cancellationToken);

        if (config is null)
        {
            throw new DomainException("Could not load casting profiles.");
        }

        // Map DTOs to domain models
        var profiles = _mapper.Map<List<CastingProfileDefinition>>(config.Profiles);

        // Validate all profiles to ensure configuration integrity
        foreach (var profile in profiles)
        {
            profile.Validate();
        }

        // Cache for subsequent requests
        _cachedProfiles = profiles;
        return _cachedProfiles;
    }

    /// <summary>
    /// Retrieves a specific casting profile definition by its unique identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the profile to retrieve (e.g., "GS_GRAY_30").
    /// Matching is case-insensitive.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the asynchronous operation. Default is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the
    /// <see cref="CastingProfileDefinition"/> with the specified ID, or <c>null</c> if not found.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="id"/> is null, empty, or contains only whitespace.
    /// </exception>
    /// <exception cref="FileNotFoundException">
    /// Thrown when the configured JSON file cannot be found (propagated from <see cref="GetAllAsync"/>).
    /// </exception>
    /// <exception cref="JsonException">
    /// Thrown when the JSON file cannot be deserialized (propagated from <see cref="GetAllAsync"/>).
    /// </exception>
    /// <exception cref="DomainException">
    /// Thrown when profile loading or validation fails (propagated from <see cref="GetAllAsync"/>).
    /// </exception>
    /// <remarks>
    /// <para>
    /// This method internally calls <see cref="GetAllAsync"/> to load profiles if not already cached,
    /// then performs a case-insensitive search for the specified ID.
    /// </para>
    /// <para>
    /// <strong>Performance:</strong> After the first call to either <c>GetAllAsync</c> or <c>GetByIdAsync</c>,
    /// profiles are cached and subsequent ID lookups are in-memory operations (O(n) linear search).
    /// </para>
    /// </remarks>
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