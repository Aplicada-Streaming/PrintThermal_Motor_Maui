#if ANDROID
using MotorDsl.Printing;

namespace MotorDsl.Bluetooth;

/// <summary>
/// Rama ANDROID del aprovisionamiento de logo en memoria no volatil (NV).
///
/// HONESTIDAD:
/// - El input es GS v 0 (el mismo formato inline que la app produce); se parsea su header y se
///   re-emiten los bits como comando de DEFINE NV.
/// - Se intenta primero GS ( L (NV graphics por keycode), que SI tiene query de verificacion.
/// - Si GS ( L no se confirma, se intenta FS q (NV bit image clasico). FS NO tiene query limpia:
///   si el define no lanzo IOException se considera Success con confirmacion LIMITADA, y el layout
///   de datos FS puede variar por impresora. El resultado de ProvisionLogoAsync es la prueba.
/// - Tras un Success se actualiza Capabilities.NvGraphics/NvGraphicsKind (esto es lo que confirma
///   FS, que en la deteccion de fase 2 quedaba Unknown).
/// - El define es una escritura pesada: usa el MISMO camino de chunking + InvalidateConnection +
///   rethrow de fase 1 (WriteHeavyAsync).
/// </summary>
public partial class BluetoothPrinterTransport
{
    public async Task<NvLogoResult> ProvisionLogoAsync(byte[] gsV0Bytes, int keycode, CancellationToken ct = default)
    {
        if (_outputStream == null)
            return new NvLogoResult(false, null, "no conectado");

        var parsed = ParseGsV0(gsV0Bytes);
        if (parsed is null)
            return new NvLogoResult(false, null, "GS v 0 invalido");

        var (widthBytes, heightDots, bits) = parsed.Value;

        // 1) GS ( L (NV graphics, define raster por keycode). Verificable por query de keycodes.
        await WriteHeavyAsync(BuildGslDefine(keycode, widthBytes, heightDots, bits), ct);
        if (await IsKeycodeDefinedViaGslAsync(keycode, ct))
        {
            UpdateNvCaps("gsl");
            return new NvLogoResult(true, "gsl", "NV graphics (GS ( L) confirmado por query de keycodes");
        }

        // 2) FS q (NV bit image clasico). Sin query limpia: si el define no lanzo IOException lo
        //    consideramos Success con confirmacion limitada (verificar con impresion de prueba).
        await WriteHeavyAsync(BuildFsqDefine(widthBytes, heightDots, bits), ct);
        UpdateNvCaps("fs");
        return new NvLogoResult(true, "fs",
            "FS q definido; confirmacion FS limitada (sin query): verificar con impresion de prueba");
    }

    public async Task<bool> IsLogoProvisionedAsync(int keycode, CancellationToken ct = default)
    {
        if (_outputStream == null) return false;
        // FS no tiene query limpia: best-effort -> false (documentado). GS ( L si se puede consultar.
        if (_capabilities?.NvGraphicsKind == "fs") return false;
        return await IsKeycodeDefinedViaGslAsync(keycode, ct);
    }

    public async Task ClearLogoAsync(int keycode, CancellationToken ct = default)
    {
        if (_outputStream == null) return;
        if (_capabilities?.NvGraphicsKind == "fs")
            // FS no tiene delete por keycode: redefinir la imagen FS #1 con un raster minimo vacio.
            await WriteHeavyAsync(BuildFsqDefine(1, 8, new byte[1]), ct);
        else
            await WriteHeavyAsync(BuildGslDelete(keycode), ct);
    }

    // Actualiza las capabilities tras un define exitoso (confirma la familia, incluida FS que en
    // fase 2 quedaba Unknown).
    private void UpdateNvCaps(string kind)
    {
        _capabilities = (_capabilities ?? PrinterCapabilities.Unknown())
            with { NvGraphics = CapabilitySupport.Supported, NvGraphicsKind = kind };
    }

    /// <summary>
    /// Escritura pesada (define NV): MISMO camino de chunking + InvalidateConnection + rethrow de
    /// fase 1, sin status gating ni pacing por perfil (pacing minimo fijo de 1ms por bloque).
    /// </summary>
    private async Task WriteHeavyAsync(byte[] data, CancellationToken ct)
    {
        const int CHUNK_SIZE = 256;
        try
        {
            foreach (var bloque in ChunkBuffer(data, CHUNK_SIZE))
            {
                await _outputStream!.WriteAsync(bloque.Array!, bloque.Offset, bloque.Count, ct);
                await _outputStream.FlushAsync(ct);
                await Task.Delay(1, ct);
            }
        }
        catch (System.IO.IOException)
        {
            InvalidateConnection();
            throw;
        }
        catch (Java.IO.IOException)
        {
            InvalidateConnection();
            throw;
        }
    }

    /// <summary>
    /// GS ( L fn=64: consulta la lista de keycodes de NV graphics definidos. Busqueda best-effort
    /// del par (kc1,kc2) del keycode en la respuesta. Si responde y aparece -> true. Nunca lanza.
    /// </summary>
    private async Task<bool> IsKeycodeDefinedViaGslAsync(int keycode, CancellationToken ct)
    {
        try
        {
            DrainInput();
            await WriteRawAsync(new byte[] { 0x1D, 0x28, 0x4C, 0x02, 0x00, 0x30, 0x40 }, ct); // GS ( L fn=64
            var resp = await ReadAvailableAsync(255, 400);
            if (resp == null || resp.Length == 0) return false;

            byte kc1 = (byte)(keycode & 0xFF);
            byte kc2 = (byte)((keycode >> 8) & 0xFF);
            for (int i = 0; i + 1 < resp.Length; i++)
                if (resp[i] == kc1 && resp[i + 1] == kc2) return true;
            return false;
        }
        catch
        {
            return false;
        }
    }

    // ─── Builders de comandos NV (privados del transport; no usan EscPosCommands de Rendering) ───

    /// <summary>
    /// GS ( L fn=67 (0x43): define NV graphics en formato raster, por keycode.
    /// Formato: GS ( L pL pH m=48 fn=67 a=48 kc1 kc2 b=1 xL xH(=width en dots) yL yH(=alto) c=49 bits.
    /// </summary>
    private static byte[] BuildGslDefine(int keycode, int widthBytes, int heightDots, byte[] bits)
    {
        int widthDots = widthBytes * 8;
        byte kc1 = (byte)(keycode & 0xFF);
        byte kc2 = (byte)((keycode >> 8) & 0xFF);

        // p = (m fn a kc1 kc2 b xL xH yL yH c) = 11 bytes + datos
        int p = 11 + bits.Length;

        var cmd = new List<byte>(13 + bits.Length)
        {
            0x1D, 0x28, 0x4C,                               // GS ( L
            (byte)(p & 0xFF), (byte)((p >> 8) & 0xFF),      // pL pH
            0x30,                                           // m = 48
            0x43,                                           // fn = 67 (define raster NV graphics)
            0x30,                                           // a = 48 (tono / monocromo)
            kc1, kc2,                                       // keycode
            0x01,                                           // b = 1 color
            (byte)(widthDots & 0xFF), (byte)((widthDots >> 8) & 0xFF), // xL xH (ancho en dots)
            (byte)(heightDots & 0xFF), (byte)((heightDots >> 8) & 0xFF), // yL yH (alto en dots)
            0x31                                            // c = color 1
        };
        cmd.AddRange(bits);
        return cmd.ToArray();
    }

    /// <summary>
    /// GS ( L fn=66 (0x42): borra el NV graphics del keycode dado.
    /// Formato: GS ( L pL pH m=48 fn=66 kc1 kc2.
    /// </summary>
    private static byte[] BuildGslDelete(int keycode)
    {
        byte kc1 = (byte)(keycode & 0xFF);
        byte kc2 = (byte)((keycode >> 8) & 0xFF);
        return new byte[] { 0x1D, 0x28, 0x4C, 0x04, 0x00, 0x30, 0x42, kc1, kc2 };
    }

    /// <summary>
    /// FS q (1C 71): define UNA NV bit image clasica (imagen #1). HONESTIDAD: el layout exacto de
    /// FS q es especifico de la impresora; aca se re-emiten los bits raster con xL/xH = bytes por
    /// fila y yL/yH = alto en dots. Best-effort, sin garantia de render correcto en toda impresora.
    /// </summary>
    private static byte[] BuildFsqDefine(int widthBytes, int heightDots, byte[] bits)
    {
        var cmd = new List<byte>(8 + bits.Length)
        {
            0x1C, 0x71,                                     // FS q
            0x01,                                           // n = 1 imagen
            (byte)(widthBytes & 0xFF), (byte)((widthBytes >> 8) & 0xFF), // xL xH (bytes por fila)
            (byte)(heightDots & 0xFF), (byte)((heightDots >> 8) & 0xFF)  // yL yH (alto en dots)
        };
        cmd.AddRange(bits);
        return cmd.ToArray();
    }
}
#endif
