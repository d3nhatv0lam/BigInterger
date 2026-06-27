using System;
using System.Collections.Specialized;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Controls;
using Bignum.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace Bignum.Views;

public partial class HistoryView : ReactiveUserControl<HistoryViewModel>
{
    public HistoryView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            if (ViewModel is not null && HistoryListBox is not null)
            {
                var entries = ViewModel.HistoryEntries;
                Observable.FromEventPattern<NotifyCollectionChangedEventHandler, NotifyCollectionChangedEventArgs>(
                        h => entries.CollectionChanged += h,
                        h => entries.CollectionChanged -= h)
                    .Throttle(TimeSpan.FromMilliseconds(200))
                    .ObserveOn(RxSchedulers.MainThreadScheduler)
                    .Subscribe(_ => ScrollToBottom(HistoryListBox))
                    .DisposeWith(disposables);

                ScrollToBottom(HistoryListBox);
            }
        });
    }

    private void ScrollToBottom(ListBox listBox)
    {
        if (ViewModel is { HistoryEntries.Count: > 0 })
        {
            var lastItem = ViewModel.HistoryEntries[^1];
            Avalonia.Threading.Dispatcher.UIThread.Post(() => { listBox.ScrollIntoView(lastItem); });
        }
    }
}