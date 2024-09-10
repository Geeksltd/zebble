using System;
using Olive;

namespace Zebble
{
    [EscapeGCop("Small names are fine here.")]
    internal static class GaussianBlur
    {
        public static byte[] Blur(byte[] image, int width, int height, int bitsPerPixel, int radial)
        {
            var newRed = new byte[width * height];
            var newGreen = new byte[width * height];
            var newBlue = new byte[width * height];
            var result = new byte[width * height * bitsPerPixel];

            var red = new byte[image.Length / bitsPerPixel];
            var green = new byte[image.Length / bitsPerPixel];
            var blue = new byte[image.Length / bitsPerPixel];

            for (var i = 0; i < image.Length / bitsPerPixel; i++)
            {
                var index = i * bitsPerPixel;

                // skipping alpha
                blue[i] = image[index];
                green[i] = image[index + 1];
                red[i] = image[index + 2];
            }

            GaussBlur(red, newRed, radial, width, height);
            GaussBlur(green, newGreen, radial, width, height);
            GaussBlur(blue, newBlue, radial, width, height);

            for (var i = 0; i < result.Length / bitsPerPixel; i++)
            {
                var index = i * bitsPerPixel;

                result[index] = newBlue[i];
                result[index + 1] = newGreen[i];
                result[index + 2] = newRed[i];
                result[index + 3] = image[index + 3];
            }

            return result;
        }

        static void GaussBlur(byte[] source, byte[] target, int radius, int width, int height)
        {
            var rs = (int)Math.Ceiling(radius * 2.57); // significant radius

            var twoR2 = 2 * radius * radius;
            var piTwoR2 = Math.PI * twoR2;
            var maxX = width - 1;

            for (var i = 0; i < height; i++)
            {
                var minY = i - rs;
                var maxY = i + rs + 1;

                for (var j = 0; j < width; j++)
                {
                    var maxJX = j + rs + 1;
                    var minX = j - rs;
                    var val = 0D;
                    var wsum = 0D;

                    for (int iy = minY; iy < maxY; iy++)
                    {
                        var currentY = Math.Min(height - 1, Math.Max(0, iy));
                        var yWidth = currentY * width;
                        var dsqy = (iy - i) * (iy - i);

                        for (int ix = minX; ix < maxJX; ix++)
                        {
                            var currentX = ix.LimitWithin(0, maxX);
                            var dsq = (ix - j) * (ix - j) + dsqy;
                            var wght = Math.Exp(-dsq / twoR2) / piTwoR2;
                            val += source[yWidth + currentX] * wght;
                            wsum += wght;
                        }
                    }

                    target[i * width + j] = (byte)Math.Round(val / wsum);
                }
            }
        }

        static void GaussBlur2(byte[] scl, byte[] tcl, int w, int h, int r)
        {
            var bxs = BoxesForGauss(r, 3);
            BoxBlur(scl, tcl, w, h, (bxs[0] - 1) / 2);
            BoxBlur(tcl, scl, w, h, (bxs[1] - 1) / 2);
            BoxBlur(scl, tcl, w, h, (bxs[2] - 1) / 2);
        }

        static void BoxBlur(byte[] scl, byte[] tcl, int w, int h, int r)
        {
            for (var i = 0; i < scl.Length; i++) tcl[i] = scl[i];
            BoxBlurH(tcl, scl, w, h, r);
            BoxBlurT(scl, tcl, w, h, r);
        }

        static int[] BoxesForGauss(int sigma, int n)  // standard deviation, number of boxes
        {
            var wIdeal = Math.Sqrt((12 * sigma * sigma / n) + 1);  // Ideal averaging filter width 
            var wl = (int)Math.Floor(wIdeal); if (wl % 2 == 0) wl--;
            var wu = wl + 2;

            var mIdeal = (12 * sigma * sigma - n * wl * wl - 4 * n * wl - 3 * n) / (-4 * wl - 4);
            var m = (int)Math.Round((double)mIdeal);
            // var sigmaActual = Math.sqrt( (m*wl*wl + (n-m)*wu*wu - n)/12 );

            var sizes = new int[n];
            for (var i = 0; i < n; i++)
                sizes[i] = (i < m ? wl : wu);

            return sizes;
        }

        static void BoxBlurH(byte[] scl, byte[] tcl, int w, int h, int r)
        {
            var iarr = 1 / (r + r + 1);
            for (var i = 0; i < h; i++)
            {
                var ti = i * w;
                var li = ti;
                var ri = ti + r;
                var fv = scl[ti];
                var lv = scl[ti + w - 1];
                var val = (r + 1) * fv;

                void blur() => tcl[ti++] = (byte)Math.Round((double)val * iarr);

                for (var j = 0; j < r; j++) val += scl[ti + j];
                for (var j = 0; j <= r; j++) { val += scl[ri++] - fv; blur(); }
                for (var j = r + 1; j < w - r; j++) { val += scl[ri++] - scl[li++]; blur(); }
                for (var j = w - r; j < w; j++) { val += lv - scl[li++]; blur(); }
            }
        }

        static void BoxBlurT(byte[] scl, byte[] tcl, int w, int h, int r)
        {
            var iarr = 1 / (r + r + 1);
            for (var i = 0; i < w; i++)
            {
                var ti = i;
                var li = ti;
                var ri = ti + r * w;
                var fv = scl[ti];
                var lv = scl[ti + w * (h - 1)];
                var val = (r + 1) * fv;

                void blur() => tcl[ti] = (byte)Math.Round((double)val * iarr);

                for (var j = 0; j < r; j++) val += scl[ti + j * w];
                for (var j = 0; j <= r; j++) { val += scl[ri] - fv; blur(); ri += w; ti += w; }
                for (var j = r + 1; j < h - r; j++) { val += scl[ri] - scl[li]; blur(); li += w; ri += w; ti += w; }
                for (var j = h - r; j < h; j++) { val += lv - scl[li]; blur(); li += w; ti += w; }
            }
        }
    }
}