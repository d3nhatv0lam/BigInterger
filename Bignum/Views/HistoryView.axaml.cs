using Bignum.ViewModels;
using ReactiveUI.Avalonia;

namespace Bignum.Views;

public partial class HistoryView : ReactiveUserControl<HistoryViewModel>
{
    public HistoryView()
    {
        InitializeComponent();
    }
}