using OpenCvSharp;
using ParticleConverter.Minecraft;
using ParticleConverter.util;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace ParticleConverter.Tests
{
    /// <summary>
    /// End-to-end: a real image file through the converter and out as a datapack.
    /// </summary>
    public class ImageToDatapackTests : IDisposable
    {
        private readonly string workDirectory;

        public ImageToDatapackTests()
        {
            workDirectory = Path.Combine(Path.GetTempPath(), "pc-e2e-" + Path.GetRandomFileName());
            Directory.CreateDirectory(workDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(workDirectory)) Directory.Delete(workDirectory, recursive: true);
        }

        private string WritePng4Channel()
        {
            string path = Path.Combine(workDirectory, "rgba.png");
            using var mat = new Mat(2, 2, MatType.CV_8UC4, Scalar.All(0));
            mat.Set(0, 0, new Vec4b(0, 0, 255, 255));     // BGRA: red
            mat.Set(0, 1, new Vec4b(0, 255, 0, 255));     // green
            mat.Set(1, 0, new Vec4b(255, 0, 0, 255));     // blue
            mat.Set(1, 1, new Vec4b(0, 0, 0, 0));         // transparent
            Cv2.ImWrite(path, mat);
            return path;
        }

        private string WriteJpeg3Channel()
        {
            string path = Path.Combine(workDirectory, "solid.jpg");
            // Solid colour so JPEG's lossy encoding does not shift the result.
            using var mat = new Mat(4, 4, MatType.CV_8UC3, new Scalar(0, 0, 255)); // BGR: red
            Cv2.ImWrite(path, mat, new ImageEncodingParam(ImwriteFlags.JpegQuality, 100));
            return path;
        }

        private string WriteGrayscalePng()
        {
            string path = Path.Combine(workDirectory, "gray.png");
            using var mat = new Mat(2, 2, MatType.CV_8UC1, new Scalar(128));
            Cv2.ImWrite(path, mat);
            return path;
        }

        [Fact]
        public void Transparent_pixels_do_not_become_particles()
        {
            var converter = new ImageConverter(WritePng4Channel());

            Particle[] particles = converter.GetParticles(0, 2, 0);

            Assert.Equal(3, particles.Length);
        }

        [Fact]
        public void Png_channels_are_read_in_the_right_order()
        {
            var converter = new ImageConverter(WritePng4Channel());

            Particle[] particles = converter.GetParticles(0, 2, 0);

            Assert.Contains(particles, p => p.r == 255 && p.g == 0 && p.b == 0);
            Assert.Contains(particles, p => p.r == 0 && p.g == 255 && p.b == 0);
            Assert.Contains(particles, p => p.r == 0 && p.g == 0 && p.b == 255);
        }

        [Fact]
        public void Three_channel_jpegs_produce_the_right_colour()
        {
            // Regression: the reader used to pull four bytes per pixel from a three-byte-per-pixel
            // buffer, so every JPEG came out with shifted colours and the last row overran.
            var converter = new ImageConverter(WriteJpeg3Channel());

            Particle[] particles = converter.GetParticles(0, 2, 0);

            Assert.Equal(16, particles.Length);
            Assert.All(particles, p =>
            {
                Assert.True(p.r > 250, $"expected red, got ({p.r},{p.g},{p.b})");
                Assert.True(p.g < 5, $"expected red, got ({p.r},{p.g},{p.b})");
                Assert.True(p.b < 5, $"expected red, got ({p.r},{p.g},{p.b})");
            });
        }

        [Fact]
        public void Single_channel_images_load_as_grey()
        {
            var converter = new ImageConverter(WriteGrayscalePng());

            Particle[] particles = converter.GetParticles(0, 2, 0);

            Assert.Equal(4, particles.Length);
            Assert.All(particles, p => Assert.True(p.r == p.g && p.g == p.b && p.r == 128));
        }

        [Fact]
        public void A_missing_or_undecodable_file_is_reported_rather_than_crashing_later()
        {
            var converter = new ImageConverter();
            string notAnImage = Path.Combine(workDirectory, "notanimage.png");
            File.WriteAllText(notAnImage, "this is not a png");

            Assert.Throws<IOException>(() => converter.Load(notAnImage));
        }

        [Fact]
        public void An_image_exports_to_a_datapack_that_matches_the_26_2_layout()
        {
            var converter = new ImageConverter(WritePng4Channel());
            Particle[] particles = converter.GetParticles(0, 2, 0);

            McVersionProfile version = McVersionProfile.ById("26.2");
            var settings = new ParticleCommandSettings
            {
                Version = version,
                ParticleId = "dust",
                Scale = 4.0,
                CoordinateMode = CoordinateMode.RelativeLocal,
            };

            string output = Path.Combine(workDirectory, "out");
            DatapackLayout layout = DatapackLayout.Resolve(output, true, "particles", "rgba", version);

            DatapackWriter.WritePackMeta(layout, version, "e2e");
            using (StreamWriter writer = DatapackWriter.OpenFunction(layout))
            {
                foreach (Particle p in particles)
                {
                    writer.WriteLine(ParticleCommand.Build(p.x, p.y, p.z, new McColor(p.r, p.g, p.b), settings));
                }
            }

            string[] lines = File.ReadAllLines(layout.FunctionPath);
            Assert.Equal(3, lines.Length);
            Assert.All(lines, l => Assert.StartsWith("particle minecraft:dust{color:[", l));

            // Size 4 is the point of this port: the old UI refused anything above 1.
            Assert.All(lines, l => Assert.Contains("scale:4}", l));

            Assert.Contains("data/particles/function/rgba.mcfunction",
                layout.FunctionPath.Replace(Path.DirectorySeparatorChar, '/'));

            string meta = File.ReadAllText(layout.PackMetaPath);
            Assert.Contains("107", meta);
            Assert.Contains("min_format", meta);
        }

        [Fact]
        public void Every_supported_version_produces_a_command_the_parser_shape_allows()
        {
            var converter = new ImageConverter(WritePng4Channel());
            Particle[] particles = converter.GetParticles(0, 2, 0);
            Particle first = particles.First();

            foreach (McVersionProfile version in McVersionProfile.All)
            {
                string command = ParticleCommand.Build(
                    first.x, first.y, first.z,
                    new McColor(first.r, first.g, first.b),
                    new ParticleCommandSettings { Version = version, ParticleId = "dust", Scale = 1 });

                Assert.StartsWith("particle minecraft:dust", command);
                Assert.DoesNotContain("E-", command);
                Assert.DoesNotContain("NaN", command);
                Assert.DoesNotContain("Infinity", command);

                // pos(3) + delta(3) + speed + count + mode + viewers, after the particle argument.
                string[] parts = command.Split(' ');
                Assert.Equal(version.UsesSnbtParticleOptions ? 12 : 16, parts.Length);
            }
        }
    }
}
