namespace Castara.Wpf.Models;

public sealed record StatusState(
    AppStatusLevel Level,
    string LeftText,
    string RightText);