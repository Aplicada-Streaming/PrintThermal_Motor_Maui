using SkiaSharp;

namespace MotorDsl.Maui.Controls;

/// <summary>
/// ContentView que muestra el PNG producido por <c>RasterPreviewRenderer</c>
/// con escalado configurable (zoom) para simular vista pixelada.
/// </summary>
public class MauiRasterPreview : ContentView
{
    public static readonly BindableProperty ImageBytesProperty =
        BindableProperty.Create(
            nameof(ImageBytes),
            typeof(byte[]),
            typeof(MauiRasterPreview),
            null,
            propertyChanged: OnImageChanged);

    public static readonly BindableProperty ZoomFactorProperty =
        BindableProperty.Create(
            nameof(ZoomFactor),
            typeof(double),
            typeof(MauiRasterPreview),
            2.0,
            propertyChanged: OnImageChanged);

    public byte[]? ImageBytes
    {
        get => (byte[]?)GetValue(ImageBytesProperty);
        set => SetValue(ImageBytesProperty, value);
    }

    public double ZoomFactor
    {
        get => (double)GetValue(ZoomFactorProperty);
        set => SetValue(ZoomFactorProperty, value);
    }

    private readonly Image _image;
    private readonly Label _placeholder;
    private readonly ScrollView _scroll;

    public MauiRasterPreview()
    {
        _image = new Image
        {
            Aspect = Aspect.AspectFit,
            BackgroundColor = Colors.LightGray,
            HorizontalOptions = LayoutOptions.Start,
            VerticalOptions = LayoutOptions.Start
        };
        _placeholder = new Label
        {
            Text = "(sin vista previa)",
            TextColor = Colors.Gray,
            FontSize = 12,
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center,
            IsVisible = true
        };
        _scroll = new ScrollView
        {
            Orientation = ScrollOrientation.Both,
            Content = _image
        };
        Content = new Grid
        {
            Children = { _scroll, _placeholder }
        };
    }

    private static void OnImageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MauiRasterPreview view)
            view.UpdateImage();
    }

    private void UpdateImage()
    {
        var bytes = ImageBytes;
        if (bytes == null || bytes.Length == 0)
        {
            _image.Source = null;
            _image.WidthRequest = -1;
            _image.HeightRequest = -1;
            _placeholder.IsVisible = true;
            return;
        }

        _placeholder.IsVisible = false;

        // Medir tamaño nativo del PNG con SkiaSharp para escalar el WidthRequest.
        int nativeWidth = 0;
        int nativeHeight = 0;
        try
        {
            using var bmp = SKBitmap.Decode(bytes);
            if (bmp != null)
            {
                nativeWidth = bmp.Width;
                nativeHeight = bmp.Height;
            }
        }
        catch
        {
            // Si decode falla, dejamos WidthRequest auto.
        }

        // Asignar ImageSource desde stream nuevo cada vez.
        var local = bytes; // captura local para closure
        _image.Source = ImageSource.FromStream(() => new MemoryStream(local));

        if (nativeWidth > 0)
        {
            _image.WidthRequest = nativeWidth * ZoomFactor;
            _image.HeightRequest = nativeHeight * ZoomFactor;
        }
        else
        {
            _image.WidthRequest = -1;
            _image.HeightRequest = -1;
        }
    }
}
