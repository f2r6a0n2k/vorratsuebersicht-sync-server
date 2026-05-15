using Avalonia.Data.Converters;
using Avalonia.Media;
using System.Globalization;

namespace Vorratsuebersicht.Desktop;

public static class BoolConverters
{
    public static readonly BrushConverter BrushConv = new();

    public static readonly IValueConverter ToBrush = new FuncValueConverter<bool, IBrush?>(
        v => v ? new SolidColorBrush(Color.Parse("#27ae60")) : new SolidColorBrush(Color.Parse("#95a5a6")));

    public static readonly IValueConverter Not = new FuncValueConverter<bool, bool>(v => !v);

    public static readonly IValueConverter HasText = new FuncValueConverter<string?, bool>(
        v => !string.IsNullOrWhiteSpace(v));

    public static readonly IValueConverter ToOpacity = new FuncValueConverter<bool, double>(
        v => v ? 0.5 : 1.0);

    public static readonly IValueConverter ToTextDecorations = new FuncValueConverter<bool, TextDecorationCollection?>(
        v => v ? TextDecorations.Strikethrough : null);

    public static readonly IValueConverter ConnectText = new FuncValueConverter<bool, string>(
        v => v ? "Verbinde..." : "Verbinden");
}
