using System;

namespace Castara.Domain.Exceptions;

/// <summary>
/// Represents errors that occur within the domain layer due to business rule violations,
/// invalid state, or constraint failures.
/// </summary>
/// <remarks>
/// <para>
/// This exception should be thrown when domain invariants are violated, such as:
/// <list type="bullet">
///   <item><description>Invalid configuration data (e.g., min &gt; max)</description></item>
///   <item><description>Business rule violations</description></item>
///   <item><description>Invalid domain object state</description></item>
///   <item><description>Constraint validation failures</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage Guidelines:</strong> Use this exception for domain-specific errors that
/// represent violations of domain rules rather than technical failures. Technical failures
/// (I/O, network, etc.) should use appropriate framework exceptions.
/// </para>
/// </remarks>
public class DomainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class.
    /// </summary>
    public DomainException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DomainException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DomainException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or null if no inner exception is specified.
    /// </param>
    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}