using System.Threading.Tasks;
using Avalonia;
using Avalonia.Browser;
using Bignum.Services;
using ReactiveUI.Avalonia;

namespace Bignum.Browser;


internal sealed partial class Program
{
    private static Task Main(string[] args) => BuildAvaloniaApp()
        .WithInterFont()
#if DEBUG
        .WithDeveloperTools()
#endif
        .UseReactiveUI(builder => {})
        .StartBrowserAppAsync("out");

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .AfterSetup(builder =>
            {
                Splat.Locator.CurrentMutable.RegisterLazySingleton<IHistoryService>(() => new WebHistoryService());
                Splat.Locator.CurrentMutable.RegisterLazySingleton<IThemeService>(() => new WebThemeService());
            });
}