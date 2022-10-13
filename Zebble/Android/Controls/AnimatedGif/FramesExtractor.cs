namespace Zebble.AndroidOS.Gif
{
    using System;
    using Android.Graphics;
    using Java.Lang;
    using Java.Nio;
    using Java.Util;
    using Olive;

    [EscapeGCop("Hardcoded numbers are ok here")]
    internal class FramesExtractor : Java.Lang.Object
    {
        public static int STATUSOK;
        public static int STATUSFORMATERROR = 1;
        public static int STATUSOPENERROR = 2;
        public static int STATUSPARTIALDECODE = 3;
        public static int MAXSTACKSIZE = 4096;
        public static int DISPOSALUNSPECIFIED;
        public static int DISPOSALNONE = 1;
        public static int DISPOSALBACKGROUND = 2;
        public static int DISPOSALPREVIOUS = 3;
        public static int NULLCODE = -1;
        public static int INITIALFRAMEPOINTER = -1;
        public static int LOOPFOREVER = -1;
        public static int BYTESPERINTEGER = 4;
        private int[] Act;
        private int[] MainScratch;
        private readonly int[] Pct = new int[256];
        ByteBuffer RawData;
        static readonly int WORKBUFFERSIZE = 16384;
        byte[] WorkBuffer, Block, Suffix, PixelStack, MainPixels;
        HeaderParser Parser;
        short[] Prefix;

        internal int FramePointer, LoopIndex, Status, SampleSize, DownsampledHeight, DownsampledWidth, WorkBufferSize, WorkBufferPosition;
        Header Header;
        Bitmap PreviousImage;
        bool SavePrevious, IsFirstFrameTransparent;

        public FramesExtractor() => Header = new Header();

        ByteBuffer GetData() => RawData;

        public bool Advance()
        {
            if (Header.TotalFrames <= 0) return false;
            if (FramePointer == Header.TotalFrames - 1) LoopIndex++;
            if (Header.Loops != LOOPFOREVER && LoopIndex > Header.Loops) return false;
            FramePointer = (FramePointer + 1) % Header.TotalFrames;
            return true;
        }

        public int GetNextDelay()
        {
            if (Header.TotalFrames <= 0 || FramePointer < 0) return 0;
            if (FramePointer >= Header.TotalFrames) return -1;

            return Header.Frames[FramePointer].Delay;
        }

        public bool SetFrameIndex(int frame)
        {
            if (frame < INITIALFRAMEPOINTER || frame >= Header.TotalFrames) return false;
            FramePointer = frame;
            return true;
        }

        public Bitmap NextFrame()
        {
            if (Header.TotalFrames <= 0 || FramePointer < 0)
            {
                Log.For(this).Warning($"Failed to decode frame {FramePointer} of {Header.TotalFrames}");

                Status = STATUSFORMATERROR;
            }

            if (Status == STATUSFORMATERROR || Status == STATUSOPENERROR)
            {
                Log.For(this).Error($"Failed to decode a frame --> {Status}");
                return null;
            }

            Status = STATUSOK;

            var currentFrame = Header.Frames[FramePointer];
            Header.Frame previousFrame = null;
            var previousIndex = FramePointer - 1;
            if (previousIndex >= 0) previousFrame = Header.Frames[previousIndex];

            if (currentFrame.Image != null)
                return currentFrame.Image;

            Act = currentFrame.Lct ?? Header.ColorTable;
            if (Act is null)
            {
                Log.For(this).Error("Invalid color table found for frame {FramePointer}");
                Status = STATUSFORMATERROR;
                return null;
            }

            if (currentFrame.Transparency)
            {
                Array.Copy(Act, 0, Pct, 0, Act.Length);
                Act = Pct;
                Act[currentFrame.TransIndex] = 0;
            }

            currentFrame.Image = SetPixels(currentFrame, previousFrame);
            return currentFrame.Image;
        }

        void SetData(Header header, byte[] data) => SetData(header, ByteBuffer.Wrap(data));

        void SetData(Header header, ByteBuffer buffer) => SetData(header, buffer, 1);

        void SetData(Header header, ByteBuffer buffer, int sampleSize)
        {
            if (sampleSize <= 0) throw new System.Exception("Sample size must be >=0, not: " + sampleSize);
            sampleSize = Integer.HighestOneBit(sampleSize);
            Status = STATUSOK;
            Header = header;
            IsFirstFrameTransparent = false;
            FramePointer = INITIALFRAMEPOINTER;
            LoopIndex = 0;
            RawData = buffer.AsReadOnlyBuffer();
            RawData.Position(0);
            RawData.Order(ByteOrder.LittleEndian);

            SavePrevious = false;
            foreach (var frame in header.Frames)
            {
                if (frame.Dispose == DISPOSALPREVIOUS)
                {
                    SavePrevious = true;
                    break;
                }
            }

            SampleSize = sampleSize;
            DownsampledWidth = header.Width / sampleSize;
            DownsampledHeight = header.Height / sampleSize;
            MainPixels = new byte[header.Width * header.Height];
            MainScratch = new int[DownsampledWidth * DownsampledHeight];
        }

        HeaderParser GetHeaderParser()
        {
            if (Parser is null) Parser = new HeaderParser();
            return Parser;
        }

        public int Read(byte[] data)
        {
            Header = GetHeaderParser().SetData(data).ParseHeader();
            if (data != null) SetData(Header, data);
            return Status;
        }

        Bitmap SetPixels(Header.Frame currentFrame, Header.Frame previousFrame)
        {
            var dest = MainScratch;

            if (previousFrame is null) Arrays.Fill(dest, 0);

            int downsampledIH, downsampledIY, downsampledIW, downsampledIX, topLeft;

            if (previousFrame != null && previousFrame.Dispose > DISPOSALUNSPECIFIED)
            {
                if (previousFrame.Dispose == DISPOSALBACKGROUND)
                {
                    var canvas = 0;
                    if (!currentFrame.Transparency)
                    {
                        canvas = Header.BackgroundColor;
                        if (currentFrame.Lct != null && Header.BackgroundIndex == currentFrame.TransIndex) canvas = 0;
                    }
                    else if (FramePointer == 0) IsFirstFrameTransparent = true;
                    FillRect(dest, previousFrame, canvas);
                }
                else if (previousFrame.Dispose == DISPOSALPREVIOUS)
                {
                    if (PreviousImage is null) FillRect(dest, previousFrame, 0);
                    else
                    {
                        downsampledIH = previousFrame.Ih / SampleSize;
                        downsampledIY = previousFrame.Iy / SampleSize;
                        downsampledIW = previousFrame.Iw / SampleSize;
                        downsampledIX = previousFrame.Ix / SampleSize;
                        topLeft = downsampledIY * DownsampledWidth + downsampledIX;
                        PreviousImage.GetPixels(dest, topLeft, DownsampledWidth,
                            downsampledIX, downsampledIY, downsampledIW, downsampledIH);
                    }
                }
            }

            DecodeBitmapData(currentFrame);

            downsampledIH = currentFrame.Ih / SampleSize;
            downsampledIY = currentFrame.Iy / SampleSize;
            downsampledIW = currentFrame.Iw / SampleSize;
            downsampledIX = currentFrame.Ix / SampleSize;
            int pass = 1, inc = 8, iline = 0;
            var isFirstFrame = FramePointer == 0;
            for (var i = 0; i < downsampledIH; i++)
            {
                var line = i;
                if (currentFrame.Interlace)
                {
                    if (iline >= downsampledIH)
                    {
                        pass++;
                        switch (pass)
                        {
                            case 2: iline = 4; break;
                            case 3: iline = 2; inc = 4; break;
                            case 4: iline = 1; inc = 2; break;
                            default: break;
                        }
                    }

                    line = iline;
                    iline += inc;
                }

                line += downsampledIY;
                if (line > DownsampledHeight) continue;

                var kIndex = line * DownsampledWidth;
                var dx = kIndex + downsampledIX;
                var dlim = dx + downsampledIW;
                if (kIndex + DownsampledWidth < dlim) dlim = kIndex + DownsampledWidth;

                var sx = i * SampleSize * currentFrame.Iw;
                var maxPositionInSource = sx + ((dlim - dx) * SampleSize);
                while (dx < dlim)
                {
                    int averageColor;
                    if (SampleSize == 1)
                    {
                        var currentColorIndex = ((int)MainPixels[sx]) & 0x000000ff;
                        averageColor = Act[currentColorIndex];
                    }
                    else
                        averageColor = AverageColorsNear(sx, maxPositionInSource, currentFrame.Iw);
                    if (averageColor != 0) dest[dx] = averageColor;
                    else if (!IsFirstFrameTransparent && isFirstFrame)
                        IsFirstFrameTransparent = true;
                    sx += SampleSize;
                    dx++;
                }
            }

            if (SavePrevious && (currentFrame.Dispose == DISPOSALUNSPECIFIED
                || currentFrame.Dispose == DISPOSALNONE))
            {
                if (PreviousImage is null) PreviousImage = GetNextBitmap();
                PreviousImage.SetPixels(dest, 0, DownsampledWidth, 0, 0, DownsampledWidth, DownsampledHeight);
            }

            var result = GetNextBitmap();
            result.SetPixels(dest, 0, DownsampledWidth, 0, 0, DownsampledWidth, DownsampledHeight);
            return result;
        }

        void FillRect(int[] dest, Header.Frame frame, int bgColor)
        {
            var downsampledIH = frame.Ih / SampleSize;
            var downsampledIY = frame.Iy / SampleSize;
            var downsampledIW = frame.Iw / SampleSize;
            var downsampledIX = frame.Ix / SampleSize;
            var topLeft = downsampledIY * DownsampledWidth + downsampledIX;
            var bottomLeft = topLeft + downsampledIH * DownsampledWidth;
            for (var left = topLeft; left < bottomLeft; left += DownsampledWidth)
            {
                var right = left + downsampledIW;
                for (var pointer = left; pointer < right; pointer++) dest[pointer] = bgColor;
            }
        }

        int AverageColorsNear(int positionInMainPixels, int maxPositionInMainPixels,
            int currentFrameIw)
        {
            int alphaSum = 0, redSum = 0, greenSum = 0, blueSum = 0, totalAdded = 0;
            for (var i = positionInMainPixels;
                i < positionInMainPixels + SampleSize && i < MainPixels.Length
                    && i < maxPositionInMainPixels; i++)
            {
                var currentColorIndex = ((int)MainPixels[i]) & 0xff;
                var currentColor = Act[currentColorIndex];
                if (currentColor == 0) continue;

                alphaSum += currentColor >> 24 & 0x000000ff;
                redSum += currentColor >> 16 & 0x000000ff;
                greenSum += currentColor >> 8 & 0x000000ff;
                blueSum += currentColor & 0x000000ff;
                totalAdded++;
            }

            for (int i = positionInMainPixels + currentFrameIw;
                i < positionInMainPixels + currentFrameIw + SampleSize && i < MainPixels.Length
                    && i < maxPositionInMainPixels; i++)
            {
                var currentColorIndex = (MainPixels[i]) & 0xff;
                var currentColor = Act[currentColorIndex];
                if (currentColor == 0) continue;

                alphaSum += currentColor >> 24 & 0x000000ff;
                redSum += currentColor >> 16 & 0x000000ff;
                greenSum += currentColor >> 8 & 0x000000ff;
                blueSum += currentColor & 0x000000ff;
                totalAdded++;
            }

            if (totalAdded == 0)
            {
                return 0;
            }
            else
            {
                return ((alphaSum / totalAdded) << 24)
                    | ((redSum / totalAdded) << 16)
                    | ((greenSum / totalAdded) << 8)
                    | (blueSum / totalAdded);
            }
        }

        void DecodeBitmapData(Header.Frame frame)
        {
            WorkBufferSize = 0;
            WorkBufferPosition = 0;
            if (frame != null) RawData.Position(frame.BufferFrameStart);

            int npix = (frame is null) ? Header.Width * Header.Height : frame.Iw * frame.Ih,
            available, clear, codeMask, codeSize, endOfInformation, inCode, oldCode, bits, code, count,
                index, datum, dataSize, first, top, bi, pi;

            if (MainPixels == null || MainPixels.Length < npix) MainPixels = new byte[npix];
            if (Prefix is null) Prefix = new short[MAXSTACKSIZE];
            if (Suffix is null) Suffix = new byte[MAXSTACKSIZE];
            if (PixelStack is null) PixelStack = new byte[MAXSTACKSIZE + 1];

            dataSize = ReadByte();
            clear = 1 << dataSize;
            endOfInformation = clear + 1;
            available = clear + 2;
            oldCode = NULLCODE;
            codeSize = dataSize + 1;
            codeMask = (1 << codeSize) - 1;
            for (code = 0; code < clear; code++)
            {
                Prefix[code] = 0;
                Suffix[code] = (byte)code;
            }

            datum = bits = count = first = top = pi = bi = 0;
            for (index = 0; index < npix;)
            {
                if (count == 0)
                {
                    count = ReadBlock;
                    if (count <= 0)
                    {
                        Status = STATUSPARTIALDECODE;
                        break;
                    }

                    bi = 0;
                }

                datum += ((Block[bi]) & 0xff) << bits;
                bits += 8;
                bi++;
                count--;

                while (bits >= codeSize)
                {
                    code = datum & codeMask;
                    datum >>= codeSize;
                    bits -= codeSize;

                    if (code == clear)
                    {
                        codeSize = dataSize + 1;
                        codeMask = (1 << codeSize) - 1;
                        available = clear + 2;
                        oldCode = NULLCODE;
                        continue;
                    }

                    if (code > available)
                    {
                        Status = STATUSPARTIALDECODE;
                        break;
                    }

                    if (code == endOfInformation) break;

                    if (oldCode == NULLCODE)
                    {
                        PixelStack[top++] = Suffix[code];
                        oldCode = code;
                        first = code;
                        continue;
                    }

                    inCode = code;
                    if (code >= available)
                    {
                        PixelStack[top++] = (byte)first;
                        code = oldCode;
                    }

                    while (code >= clear)
                    {
                        PixelStack[top++] = Suffix[code];
                        code = Prefix[code];
                    }

                    first = (Suffix[code]) & 0xff;
                    PixelStack[top++] = (byte)first;

                    if (available < MAXSTACKSIZE)
                    {
                        Prefix[available] = (short)oldCode;
                        Suffix[available] = (byte)first;
                        available++;
                        if (((available & codeMask) == 0) && (available < MAXSTACKSIZE))
                        {
                            codeSize++;
                            codeMask += available;
                        }
                    }

                    oldCode = inCode;

                    while (top > 0)
                    {
                        MainPixels[pi++] = PixelStack[--top];
                        index++;
                    }
                }
            }

            for (index = pi; index < npix; index++) MainPixels[index] = 0;
        }

        void ReadChunkIfNeeded()
        {
            if (WorkBufferSize > WorkBufferPosition) return;
            if (WorkBuffer is null) WorkBuffer = new byte[WORKBUFFERSIZE];
            WorkBufferPosition = 0;
            WorkBufferSize = Java.Lang.Math.Min(RawData.Remaining(), WORKBUFFERSIZE);
            RawData.Get(WorkBuffer, 0, WorkBufferSize);
        }

        int ReadByte()
        {
            try
            {
                ReadChunkIfNeeded();
                return WorkBuffer[WorkBufferPosition++] & 0xFF;
            }
            catch
            {
                // No logging is needed
                Status = STATUSFORMATERROR;
                return 0;
            }
        }

        int ReadBlock
        {
            get
            {
                var blockSize = ReadByte();
                if (blockSize > 0)
                {
                    try
                    {
                        if (Block is null) Block = new byte[byte.MaxValue];
                        var remaining = WorkBufferSize - WorkBufferPosition;
                        if (remaining >= blockSize)
                        {
                            Array.Copy(WorkBuffer, WorkBufferPosition, Block, 0, blockSize);
                            WorkBufferPosition += blockSize;
                        }
                        else if (RawData.Remaining() + remaining >= blockSize)
                        {
                            Array.Copy(WorkBuffer, WorkBufferPosition, Block, 0, remaining);
                            WorkBufferPosition = WorkBufferSize;
                            ReadChunkIfNeeded();
                            var secondHalfRemaining = blockSize - remaining;
                            Array.Copy(WorkBuffer, 0, Block, remaining, secondHalfRemaining);
                            WorkBufferPosition += secondHalfRemaining;
                        }
                        else Status = STATUSFORMATERROR;
                    }
                    catch (Java.Lang.Exception ex)
                    {
                        Log.For(this).Error(ex, "Failed to read a block.");
                        Status = STATUSFORMATERROR;
                    }
                }

                return blockSize;
            }
        }

        Bitmap GetNextBitmap()
        {
            var config = IsFirstFrameTransparent
                ? Bitmap.Config.Argb8888 : Bitmap.Config.Rgb565;
            var result = Bitmap.CreateBitmap(DownsampledWidth, DownsampledHeight, config);

            result.HasAlpha = true;
            return result;
        }
    }
}