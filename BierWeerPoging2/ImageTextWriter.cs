using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using SixLabors.ImageSharp;
using SkiaSharp;

namespace BierWeerPoging2
{
    class ImageTextWriter 
    {

        public Stream WriteTextOnImage(Stream responseContent, string textToWrite)
        {
            ///oh nee system.drawing is niet iets wat we accepteren in azure functions
            ///sixlaborers.imagesharp kan wel volgens internet advies
            ///dat werkt ook niet
            ///beter werkt skiasharp
            ///eureka

            //maak canvasvanimage en plaats teksten
            SKBitmap sKBitmap = SKBitmap.Decode(responseContent);
            SKCanvas canvas = new SKCanvas(sKBitmap);


            using (SKPaint paint = new SKPaint())
            {
                paint.Color = SKColors.Blue;
                paint.TextAlign = SKTextAlign.Center;
                paint.TextSize = 16;

                canvas.DrawText(textToWrite, 250, 50, paint);
            }

            //vertaal die canvasimage/bitmap weer naar een stream
            SKBitmap bitmap = sKBitmap;
            SKImage image = SKImage.FromBitmap(bitmap);
            SKData encoded = image.Encode();
            Stream stream = encoded.AsStream();

            return stream;
        }
    }
}
