using ParticleConverter.Minecraft;
using System.Globalization;
using System.Threading;
using Xunit;

namespace ParticleConverter.Tests
{
    public class ParticleCommandTests
    {
        private static ParticleCommandSettings Settings(string versionId, string particle = "dust")
        {
            return new ParticleCommandSettings
            {
                Version = McVersionProfile.ById(versionId),
                ParticleId = particle,
                Scale = 0.75,
                CoordinateMode = CoordinateMode.RelativeLocal,
                DisplayMode = ParticleDisplayMode.Force,
                Viewers = "@a",
            };
        }

        [Fact]
        public void Dust_on_26_2_uses_snbt_options()
        {
            string command = ParticleCommand.Build(0, 1, 0, new McColor(255, 0, 0), Settings("26.2"));

            Assert.Equal(
                "particle minecraft:dust{color:[1,0,0],scale:0.75} ^0 ^1 ^0 0 0 0 0 1 force @a",
                command);
        }

        [Fact]
        public void Dust_before_1_20_5_uses_space_separated_arguments()
        {
            string command = ParticleCommand.Build(0, 1, 0, new McColor(255, 0, 0), Settings("1.16.5"));

            Assert.Equal(
                "particle minecraft:dust 1 0 0 0.75 ^0 ^1 ^0 0 0 0 0 1 force @a",
                command);
        }

        [Fact]
        public void Snbt_starts_at_1_20_5_not_1_21()
        {
            // 1.20.4 is the last version with the old form; 1.20.6 is the first with SNBT.
            Assert.DoesNotContain("{", ParticleCommand.Build(0, 0, 0, new McColor(1, 2, 3), Settings("1.20.4")));
            Assert.Contains("{", ParticleCommand.Build(0, 0, 0, new McColor(1, 2, 3), Settings("1.20.6")));
        }

        [Theory]
        [InlineData(CoordinateMode.RelativeLocal, "^")]
        [InlineData(CoordinateMode.RelativeWorld, "~")]
        public void Coordinate_mode_selects_the_prefix(CoordinateMode mode, string expected)
        {
            ParticleCommandSettings settings = Settings("26.2");
            settings.CoordinateMode = mode;

            string command = ParticleCommand.Build(1.5, 2, -3, new McColor(0, 0, 0), settings);

            Assert.Contains($" {expected}1.5 {expected}2 {expected}-3 ", command);
        }

        [Fact]
        public void Scale_above_the_vanilla_maximum_is_clamped()
        {
            // The old workaround of hand-editing the file to 5 produces a parse error from
            // 1.20.5 onward, so the writer must never emit one.
            ParticleCommandSettings settings = Settings("26.2");
            settings.Scale = 5.0;

            Assert.Contains("scale:4}", ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), settings));
        }

        [Fact]
        public void Scale_below_the_vanilla_minimum_is_clamped()
        {
            ParticleCommandSettings settings = Settings("26.2");
            settings.Scale = 0;

            Assert.Contains("scale:0.01}", ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), settings));
        }

        [Fact]
        public void Vanilla_scale_range_is_documented_as_0_01_to_4()
        {
            Assert.Equal(0.01, ParticleCommandSettings.MinScale);
            Assert.Equal(4.0, ParticleCommandSettings.MaxScale);
        }

        [Fact]
        public void Fixed_colour_overrides_the_pixel_colour()
        {
            ParticleCommandSettings settings = Settings("26.2");
            settings.UseFixedColor = true;
            settings.FixedColor = new McColor(0, 0, 255);

            Assert.Contains("color:[0,0,1]", ParticleCommand.Build(0, 0, 0, new McColor(255, 0, 0), settings));
        }

        [Fact]
        public void Colour_keeps_more_than_two_decimals()
        {
            // The original rounded to two decimals, collapsing 256 levels down to about 101.
            string command = ParticleCommand.Build(0, 0, 0, new McColor(128, 0, 0), Settings("26.2"));

            Assert.Contains("color:[0.502,0,0]", command);
        }

        [Fact]
        public void Small_coordinates_never_use_exponential_notation()
        {
            // "R" formatting turns 1e-07 into "1E-07", which Minecraft rejects as a coordinate.
            string command = ParticleCommand.Build(0.00000001, 0, 0, new McColor(0, 0, 0), Settings("26.2"));

            Assert.DoesNotContain("E-", command);
            Assert.DoesNotContain("e-", command);
        }

        [Fact]
        public void Negative_zero_is_written_as_zero()
        {
            string command = ParticleCommand.Build(-0.000000001, 0, 0, new McColor(0, 0, 0), Settings("26.2"));

            Assert.Contains(" ^0 ^0 ^0 ", command);
        }

        [Fact]
        public void Numbers_use_invariant_culture()
        {
            CultureInfo original = Thread.CurrentThread.CurrentCulture;
            try
            {
                // A comma decimal separator would produce "~0,5", which does not parse in game.
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                string command = ParticleCommand.Build(0.5, 0, 0, new McColor(0, 0, 0), Settings("26.2"));

                Assert.Contains("^0.5", command);
                Assert.DoesNotContain(",5", command);
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = original;
            }
        }

        [Fact]
        public void Blank_viewers_omits_the_argument()
        {
            ParticleCommandSettings settings = Settings("26.2");
            settings.Viewers = "   ";

            Assert.EndsWith(" 0 0 0 0 1 force", ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), settings));
        }

        [Fact]
        public void Normal_display_mode_is_written()
        {
            ParticleCommandSettings settings = Settings("26.2");
            settings.DisplayMode = ParticleDisplayMode.Normal;

            Assert.Contains(" normal ", ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), settings));
        }

        [Fact]
        public void Particle_id_gets_the_minecraft_namespace()
        {
            Assert.Contains("minecraft:flame", ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), Settings("26.2", "flame")));
        }

        [Fact]
        public void An_already_qualified_particle_id_is_left_alone()
        {
            Assert.Contains("mypack:custom", ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), Settings("26.2", "mypack:custom")));
        }

        [Fact]
        public void Particles_without_options_emit_none()
        {
            Assert.Equal(
                "particle minecraft:flame ^0 ^1 ^0 0 0 0 0 1 force @a",
                ParticleCommand.Build(0, 1, 0, new McColor(255, 0, 0), Settings("26.2", "flame")));
        }

        [Fact]
        public void Block_state_particles_use_snbt_on_modern_versions()
        {
            ParticleCommandSettings settings = Settings("26.2", "block");
            settings.BlockState = "minecraft:diamond_block";

            Assert.Contains("minecraft:block{block_state:\"minecraft:diamond_block\"}",
                ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), settings));
        }

        [Fact]
        public void Block_state_particles_use_a_bare_argument_on_legacy_versions()
        {
            ParticleCommandSettings settings = Settings("1.16.5", "block");
            settings.BlockState = "minecraft:diamond_block";

            Assert.Contains("minecraft:block minecraft:diamond_block ",
                ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), settings));
        }

        [Fact]
        public void Dust_color_transition_writes_both_colours()
        {
            ParticleCommandSettings settings = Settings("26.2", "dust_color_transition");
            settings.TransitionToColor = new McColor(0, 0, 255);

            Assert.Contains("{from_color:[1,0,0],to_color:[0,0,1],scale:0.75}",
                ParticleCommand.Build(0, 0, 0, new McColor(255, 0, 0), settings));
        }

        [Fact]
        public void Dust_color_transition_puts_scale_between_the_colours_on_legacy_versions()
        {
            // The pre-1.20.5 argument order is from RGB, scale, to RGB - not from, to, scale.
            ParticleCommandSettings settings = Settings("1.20.4", "dust_color_transition");
            settings.TransitionToColor = new McColor(0, 0, 255);

            Assert.Contains("minecraft:dust_color_transition 1 0 0 0.75 0 0 1 ",
                ParticleCommand.Build(0, 0, 0, new McColor(255, 0, 0), settings));
        }

        [Fact]
        public void Dust_color_transition_defaults_to_fading_to_white()
        {
            ParticleCommandSettings settings = Settings("26.2", "dust_color_transition");

            Assert.Contains("to_color:[1,1,1]",
                ParticleCommand.Build(0, 0, 0, new McColor(255, 0, 0), settings));
        }

        [Fact]
        public void Raw_options_are_written_verbatim()
        {
            ParticleCommandSettings settings = Settings("26.2", "shriek");
            settings.RawOptions = "{delay:10}";

            Assert.Contains("minecraft:shriek{delay:10} ",
                ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), settings));
        }

        [Fact]
        public void Raw_particles_without_options_emit_nothing_extra()
        {
            ParticleCommandSettings settings = Settings("26.2", "shriek");
            settings.RawOptions = "";

            Assert.Contains("minecraft:shriek ", ParticleCommand.Build(0, 0, 0, new McColor(0, 0, 0), settings));
        }
    }
}
