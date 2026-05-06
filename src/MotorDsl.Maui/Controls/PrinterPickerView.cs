using MotorDsl.Printing;

namespace MotorDsl.Maui.Controls;

/// <summary>
/// Picker MAUI que descubre dispositivos via <see cref="IThermalPrinterService"/>
/// y permite conectarse seleccionando uno. Si solo se descubre uno y
/// <see cref="AutoConnectIfSingle"/> esta habilitado, conecta automaticamente.
/// </summary>
public class PrinterPickerView : ContentView
{
    public static readonly BindableProperty ServiceProperty =
        BindableProperty.Create(
            nameof(Service),
            typeof(IThermalPrinterService),
            typeof(PrinterPickerView),
            null);

    public static readonly BindableProperty FilterKindProperty =
        BindableProperty.Create(
            nameof(FilterKind),
            typeof(string),
            typeof(PrinterPickerView),
            null);

    public static readonly BindableProperty AutoConnectIfSingleProperty =
        BindableProperty.Create(
            nameof(AutoConnectIfSingle),
            typeof(bool),
            typeof(PrinterPickerView),
            true);

    public IThermalPrinterService? Service
    {
        get => (IThermalPrinterService?)GetValue(ServiceProperty);
        set => SetValue(ServiceProperty, value);
    }

    public string? FilterKind
    {
        get => (string?)GetValue(FilterKindProperty);
        set => SetValue(FilterKindProperty, value);
    }

    public bool AutoConnectIfSingle
    {
        get => (bool)GetValue(AutoConnectIfSingleProperty);
        set => SetValue(AutoConnectIfSingleProperty, value);
    }

    public event EventHandler<PrinterDevice>? DeviceSelected;
    public event EventHandler<Exception>? ScanError;

    private readonly Button _scanButton;
    private readonly ActivityIndicator _spinner;
    private readonly Label _emptyLabel;
    private readonly CollectionView _list;

    public PrinterPickerView()
    {
        _scanButton = new Button
        {
            Text = "Escanear",
            HorizontalOptions = LayoutOptions.Start
        };
        _scanButton.Clicked += async (_, _) => await ScanAsync();

        _spinner = new ActivityIndicator
        {
            IsRunning = false,
            IsVisible = false,
            VerticalOptions = LayoutOptions.Center,
            Margin = new Thickness(8, 0, 0, 0)
        };

        var topRow = new HorizontalStackLayout
        {
            Spacing = 8,
            Children = { _scanButton, _spinner }
        };

        _emptyLabel = new Label
        {
            Text = "Sin escanear",
            FontSize = 13,
            TextColor = Colors.Gray,
            IsVisible = true
        };

        _list = new CollectionView
        {
            SelectionMode = SelectionMode.Single,
            IsVisible = false,
            ItemTemplate = new DataTemplate(BuildItemTemplate)
        };
        _list.SelectionChanged += OnListSelectionChanged;

        Content = new VerticalStackLayout
        {
            Spacing = 6,
            Children = { topRow, _emptyLabel, _list }
        };
    }

    private static View BuildItemTemplate()
    {
        var name = new Label
        {
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.Black
        };
        name.SetBinding(Label.TextProperty, nameof(PrinterDevice.Name));

        var id = new Label
        {
            FontSize = 11,
            TextColor = Colors.Gray
        };
        id.SetBinding(Label.TextProperty, nameof(PrinterDevice.Id));

        return new Border
        {
            Padding = new Thickness(10, 6),
            Margin = new Thickness(0, 2),
            Stroke = Color.FromArgb("#BDBDBD"),
            BackgroundColor = Color.FromArgb("#F5F5F5"),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
            Content = new VerticalStackLayout
            {
                Spacing = 2,
                Children = { name, id }
            }
        };
    }

    private async void OnListSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is PrinterDevice device)
        {
            // Limpiamos la seleccion para permitir re-seleccionar el mismo item luego.
            _list.SelectedItem = null;
            await ConnectAsync(device);
        }
    }

    public async Task ScanAsync()
    {
        var service = Service;
        if (service is null)
        {
            var ex = new InvalidOperationException("PrinterPickerView.Service no esta seteado.");
            ScanError?.Invoke(this, ex);
            return;
        }

        _spinner.IsVisible = true;
        _spinner.IsRunning = true;
        _scanButton.IsEnabled = false;
        _emptyLabel.IsVisible = false;
        _list.IsVisible = false;

        try
        {
            var devices = await service.DiscoverDevicesAsync(FilterKind);
            _list.ItemsSource = devices;

            if (devices.Count == 0)
            {
                _emptyLabel.Text = "Sin dispositivos encontrados";
                _emptyLabel.IsVisible = true;
                _list.IsVisible = false;
            }
            else if (devices.Count == 1 && AutoConnectIfSingle)
            {
                _list.IsVisible = true;
                _emptyLabel.IsVisible = false;
                await ConnectAsync(devices[0]);
            }
            else
            {
                _list.IsVisible = true;
                _emptyLabel.IsVisible = false;
            }
        }
        catch (Exception ex)
        {
            _emptyLabel.Text = $"Error: {ex.Message}";
            _emptyLabel.IsVisible = true;
            _list.IsVisible = false;
            ScanError?.Invoke(this, ex);
        }
        finally
        {
            _spinner.IsRunning = false;
            _spinner.IsVisible = false;
            _scanButton.IsEnabled = true;
        }
    }

    public async Task ConnectAsync(PrinterDevice device)
    {
        var service = Service;
        if (service is null) return;

        var ok = await service.ConnectAsync(device);
        if (ok)
            DeviceSelected?.Invoke(this, device);
        // Si falla, el servicio emite ErrorOccurred y el badge refleja el estado.
    }
}
