using System;
using System.IO;
using System.Text.Json;
using Avalonia;
using Avalonia.Styling;

namespace Bignum.Services;

public class ThemeService : IThemeService
{
    private readonly string _filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");

    public bool IsDarkMode
    {
        get
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
        set
        {
            try
            {
                var settings = new ThemeSettings { IsDarkMode = value };
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // ignore
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
