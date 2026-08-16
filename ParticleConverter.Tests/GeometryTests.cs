using OpenCvSharp;
using ParticleConverter.util;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ParticleConverter.Tests
{
    /// <summary>
    /// The coordinate maths: density, alignment, axis mapping and rotation.
    /// </summary>
    public class GeometryTests : IDisposable
    {
        private readonly string workDirectory;

        public GeometryTests()
        {
            workDirectory = Path.Combine(Path.GetTempPath(), "pc-geo-" + Path.GetRandomFileName());
            Directory.CreateDirectory(workDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(workDirectory)) Directory.Delete(workDirectory, recursive: true);
        }

        /// <summary>Writes a fully opaque image of the given size so every pixel becomes a particle.</summary>
        private string WriteOpaque(int width, int height)
        {
            string path = Path.Combine(workDirectory, $"solid_{width}x{height}.png");
            using var mat = new Mat(height, width, MatType.CV_8UC4, new Scalar(0, 0, 255, 255));
            Cv2.ImWrite(path, mat);
            return path;
        }

        private const int Left = 0, HCenter = 1, Right = 2;
        private const int Top = 0, VCenter = 1, Bottom = 2;
        private const int AxisXY = 0;

        [Fact]
        public void Density_is_pixels_per_block()
        {
            var converter = new ImageConverter(WriteOpaque(32, 16)) { Density = 8 };

            System.Windows.Size blocks = converter.GetBlocks();

            Assert.Equal(4.0, blocks.Width);
            Assert.Equal(2.0, blocks.Height);
        }

        [Fact]
        public void Bottom_left_alignment_puts_the_image_up_and_to_the_right_of_the_origin()
        {
            var converter = new ImageConverter(WriteOpaque(32, 16)) { Density = 8 };

            Particle[] p = converter.GetParticles(AxisXY, Bottom, Left);

            Assert.Equal(0.0, p.Min(q => q.x), 6);
            Assert.Equal(3.875, p.Max(q => q.x), 6);   // 4 blocks wide, less one 1/8 step
            Assert.Equal(0.125, p.Min(q => q.y), 6);
            Assert.Equal(2.0, p.Max(q => q.y), 6);
        }

        [Fact]
        public void Centre_alignment_straddles_the_origin()
        {
            var converter = new ImageConverter(WriteOpaque(32, 16)) { Density = 8 };

            Particle[] p = converter.GetParticles(AxisXY, VCenter, HCenter);

            Assert.Equal(-2.0, p.Min(q => q.x), 6);
            Assert.Equal(1.875, p.Max(q => q.x), 6);
            Assert.Equal(-0.875, p.Min(q => q.y), 6);
            Assert.Equal(1.0, p.Max(q => q.y), 6);
        }

        [Fact]
        public void Image_rows_are_flipped_because_screen_y_grows_downward_and_world_y_grows_up()
        {
            // Top-left pixel of the source must end up at the highest world y.
            var converter = new ImageConverter(WriteOpaque(8, 8)) { Density = 8 };

            Particle[] p = converter.GetParticles(AxisXY, Bottom, Left);
            Particle topLeft = p.First();

            Assert.Equal(0.0, topLeft.x, 6);
            Assert.Equal(1.0, topLeft.y, 6);
            Assert.Equal(1.0, p.Max(q => q.y), 6);
        }

        [Fact]
        public void Zx_axis_lays_the_image_flat()
        {
            var converter = new ImageConverter(WriteOpaque(16, 16)) { Density = 8 };

            Particle[] p = converter.GetParticles(coordinateAxis: 2, Bottom, Left);

            Assert.All(p, q => Assert.Equal(0.0, q.y));
            Assert.True(p.Max(q => q.z) > 0);
            Assert.True(p.Max(q => q.x) > 0);
        }

        [Fact]
        public void Rotating_a_non_square_image_breaks_alignment()
        {
            // Known defect, inherited from upstream. GetParticles derives its alignment offsets
            // from GetBlocks(), which uses the pre-rotation ResizedWidth/Height, but then walks
            // the post-rotation bitmap. At 90 degrees a 40x20 image becomes 20x40 pixels while
            // the offsets still describe a 5.0 x 2.5 block box, so the extra rows run off the
            // bottom: "Bottom" alignment should never produce a negative y, and here it does.
            var converter = new ImageConverter(WriteOpaque(40, 20)) { Density = 8, Angle = 90 };

            Particle[] p = converter.GetParticles(AxisXY, Bottom, Left);

            Assert.True(p.Min(q => q.y) < 0,
                "if this now passes at y >= 0 the rotation offset bug has been fixed - update the docs");
            Assert.Equal(-2.375, p.Min(q => q.y), 6);
        }

        [Fact]
        public void Rotation_by_90_degrees_swaps_the_pixel_extents()
        {
            var converter = new ImageConverter(WriteOpaque(40, 20)) { Density = 8, Angle = 90 };

            Particle[] p = converter.GetParticles(AxisXY, Bottom, Left);

            // The bounding box becomes 20 wide by 40 tall, so x now spans the shorter side.
            Assert.True(p.Max(q => q.x) < p.Max(q => q.y) - p.Min(q => q.y));
        }

        [Fact]
        public void Rotation_shaves_edge_pixels_because_it_resamples_with_cubic()
        {
            // WarpAffine uses InterpolationFlags.Cubic, which samples a neighbourhood. On the
            // outermost row and column that neighbourhood falls partly outside the source, so
            // those pixels come back transparent and never become particles. An unrotated
            // 40x20 gives all 800; rotated it gives fewer.
            string image = WriteOpaque(40, 20);

            int unrotated = new ImageConverter(image) { Density = 8 }
                .GetParticles(AxisXY, Bottom, Left).Length;
            int rotated = new ImageConverter(image) { Density = 8, Angle = 90 }
                .GetParticles(AxisXY, Bottom, Left).Length;

            Assert.Equal(800, unrotated);
            Assert.True(rotated < unrotated, $"expected edge loss, got {rotated} of {unrotated}");
        }

        [Fact]
        public void Square_images_rotate_without_the_alignment_problem()
        {
            // The offsets are only wrong when rotation changes the bounding box.
            var converter = new ImageConverter(WriteOpaque(24, 24)) { Density = 8, Angle = 90 };

            Particle[] p = converter.GetParticles(AxisXY, Bottom, Left);

            Assert.True(p.Min(q => q.y) >= 0);
        }
    }
}
