using ParticleConverter.Minecraft;
using System.IO;
using Xunit;

namespace ParticleConverter.Tests
{
    public class DatapackTests
    {
        [Theory]
        [InlineData("1.16.5", "functions")]
        [InlineData("1.20.4", "functions")]
        [InlineData("1.20.6", "functions")]
        [InlineData("1.21", "function")]
        [InlineData("26.2", "function")]
        public void Function_directory_became_singular_in_1_21(string versionId, string expected)
        {
            Assert.Equal(expected, McVersionProfile.ById(versionId).FunctionDirectory);
        }

        [Fact]
        public void Datapack_layout_puts_the_function_where_the_game_looks_for_it()
        {
            DatapackLayout layout = DatapackLayout.Resolve(
                @"C:\saves\world\datapacks",
                asDatapack: true,
                ns: "particles",
                functionName: "my_image",
                version: McVersionProfile.ById("26.2"));

            Assert.Equal(Path.Combine(@"C:\saves\world\datapacks", "particles"), layout.PackRoot);
            Assert.Equal(
                Path.Combine(@"C:\saves\world\datapacks", "particles", "data", "particles", "function", "my_image.mcfunction"),
                layout.FunctionPath);
            Assert.Equal("particles:my_image", layout.FunctionReference);
        }

        [Fact]
        public void Legacy_datapack_layout_uses_the_plural_directory()
        {
            DatapackLayout layout = DatapackLayout.Resolve(
                @"C:\packs", true, "particles", "my_image", McVersionProfile.ById("1.20.6"));

            Assert.Contains(Path.Combine("data", "particles", "functions"), layout.FunctionPath);
        }

        [Fact]
        public void Bare_mode_writes_a_single_file_and_has_no_function_reference()
        {
            DatapackLayout layout = DatapackLayout.Resolve(
                @"C:\out", asDatapack: false, ns: "particles", functionName: "my_image",
                version: McVersionProfile.ById("26.2"));

            Assert.Equal(Path.Combine(@"C:\out", "my_image.mcfunction"), layout.FunctionPath);
            Assert.Null(layout.PackMetaPath);
            Assert.Null(layout.FunctionReference);
            Assert.False(layout.IsDatapack);
        }

        [Fact]
        public void Subfolders_in_the_function_name_are_kept()
        {
            DatapackLayout layout = DatapackLayout.Resolve(
                @"C:\packs", true, "particles", "images/my_image", McVersionProfile.ById("26.2"));

            Assert.Equal("particles:images/my_image", layout.FunctionReference);
            Assert.EndsWith(Path.Combine("images", "my_image.mcfunction"), layout.FunctionPath);
        }

        [Fact]
        public void Pack_meta_uses_the_array_form_from_1_21_9()
        {
            string json = DatapackWriter.BuildPackMeta(McVersionProfile.ById("26.2"), "test");

            Assert.Contains("\"min_format\"", json);
            Assert.Contains("\"max_format\"", json);
            Assert.DoesNotContain("\"pack_format\"", json);
            Assert.Contains("107", json);
            Assert.Contains("\"description\": \"test\"", json);
        }

        [Fact]
        public void Pack_meta_uses_a_flat_integer_before_1_21_9()
        {
            string json = DatapackWriter.BuildPackMeta(McVersionProfile.ById("1.21"), "test");

            Assert.Contains("\"pack_format\": 48", json);
            Assert.DoesNotContain("min_format", json);
        }

        [Theory]
        [InlineData("1.16.5", 6)]
        [InlineData("1.20.4", 26)]
        [InlineData("1.20.6", 41)]
        [InlineData("1.21", 48)]
        [InlineData("1.21.4", 61)]
        [InlineData("1.21.5", 71)]
        [InlineData("1.21.8", 81)]
        [InlineData("1.21.9", 88)]
        [InlineData("1.21.11", 94)]
        [InlineData("26.1", 101)]
        [InlineData("26.2", 107)]
        public void Pack_formats_match_the_values_shipped_by_the_game(string versionId, int expected)
        {
            Assert.Equal(expected, McVersionProfile.ById(versionId).PackFormat);
        }

        [Fact]
        public void An_unknown_version_id_falls_back_to_the_latest()
        {
            Assert.Same(McVersionProfile.Latest, McVersionProfile.ById("does-not-exist"));
            Assert.Equal("26.2", McVersionProfile.Latest.Id);
        }

        [Fact]
        public void Version_indexes_match_their_position_in_the_list()
        {
            // The particle availability mask depends on this, so guard it.
            for (int i = 0; i < McVersionProfile.All.Count; i++)
            {
                Assert.Equal(i, McVersionProfile.All[i].Index);
            }
        }

        [Theory]
        [InlineData("My Pack", "my_pack")]
        [InlineData("Particles!", "particles")]
        [InlineData("  spaced  ", "spaced")]
        [InlineData("UPPER", "upper")]
        public void Namespaces_are_forced_into_the_allowed_character_set(string input, string expected)
        {
            Assert.Equal(expected, McResourceLocation.SanitizeNamespace(input));
        }

        [Fact]
        public void Slashes_are_only_allowed_in_paths()
        {
            Assert.Equal("abc", McResourceLocation.SanitizeNamespace("a/b/c"));
            Assert.Equal("a/b/c", McResourceLocation.SanitizePath("a/b/c"));
            Assert.Equal("a/b", McResourceLocation.SanitizePath(@"a\b"));
        }

        [Fact]
        public void Writing_a_datapack_produces_a_loadable_tree()
        {
            string root = Path.Combine(Path.GetTempPath(), "pc-test-" + Path.GetRandomFileName());
            try
            {
                McVersionProfile version = McVersionProfile.ById("26.2");
                DatapackLayout layout = DatapackLayout.Resolve(root, true, "particles", "img", version);

                DatapackWriter.WritePackMeta(layout, version, "test pack");
                using (StreamWriter writer = DatapackWriter.OpenFunction(layout))
                {
                    writer.WriteLine("### header");
                    writer.WriteLine(ParticleCommand.Build(0, 1, 0, new McColor(255, 0, 0),
                        new ParticleCommandSettings { Version = version, ParticleId = "dust", Scale = 1 }));
                }

                Assert.True(File.Exists(layout.PackMetaPath));
                Assert.True(File.Exists(layout.FunctionPath));

                string function = File.ReadAllText(layout.FunctionPath);
                Assert.Contains("particle minecraft:dust{color:[1,0,0],scale:1}", function);

                // A BOM here makes the game fail to parse the first command in the file.
                byte[] raw = File.ReadAllBytes(layout.FunctionPath);
                Assert.False(raw.Length >= 3 && raw[0] == 0xEF && raw[1] == 0xBB && raw[2] == 0xBF);
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
            }
        }
    }
}
