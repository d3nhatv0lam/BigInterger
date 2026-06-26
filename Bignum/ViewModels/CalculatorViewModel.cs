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

public enum OperationType
{
    Add,
    Subtract,
    Multiply,
    Divide
}

public partial class CalculatorViewModel : ViewModelBase, IActivatableViewModel, IValidatableViewModel, IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    [Reactive] private string _numberA = string.Empty;
    [Reactive] private string _numberB = string.Empty;
    [ObservableAsProperty] private string _resultString = string.Empty;
    [ObservableAsProperty] private string _remainderString = string.Empty;

    [ObservableAsProperty] private CoreBignum.Bignum? _cachedA;
    [ObservableAsProperty] private CoreBignum.Bignum? _cachedB;

    [Reactive] private CoreBignum.Bignum? _resultBignum;
    [Reactive] private CoreBignum.Bignum? _remainderBignum;

    [Reactive] private OperationType? _selectedOperation = OperationType.Add;
    [Reactive] private string? _calculationError;

    [ObservableAsProperty] private bool _isAddActive;
    [ObservableAsProperty] private bool _isSubtractActive;
    [ObservableAsProperty] private bool _isMultiplyActive;
    [ObservableAsProperty] private bool _isDivideActive;

    public ViewModelActivator Activator { get; } = new();
    public IValidationContext ValidationContext { get; } = new ValidationContext();

    public ReactiveCommand<OperationType?, Unit> SelectOperationCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearNumberACommand { get; }
    public ReactiveCommand<Unit, Unit> ClearNumberBCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearResultCommand { get; }

    public int InputMaxLength => CoreBignum.BignumConstants.MaxDigitOfBignum;

    public CalculatorViewModel()
    {
        this.ValidationRule(
            vm => vm.NumberA,
            str => !string.IsNullOrWhiteSpace(str) && CoreBignum.Bignum.IsValid(str, out _),
            str =>
            {
                if (string.IsNullOrWhiteSpace(str)) return "Số A không được để trống";
                CoreBignum.Bignum.IsValid(str, out var error);
                return error ?? "Định dạng không hợp lệ";
            });

        this.ValidationRule(
            vm => vm.NumberB,
            str => !string.IsNullOrWhiteSpace(str) && CoreBignum.Bignum.IsValid(str, out _),
            str =>
            {
                if (string.IsNullOrWhiteSpace(str)) return "Số B không được để trống";
                CoreBignum.Bignum.IsValid(str, out var error);
                return error ?? "Định dạng không hợp lệ";
            });

        _isAddActiveHelper = this.WhenAnyValue(x => x.SelectedOperation)
            .Select(op => op == OperationType.Add)
            .ToProperty(this, nameof(IsAddActive))
            .DisposeWith(_disposables);

        _isSubtractActiveHelper = this.WhenAnyValue(x => x.SelectedOperation)
            .Select(op => op == OperationType.Subtract)
            .ToProperty(this, nameof(IsSubtractActive))
            .DisposeWith(_disposables);

        _isMultiplyActiveHelper = this.WhenAnyValue(x => x.SelectedOperation)
            .Select(op => op == OperationType.Multiply)
            .ToProperty(this, nameof(IsMultiplyActive))
            .DisposeWith(_disposables);

        _isDivideActiveHelper = this.WhenAnyValue(x => x.SelectedOperation)
            .Select(op => op == OperationType.Divide)
            .ToProperty(this, nameof(IsDivideActive))
            .DisposeWith(_disposables);

        // 6. Parsers
        _cachedAHelper = this.WhenAnyValue(x => x.NumberA)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Select(numberStr =>
            {
                if (string.IsNullOrWhiteSpace(numberStr)) return null;
                try
                {
                    return new CoreBignum.Bignum(numberStr);
                }
                catch
                {
                    return null;
                }
            })
            .ToProperty(this, nameof(CachedA))
            .DisposeWith(_disposables);

        _cachedBHelper = this.WhenAnyValue(x => x.NumberB)
            .Throttle(TimeSpan.FromMilliseconds(200))
            .Select(numberStr =>
            {
                if (string.IsNullOrWhiteSpace(numberStr)) return null;
                try
                {
                    return new CoreBignum.Bignum(numberStr);
                }
                catch
                {
                    return null;
                }
            })
            .ToProperty(this, nameof(CachedB))
            .DisposeWith(_disposables);

        this.WhenAnyValue(
                x => x.CachedA,
                x => x.CachedB,
                x => x.SelectedOperation)
            .SubscribeOn(RxSchedulers.TaskpoolScheduler)
            .Select(state =>
            {
                var (a, b, op) = state;
                if (a is null || b is null)
                {
                    return (Result: null, Remainder: null, Error: null);
                }

                try
                {
                    switch (op)
                    {
                        case OperationType.Add:
                            return (Result: a + b, Remainder: null, Error: null);
                        case OperationType.Subtract:
                            return (Result: a - b, Remainder: null, Error: null);
                        case OperationType.Multiply:
                            return (Result: a * b, Remainder: null, Error: null);
                        case OperationType.Divide:
                            var (quot, rem) = CoreBignum.Bignum.Divide(a, b);
                            return (Result: quot, Remainder: rem, Error: null);
                        default:
                            return (Result: null, Remainder: null, Error: null);
                    }
                }
                catch (DivideByZeroException)
                {
                    return (Result: null, Remainder: null,
                        Error: "Lỗi: Không thể chia cho 0");
                }
                catch (Exception ex)
                {
                    return (Result: (CoreBignum.Bignum?)null, Remainder: (CoreBignum.Bignum?)null,
                        Error: $"Lỗi: {ex.Message}");
                }
            })
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(res =>
            {
                CalculationError = res.Error;
                ResultBignum = res.Result;
                RemainderBignum = res.Remainder;
            })
            .DisposeWith(_disposables);

        SelectOperationCommand = ReactiveCommand.Create<OperationType?, Unit>(op =>
        {
            SelectedOperation = op;
            return Unit.Default;
        }).DisposeWith(_disposables);

        ClearNumberACommand = ReactiveCommand.Create(() => { NumberA = string.Empty; }).DisposeWith(_disposables);

        ClearNumberBCommand = ReactiveCommand.Create(() => { NumberB = string.Empty; }).DisposeWith(_disposables);

        ClearResultCommand = ReactiveCommand.Create(() =>
        {
            CalculationError = null;
        }).DisposeWith(_disposables);

        // xóa selection khi nấn delete kết quả luôn
        ClearResultCommand
            .Select(_ => (OperationType?)null)
            .InvokeCommand(SelectOperationCommand)
            .DisposeWith(_disposables);
        
        _resultStringHelper =
            Observable.Merge(
                    this.WhenAnyValue(x => x.ResultBignum, x => x.CalculationError)
                        .Select(state => state.Item2 ?? state.Item1?.ToStringNumber() ?? "0"),
                    ClearResultCommand.Select(_ => "0")
                )
                .DistinctUntilChanged()
                .ToProperty(this, nameof(ResultString), scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(_disposables);

        _remainderStringHelper =
            Observable.Merge(
                    this.WhenAnyValue(x => x.RemainderBignum)
                        .Select(x => x?.ToStringNumber() ?? "0"),
                    ClearResultCommand.Select(_ => "0")
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