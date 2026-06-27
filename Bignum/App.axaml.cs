using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Bignum.ViewModels;
using Bignum.Views;
using Splat;
using Bignum.Services;

namespace Bignum;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Đăng ký dịch vụ lịch sử tính toán nếu không phải chạy trên trình duyệt (WebAssembly)
        if (!System.OperatingSystem.IsBrowser())
        {
            Locator.CurrentMutable.RegisterLazySingleton<IHistoryService>(() => new HistoryService());
            Locator.CurrentMutable.RegisterLazySingleton<IThemeService>(() => new ThemeService());
        }

        // Áp dụng cấu hình giao diện đã lưu
        Locator.Current.GetService<IThemeService>()?.ApplyTheme();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime singleViewFactoryApplicationLifetime)
        {
            singleViewFactoryApplicationLifetime.MainViewFactory =
                () => new MainView { DataContext = new MainViewModel() };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}