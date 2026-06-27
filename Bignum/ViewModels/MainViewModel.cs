using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;

namespace Bignum.ViewModels;

public class MainViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    public CalculatorViewModel Calculator { get; } = new();
    public HistoryViewModel History { get; } = new();

    public MainViewModel()
    {
        Calculator.DisposeWith(_disposables);
        History.DisposeWith(_disposables);
    }
    
    public void Dispose()
    {
        _disposables.Dispose();
    }
}