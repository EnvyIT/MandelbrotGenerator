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
        private static volatile Bitmap _bitmap;
        private const ushort Scale = 120;
        private static readonly List<Area> _areas = new List<Area>(Scale * Scale);

        public void GenerateImage(Area area)
        {
            SplitAreas(area);
            _bitmap = new Bitmap(area.Width, area.Height);
            ThreadPool.SetMaxThreads(Environment.ProcessorCount, Environment.ProcessorCount);

            using (var countdownEvent = new CountdownEvent(_areas.Count))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                for (int i = 0; i < _areas.Count; ++i)
                {
                    var index = i;
                    ThreadPool.QueueUserWorkItem(param =>
                    {
                        GenerateMandelbrotSet(_areas[index]);
                        countdownEvent.Signal();
                    });
                }

                countdownEvent.Wait();
                stopwatch.Stop();
                OnImageGenerated(area, _bitmap, stopwatch.Elapsed);
            }

        }

        private void SplitAreas(Area area)
        {
            _areas.Clear();
            var widthStep = area.Width / Scale;
            var heightStep = area.Height / Scale;
            var widthDiff = Math.Abs(area.Width - widthStep * Scale);
            var heightDiff = Math.Abs(area.Height - heightStep * Scale);
            var fromWidth = 0;
            var toWidth = widthStep;
            var fromHeight = 0;
            var toHeight = heightStep;

            for (var row = 0; row < Scale; ++row)
            {
                if (Scale - 1 == row)
                {
                    toWidth += widthDiff;
                }
                for (int col = 0; col < Scale; ++col)
                {
                    if (Scale - 1 == col)
                    {
                        toHeight += heightDiff;
                    }
                    _areas.Add(new Area(area, fromWidth, toWidth, fromHeight, toHeight));
                    fromHeight = toHeight;
                    toHeight += heightStep;
                }
                fromWidth = toWidth;
                toWidth += widthStep;
                fromHeight = 0;
                toHeight = heightStep;
            }
        }

        private void OnImageGenerated(Area area, Bitmap bitmap, TimeSpan elapsed)
        {
            ImageGenerated?.Invoke(this, new EventArgs<Tuple<Area, Bitmap, TimeSpan>>(
                new Tuple<Area, Bitmap, TimeSpan>(area, bitmap, elapsed))
                );
        }

        private void GenerateMandelbrotSet(Area area)
        {
            int maxIterations = Settings.DefaultSettings.MaxIterations;
            double zBorder = Settings.DefaultSettings.ZBorder * Settings.DefaultSettings.ZBorder;
            double cReal, cImg, zReal, zImg, zNewReal, zNewImg;

            for (int i = area.FromWidth; i < area.ToWidth; ++i)
            {
                for (int j = area.FromHeight; j < area.ToHeight; ++j)
                {
                    cReal = area.MinReal + i * area.PixelWidth;
                    cImg = area.MinImg + j * area.PixelHeight;

                    zReal = 0;
                    zImg = 0;
                    var k = 0;
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
