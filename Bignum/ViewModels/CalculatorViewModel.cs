using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ReactiveUI.Validation.Abstractions;
using ReactiveUI.Validation.Contexts;
using ReactiveUI.Validation.Extensions;
using CoreBignum = Bignum.Core.Bignum;

namespace Bignum.ViewModels;

public partial class CalculatorViewModel : ViewModelBase, IActivatableViewModel, IValidatableViewModel, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    [Reactive] private string _numberA = string.Empty;
    [Reactive] private string _numberB = string.Empty;
    [ObservableAsProperty] private string _resultString = string.Empty;
    [ObservableAsProperty] private string _remainderString = string.Empty;

    [ObservableAsProperty] private CoreBignum.Bignum? _cachedA;
    [ObservableAsProperty] private CoreBignum.Bignum? _cachedB;

    [Reactive] private CoreBignum.Bignum _resultBignum;
    [Reactive] private CoreBignum.Bignum _remainderBignum;


    public ViewModelActivator Activator { get; } = new();
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    public ReactiveCommand<Unit, Unit> ClearResultCommand { get; }

    public int InputMaxLength => CoreBignum.BignumConstants.MaxDigitOfBignum;

    public CalculatorViewModel()
    {
        this.ValidationRule(
            x => x.NumberA,
            str => string.IsNullOrWhiteSpace(str),
            "Number A is required");


        // test
        _cachedAHelper = this.WhenAnyValue(x => x.NumberA)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Where(numberStr => !string.IsNullOrWhiteSpace(numberStr))
            .Select(numberStr =>
            {
                try
                {
                    return new CoreBignum.Bignum(numberStr);
                }
                catch (Exception ex)
                {
                    return null;
                }
            })
            .ToProperty(this, nameof(CachedA))
            .DisposeWith(_disposables);
        
        // test
        _cachedBHelper = this.WhenAnyValue(x => x.NumberB)
            .Throttle(TimeSpan.FromMilliseconds(300))
            .Where(numberStr => !string.IsNullOrWhiteSpace(numberStr))
            .Select(numberStr =>
            {
                try
                {
                    return new CoreBignum.Bignum(numberStr);
                }
                catch (Exception ex)
                {
                    return null;
                }
            })
            .ToProperty(this, nameof(CachedB))
            .DisposeWith(_disposables);

        // test logic
        this.WhenAnyValue(x => x.CachedA, x => x.CachedB)
            .SubscribeOn(RxSchedulers.TaskpoolScheduler)
            .Where(state => state.Item1 is not null && state.Item2 is not null)
            .Select(x => x.Item1 + x.Item2)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(res =>
            {
                ResultBignum = res;
            })
            .DisposeWith(_disposables);


        // signal
        ClearResultCommand = ReactiveCommand.Create(() => { })
            .DisposeWith(_disposables);

        _resultStringHelper =
            Observable.Merge(
                    this.WhenAnyValue(x => x.ResultBignum)
                        .WhereNotNull()
                        .Select(x => x.ToStringNumber()),
                    ClearResultCommand.Select(_ => string.Empty)
                )
                .DistinctUntilChanged()
                .ToProperty(this, nameof(ResultString), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(_disposables);

        _remainderStringHelper =
            Observable.Merge(
                    this.WhenAnyValue(x => x.RemainderBignum)
                        .WhereNotNull()
                        .Select(x => x.ToStringNumber()),
                    ClearResultCommand.Select(_ => string.Empty)
                )
                .DistinctUntilChanged()
                .ToProperty(this, nameof(RemainderString), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}