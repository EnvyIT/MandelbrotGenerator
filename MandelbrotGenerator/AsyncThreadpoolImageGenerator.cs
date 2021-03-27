using System;
using System.Diagnostics;
using System.Drawing;
using System.Threading;

namespace MandelbrotGenerator
{
    public class AsyncThreadPoolImageGenerator : IImageGenerator
    {
        public CancellationTokenSource cancellationTokenSource;
        public event EventHandler<EventArgs<Tuple<Area, Bitmap, TimeSpan>>> ImageGenerated;

        public void GenerateImage(Area area)
        {
            ThreadPool.QueueUserWorkItem(Run, area);
        }

        private void Run(object parameter)
        {
            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel(false);
            }
            cancellationTokenSource = new CancellationTokenSource();

            var area = parameter as Area;
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            var bitmap = SyncImageGenerator.GenerateMandelbrotSet(area, cancellationTokenSource.Token);
            stopwatch.Stop();

            OnImageGenerated(area, bitmap, stopwatch.Elapsed);
        }

        private void OnImageGenerated(Area area, Bitmap bitmap, TimeSpan elapsed)
        {
            var tuple = new Tuple<Area, Bitmap, TimeSpan>(area, bitmap, elapsed);
            ImageGenerated?.Invoke(this, new EventArgs<Tuple<Area, Bitmap, TimeSpan>>(tuple));         
        }
    }
}
