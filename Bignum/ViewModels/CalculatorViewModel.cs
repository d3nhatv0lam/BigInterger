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
using Splat;
using Bignum.Services;
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
    private readonly IHistoryService _historyService;
    private string? _lastSavedKey;

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

    [ObservableAsProperty] private string _numberADigitCountDisplay = "Số chữ số: 0";
    [ObservableAsProperty] private string _numberBDigitCountDisplay = "Số chữ số: 0";

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
        _historyService = Locator.Current.GetService<IHistoryService>()
            ?? throw new InvalidOperationException("Chưa đăng ký IHistoryService");

        this.ValidationRule(
            vm => vm.NumberA,
            str => string.IsNullOrEmpty(str) || CoreBignum.Bignum.IsValid(str, out _),
            str =>
            {
                if (string.IsNullOrEmpty(str)) return string.Empty;
                CoreBignum.Bignum.IsValid(str, out var error);
                return error ?? "Định dạng không hợp lệ";
            });

        this.ValidationRule(
            vm => vm.NumberB,
            str => string.IsNullOrEmpty(str) || CoreBignum.Bignum.IsValid(str, out _),
            str =>
            {
                if (string.IsNullOrEmpty(str)) return string.Empty;
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

        _numberADigitCountDisplayHelper = this.WhenAnyValue(x => x.NumberA)
            .Select(GetDigitCountDisplay)
            .ToProperty(this, nameof(NumberADigitCountDisplay))
            .DisposeWith(_disposables);

        _numberBDigitCountDisplayHelper = this.WhenAnyValue(x => x.NumberB)
            .Select(GetDigitCountDisplay)
            .ToProperty(this, nameof(NumberBDigitCountDisplay))
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
                CoreBignum.Bignum? result = null;
                CoreBignum.Bignum? remainder = null;
                string? error = null;

                if (a is not null && b is not null)
                {
                    try
                    {
                        switch (op)
                        {
                            case OperationType.Add:
                                result = a + b;
                                break;
                            case OperationType.Subtract:
                                result = a - b;
                                break;
                            case OperationType.Multiply:
                                result = a * b;
                                break;
                            case OperationType.Divide:
                                var (quot, rem) = CoreBignum.Bignum.Divide(a, b);
                                result = quot;
                                remainder = rem;
                                break;
                        }
                    }
                    catch (DivideByZeroException)
                    {
                        error = "Lỗi: Không thể chia cho 0";
                    }
                    catch (Exception ex)
                    {
                        error = $"Lỗi: {ex.Message}";
                    }
                }

                return (Result: result, Remainder: remainder, Error: error);
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

        // Tự động lưu lịch sử khi phép tính thành công và ổn định sau 1 giây (để tránh lưu khi đang gõ số)
        this.WhenAnyValue(
                x => x.ResultBignum,
                x => x.RemainderBignum,
                x => x.CalculationError)
            .Throttle(TimeSpan.FromSeconds(1.0), RxSchedulers.TaskpoolScheduler)
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(state =>
            {
                var (res, rem, err) = state;
                if (res == null)
                {
                    _lastSavedKey = null;
                    return;
                }

                if (err == null && CachedA != null && CachedB != null && SelectedOperation != null)
                {
                    string opName = SelectedOperation switch
                    {
                        OperationType.Add => "Addition",
                        OperationType.Subtract => "Subtraction",
                        OperationType.Multiply => "Multiply",
                        OperationType.Divide => "Division",
                        _ => string.Empty
                    };

                    if (!string.IsNullOrEmpty(opName))
                    {
                        string op1 = CachedA.ToStringNumber();
                        string op2 = CachedB.ToStringNumber();
                        string key = $"{opName}:{op1}:{op2}";

                        if (key != _lastSavedKey)
                        {
                            _lastSavedKey = key;

                            var entry = new HistoryEntry
                            {
                                Id = Guid.NewGuid().ToString(),
                                Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                Operation = opName,
                                Operand1 = op1,
                                Operand2 = op2,
                                Result1 = res.ToStringNumber(),
                                Result2 = rem?.ToStringNumber()
                            };

                            _historyService.AddEntry(entry);
                        }
                    }
                }
            })
            .DisposeWith(_disposables);
    }

    private static string GetDigitCountDisplay(string? input)
    {
        if (string.IsNullOrEmpty(input)) 
        {
            return "Số chữ số: 0";
        }
        
        try
        {
            var temp = new CoreBignum.Bignum(input);
            return $"Số chữ số: {temp.DigitCount}";
        }
        catch
        {
            return "Số chữ số: --";
        }
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}