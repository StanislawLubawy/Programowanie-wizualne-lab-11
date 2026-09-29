using System;
using QRCoder;

namespace probkibiologiczne.Services;

public class QrSerwis
{
    public byte[] GenerujPngDlaProbki(string probkaId, int pixelsPerModule = 20)
    {
        if (string.IsNullOrWhiteSpace(probkaId)) throw new ArgumentNullException(nameof(probkaId));
        using var qrGenerator = new QRCodeGenerator();
        var data = qrGenerator.CreateQrCode(probkaId, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        return png.GetGraphic(pixelsPerModule);
    }
}
