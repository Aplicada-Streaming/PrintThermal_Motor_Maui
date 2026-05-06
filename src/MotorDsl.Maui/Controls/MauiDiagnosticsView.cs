using MotorDsl.Printing;

namespace MotorDsl.Maui.Controls;

/// <summary>
/// ContentView que muestra un <see cref="DiagnosticsReport"/> en pantalla con
/// secciones titulares (Librería, Aplicación, Dispositivo, Impresora, Permisos,
/// Notas, footer con fecha de captura). Pensada como vista de detalle del
/// reporte cuando el consumer quiere mostrar la captura antes de compartirla
/// o imprimirla.
/// </summary>
public class MauiDiagnosticsView : ContentView
{
    public static readonly BindableProperty ReportProperty =
        BindableProperty.Create(
            nameof(Report),
            typeof(DiagnosticsReport),
            typeof(MauiDiagnosticsView),
            null,
            propertyChanged: OnReportChanged);

    public DiagnosticsReport? Report
    {
        get => (DiagnosticsReport?)GetValue(ReportProperty);
        set => SetValue(ReportProperty, value);
    }

    private readonly VerticalStackLayout _stack;

    public MauiDiagnosticsView()
    {
        _stack = new VerticalStackLayout { Spacing = 10, Padding = new Thickness(8) };
        Content = new ScrollView { Content = _stack };
        Render(null);
    }

    private static void OnReportChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MauiDiagnosticsView view)
            view.Render(newValue as DiagnosticsReport);
    }

    private void Render(DiagnosticsReport? report)
    {
        _stack.Clear();

        if (report == null)
        {
            _stack.Add(new Label
            {
                Text = "(sin reporte)",
                TextColor = Colors.Gray,
                FontSize = 12,
                HorizontalTextAlignment = TextAlignment.Center
            });
            return;
        }

        // Librería
        var libContent = new VerticalStackLayout { Spacing = 2 };
        if (report.Libraries.Count == 0)
        {
            libContent.Add(MonoLabel("(sin librerías detectadas)"));
        }
        else
        {
            foreach (var lib in report.Libraries)
            {
                var ver = lib.InformationalVersion ?? lib.Version ?? "?";
                libContent.Add(MonoLabel($"{lib.Name}  {ver}"));
            }
        }
        _stack.Add(Section("Librería", libContent));

        // Aplicación
        _stack.Add(Section("Aplicación", new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                KvLabel("Nombre", report.App.Name),
                KvLabel("Versión", report.App.Version),
                KvLabel("Build", report.App.Build),
                KvLabel("Paquete", report.App.PackageName),
            }
        }));

        // Dispositivo
        _stack.Add(Section("Dispositivo", new VerticalStackLayout
        {
            Spacing = 2,
            Children =
            {
                KvLabel("Fabricante", report.Device.Manufacturer),
                KvLabel("Modelo", report.Device.Model),
                KvLabel("Plataforma", $"{report.Device.OsPlatform} {report.Device.OsVersion}"),
                KvLabel("Idiom", report.Device.Idiom),
                KvLabel("Tipo", report.Device.DeviceType),
            }
        }));

        // Impresora (opcional)
        if (report.Printer != null)
        {
            _stack.Add(Section("Impresora", new VerticalStackLayout
            {
                Spacing = 2,
                Children =
                {
                    KvLabel("Kind", report.Printer.Kind),
                    KvLabel("Nombre", report.Printer.Name),
                    KvLabel("Id", report.Printer.Id),
                    KvLabel("Estado", report.Printer.ConnectionState),
                    KvLabel("Profile", report.Printer.ProfileName),
                }
            }));
        }

        // Permisos (opcional)
        if (report.Permissions != null && report.Permissions.Statuses.Count > 0)
        {
            var permContent = new VerticalStackLayout { Spacing = 2 };
            foreach (var kv in report.Permissions.Statuses)
                permContent.Add(KvLabel(kv.Key, kv.Value));
            _stack.Add(Section("Permisos", permContent));
        }

        // Notas (opcional)
        if (!string.IsNullOrWhiteSpace(report.Notes))
        {
            _stack.Add(Section("Notas", new Label
            {
                Text = report.Notes,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap
            }));
        }

        // Footer
        _stack.Add(new Label
        {
            Text = $"Capturado: {report.CapturedAt:yyyy-MM-dd HH:mm:ss zzz}",
            FontSize = 11,
            TextColor = Colors.Gray,
            HorizontalTextAlignment = TextAlignment.End,
            Margin = new Thickness(0, 4, 0, 0)
        });
    }

    private static Border Section(string title, View content)
    {
        return new Border
        {
            Stroke = Colors.LightGray,
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
            Padding = new Thickness(10),
            Content = new VerticalStackLayout
            {
                Spacing = 6,
                Children =
                {
                    new Label
                    {
                        Text = title,
                        FontAttributes = FontAttributes.Bold,
                        FontSize = 14
                    },
                    content
                }
            }
        };
    }

    private static Label KvLabel(string key, string value)
    {
        return new Label
        {
            FontSize = 13,
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = key + ": ", FontAttributes = FontAttributes.Bold },
                    new Span { Text = value }
                }
            }
        };
    }

    private static Label MonoLabel(string text)
    {
        return new Label
        {
            Text = text,
            FontFamily = "monospace",
            FontSize = 12,
            LineBreakMode = LineBreakMode.NoWrap
        };
    }
}
