using Castara.Api.Dtos;
using Castara.Web.Api.Dtos.Diagnostics.Requests;
using Castara.Web.Api.Dtos.Diagnostics.Responses;
using Castara.Web.Api.Dtos.HealthReport;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Castara.Api.Serialization;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(ProblemDetails))]
[JsonSerializable(typeof(ValidationProblemDetails))]
[JsonSerializable(typeof(HealthReportDto))]
[JsonSerializable(typeof(HealthReportEntryDto))]
[JsonSerializable(typeof(SubmitCrashReportRequest))]
[JsonSerializable(typeof(SubmitCrashReportResponse))]
[JsonSerializable(typeof(ApiErrorDto))]
internal partial class CastaraJsonContext : JsonSerializerContext
{
}