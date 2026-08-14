using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace ParticleConverter.util
{

    public struct Particle
    {
        public double x;
        public double y;
        public double z;
        public byte r;
        public byte g;
        public byte b;
    }

    public enum CoordinateAxis
    {
        XY = 0,
        YZ = 1,
        ZX = 2,
    }

    // 画像ファイルをパーティクルに変換するためのクラス
    class ImageConverter
    {
        private Mat SourseImage; // もとの画像 (常にCV_8UC4)

        public bool IsLoaded = false;

        private int _SourseWidth;
        private int _SourseHeight;
        private int _ResizedWidth;
        private int _ResizedHeight;

        private bool _IsFlip = false;

        private int _Angle = 0;
        private double _Density = 8;


        public int SourseWidth { get => this._SourseWidth; set => SetProperty(ref this._SourseWidth, value); }
        public int SourseHeight { get => this._SourseHeight; set => SetProperty(ref this._SourseHeight, value); }
        public int ResizedWidth { get => this._ResizedWidth; set => SetProperty(ref this._ResizedWidth, value); }
        public int ResizedHeight { get => this._ResizedHeight; set => SetProperty(ref this._ResizedHeight, value); }

        public bool IsFlip { get => this._IsFlip; set => SetProperty(ref this._IsFlip, value); }

        public int Angle { get => this._Angle; set => SetProperty(ref this._Angle, value); }
        public double Density { get => this._Density; set => SetProperty(ref this._Density, value); }

        public ImageConverter()
        {
        }

        public ImageConverter(string imagePath)
        {
            Load(imagePath);
        }

        private static void SetProperty(ref int propety, int value) => propety = value;

        private static void SetProperty(ref double propety, double value) => propety = value;

        private static void SetProperty(ref bool propety, bool value) => propety = value;


        /// <summary>
        /// 画像ファイルをよみこむ
        /// </summary>
        /// <param name="imagePath">画像ファイルのパス</param>
        public void Load(string imagePath)
        {
            using (Mat loaded = new Mat(imagePath, ImreadModes.Unchanged))
            {
                if (loaded.Empty())
                {
                    throw new IOException($"Could not decode an image from '{imagePath}'.");
                }

                Mat normalized = Normalize(loaded);
                SourseImage?.Dispose();
                SourseImage = normalized;
            }

            SourseWidth = SourseImage.Width;
            SourseHeight = SourseImage.Height;
            ResizedHeight = SourseImage.Height;
            ResizedWidth = SourseImage.Width;
            IsLoaded = true;
        }

        /// <summary>
        /// 8ビット4チャンネル(BGRA)に統一する
        /// </summary>
        /// <remarks>
        /// GetParticles reads four bytes per pixel. A JPEG decodes to three channels and a
        /// greyscale PNG to one, so without this every colour would be misread and the last row
        /// would be read past its end. Both .jpg and .png are offered in the file picker.
        /// 16-bit PNGs are scaled down to 8-bit for the same reason.
        /// </remarks>
        private static Mat Normalize(Mat source)
        {
            Mat depthCorrected = source;
            bool disposeDepthCorrected = false;

            if (source.Depth() != MatType.CV_8U)
            {
                depthCorrected = new Mat();
                disposeDepthCorrected = true;
                // 16U covers the common case (16-bit PNG); anything else is best-effort.
                double scale = source.Depth() == MatType.CV_16U ? 255.0 / 65535.0 : 1.0;
                source.ConvertTo(depthCorrected, MatType.CV_8U, scale);
            }

            try
            {
                Mat bgra = new Mat();
                switch (depthCorrected.Channels())
                {
                    case 4:
                        return depthCorrected.Clone();
                    case 3:
                        Cv2.CvtColor(depthCorrected, bgra, ColorConversionCodes.BGR2BGRA);
                        return bgra;
                    case 1:
                        Cv2.CvtColor(depthCorrected, bgra, ColorConversionCodes.GRAY2BGRA);
                        return bgra;
                    default:
                        bgra.Dispose();
                        throw new NotSupportedException(
                            $"Unsupported image with {depthCorrected.Channels()} channels.");
                }
            }
            finally
            {
                if (disposeDepthCorrected) depthCorrected.Dispose();
            }
        }

        /// <summary>
        /// ブロックでどの程度の高さになるか返す
        /// </summary>
        /// <param name="density">1Mに描画するパーティクル</param>
        /// <returns></returns>
        public System.Windows.Size GetBlocks()
        {
            return new System.Windows.Size(ResizedWidth / Density, ResizedHeight / Density);
        }

        public Mat GetModifiedImage()
        {
            Mat TempImage = new Mat();
            Cv2.Resize(SourseImage, TempImage, new OpenCvSharp.Size(ResizedWidth, ResizedHeight), 0, 0, InterpolationFlags.Nearest);
            if (IsFlip)
            {
                Mat flipped = TempImage.Flip(FlipMode.Y);
                TempImage.Dispose();
                TempImage = flipped;
            }

            Mat rotated = GetRotatedImage(TempImage, Angle);
            TempImage.Dispose();
            return rotated;
        }

        /// <summary>
        /// 画像の回転
        /// </summary>
        /// <param name="input"></param>
        /// <param name="angle">0度からの回転角(度数)</param>
        public Mat GetRotatedImage(Mat input, int angle)
        {
            Mat RotatedImage = new Mat();
            int w = ResizedWidth;
            int h = ResizedHeight;

            double angleRad = angle / 180.0 * Math.PI;

            Point2f center = new Point2f(ResizedWidth / 2, ResizedHeight / 2);
            int wRot = (int)Math.Floor(h * Math.Abs(Math.Sin(angleRad)) + w * Math.Abs(Math.Cos(angleRad)));
            int hRot = (int)Math.Floor(h * Math.Abs(Math.Cos(angleRad)) + w * Math.Abs(Math.Sin(angleRad)));

            Mat rotMat = Cv2.GetRotationMatrix2D(center, angle, 1.0);

            // ずらす
            rotMat.At<double>(0, 2) = Math.Floor(rotMat.At<double>(0, 2) - w / 2.0 + wRot / 2.0);
            rotMat.At<double>(1, 2) = Math.Floor(rotMat.At<double>(1, 2) - h / 2.0 + hRot / 2.0);

            Cv2.WarpAffine(input, RotatedImage, rotMat, new OpenCvSharp.Size(wRot, hRot), InterpolationFlags.Cubic);
            rotMat.Dispose();
            return RotatedImage;
        }

        //public void Transparent(Vec4b[] gbra)
        //{
        //    for(int y = 0; y < RotatedImage.Rows; y++)
        //    {
        //        for(int x = 0; x < RotatedImage.Cols; x++)
        //        {

        //        }
        //    }
        //}

        public Particle[] GetParticles(int coordinateAxis, int verticalAlignment, int horizontalTextAlignment)
        {
            Mat Image = GetModifiedImage();
            List<Particle> particles = new List<Particle>();

            double span = 1.0 / Density;

            System.Windows.Size blocks = GetBlocks();

            double offsetX = 0;
            double offsetY = 0;

            switch (verticalAlignment)
            {
                //top
                case (int)VerticalAlignment.Top:
                    offsetY -= blocks.Height;
                    break;
                //center
                case (int)VerticalAlignment.Center:
                    offsetY -= blocks.Height / 2;
                    break;
                //bottom
                case (int)VerticalAlignment.Bottom:
                    break;
            }

            switch (horizontalTextAlignment)
            {
                //left
                case (int)HorizontalAlignment.Left:
                    break;
                //center
                case (int)HorizontalAlignment.Center:
                    offsetX -= blocks.Width / 2;
                    break;
                //right
                case (int)HorizontalAlignment.Right:
                    offsetX -= blocks.Width;
                    break;
            }

            // Image is always CV_8UC4 (see Normalize), so walk the rows directly rather than
            // going through a per-pixel marshalling indexer - this runs once per preview update.
            int height = Image.Rows;
            int width = Image.Cols;
            long stride = Image.Step();

            unsafe
            {
                byte* data = (byte*)Image.Data.ToPointer();
                for (int y = 0; y < height; y++)
                {
                    byte* row = data + y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        byte* pix = row + x * 4;
                        // Fully transparent pixels are holes in the image, not black particles.
                        if (pix[3] == 0) continue;

                        Particle particle = new Particle
                        {
                            b = pix[0],
                            g = pix[1],
                            r = pix[2],
                            x = 0,
                            y = 0,
                            z = 0,
                        };
                        switch (coordinateAxis)
                        {
                            case (int)CoordinateAxis.XY:
                                particle.x = offsetX + span * x;
                                particle.y = offsetY + blocks.Height - span * y;
                                break;
                            case (int)CoordinateAxis.YZ:
                                particle.y = offsetX + span * x;
                                particle.z = offsetY + blocks.Height - span * y;
                                break;
                            case (int)CoordinateAxis.ZX:
                                particle.z = offsetX + span * x;
                                particle.x = offsetY + blocks.Height - span * y;
                                break;
                        }
                        particles.Add(particle);
                    }
                }
            }

            Image.Dispose();
            return particles.ToArray();
        }

    }

}
