using MaterialDesignThemes.Wpf;

namespace Castara.Wpf.Services.Theme;

public sealed class ThemeService
{
    private readonly PaletteHelper _paletteHelper = new();

    public void SetDark(bool dark)
    {
        var theme = _paletteHelper.GetTheme();

        theme.SetBaseTheme(
            dark
            ? BaseTheme.Dark
            : BaseTheme.Light);

        _paletteHelper.SetTheme(theme);
    }
}