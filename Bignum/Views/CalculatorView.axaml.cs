using Bignum.ViewModels;
using ReactiveUI.Avalonia;

namespace Bignum.Views;

public partial class CalculatorView : ReactiveUserControl<CalculatorViewModel>
{
    public CalculatorView()
    {
        InitializeComponent();
    }
}