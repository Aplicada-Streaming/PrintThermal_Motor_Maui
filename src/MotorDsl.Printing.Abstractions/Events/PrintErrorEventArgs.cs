using MotorDsl.Core.Models;

namespace MotorDsl.Printing;

public class PrintErrorEventArgs : EventArgs
{
    public PrintError Error { get; }
    public PrintErrorEventArgs(PrintError error) => Error = error;
}
