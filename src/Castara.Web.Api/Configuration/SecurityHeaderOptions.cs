namespace Castara.Api.Configuration;

/// <summary>
/// Configuration options for managing HTTP security headers in API responses.
/// </summary>
/// <remarks>
/// This configuration allows removal of headers that may expose sensitive information
/// about the server implementation, framework versions, or technology stack. Removing
/// such headers is a security best practice that helps reduce the attack surface by
/// preventing information disclosure to potential attackers.
/// </remarks>
public sealed class SecurityHeadersOptions
{
    /// <summary>
    /// Gets or sets the list of HTTP header names to remove from responses.
    /// </summary>
    /// <value>
    /// A list of header names (case-insensitive) to strip from all HTTP responses.
    /// Common examples include "Server", "X-Powered-By", "X-AspNet-Version", and "X-AspNetMvc-Version".
    /// </value>
    /// <remarks>
    /// Removing headers like "Server" and "X-Powered-By" prevents attackers from easily
    /// identifying the web server technology and framework versions, making it more difficult
    /// to target known vulnerabilities. This is part of a defense-in-depth security strategy.
    /// </remarks>
    /// <example>
    /// Example configuration in appsettings.json:
    /// <code>
    /// "SecurityHeaders": {
    ///   "Remove": ["Server", "X-Powered-By", "X-AspNet-Version"]
    /// }
    /// </code>
    /// </example>
    public List<string> Remove { get; set; } = [];
}