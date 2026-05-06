using MotorDsl.Core.Models;
using MotorDsl.Core.Printing;

namespace MotorDsl.Maui.Printing;

/// <summary>
/// IPrintErrorHandler para MAUI: hereda el comportamiento de retry/abort del
/// <see cref="DefaultPrintErrorHandler"/> y agrega logging via Debug.WriteLine
/// + eventos publicos consumibles para feedback (snackbar/toast). NO bloquea
/// el retry loop con dialogs.
/// </summary>
public class MauiPrintErrorHandler : DefaultPrintErrorHandler
{
    public event EventHandler<PrintError>? RetryAttempted;
    public event EventHandler<int>? Succeeded;
    public bool LogRetries { get; set; } = true;

    public override Task<bool> HandleErrorAsync(PrintError error)
    {
        if (LogRetries)
            System.Diagnostics.Debug.WriteLine($"[Print] Attempt {error.Attempt}/{error.MaxAttempts}: {error.Type} - {error.Message}");
        return base.HandleErrorAsync(error);
    }

    public override void OnRetryAttempt(PrintError error)
    {
        base.OnRetryAttempt(error);
        RetryAttempted?.Invoke(this, error);
    }

    public override void OnPrintSuccess(int totalAttempts)
    {
        base.OnPrintSuccess(totalAttempts);
        Succeeded?.Invoke(this, totalAttempts);
    }
}
