using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Splat;
using Bignum.Services;
using DynamicData;

namespace Bignum.ViewModels;

public partial class HistoryViewModel : ViewModelBase, IDisposable, IActivatableViewModel
{
    private readonly CompositeDisposable _disposables = new();
    private readonly IHistoryService _historyService;

    public ObservableCollection<HistoryEntry> HistoryEntries { get; } = new();

    [Reactive] private HistoryEntry? _selectedEntry;
    [Reactive] private bool _isConfirmDeleteVisible;

    [ObservableAsProperty] private string _selectedEntryHeader = string.Empty;

    public ReactiveCommand<Unit, Unit> LoadHistoryCommand { get; }
    public ReactiveCommand<Unit, Unit> ClearAllCommand { get; }
    public ReactiveCommand<Unit, Unit> ConfirmDeleteCommand { get; }
    public ReactiveCommand<Unit, Unit> CancelDeleteCommand { get; }
    
    public ViewModelActivator Activator { get; } = new();


    public HistoryViewModel()
    {
        _historyService = Locator.Current.GetService<IHistoryService>()
            ?? throw new InvalidOperationException($"Chưa đăng ký {nameof(IHistoryService)}");

        LoadHistoryCommand = ReactiveCommand.Create(LoadHistory).DisposeWith(_disposables);
        ClearAllCommand = ReactiveCommand.Create(() => { IsConfirmDeleteVisible = true; }).DisposeWith(_disposables);
        CancelDeleteCommand = ReactiveCommand.Create(() => { IsConfirmDeleteVisible = false; }).DisposeWith(_disposables);
        
        ConfirmDeleteCommand = ReactiveCommand.Create(() =>
        {
            _historyService.ClearHistory();
            IsConfirmDeleteVisible = false;
            LoadHistory();
        }).DisposeWith(_disposables);

        // Tạo tiêu đề hiển thị vùng chi tiết dạng: yyyy-MM-dd HH:mm:ss - Phép toán
        _selectedEntryHeaderHelper = this.WhenAnyValue(x => x.SelectedEntry)
            .Select(entry =>
            {
                if (entry == null) return string.Empty;
                string opSymbol = entry.Operation switch
                {
                    "Addition" => "Phép cộng (+)",
                    "Subtraction" => "Phép trừ (-)",
                    "Multiply" => "Phép nhân (x)",
                    "Division" => "Phép chia (/)",
                    _ => entry.Operation
                };
                return $"{entry.Timestamp} - {opSymbol}";
            })
            .ToProperty(this, nameof(SelectedEntryHeader))
            .DisposeWith(_disposables);
        
        this.WhenActivated((CompositeDisposable disposables) =>
        {
            LoadHistoryCommand.Execute(Unit.Default);
        });

        // Nạp lịch sử lần đầu tiên khi ứng dụng khởi chạy
        LoadHistory();
    }

    private void LoadHistory()
    {
        HistoryEntries.Clear();
        var list = _historyService.LoadHistory();
        
        HistoryEntries.AddRange(list);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

}