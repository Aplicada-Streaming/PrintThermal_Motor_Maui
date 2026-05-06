using MotorDsl.Printing;

namespace MotorDsl.Maui.Controls;

/// <summary>
/// Pagina modal que envuelve <see cref="PrinterPickerView"/> con un boton Cancelar.
/// Cierra el modal al conectarse exitosamente o al cancelar.
/// </summary>
public class PrinterPickerPage : ContentPage
{
    private readonly PrinterPickerView _picker;

    public PrinterPickerPage(IThermalPrinterService service, string? filterKind = null)
    {
        Title = "Seleccionar impresora";

        _picker = new PrinterPickerView
        {
            Service = service,
            FilterKind = filterKind
        };
        _picker.DeviceSelected += OnDeviceSelected;

        var cancelButton = new Button
        {
            Text = "Cancelar"
        };
        cancelButton.Clicked += OnCancelClicked;

        Content = new VerticalStackLayout
        {
            Padding = 16,
            Spacing = 12,
            Children = { _picker, cancelButton }
        };
    }

    private async void OnDeviceSelected(object? sender, PrinterDevice device)
    {
        await Navigation.PopModalAsync();
    }

    private async void OnCancelClicked(object? sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
