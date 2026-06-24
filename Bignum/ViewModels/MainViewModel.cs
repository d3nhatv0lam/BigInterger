using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace Bignum.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly CompositeDisposable _disposables = new();
    public string Greeting { get; } = "Welcome to Avalonia!";
    public CalculatorViewModel Calculator { get; } = new();
    public HistoryViewModel History { get; } = new();

    public MainViewModel()
    {
        Calculator.DisposeWith(_disposables);

    }
}