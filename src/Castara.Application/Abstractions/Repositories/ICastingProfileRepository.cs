using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Castara.Domain.Estimation.Models.Inputs;

namespace Castara.Application.Abstractions.Repositories;

/// <summary>
/// Configuration options for the casting profile repository.
/// </summary>
/// <remarks>
/// <para>
/// This options class is used with the Options pattern in ASP.NET Core / .NET dependency injection
/// to configure the location of the casting profiles JSON configuration file.
/// </para>
/// <para>
/// <strong>Configuration Example (appsettings.json):</strong>
/// <code>
/// {
///   "CastingProfileRepository": {
///     "FilePath": "Configuration/casting-profiles.json"
///   }
/// }
/// </code>
/// </para>
/// <para>
/// <strong>Registration Example:</strong>
/// <code>
/// services.Configure&lt;CastingProfileRepositoryOptions&gt;(
///     configuration.GetSection("CastingProfileRepository"));
/// </code>
/// </para>
/// </remarks>
public sealed class CastingProfileRepositoryOptions
{
    /// <summary>
    /// Gets or sets the file path to the casting profiles JSON configuration file.
    /// </summary>
    /// <value>
    /// The absolute or relative path to the JSON file containing casting profile definitions.
    /// Can be relative to the application's base directory.
    /// Default is an empty string (must be configured).
    /// </value>
    /// <remarks>
    /// <para>
    /// This path is required for the repository to function. The application will throw an
    /// <see cref="System.ArgumentNullException"/> if this value is not configured.
    /// </para>
    /// <para>
    /// <strong>Example Values:</strong>
    /// <list type="bullet">
    ///   <item><description><c>"Configuration/casting-profiles.json"</c> - Relative path</description></item>
    ///   <item><description><c>"C:\Config\profiles.json"</c> - Absolute path</description></item>
    ///   <item><description><c>"./data/profiles.json"</c> - Current directory relative</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public string FilePath { get; set; } = "";
}

/// <summary>
/// Defines a repository contract for loading and accessing casting profile definitions.
/// </summary>
/// <remarks>
/// <para>
/// This repository provides read-only access to casting profile definitions, which contain
/// process-specific constraints, defaults, and tuning parameters for cast iron estimation.
/// </para>
/// <para>
/// <strong>Purpose:</strong> Abstracts the storage mechanism for casting profiles, allowing
/// different implementations (JSON files, database, embedded resources, etc.) without affecting
/// consuming code.
/// </para>
/// <para>
/// <strong>Typical Implementations:</strong>
/// <list type="bullet">
///   <item><description>JSON file-based repository (primary implementation)</description></item>
///   <item><description>Database repository (for dynamic profile management)</description></item>
///   <item><description>In-memory repository (for testing)</description></item>
///   <item><description>Remote API repository (for centralized configuration)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage Pattern:</strong> Profiles are typically loaded at application startup and
/// cached for the application's lifetime, as they represent relatively static configuration data.
/// </para>
/// </remarks>
public interface ICastingProfileRepository
{
    /// <summary>
    /// Retrieves all available casting profile definitions from the configured source.
    /// </summary>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the asynchronous operation.
    /// Default is <see cref="CancellationToken.None"/>
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a read-only
    /// collection of all available <see cref="CastingProfileDefinition"/> instances.
    /// The collection is never null but may be empty if no profiles are configured.
    /// </returns>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Thrown by file-based implementations when the configuration file cannot be found.
    /// </exception>
    /// <exception cref="System.Text.Json.JsonException">
    /// Thrown by JSON-based implementations when the configuration cannot be deserialized.
    /// </exception>
    /// <exception cref="Castara.Domain.Exceptions.DomainException">
    /// Thrown when:
    /// <list type="bullet">
    ///   <item><description>Profile data is invalid or corrupted</description></item>
    ///   <item><description>Profile validation fails (min > max, invalid ranges, etc.)</description></item>
    ///   <item><description>Required configuration is missing or incomplete</description></item>
    /// </list>
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Performance Consideration:</strong> Implementations typically cache the loaded
    /// profiles in memory after the first successful load, making subsequent calls very fast.
    /// </para>
    /// <para>
    /// <strong>Data Integrity:</strong> All returned profiles should be validated to ensure their
    /// configuration is internally consistent (ranges properly ordered, positive values where
    /// required, etc.).
    /// </para>
    /// </remarks>
    Task<IReadOnlyList<CastingProfileDefinition>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a specific casting profile definition by its unique identifier.
    /// </summary>
    /// <param name="id">
    /// The unique identifier of the profile to retrieve (e.g., "GS_GRAY_30", "NB_GRAY_HIGHPROD").
    /// Must not be null, empty, or whitespace.
    /// </param>
    /// <param name="cancellationToken">
    /// A cancellation token to cancel the asynchronous operation.
    /// Default is <see cref="CancellationToken.None"/>
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains:
    /// <list type="bullet">
    ///   <item><description>The <see cref="CastingProfileDefinition"/> with the specified ID if found</description></item>
    ///   <item><description><c>null</c> if no profile with the specified ID exists</description></item>
    /// </list>
    /// </returns>
    /// <exception cref="System.ArgumentException">
    /// Thrown when <paramref name="id"/> is null, empty, or contains only whitespace.
    /// </exception>
    /// <exception cref="System.IO.FileNotFoundException">
    /// Thrown by file-based implementations when the configuration file cannot be found
    /// (propagated from <see cref="GetAllAsync"/>).
    /// </exception>
    /// <exception cref="System.Text.Json.JsonException">
    /// Thrown by JSON-based implementations when the configuration cannot be deserialized
    /// (propagated from <see cref="GetAllAsync"/>).
    /// </exception>
    /// <exception cref="Castara.Domain.Exceptions.DomainException">
    /// Thrown when profile loading or validation fails (propagated from <see cref="GetAllAsync"/>).
    /// </exception>
    /// <remarks>
    /// <para>
    /// <strong>Case Sensitivity:</strong> Implementations should perform case-insensitive matching
    /// for profile IDs to provide a better user experience and reduce configuration errors.
    /// </para>
    /// <para>
    /// <strong>Performance:</strong> This method typically loads all profiles via <see cref="GetAllAsync"/>
    /// (which caches them), then performs an in-memory search. After the first call, lookups are very fast.
    /// </para>
    /// <para>
    /// <strong>Null Handling:</strong> A return value of <c>null</c> indicates the profile was not found,
    /// allowing calling code to distinguish between missing profiles and other error conditions.
    /// </para>
    /// </remarks>
    Task<CastingProfileDefinition?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);
}