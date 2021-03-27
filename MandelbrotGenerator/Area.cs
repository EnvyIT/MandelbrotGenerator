namespace MandelbrotGenerator
{
    public class Area
    {
        public double MinReal { get; set; }
        public double MinImg { get; set; }
        public double MaxReal { get; set; }
        public double MaxImg { get; set; }
        private int width;
        public int Width { get { return width; } set { if (value > 0) width = value; } }

        public int FromWidth { get; set; } = 0;
        public int ToWidth { get; }
        public int FromHeight { get; private set; }
        public int ToHeight { get; private set; }
        public double FromImg { get; private set; }
        public double ToImg { get; private set; }
        public double FromReal { get; }
        public double ToReal { get; }

        private int height;
        private double pixelWidth;
        private double pixelHeight;

        public int Height { get { return height; } set { if (value > 0) height = value; } }
        public double PixelWidth
        {
            get => pixelWidth;
            set => pixelWidth = value;
        }
        public double PixelHeight
        {
            get => pixelHeight;
            set => pixelHeight = value;
        }

        public Area() : this(-2, -1, 1, 1, 640, 480)
        {
        }

        public Area(double minReal, double minImg, double maxReal, double maxImg, int width, int height)
        {
            MinReal = minReal;
            MinImg = minImg;
            MaxReal = maxReal;
            MaxImg = maxImg;
            Width = width;
            Height = height;
            PixelWidth = (MaxReal - MinReal) / Width;
            PixelHeight = (MaxImg - MinImg) / Height;
        }

        public Area(Area area, int fromWidth, int toWidth, int fromHeight, int toHeight) : this(area.MinReal, area.MinImg, area.MaxReal, area.MaxImg, area.Width, area.Height)
        {
            FromWidth = fromWidth;
            ToWidth = toWidth;
            FromHeight = fromHeight;
            ToHeight = toHeight;
        }


    }
}
