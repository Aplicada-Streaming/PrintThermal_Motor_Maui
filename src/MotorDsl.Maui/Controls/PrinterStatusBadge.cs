using System.ComponentModel;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls.Shapes;
using MotorDsl.Printing;

namespace MotorDsl.Maui.Controls;

/// <summary>
/// Badge MAUI que muestra el estado actual de una <see cref="IThermalPrinterService"/>
/// con color y texto descriptivo. Se actualiza automaticamente al cambiar el
/// ConnectionState, CurrentDevice o LastError del servicio.
/// </summary>
public class PrinterStatusBadge : ContentView
{
    public static readonly BindableProperty ServiceProperty =
        BindableProperty.Create(
            nameof(Service),
            typeof(IThermalPrinterService),
            typeof(PrinterStatusBadge),
            null,
            propertyChanged: OnServiceChanged);

    public IThermalPrinterService? Service
    {
        get => (IThermalPrinterService?)GetValue(ServiceProperty);
        set => SetValue(ServiceProperty, value);
    }

    private readonly Border _border;
    private readonly Label _label;
    private IThermalPrinterService? _subscribedService;

    public PrinterStatusBadge()
    {
        _label = new Label
        {
            TextColor = Colors.White,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center
        };

        _border = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 6 },
            Padding = new Thickness(10, 4),
            Stroke = Colors.Transparent,
            Content = _label
        };

        Content = _border;
        ApplyState();

        Unloaded += OnUnloaded;
    }

    private static void OnServiceChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is PrinterStatusBadge badge)
            badge.RebindService(newValue as IThermalPrinterService);
    }

    private void RebindService(IThermalPrinterService? newService)
    {
        if (_subscribedService is not null)
            _subscribedService.PropertyChanged -= OnServicePropertyChanged;

        _subscribedService = newService;

        if (_subscribedService is not null)
            _subscribedService.PropertyChanged += OnServicePropertyChanged;

        ApplyState();
    }

    private void OnServicePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(IThermalPrinterService.ConnectionState)
            || e.PropertyName == nameof(IThermalPrinterService.CurrentDevice)
            || e.PropertyName == nameof(IThermalPrinterService.LastError)
            || string.IsNullOrEmpty(e.PropertyName))
        {
            MainThread.BeginInvokeOnMainThread(ApplyState);
        }
    }

    private void OnUnloaded(object? sender, EventArgs e)
    {
        if (_subscribedService is not null)
        {
            _subscribedService.PropertyChanged -= OnServicePropertyChanged;
            _subscribedService = null;
        }
    }

    private void ApplyState()
    {
        var service = Service;
        if (service is null)
        {
            _border.BackgroundColor = Color.FromArgb("#9E9E9E");
            _label.Text = "Desconectado";
            return;
        }

        switch (service.ConnectionState)
        {
            case PrinterConnectionState.Disconnected:
                _border.BackgroundColor = Color.FromArgb("#9E9E9E");
                _label.Text = "Desconectado";
                break;
            case PrinterConnectionState.Scanning:
                _border.BackgroundColor = Color.FromArgb("#1976D2");
                _label.Text = "Buscando...";
                break;
            case PrinterConnectionState.Connecting:
                _border.BackgroundColor = Color.FromArgb("#F57C00");
                _label.Text = "Conectando...";
                break;
            case PrinterConnectionState.Connected:
                _border.BackgroundColor = Color.FromArgb("#388E3C");
                _label.Text = $"Conectado: {service.CurrentDevice?.Name ?? "?"}";
                break;
            case PrinterConnectionState.Reconnecting:
                _border.BackgroundColor = Color.FromArgb("#F57C00");
                _label.Text = "Reconectando...";
                break;
            case PrinterConnectionState.Failed:
                _border.BackgroundColor = Color.FromArgb("#D32F2F");
                _label.Text = $"Error: {service.LastError ?? "desconocido"}";
                break;
            default:
                _border.BackgroundColor = Color.FromArgb("#9E9E9E");
                _label.Text = "Desconectado";
                break;
        }
    }
}
