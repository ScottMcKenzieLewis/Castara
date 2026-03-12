using Castara.Infrastructure.Tests.TestHelpers;
using FluentAssertions;
using Xunit;

namespace Castara.Application.Tests.Mapping;

public sealed class CastingProfileMappingProfileTests
{
    [Fact]
    public void Configuration_ShouldBeValid()
    {
        // Act
        var act = () => TestMapperFactory.Create();

        // Assert
        act.Should().NotThrow();
    }
}