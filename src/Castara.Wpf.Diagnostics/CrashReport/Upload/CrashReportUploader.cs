using Castara.Wpf.Diagnostics.CrashReport.Upload.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Castara.Wpf.Diagnostics.CrashReport.Upload;

public sealed class CrashReportUploader : ICrashReportUploader
{
    private readonly HttpClient _httpClient;
    private readonly ICrashReportUploadMapper _mapper;
    private readonly CrashReportUploadOptions _options;
    private readonly ILogger<CrashReportUploader> _logger;

    public CrashReportUploader(
        HttpClient httpClient,
        ICrashReportUploadMapper mapper,
        IOptions<CrashReportUploadOptions> options,
        ILogger<CrashReportUploader> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (!string.IsNullOrWhiteSpace(_options.BaseUrl))
        {
            _httpClient.BaseAddress = new Uri(_options.BaseUrl, UriKind.Absolute);
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);
    }

    public async Task<CrashReportUploadResult> UploadAsync(
        CrashReport report,
        CancellationToken cancellationToken)
    {
        try
        {
            var requestModel = _mapper.Map(report);
            var json = CrashReportUploadJson.Serialize(requestModel);

            var (timestamp, signature) =
                CrashReportRequestSigner.Sign(_options.HmacKey, json);

            using var requestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                "/api/v1/diagnostics/crash-reports")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            requestMessage.Headers.Add("X-Castara-Key-Id", _options.KeyId);
            requestMessage.Headers.Add("X-Castara-Timestamp", timestamp);
            requestMessage.Headers.Add("X-Castara-Signature", signature);

            using var response = await _httpClient.SendAsync(
                requestMessage,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken).ConfigureAwait(false);

            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Crash report upload failed. ReportId={ReportId}, StatusCode={StatusCode}, Response={Response}",
                    report.ReportId,
                    (int)response.StatusCode,
                    responseText);

                return new CrashReportUploadResult(
                    Success: false,
                    IncidentId: null,
                    Status: response.StatusCode.ToString(),
                    ErrorMessage: responseText);
            }

            var uploadResponse = System.Text.Json.JsonSerializer.Deserialize<SubmitCrashReportResponseDto>(
                responseText,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            return new CrashReportUploadResult(
                Success: true,
                IncidentId: uploadResponse?.IncidentId,
                Status: uploadResponse?.Status,
                ErrorMessage: null);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Crash report upload timed out. ReportId={ReportId}", report.ReportId);

            return new CrashReportUploadResult(
                Success: false,
                IncidentId: null,
                Status: "Timeout",
                ErrorMessage: "The upload timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Crash report upload failed unexpectedly. ReportId={ReportId}", report.ReportId);

            return new CrashReportUploadResult(
                Success: false,
                IncidentId: null,
                Status: "Error",
                ErrorMessage: ex.Message);
        }
    }
}