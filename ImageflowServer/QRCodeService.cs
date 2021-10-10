
using System.Drawing;
using System.IO;
using ZXing.QrCode;

namespace ImageflowServer
{
    public static class QRCodeService
    {
        private static Bitmap zXingQREncode(string toCode, int width = 1500, int height = 1500)
        {
            ZXing.IBarcodeWriter qrWriter = new ZXing.BarcodeWriter
            {
                Format = ZXing.BarcodeFormat.QR_CODE,
                Renderer = new ZXing.Rendering.BitmapRenderer(),
                Options = new QrCodeEncodingOptions
                {
                    Width = width,
                    Height = height,
                    //PureBarcode = true,
                    Margin = 1
                }
            };

            Bitmap barcodeBitmap = new Bitmap(width, height);

            ZXing.Common.BitMatrix bm = qrWriter.Encode(toCode);

            int bottomQRPixelPos = bm.getBottomRightOnBit()[0];
            int topQRPixelPos = bm.getTopLeftOnBit()[0];


            ZXing.Rendering.BitmapRenderer r = new ZXing.Rendering.BitmapRenderer();

            //r.Background = Color.Red;
            //r.Foreground = Color.Green;
            //r.TextFont = new Font("Arial", 60, FontStyle.Regular, GraphicsUnit.Pixel);
            barcodeBitmap.SetResolution(300, 300);

            return r.Render(bm, ZXing.BarcodeFormat.QR_CODE, toCode);

            //var result = qrWriter.Write(toCode);

            //barcodeBitmap = new Bitmap(result);

        }


        public static Bitmap GenerateQRCode(string toCode, int width, int height)
        {
            Bitmap barcodeBitmap = zXingQREncode(toCode, width, height);

            return barcodeBitmap;
        }


    }
}
