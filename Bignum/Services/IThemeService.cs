namespace Bignum.Services;

public interface IThemeService
{
    bool IsDarkMode { get; set; }
    void ApplyTheme();
}
