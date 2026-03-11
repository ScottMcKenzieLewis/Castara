using AutoMapper;
using Castara.Application.Abstractions.Repositories;
using Castara.Application.DTOs;
using Castara.Application.Mapping;
using Castara.Application.Repositories;
using Castara.Domain.Exceptions;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Text;
using Xunit;

namespace Castara.Infrastructure.Tests.Repositories;

public sealed class JsonCastingProfileRepositoryTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly IMapper _mapper;

    public JsonCastingProfileRepositoryTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "CastaraTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);

        var mapperConfig = new MapperConfiguration(
            cfg =>
            {
                cfg.AddProfile<CastingProfileMappingProfile>();
            },
            NullLoggerFactory.Instance);

        mapperConfig.AssertConfigurationIsValid();
        _mapper = mapperConfig.CreateMapper();
    }

    [Fact]
    public async Task GetAllAsync_WithValidJson_ShouldReturnMappedProfiles()
    {
        // Arrange
        var path = WriteJson("casting-profiles.json", ValidProfilesJson());
        var sut = CreateSut(path);

        // Act
        var result = await sut.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);

        var profile = result.Single();
        profile.Id.Should().Be("GS_GRAY_30");
        profile.DisplayName.Should().Be("Green Sand Gray Iron - Class 30");
        profile.ProcessFamily.Should().Be("GreenSand");
        profile.IronType.Should().Be("Gray");
        profile.DefaultSectionThicknessIn.Should().Be(1.0);
        profile.CarbonMin.Should().Be(3.2);
        profile.CarbonMax.Should().Be(3.6);
        profile.SiliconMin.Should().Be(1.8);
        profile.SiliconMax.Should().Be(2.4);
        profile.ManganeseMin.Should().Be(0.6);
        profile.ManganeseMax.Should().Be(0.9);
        profile.PhosphorusMin.Should().Be(0.02);
        profile.PhosphorusMax.Should().Be(0.08);
        profile.SulfurMin.Should().Be(0.01);
        profile.SulfurMax.Should().Be(0.06);
        profile.PreferredCarbonEquivalentMin.Should().Be(4.0);
        profile.PreferredCarbonEquivalentMax.Should().Be(4.3);
        profile.GraphitizationBias.Should().Be(0.15);
        profile.CoolingSeverityFactor.Should().Be(1.1);
        profile.ChillRiskCeiling.Should().Be(3.9);
        profile.ShrinkageRiskFloor.Should().Be(4.35);
        profile.HardnessWarningMinBhn.Should().Be(170);
        profile.HardnessWarningMaxBhn.Should().Be(240);
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingId_ShouldReturnMatchingProfile()
    {
        // Arrange
        var path = WriteJson("casting-profiles.json", ValidProfilesJson());
        var sut = CreateSut(path);

        // Act
        var result = await sut.GetByIdAsync("gs_gray_30");

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("GS_GRAY_30");
        result.DisplayName.Should().Be("Green Sand Gray Iron - Class 30");
    }

    [Fact]
    public async Task GetByIdAsync_WithMissingId_ShouldReturnNull()
    {
        // Arrange
        var path = WriteJson("casting-profiles.json", ValidProfilesJson());
        var sut = CreateSut(path);

        // Act
        var result = await sut.GetByIdAsync("DOES_NOT_EXIST");

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task GetByIdAsync_WithInvalidId_ShouldThrowArgumentException(string? id)
    {
        var path = WriteJson("casting-profiles.json", ValidProfilesJson());
        var sut = CreateSut(path);

        Func<Task> act = async () => await sut.GetByIdAsync(id!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetAllAsync_WithMissingFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var missingPath = Path.Combine(_tempDirectory, "missing.json");
        var sut = CreateSut(missingPath);

        // Act
        Func<Task> act = async () => await sut.GetAllAsync();

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task GetAllAsync_WithMalformedJson_ShouldThrowJsonException()
    {
        // Arrange
        var path = WriteJson("casting-profiles.json", "{ not valid json");
        var sut = CreateSut(path);

        // Act
        Func<Task> act = async () => await sut.GetAllAsync();

        // Assert
        await act.Should().ThrowAsync<System.Text.Json.JsonException>();
    }

    [Fact]
    public async Task GetAllAsync_WithInvalidProfileRanges_ShouldThrowDomainException()
    {
        // Arrange
        var path = WriteJson("casting-profiles.json", InvalidRangesJson());
        var sut = CreateSut(path);

        // Act
        Func<Task> act = async () => await sut.GetAllAsync();

        // Assert
        var ex = await act.Should().ThrowAsync<DomainException>();
        ex.Which.Message.Should().Contain("CarbonMin");
    }

    [Fact]
    public async Task GetAllAsync_WhenCalledTwice_ShouldReturnCachedProfiles()
    {
        // Arrange
        var path = WriteJson("casting-profiles.json", ValidProfilesJson());
        var sut = CreateSut(path);

        // Act
        var first = await sut.GetAllAsync();

        File.WriteAllText(path, DifferentProfilesJson());

        var second = await sut.GetAllAsync();

        // Assert
        first.Should().HaveCount(1);
        second.Should().HaveCount(1);

        first.Single().Id.Should().Be("GS_GRAY_30");
        second.Single().Id.Should().Be("GS_GRAY_30");
    }

    [Fact]
    public async Task GetAllAsync_WhenCalledTwice_ShouldReturnSameCachedInstance()
    {
        // Arrange
        var path = WriteJson("casting-profiles.json", ValidProfilesJson());
        var sut = CreateSut(path);

        // Act
        var first = await sut.GetAllAsync();
        var second = await sut.GetAllAsync();

        // Assert
        ReferenceEquals(first, second).Should().BeTrue();
    }

    private JsonCastingProfileRepository CreateSut(string filePath)
    {
        var options = Options.Create(new CastingProfileRepositoryOptions
        {
            FilePath = filePath
        });

        return new JsonCastingProfileRepository(options, _mapper);
    }

    private string WriteJson(string fileName, string json)
    {
        var path = Path.Combine(_tempDirectory, fileName);
        File.WriteAllText(path, json, Encoding.UTF8);
        return path;
    }

    private static string ValidProfilesJson() =>
        """
        {
          "Profiles": [
            {
              "Id": "GS_GRAY_30",
              "DisplayName": "Green Sand Gray Iron - Class 30",
              "ProcessFamily": "GreenSand",
              "IronType": "Gray",
              "Defaults": {
                "SectionThicknessIn": 1.0
              },
              "Ranges": {
                "CarbonMin": 3.2,
                "CarbonMax": 3.6,
                "SiliconMin": 1.8,
                "SiliconMax": 2.4,
                "ManganeseMin": 0.6,
                "ManganeseMax": 0.9,
                "PhosphorusMin": 0.02,
                "PhosphorusMax": 0.08,
                "SulfurMin": 0.01,
                "SulfurMax": 0.06
              },
              "Targets": {
                "PreferredCarbonEquivalentMin": 4.0,
                "PreferredCarbonEquivalentMax": 4.3,
                "GraphitizationBias": 0.15,
                "CoolingSeverityFactor": 1.1
              },
              "RiskThresholds": {
                "ChillRiskCeiling": 3.9,
                "ShrinkageRiskFloor": 4.35,
                "HardnessWarningMinBhn": 170,
                "HardnessWarningMaxBhn": 240
              }
            }
          ]
        }
        """;

    private static string InvalidRangesJson() =>
        """
        {
          "Profiles": [
            {
              "Id": "BAD_PROFILE",
              "DisplayName": "Invalid Profile",
              "ProcessFamily": "GreenSand",
              "IronType": "Gray",
              "Defaults": {
                "SectionThicknessIn": 1.0
              },
              "Ranges": {
                "CarbonMin": 3.7,
                "CarbonMax": 3.2,
                "SiliconMin": 1.8,
                "SiliconMax": 2.4,
                "ManganeseMin": 0.6,
                "ManganeseMax": 0.9,
                "PhosphorusMin": 0.02,
                "PhosphorusMax": 0.08,
                "SulfurMin": 0.01,
                "SulfurMax": 0.06
              },
              "Targets": {
                "PreferredCarbonEquivalentMin": 4.0,
                "PreferredCarbonEquivalentMax": 4.3,
                "GraphitizationBias": 0.15,
                "CoolingSeverityFactor": 1.1
              },
              "RiskThresholds": {
                "ChillRiskCeiling": 3.9,
                "ShrinkageRiskFloor": 4.35,
                "HardnessWarningMinBhn": 170,
                "HardnessWarningMaxBhn": 240
              }
            }
          ]
        }
        """;

    private static string DifferentProfilesJson() =>
        """
        {
          "Profiles": [
            {
              "Id": "NB_GRAY_HIGHPROD",
              "DisplayName": "No-Bake Gray Iron - High Production",
              "ProcessFamily": "NoBake",
              "IronType": "Gray",
              "Defaults": {
                "SectionThicknessIn": 0.75
              },
              "Ranges": {
                "CarbonMin": 3.1,
                "CarbonMax": 3.5,
                "SiliconMin": 1.7,
                "SiliconMax": 2.3,
                "ManganeseMin": 0.5,
                "ManganeseMax": 0.8,
                "PhosphorusMin": 0.02,
                "PhosphorusMax": 0.07,
                "SulfurMin": 0.01,
                "SulfurMax": 0.05
              },
              "Targets": {
                "PreferredCarbonEquivalentMin": 3.95,
                "PreferredCarbonEquivalentMax": 4.2,
                "GraphitizationBias": 0.10,
                "CoolingSeverityFactor": 1.2
              },
              "RiskThresholds": {
                "ChillRiskCeiling": 3.85,
                "ShrinkageRiskFloor": 4.30,
                "HardnessWarningMinBhn": 180,
                "HardnessWarningMaxBhn": 250
              }
            }
          ]
        }
        """;

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}