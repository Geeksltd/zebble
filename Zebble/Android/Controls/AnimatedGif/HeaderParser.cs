namespace Zebble.AndroidOS.Gif
{
    using Java.Lang;
    using Java.Nio;
    using Java.Util;
    using Olive;

    internal class HeaderParser
    {
        const int BIT_IGNORE = 0xFF;
        const int SEPARATOR = 0x2C;
        const int EXTENSION = 0x21;
        const int GRAPHIC_EX_CONTROL = 0xf9;
        const int PLAIN_TEXT = 0x01;
        const int COMMENT = 0xfe;
        const int TERMINATOR = 0x3b;

        static readonly int MinFrameDelay = 2, DefaultFrameDelay = 10, MaxBlockSize = 256;
        readonly byte[] Block = new byte[MaxBlockSize];
        ByteBuffer RawData;
        Header Header;
        int BlockSize;

        public HeaderParser SetData(ByteBuffer data)
        {
            Reset();
            RawData = data.AsReadOnlyBuffer();
            RawData.Position(0);
            RawData.Order(ByteOrder.LittleEndian);
            return this;
        }

        public HeaderParser SetData(byte[] data)
        {
            if (data != null) SetData(ByteBuffer.Wrap(data));
            else
            {
                RawData = null;
                Header.Status = FramesExtractor.STATUSOPENERROR;
            }

            return this;
        }

        public void Clear()
        {
            RawData = null;
            Header = null;
        }

        void Reset()
        {
            RawData = null;
            Arrays.Fill(Block, 0);
            Header = new Header();
            BlockSize = 0;
        }

        public Header ParseHeader()
        {
            if (RawData is null) throw new System.Exception("You must call setData() before parseHeader()");
            if (Failed()) return Header;

            ReadHeader();
            if (!Failed())
            {
                ReadContents();
                if (Header.TotalFrames < 0) Header.Status = FramesExtractor.STATUSFORMATERROR;
            }

            return Header;
        }

        public bool IsAnimated()
        {
            ReadHeader();
            if (!Failed()) ReadContents(2);
            return Header.TotalFrames > 1;
        }

        void ReadContents() => ReadContents(Integer.MaxValue);

        void ReadContents(int maxFrames)
        {
            var done = false;
            while (!(done || Failed() || Header.TotalFrames > maxFrames))
            {
                var code = Read();
                switch (code)
                {
                    case SEPARATOR:
                        if (Header.CurrentFrame is null) Header.CurrentFrame = new Header.Frame();
                        ReadBitmap();
                        break;
                    case EXTENSION:
                        code = Read();
                        switch (code)
                        {
                            case GRAPHIC_EX_CONTROL:
                                Header.CurrentFrame = new Header.Frame();
                                ReadGraphicControlExt();
                                break;
                            case BIT_IGNORE:
                                ReadBlock();
                                var app = "";
                                for (int i = 0; i < 11; i++) app += (char)Block[i];
                                if (app.Equals("NETSCAPE2.0")) ReadNetscapeExt();
                                else Skip();
                                break;
                            default:
                                Skip();
                                break;
                        }

                        break;
                    case TERMINATOR: done = true; break;

                    default:
                        Header.Status = FramesExtractor.STATUSFORMATERROR;
                        break;
                }
            }
        }

        void ReadGraphicControlExt()
        {
            Read();
            var packed = Read();
            Header.CurrentFrame.Dispose = (packed & 0x1c) >> 2;
            if (Header.CurrentFrame.Dispose == 0) Header.CurrentFrame.Dispose = 1;
            Header.CurrentFrame.Transparency = (packed & 1) != 0;
            var delayInHundredthsOfASecond = ReadShort();
            if (delayInHundredthsOfASecond < MinFrameDelay) delayInHundredthsOfASecond = DefaultFrameDelay;
            Header.CurrentFrame.Delay = delayInHundredthsOfASecond * 10;
            Header.CurrentFrame.TransIndex = Read();
            Read();
        }

        void ReadBitmap()
        {
            Header.CurrentFrame.Ix = ReadShort();
            Header.CurrentFrame.Iy = ReadShort();
            Header.CurrentFrame.Iw = ReadShort();
            Header.CurrentFrame.Ih = ReadShort();

            var packed = Read();
            var lctFlag = (packed & 0x80) != 0;
            var lctSize = (int)Java.Lang.Math.Pow(2, (packed & 0x07) + 1);
            Header.CurrentFrame.Interlace = (packed & 0x40) != 0;
            if (lctFlag) Header.CurrentFrame.Lct = ReadColorTable(lctSize);
            else Header.CurrentFrame.Lct = null;

            Header.CurrentFrame.BufferFrameStart = RawData.Position();

            SkipImageData();

            if (Failed()) return;

            Header.TotalFrames++;
            Header.Frames.Add(Header.CurrentFrame);
        }

        void ReadNetscapeExt()
        {
            do
            {
                ReadBlock();
                if (Block[0] == 1)
                {
                    var b1 = (Block[1]) & BIT_IGNORE;
                    var b2 = (Block[2]) & BIT_IGNORE;
                    Header.Loops = (b2 << 8) | b1;
                    if (Header.Loops == 0) Header.Loops = FramesExtractor.LOOPFOREVER;
                }
            }
            while ((BlockSize > 0) && !Failed());
        }

        void ReadHeader()
        {
            var id = "";
            for (var i = 0; i < 6; i++) id += (char)Read();
            if (!id.StartsWith("GIF"))
            {
                Header.Status = FramesExtractor.STATUSFORMATERROR;
                return;
            }

            ReadLSD();
            if (Header.GotColorTableFlag && !Failed())
            {
                Header.ColorTable = ReadColorTable(Header.ColorTableSize);
                Header.BackgroundColor = Header.ColorTable[Header.BackgroundIndex];
            }
        }

        void ReadLSD()
        {
            Header.Width = ReadShort();
            Header.Height = ReadShort();
            var packed = Read();
            Header.GotColorTableFlag = (packed & 0x80) != 0;
            Header.ColorTableSize = 2 << (packed & 7);
            Header.BackgroundIndex = Read();
            Header.AspectRatio = Read();
        }

        int[] ReadColorTable(int ncolors)
        {
            var nbytes = 3 * ncolors;
            int[] tab = null;
            var canvas = new byte[nbytes];

            try
            {
                RawData.Get(canvas);

                tab = new int[MaxBlockSize];
                var iIndex = 0;
                var jIndex = 0;
                while (iIndex < ncolors)
                {
                    var red = (canvas[jIndex++]) & BIT_IGNORE;
                    var green = (canvas[jIndex++]) & BIT_IGNORE;
                    var blue = (canvas[jIndex++]) & BIT_IGNORE;
                    tab[iIndex++] = (int)((0xff000000 | (red << 16) | (green << 8) | blue) - 0x100000000);
                }
            }
            catch (BufferUnderflowException ex)
            {
                Log.For(this).Error(ex, "GifHeaderParser: Failed to read color table.");
                Header.Status = FramesExtractor.STATUSFORMATERROR;
            }

            return tab;
        }

        void SkipImageData()
        {
            Read();
            Skip();
        }

        void Skip()
        {
            try
            {
                int blockSize;
                do
                {
                    blockSize = Read();
                    RawData.Position(RawData.Position() + blockSize);
                } while (blockSize > 0);
            }
            catch
            {
                // No logging is needed
            }
        }

        int ReadBlock()
        {
            BlockSize = Read();

            if (BlockSize <= 0) return 0;

            var number = 0;
            var total = 0;
            try
            {
                while (number < BlockSize)
                {
                    total = BlockSize - number;
                    RawData.Get(Block, number, total);
                    number += total;
                }
            }
            catch (Exception ex)
            {
                Log.For(this).Error(ex, $"GifHeaderParser: Failed to read block #{number} with total {total} and block size {BlockSize}.");
                Header.Status = FramesExtractor.STATUSFORMATERROR;
            }

            return number;
        }

        int Read()
        {
            try { return RawData.Get() & BIT_IGNORE; }
            catch
            {
                // No logging is needed
                Header.Status = FramesExtractor.STATUSFORMATERROR;
                return 0;
            }
        }

        int ReadShort() => RawData.Short;

        bool Failed() => Header.Status != FramesExtractor.STATUSOK;
    }
}