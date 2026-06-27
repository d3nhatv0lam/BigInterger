using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;
using Bignum.Services;

namespace Bignum.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    public CalculatorViewModel Calculator { get; } = new();
    public HistoryViewModel History { get; } = new();

    [Reactive] private string _currentThemeIcon = "WeatherNight";

    public ReactiveCommand<Unit, Unit> ToggleThemeCommand { get; }

    public MainViewModel()
    {
        Calculator.DisposeWith(_disposables);
        History.DisposeWith(_disposables);

        var themeService = Locator.Current.GetService<IThemeService>()
            ?? throw new InvalidOperationException("Chưa đăng ký IThemeService");

        // Đồng bộ icon theo trạng thái theme hiện tại
        CurrentThemeIcon = themeService.IsDarkMode ? "WeatherSunny" : "WeatherNight";

        ToggleThemeCommand = ReactiveCommand.Create(() =>
        {
            themeService.IsDarkMode = !themeService.IsDarkMode;
            themeService.ApplyTheme();
            CurrentThemeIcon = themeService.IsDarkMode ? "WeatherSunny" : "WeatherNight";
        }).DisposeWith(_disposables);
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}