using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace MandelbrotGenerator
{
    public class SyncImageGenerator : IImageGenerator
    {
        public CancellationTokenSource cancellationTokenSource;
        public event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;

        public static Bitmap GenerateMandelbrotSet(Area area, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return null;
            }
            int maxIterations;
            double zBorder;
            double cReal, cImg, zReal, zImg, zNewReal, zNewImg;

            maxIterations = Settings.DefaultSettings.MaxIterations;
            zBorder = Settings.DefaultSettings.ZBorder * Settings.DefaultSettings.ZBorder;

            Bitmap bitmap = new Bitmap(area.Width, area.Height);

            for (int i = 0; i < area.Width; ++i)
            {
                for (int j = 0; j < area.Height; ++j)
                {
                    cReal = area.MinReal + i * area.PixelWidth;
                    cImg = area.MinImg + j * area.PixelHeight;

                    zReal = 0;
                    zImg = 0;
                    int k = 0;
                    while ((zReal * zReal + zImg * zImg < zBorder) && k < maxIterations)
                    {
                        zNewReal = zReal * zReal - zImg * zImg + cReal;
                        zNewImg = 2 * zReal * zImg + cImg;
                        zReal = zNewReal;
                        zImg = zNewImg;
                        ++k;
                    }
                    bitmap.SetPixel(i, j, ColorSchema.GetColor(k));
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return null;
                    }
                }
            }
            return bitmap;
        }

        public void GenerateImage(Area area)
        {
            if(cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel(false);
            }

            cancellationTokenSource = new CancellationTokenSource();
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Bitmap bitmap = GenerateMandelbrotSet(area, cancellationTokenSource.Token);
            stopwatch.Stop();

            if (bitmap != null)
            {
                //fire event
                OnImageGenerated(area, bitmap, stopwatch.Elapsed);
            }
        }

        private void OnImageGenerated(Area area, Bitmap bitmap, TimeSpan elapsed)
        {
            var tuple = new Tuple<Area, Bitmap, TimeSpan>(area, bitmap, elapsed);
            var handler = ImageGenerated;
            if (handler != null)
            {
                handler.Invoke(this, new EventArgs<Tuple<Area, Bitmap, TimeSpan>>(tuple));
            }
        }
    }
}
