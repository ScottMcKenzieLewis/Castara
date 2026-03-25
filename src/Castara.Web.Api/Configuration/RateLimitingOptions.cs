namespace Castara.Api.Configuration;

/// <summary>
/// Configuration options for API rate limiting to protect against excessive request rates.
/// </summary>
/// <remarks>
/// These settings control the rate limiting behavior using a fixed window algorithm,
/// which limits the number of requests allowed within a specified time window.
/// Rate limiting helps protect the API from abuse, ensures fair resource allocation,
/// and maintains service availability for all clients.
/// </remarks>
public sealed class RateLimitingOptions
{
    /// <summary>
    /// Gets or sets the maximum number of requests permitted within the time window.
    /// </summary>
    /// <value>
    /// The number of requests allowed per window. For example, a value of 100 allows
    /// up to 100 requests within the specified window duration.
    /// </value>
    /// <remarks>
    /// Once this limit is reached within a window, additional requests will be rejected
    /// or queued (depending on queue availability) until the window resets.
    /// </remarks>
    public int PermitLimit { get; set; }

    /// <summary>
    /// Gets or sets the duration of the rate limiting window in seconds.
    /// </summary>
    /// <value>
    /// The window duration in seconds. For example, a value of 60 creates a 60-second window.
    /// </value>
    /// <remarks>
    /// The window resets after this duration, allowing a new set of requests up to the permit limit.
    /// Common values include 60 (1 minute), 300 (5 minutes), or 3600 (1 hour).
    /// </remarks>
    public int WindowSeconds { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of requests that can be queued when the permit limit is exceeded.
    /// </summary>
    /// <value>
    /// The maximum queue size. A value of 0 means no queuing is allowed, and requests
    /// exceeding the permit limit are immediately rejected.
    /// </value>
    /// <remarks>
    /// Queued requests are held until permits become available in the next window.
    /// This provides a grace period for clients experiencing temporary bursts, but large
    /// queue sizes may increase response latency. Setting this to 0 enforces strict
    /// rate limiting with immediate rejection of excess requests.
    /// </remarks>
    public int QueueLimit { get; set; }
}