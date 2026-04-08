namespace Castara.Web.Api.Infrastructure;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}