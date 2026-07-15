using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Styling;

namespace Bignum.Services;

public class ThemeService : IThemeService
{
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
    private bool _isDarkMode;

    public ThemeService()
    {
        _isDarkMode = GetIsDarkMode();
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
                var settings = new ThemeSettings { IsDarkMode = value };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
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

    private bool GetIsDarkMode()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                var settings = JsonSerializer.Deserialize<ThemeSettings>(json);
                return settings?.IsDarkMode ?? false;
            }
        }
        catch
        {
            // ignore
        }

        return false; // Default to Light mode
    }
}