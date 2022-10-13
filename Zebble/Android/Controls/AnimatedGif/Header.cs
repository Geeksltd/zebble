namespace Zebble.AndroidOS.Gif
{
    using System.Collections.Generic;
    using Android.Graphics;

    internal class Header
    {
        public int[] ColorTable;
        public bool GotColorTableFlag;
        public int ColorTableSize, BackgroundIndex, AspectRatio, BackgroundColor, Width, Height, Loops, TotalFrames;
        public int Status = FramesExtractor.STATUSOK;

        internal List<Frame> Frames = new();
        internal Frame CurrentFrame;

        internal class Frame
        {
            public int Ix, Iy, Iw, Ih, Dispose, TransIndex, Delay, BufferFrameStart;
            public bool Interlace, Transparency;
            public int[] Lct;
            public Bitmap Image;
        }
    }
}