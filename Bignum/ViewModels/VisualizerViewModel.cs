using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Bignum.Services;
using CoreBignum = Bignum.Core.Bignum;

namespace Bignum.ViewModels;

public class VisualNode
{
    public string Value { get; set; } = string.Empty;
    public int Index { get; set; }
    public bool IsHighlighted { get; set; }
    public bool IsHead { get; set; }
    public bool IsTail { get; set; }
}

public partial class VisualizerViewModel : ViewModelBase, IDisposable
{
    private readonly CompositeDisposable _disposables = new();
    private IDisposable? _autoPlaySubscription;
    private CoreBignum.Bignum? _bignumA;
    private CoreBignum.Bignum? _bignumB;

    [Reactive] private string _numberA = string.Empty;
    [Reactive] private string _numberB = string.Empty;
    [Reactive] private string _selectedOperation = "+";
    [Reactive] private bool _isSimulating;
    [Reactive] private int _currentStepIndex = -1;
    [Reactive] private bool _isAutoPlaying;

    [Reactive] private List<VisualNode> _nodesA = new();
    [Reactive] private List<VisualNode> _nodesB = new();
    [Reactive] private List<VisualNode> _nodesResult = new();
    [Reactive] private string _explanationText = string.Empty;
    [Reactive] private string _carryText = string.Empty;

    [Reactive] private bool _aIsValid;
    [Reactive] private bool _bIsValid;
    [Reactive] private string? _aError;
    [Reactive] private string? _bError;

    public List<SimStep> Steps { get; private set; } = new();

    public ReactiveCommand<Unit, Unit> StartSimCommand { get; }
    public ReactiveCommand<Unit, Unit> NextStepCommand { get; }
    public ReactiveCommand<Unit, Unit> PrevStepCommand { get; }
    public ReactiveCommand<Unit, Unit> ToggleAutoPlayCommand { get; }
    public ReactiveCommand<Unit, Unit> ResetCommand { get; }

    public VisualizerViewModel()
    {
        // 1. Theo dõi thay đổi số A để kiểm tra hợp lệ và vẽ sơ đồ tĩnh
        this.WhenAnyValue(x => x.NumberA)
            .Subscribe(val =>
            {
                if (IsSimulating) return;

                if (string.IsNullOrWhiteSpace(val))
                {
                    AIsValid = false;
                    AError = null;
                    _bignumA = null;
                    NodesA = new List<VisualNode>();
                    return;
                }

                if (CoreBignum.Bignum.IsValid(val, out var error))
                {
                    AIsValid = true;
                    AError = null;
                    _bignumA = new CoreBignum.Bignum(val);
                    NodesA = BignumToVisualNodes(_bignumA);
                }
                else
                {
                    AIsValid = false;
                    AError = error;
                    _bignumA = null;
                    NodesA = new List<VisualNode>();
                }
            })
            .DisposeWith(_disposables);

        // 2. Theo dõi thay đổi số B để kiểm tra hợp lệ và vẽ sơ đồ tĩnh
        this.WhenAnyValue(x => x.NumberB)
            .Subscribe(val =>
            {
                if (IsSimulating) return;

                if (string.IsNullOrWhiteSpace(val))
                {
                    BIsValid = false;
                    BError = null;
                    _bignumB = null;
                    NodesB = new List<VisualNode>();
                    return;
                }

                if (CoreBignum.Bignum.IsValid(val, out var error))
                {
                    BIsValid = true;
                    BError = null;
                    _bignumB = new CoreBignum.Bignum(val);
                    NodesB = BignumToVisualNodes(_bignumB);
                }
                else
                {
                    BIsValid = false;
                    BError = error;
                    _bignumB = null;
                    NodesB = new List<VisualNode>();
                }
            })
            .DisposeWith(_disposables);

        // 3. Khởi tạo các Lệnh điều khiển
        var canStart = this.WhenAnyValue(
            x => x.AIsValid,
            x => x.BIsValid,
            x => x.IsSimulating,
            (aValid, bValid, isSim) => aValid && bValid && !isSim
        );

        StartSimCommand = ReactiveCommand.Create(StartSimulation, canStart).DisposeWith(_disposables);

        var canNext = this.WhenAnyValue(
            x => x.IsSimulating,
            x => x.CurrentStepIndex,
            (isSim, idx) => isSim && idx < Steps.Count - 1
        );
        NextStepCommand = ReactiveCommand.Create(() => { CurrentStepIndex++; }, canNext).DisposeWith(_disposables);

        var canPrev = this.WhenAnyValue(
            x => x.IsSimulating,
            x => x.CurrentStepIndex,
            (isSim, idx) => isSim && idx > 0
        );
        PrevStepCommand = ReactiveCommand.Create(() => { CurrentStepIndex--; }, canPrev).DisposeWith(_disposables);

        ToggleAutoPlayCommand = ReactiveCommand.Create(ToggleAutoPlay, this.WhenAnyValue(x => x.IsSimulating)).DisposeWith(_disposables);

        ResetCommand = ReactiveCommand.Create(ResetSimulation).DisposeWith(_disposables);

        // 4. Theo dõi sự thay đổi bước mô phỏng để vẽ lại sơ đồ động
        this.WhenAnyValue(x => x.CurrentStepIndex)
            .Subscribe(idx =>
            {
                if (!IsSimulating || Steps.Count == 0 || idx < 0 || idx >= Steps.Count) return;

                var step = Steps[idx];

                // Cập nhật câu giải thích
                ExplanationText = step.Description;

                // Cập nhật số nhớ/mượn
                if (step.IsFinished)
                {
                    CarryText = string.Empty;
                }
                else
                {
                    string label = SelectedOperation == "+" ? "Nhớ" : "Mượn";
                    // Phép cộng/trừ có thể đổi giải thuật thực tế bên dưới phụ thuộc vào dấu
                    // Nếu là cộng cùng dấu -> Phép Cộng. Nếu cộng khác dấu -> Phép Trừ thô
                    var isRealAddition = _bignumA!.IsNegative == (_selectedOperation == "-" ? !_bignumB!.IsNegative : _bignumB!.IsNegative);
                    label = isRealAddition ? "Nhớ" : "Mượn";
                    CarryText = $"{label}: {step.CarryOut}";
                }

                // Vẽ lại các Node có Highlight theo bước đang tính toán
                NodesA = BignumToVisualNodes(_bignumA, step.NodeIndex);
                
                // Trường hợp phép trừ tuyệt đối |B| > |A|, BignumSimulator nhận tham số (b, a)
                // Nên ta cần highlight đúng node tương ứng
                NodesB = BignumToVisualNodes(_bignumB, step.NodeIndex);

                // Vẽ danh sách kết quả trung gian
                NodesResult = ListToVisualNodes(step.IntermediateResult, step.IsFinished ? null : step.NodeIndex);
            })
            .DisposeWith(_disposables);
    }

    private void StartSimulation()
    {
        if (_bignumA == null || _bignumB == null) return;

        try
        {
            var a = _bignumA;
            var b = _bignumB;

            // Xử lý đảo dấu của B nếu là phép trừ
            if (SelectedOperation == "-")
            {
                b = -b;
            }

            List<SimStep> generatedSteps;

            // Quyết định thuật toán mô phỏng thô bên dưới (Cộng thô hay Trừ thô)
            if (a.IsNegative == b.IsNegative)
            {
                // Cùng dấu -> Cộng thô
                generatedSteps = BignumSimulator.GenerateAddSteps(a, b);
            }
            else
            {
                // Khác dấu -> Trừ thô (Lấy trị tuyệt đối lớn trừ trị tuyệt đối nhỏ)
                var aChain = new CoreBignum.NodeChain(a.Head, a.NodeCount);
                var bChain = new CoreBignum.NodeChain(b.Head, b.NodeCount);
                var cmp = CoreBignum.BignumHelper.CompareRaw(aChain, bChain);

                if (cmp >= 0)
                {
                    generatedSteps = BignumSimulator.GenerateSubtractSteps(a, b);
                }
                else
                {
                    generatedSteps = BignumSimulator.GenerateSubtractSteps(b, a);
                }
            }

            Steps = generatedSteps;
            IsSimulating = true;
            CurrentStepIndex = 0;
        }
        catch (Exception ex)
        {
            ExplanationText = $"Lỗi khởi tạo mô phỏng: {ex.Message}";
        }
    }

    private void ToggleAutoPlay()
    {
        if (IsAutoPlaying)
        {
            StopAutoPlay();
        }
        else
        {
            StartAutoPlay();
        }
    }

    private void StartAutoPlay()
    {
        IsAutoPlaying = true;
        _autoPlaySubscription = Observable.Interval(TimeSpan.FromSeconds(1.5), RxSchedulers.MainThreadScheduler)
            .Subscribe(_ =>
            {
                if (CurrentStepIndex < Steps.Count - 1)
                {
                    CurrentStepIndex++;
                }
                else
                {
                    StopAutoPlay();
                }
            });
    }

    private void StopAutoPlay()
    {
        IsAutoPlaying = false;
        _autoPlaySubscription?.Dispose();
        _autoPlaySubscription = null;
    }

    private void ResetSimulation()
    {
        StopAutoPlay();
        IsSimulating = false;
        Steps.Clear();
        CurrentStepIndex = -1;
        ExplanationText = string.Empty;
        CarryText = string.Empty;

        // Vẽ lại sơ đồ tĩnh
        NodesA = BignumToVisualNodes(_bignumA);
        NodesB = BignumToVisualNodes(_bignumB);
        NodesResult = new List<VisualNode>();
    }

    private static List<VisualNode> BignumToVisualNodes(CoreBignum.Bignum? bignum, int? activeIndex = null)
    {
        var list = new List<VisualNode>();
        if (bignum?.Head is null) return list;

        var current = bignum.Head;
        var index = 0;
        while (current is not null)
        {
            list.Add(new VisualNode
            {
                Value = current.Next is null ? current.Value.ToString() : current.Value.ToString(CoreBignum.BignumConstants.BignumFormat),
                Index = index,
                IsHighlighted = activeIndex.HasValue && activeIndex.Value == index,
                IsHead = index == 0,
                IsTail = current.Next is null
            });
            index++;
            current = current.Next;
        }
        return list;
    }

    private static List<VisualNode> ListToVisualNodes(List<int> values, int? activeIndex = null)
    {
        var list = new List<VisualNode>();
        if (values == null || values.Count == 0) return list;

        for (int i = 0; i < values.Count; i++)
        {
            var isTail = i == values.Count - 1;
            list.Add(new VisualNode
            {
                Value = isTail ? values[i].ToString() : values[i].ToString(CoreBignum.BignumConstants.BignumFormat),
                Index = i,
                IsHighlighted = activeIndex.HasValue && activeIndex.Value == i,
                IsHead = i == 0,
                IsTail = isTail
            });
        }
        return list;
    }

    public void Dispose()
    {
        StopAutoPlay();
        _disposables.Dispose();
    }
}
