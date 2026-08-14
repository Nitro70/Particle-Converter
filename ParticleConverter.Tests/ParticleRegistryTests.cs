using ParticleConverter.Minecraft;
using System.Linq;
using Xunit;

namespace ParticleConverter.Tests
{
    public class ParticleRegistryTests
    {
        private static bool Has(string id, string versionId) =>
            ParticleRegistry.ForVersion(McVersionProfile.ById(versionId)).Any(p => p.Id == id);

        [Fact]
        public void Barrier_existed_in_1_16_but_was_removed_before_26_2()
        {
            // The old hardcoded list still offered it, producing a command that cannot parse.
            Assert.True(Has("barrier", "1.16.5"));
            Assert.False(Has("barrier", "26.2"));
        }

        [Fact]
        public void Ambient_entity_effect_was_removed_too()
        {
            Assert.True(Has("ambient_entity_effect", "1.16.5"));
            Assert.False(Has("ambient_entity_effect", "26.2"));
        }

        [Theory]
        [InlineData("geyser")]
        [InlineData("geyser_base")]
        [InlineData("geyser_plume")]
        [InlineData("geyser_poof")]
        [InlineData("sulfur_cube_goo")]
        public void Particles_added_in_26_2_are_not_offered_on_26_1(string id)
        {
            Assert.True(Has(id, "26.2"));
            Assert.False(Has(id, "26.1"));
        }

        [Fact]
        public void Sculk_particles_arrived_after_1_16()
        {
            Assert.False(Has("sculk_charge", "1.16.5"));
            Assert.True(Has("sculk_charge", "26.2"));
        }

        [Fact]
        public void Dust_exists_in_every_supported_version()
        {
            foreach (McVersionProfile version in McVersionProfile.All)
            {
                Assert.True(Has("dust", version.Id), $"dust missing from {version.Id}");
            }
        }

        [Fact]
        public void Version_particle_counts_match_the_registry_dumps()
        {
            Assert.Equal(72, ParticleRegistry.ForVersion(McVersionProfile.ById("1.16.5")).Count);
            Assert.Equal(109, ParticleRegistry.ForVersion(McVersionProfile.ById("1.21")).Count);
            Assert.Equal(117, ParticleRegistry.ForVersion(McVersionProfile.ById("26.1")).Count);
            Assert.Equal(125, ParticleRegistry.ForVersion(McVersionProfile.ById("26.2")).Count);
        }

        [Theory]
        [InlineData("dust", ParticleOptionKind.Dust)]
        [InlineData("dust_color_transition", ParticleOptionKind.DustColorTransition)]
        [InlineData("block", ParticleOptionKind.BlockState)]
        [InlineData("block_marker", ParticleOptionKind.BlockState)]
        [InlineData("falling_dust", ParticleOptionKind.BlockState)]
        [InlineData("dust_pillar", ParticleOptionKind.BlockState)]
        [InlineData("item", ParticleOptionKind.Item)]
        [InlineData("entity_effect", ParticleOptionKind.ColorArgb)]
        [InlineData("shriek", ParticleOptionKind.Raw)]
        [InlineData("vibration", ParticleOptionKind.Raw)]
        [InlineData("flame", ParticleOptionKind.None)]
        public void Option_kinds_are_classified(string id, ParticleOptionKind expected)
        {
            Assert.Equal(expected, ParticleRegistry.OptionKindOf(id));
        }

        [Fact]
        public void Lookup_ignores_the_minecraft_namespace()
        {
            Assert.Equal(ParticleOptionKind.Dust, ParticleRegistry.OptionKindOf("minecraft:dust"));
        }

        [Fact]
        public void An_unknown_particle_is_treated_as_taking_no_options_and_being_valid()
        {
            // A particle added after this build should still produce a runnable command.
            Assert.Equal(ParticleOptionKind.None, ParticleRegistry.OptionKindOf("some_future_particle"));
            Assert.True(ParticleRegistry.ExistsIn("some_future_particle", McVersionProfile.Latest));
        }

        [Fact]
        public void The_list_is_alphabetical_so_the_dropdown_is_navigable()
        {
            var ids = ParticleRegistry.ForVersion(McVersionProfile.Latest).Select(p => p.Id).ToList();

            Assert.Equal(ids.OrderBy(id => id, System.StringComparer.Ordinal).ToList(), ids);
        }
    }
}
