using Bignum.ViewModels;
using ReactiveUI.Avalonia;

namespace Bignum.Views;

public partial class MainView : ReactiveUserControl<MainViewModel>
{
    public MainView()
    {
        InitializeComponent();
    }
}