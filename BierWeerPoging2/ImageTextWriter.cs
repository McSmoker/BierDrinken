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

            SKBitmap sKBitmap = SKBitmap.Decode(responseContent);
            //SKBitmap sKBitmap = new SKBitmap(900, 900);
            SKCanvas canvas = new SKCanvas(sKBitmap);
            //
            //
            using (SKPaint paint = new SKPaint())
            {
                paint.Color = SKColors.Blue;
                paint.TextAlign = SKTextAlign.Center;
                paint.TextSize = 16;

                canvas.DrawText(textToWrite, 250, 50, paint);
            }

            //
            //
            // get the bitmap we want to convert to a stream
            SKBitmap bitmap = sKBitmap;

            // create an image COPY
            SKImage image = SKImage.FromBitmap(bitmap);
            // OR
            // create an image WRAPPER
            //SKImage image = SKImage.FromPixels(bitmap.PeekPixels());

            // encode the image (defaults to PNG)
            SKData encoded = image.Encode();

            // get a stream over the encoded data
            Stream stream = encoded.AsStream();
            //
            //
            //
            return stream;
        }
    }
}
