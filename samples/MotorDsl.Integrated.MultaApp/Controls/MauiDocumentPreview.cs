using MotorDsl.Core.Models;

namespace MotorDsl.Integrated.MultaApp.Controls;

/// <summary>
/// Control MAUI que genera una vista previa visual de un LayoutedDocument.
/// Reutilizado de SampleApp — Sprint 08 | TK-66
/// </summary>
public class MauiDocumentPreview : ContentView
{
    public static readonly BindableProperty DocumentProperty =
        BindableProperty.Create(
            nameof(Document),
            typeof(LayoutedDocument),
            typeof(MauiDocumentPreview),
            null,
            propertyChanged: OnDocumentChanged);

    public LayoutedDocument? Document
    {
        get => (LayoutedDocument?)GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    private readonly VerticalStackLayout _stack;

    public MauiDocumentPreview()
    {
        _stack = new VerticalStackLayout
        {
            Spacing = 0,
            BackgroundColor = Colors.White
        };
        Content = _stack;
    }

    private static void OnDocumentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MauiDocumentPreview preview)
            preview.BuildPreview();
    }

    private void BuildPreview()
    {
        _stack.Children.Clear();

        if (Document?.NodeLayoutInfo == null || Document.NodeLayoutInfo.Count == 0)
        {
            _stack.Children.Add(new Label
            {
                Text = "(sin contenido)",
                TextColor = Colors.Gray,
                FontSize = 12
            });
            return;
        }

        var entries = Document.NodeLayoutInfo
            .Where(kvp => !string.IsNullOrEmpty(kvp.Value.WrappedText))
            .OrderBy(kvp => kvp.Value.LineNumber)
            .ThenBy(kvp => kvp.Value.ColumnNumber);

        foreach (var kvp in entries)
        {
            var info = kvp.Value;

            var label = new Label
            {
                Text = info.WrappedText,
                FontSize = 13,
                FontFamily = "Consolas",
                TextColor = Colors.Black,
                LineBreakMode = LineBreakMode.WordWrap,
                HorizontalOptions = GetAlignment(info.Alignment)
            };

            if (info.DeviceMetadata.TryGetValue("bold", out var bv) && bv is true)
                label.FontAttributes = FontAttributes.Bold;

            if (info.DeviceMetadata.ContainsKey("is_qr") || info.DeviceMetadata.ContainsKey("is_barcode"))
            {
                label.TextColor = Colors.DarkBlue;
                label.FontAttributes = FontAttributes.Italic;
            }

            if (info.DeviceMetadata.ContainsKey("is_bitmap"))
            {
                label.TextColor = Colors.DarkGreen;
                label.FontAttributes = FontAttributes.Italic;
            }

            _stack.Children.Add(label);
        }
    }

    private static LayoutOptions GetAlignment(string? alignment) => alignment?.ToLower() switch
    {
        "center" => LayoutOptions.Center,
        "right" => LayoutOptions.End,
        _ => LayoutOptions.Start
    };
}
