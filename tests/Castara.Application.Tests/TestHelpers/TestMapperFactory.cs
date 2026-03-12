using AutoMapper;
using Castara.Application.Mapping;
using Microsoft.Extensions.Logging.Abstractions;

namespace Castara.Infrastructure.Tests.TestHelpers;

public static class TestMapperFactory
{
    public static IMapper Create()
    {
        var config = new MapperConfiguration(
            cfg => cfg.AddMaps(typeof(CastingProfileMappingProfile).Assembly),
            NullLoggerFactory.Instance);

        config.AssertConfigurationIsValid();

        return config.CreateMapper();
    }
}
