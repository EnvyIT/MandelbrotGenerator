using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace MandelbrotGenerator
{
    public class ParallelImageGenerator : IImageGenerator
    {
        private static readonly object _lock = new object();
        public CancellationTokenSource cancellationTokenSource;
        public event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;
        private List<Thread> _threads;
        private static volatile Bitmap _bitmap;

        public void GenerateImage(Area area)
        {
            var processorCount = Environment.ProcessorCount;
            _bitmap = new Bitmap(area.Width, area.Height);
            _threads = new List<Thread>(processorCount);

            var partWidth = area.Width / processorCount;
            var partReal = Math.Abs(area.MinReal - area.MaxReal) / processorCount;
            var lastDiff = Math.Abs(area.Width - partWidth * processorCount);
            var fromWidth = 0;
            var toWidth = partWidth;
            var fromReal = area.MinReal;
            var toReal = fromReal + partReal;

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < processorCount; ++i)
            {
                if (processorCount - 1 == i)
                {
                    toWidth += lastDiff;
                }
                _threads.Add(new Thread(new ParameterizedThreadStart(Run)));
                var newArea = new Area(area, fromWidth, toWidth, fromReal, toReal);
                _threads[i].Start(newArea);
                toWidth += partWidth;
                fromWidth += partWidth;
                toReal += partReal;
                fromReal += partReal;
            }
            _threads.ForEach(thread => thread.Join());
            stopwatch.Stop();
            OnImageGenerated(area, _bitmap, stopwatch.Elapsed);
        }

        private void Run(object parameter)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel(false);
            }
            cancellationTokenSource = new CancellationTokenSource();
            var area = parameter as Area;
            GenerateMandelbrotSet(area, cancellationTokenSource.Token);
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

        private void GenerateMandelbrotSet(Area area, CancellationToken cancellationToken)
        {
            int maxIterations = Settings.DefaultSettings.MaxIterations;
            double zBorder = Settings.DefaultSettings.ZBorder * Settings.DefaultSettings.ZBorder;
            double cReal, cImg, zReal, zImg, zNewReal, zNewImg;

            for (int i = area.FromWidth; i < area.ToWidth; ++i)
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
                    lock (_lock)
                    {
                        _bitmap.SetPixel(i, j, ColorSchema.GetColor(k));
                    }
                }
            }
        }

    }
}
