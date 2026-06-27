using System.Runtime.InteropServices.JavaScript;
using Avalonia;
using Avalonia.Styling;
using Bignum.Services;

namespace Bignum.Browser;

public partial class WebThemeService : IThemeService
{
    private const string ThemeKey = "bignum_theme_variant";

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void SetLocalStorageItem(string key, string value);

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string? GetLocalStorageItem(string key);

    public bool IsDarkMode
    {
        get => GetLocalStorageItem(ThemeKey) == "dark";
        set => SetLocalStorageItem(ThemeKey, value ? "dark" : "light");
    }

    public void ApplyTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}
