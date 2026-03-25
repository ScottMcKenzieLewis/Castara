using FluentValidation;

namespace Castara.Web.Api.Dtos.Diagnostics;

public sealed class CrashReportDtoValidator : AbstractValidator<CrashReportDto>
{
    public CrashReportDtoValidator()
    {
        RuleFor(x => x.ReportId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.TimestampUtc)
            .NotEmpty();

        RuleFor(x => x.ApplicationName)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.ApplicationVersion)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.RuntimeVersion)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.OperatingSystem)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Source)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.Exception)
            .NotNull()
            .SetValidator(new CrashExceptionInfoDtoValidator());

        RuleFor(x => x.InnerExceptions)
            .NotNull()
            .Must(x => x.Count <= 10)
            .WithMessage("No more than 10 inner exceptions are allowed.");

        RuleForEach(x => x.InnerExceptions)
            .SetValidator(new CrashExceptionInfoDtoValidator());

        RuleFor(x => x.Context)
            .NotNull()
            .Must(x => x.Count <= 64)
            .WithMessage("No more than 64 context entries are allowed.");

        RuleForEach(x => x.Context)
            .ChildRules(kvp =>
            {
                kvp.RuleFor(x => x.Key)
                    .NotEmpty()
                    .MaximumLength(128);

                kvp.RuleFor(x => x.Value)
                    .NotNull()
                    .MaximumLength(2048);
            });

        RuleFor(x => x.RecentLogs)
            .NotNull()
            .Must(x => x.Count <= 250)
            .WithMessage("No more than 250 recent log entries are allowed.");

        RuleForEach(x => x.RecentLogs)
            .SetValidator(new CrashLogEntryDtoValidator());
    }
}

public sealed class CrashExceptionInfoDtoValidator
    : AbstractValidator<CrashExceptionInfoDto>
{
    public CrashExceptionInfoDtoValidator()
    {
        RuleFor(x => x.Type)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Message)
            .NotNull()
            .MaximumLength(4096);

        RuleFor(x => x.StackTrace)
            .MaximumLength(32768);
    }
}

public sealed class CrashLogEntryDtoValidator
    : AbstractValidator<CrashLogEntryDto>
{
    public CrashLogEntryDtoValidator()
    {
        RuleFor(x => x.TimestampUtc)
            .NotEmpty();

        RuleFor(x => x.Level)
            .NotEmpty()
            .MaximumLength(32);

        RuleFor(x => x.Category)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Message)
            .NotNull()
            .MaximumLength(4096);
    }
}