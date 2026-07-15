using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Bignum.ViewModels;
using ReactiveUI.Avalonia;

namespace Bignum.Views;

public partial class VisualizerView : ReactiveUserControl<VisualizerViewModel>
{
    public static IMultiValueConverter PointerTextConverter { get; } = new PointerTextMultiConverter();

    public VisualizerView()
    {
        InitializeComponent();
    }
}

public class PointerTextMultiConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is [bool isHead, bool isTail, ..])
        {
            if (isHead && isTail) return "Head, Tail";
            if (isHead) return "Head";
            if (isTail) return "Tail";
        }
        return string.Empty;
    }
}
