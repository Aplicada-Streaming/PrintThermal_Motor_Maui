namespace MotorDsl.Bluetooth;

/// <summary>
/// Parte pura del transport: calculo de bloques de tamano fijo para la escritura al socket.
/// Vive fuera de cualquier bloque #if y sin dependencias de plataforma, de modo que pueda
/// testearse en net10.0 (el proyecto de tests la enlaza por fuente, ya que MotorDsl.Bluetooth
/// solo targetea android/ios). El transport real la consume desde WriteBytesAsync (Android).
/// </summary>
public partial class BluetoothPrinterTransport
{
    /// <summary>
    /// Parte <paramref name="data"/> en segmentos contiguos de a lo sumo
    /// <paramref name="size"/> bytes, independiente del contenido: no se corta por LF (0x0A)
    /// ni por ningun byte particular. Reensamblar los segmentos en orden reproduce
    /// exactamente el array original. Es una vista (ArraySegment) sin copiar el buffer.
    /// </summary>
    internal static IEnumerable<ArraySegment<byte>> ChunkBuffer(byte[] data, int size)
    {
        if (data is null) throw new ArgumentNullException(nameof(data));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size), "El tamano de bloque debe ser mayor a cero");

        for (int offset = 0; offset < data.Length; offset += size)
        {
            int len = Math.Min(size, data.Length - offset);
            yield return new ArraySegment<byte>(data, offset, len);
        }
    }
}
