using System;
using System.Diagnostics;
using System.Runtime.InteropServices.JavaScript;
using System.Threading;
using Avalonia;
using Avalonia.Styling;
using Bignum.Services;

namespace Bignum.Browser;

public partial class WebThemeService : IThemeService
{
    private const string ThemeKey = "bignum_theme_variant";
    private bool _isDarkMode;

    [JSImport("globalThis.localStorage.setItem")]
    private static partial void SetLocalStorageItem(string key, string value);

    [JSImport("globalThis.localStorage.getItem")]
    private static partial string? GetLocalStorageItem(string key);

    public WebThemeService()
    {
        var isDarkMode = GetLocalStorageItem(ThemeKey) == "dark";
        _isDarkMode = isDarkMode;
    }

    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            var previousValue = _isDarkMode;
            try
            {
                _isDarkMode = value;
                SetLocalStorageItem(ThemeKey, value ? "dark" : "light");
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                _isDarkMode = previousValue;
            }
        }
    }

    public void ApplyTheme()
    {
        if (Application.Current is not null)
        {
            Application.Current.RequestedThemeVariant = IsDarkMode ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}