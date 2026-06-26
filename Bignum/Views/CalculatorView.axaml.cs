using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using Avalonia.Input;
using Bignum.ViewModels;
using ReactiveUI;
using ReactiveUI.Validation.Extensions;
using ReactiveUI.Avalonia;

namespace Bignum.Views;

public partial class CalculatorView : ReactiveUserControl<CalculatorViewModel>
{
    public CalculatorView()
    {
        InitializeComponent();
        
        this.WhenActivated(disposables =>
        {
            this.BindValidation(ViewModel, vm => vm.NumberA, view => view.NumberAErrorText.Text)
                .DisposeWith(disposables);

            this.BindValidation(ViewModel, vm => vm.NumberB, view => view.NumberBErrorText.Text)
                .DisposeWith(disposables);
        });
    }

    private void Background_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        this.Focus();
    }
}