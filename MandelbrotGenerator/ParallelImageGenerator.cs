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
        private static readonly ushort Scale = 40;


        public void GenerateImage(Area area)
        {
            List<Area> areas = SplitAreas(area);
            _bitmap = new Bitmap(area.Width, area.Height);
            var processorCount = Environment.ProcessorCount;
            ThreadPool.SetMaxThreads(processorCount, processorCount);
           

            using (var countdownEvent = new CountdownEvent(areas.Count))
            {
                Stopwatch stopwatch = new Stopwatch();
                stopwatch.Start();

                areas.ForEach(a =>
                {
                    ThreadPool.QueueUserWorkItem(param =>
                    {
                        Run(a);
                        countdownEvent.Signal();
                    });
                });

                countdownEvent.Wait();
                stopwatch.Stop();
                OnImageGenerated(area, _bitmap, stopwatch.Elapsed);
            }

        }

        private List<Area> SplitAreas(Area area)
        {
            var areas = new List<Area>(Scale * Scale);
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
                    areas.Add(new Area(area, fromWidth, toWidth, fromHeight, toHeight));
                    fromHeight = toHeight;
                    toHeight += heightStep;
                }
                fromWidth = toWidth;
                toWidth += widthStep;
                fromHeight = 0;
                toHeight = heightStep;
            }
            return areas;
        }

        private void Run(object state)
        {
            GenerateMandelbrotSet(state as Area);
        }

        private void OnImageGenerated(Area area, Bitmap bitmap, TimeSpan elapsed)
        {
            var tuple = new Tuple<Area, Bitmap, TimeSpan>(area, bitmap, elapsed);
            ImageGenerated?.Invoke(this, new EventArgs<Tuple<Area, Bitmap, TimeSpan>>(tuple));
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
