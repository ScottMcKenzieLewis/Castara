namespace Castara.Web.Api.Attributes.Diagnostics;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RequireCrashReportHmacAttribute : Attribute
{
}