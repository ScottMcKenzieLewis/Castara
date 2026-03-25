using FluentValidation;

namespace Castara.Web.Api.Dtos.Diagnostics.Requests;

public sealed class SubmitCrashReportRequestValidator
    : AbstractValidator<SubmitCrashReportRequest>
{
    public SubmitCrashReportRequestValidator()
    {
        RuleFor(x => x.Report)
            .NotNull();

        When(x => x.Report is not null, () =>
        {
            RuleFor(x => x.Report!)
                .SetValidator(new CrashReportDtoValidator());
        });
    }
}
