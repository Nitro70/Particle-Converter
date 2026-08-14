using System;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ParticleConverter.Minecraft
{
    /// <summary>
    /// Where the exported files go, resolved from the user's settings.
    /// </summary>
    public sealed class DatapackLayout
    {
        private DatapackLayout(string packRoot, string packMetaPath, string functionPath, string functionReference)
        {
            PackRoot = packRoot;
            PackMetaPath = packMetaPath;
            FunctionPath = functionPath;
            FunctionReference = functionReference;
        }

        /// <summary>Root of the datapack, or the plain output folder in bare mode.</summary>
        public string PackRoot { get; }

        /// <summary>Path to pack.mcmeta, or null in bare mode.</summary>
        public string PackMetaPath { get; }

        /// <summary>Full path of the .mcfunction file.</summary>
        public string FunctionPath { get; }

        /// <summary>What to type in game, e.g. <c>/function particles:my_image</c>. Null in bare mode.</summary>
        public string FunctionReference { get; }

        public bool IsDatapack => PackMetaPath != null;

        /// <summary>
        /// Works out the output paths.
        /// </summary>
        /// <remarks>
        /// In datapack mode the pack folder is named after the namespace, so exporting several
        /// images with the same namespace builds up one pack rather than one pack per image:
        /// <c>&lt;output&gt;/particles/data/particles/function/my_image.mcfunction</c>.
        ///
        /// The <c>function</c> directory is singular from 1.21 onwards and plural before it -
        /// getting this wrong is the single most common reason a converted pack silently fails
        /// to load, so it comes from the version profile rather than a constant.
        /// </remarks>
        public static DatapackLayout Resolve(
            string outputDirectory,
            bool asDatapack,
            string ns,
            string functionName,
            McVersionProfile version)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("Output directory is required.", nameof(outputDirectory));
            if (version == null) throw new ArgumentNullException(nameof(version));

            string safeFunction = McResourceLocation.SanitizePath(functionName);
            if (safeFunction.Length == 0) throw new ArgumentException("Function name is required.", nameof(functionName));

            if (!asDatapack)
            {
                return new DatapackLayout(
                    outputDirectory,
                    null,
                    Path.Combine(outputDirectory, safeFunction.Replace('/', Path.DirectorySeparatorChar) + ".mcfunction"),
                    null);
            }

            string safeNamespace = McResourceLocation.SanitizeNamespace(ns);
            if (safeNamespace.Length == 0) throw new ArgumentException("Namespace is required.", nameof(ns));

            string packRoot = Path.Combine(outputDirectory, safeNamespace);
            string functionPath = Path.Combine(
                packRoot,
                "data",
                safeNamespace,
                version.FunctionDirectory,
                safeFunction.Replace('/', Path.DirectorySeparatorChar) + ".mcfunction");

            return new DatapackLayout(
                packRoot,
                Path.Combine(packRoot, "pack.mcmeta"),
                functionPath,
                $"{safeNamespace}:{safeFunction}");
        }
    }

    /// <summary>
    /// Writes the datapack scaffolding around the generated function.
    /// </summary>
    public static class DatapackWriter
    {
        /// <summary>UTF-8 without a BOM. Minecraft's function parser chokes on a leading BOM.</summary>
        public static readonly Encoding FunctionEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        /// <summary>
        /// Writes pack.mcmeta for the target version.
        /// </summary>
        /// <remarks>
        /// From 1.21.9 the pack format carries a minor version and vanilla writes it as
        /// <c>min_format</c>/<c>max_format</c> arrays. Before that it is a single
        /// <c>pack_format</c> integer.
        /// </remarks>
        public static void WritePackMeta(DatapackLayout layout, McVersionProfile version, string description)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));
            if (!layout.IsDatapack) return;

            Directory.CreateDirectory(Path.GetDirectoryName(layout.PackMetaPath));
            File.WriteAllText(layout.PackMetaPath, BuildPackMeta(version, description), FunctionEncoding);
        }

        /// <summary>Builds the pack.mcmeta JSON. Exposed separately so it can be asserted on in tests.</summary>
        public static string BuildPackMeta(McVersionProfile version, string description)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));

            var buffer = new MemoryStream();
            using (var writer = new Utf8JsonWriter(buffer, new JsonWriterOptions { Indented = true }))
            {
                writer.WriteStartObject();
                writer.WriteStartObject("pack");
                writer.WriteString("description", description ?? "");

                if (version.UsesPackFormatRange)
                {
                    WriteFormatArray(writer, "min_format", version);
                    WriteFormatArray(writer, "max_format", version);
                }
                else
                {
                    writer.WriteNumber("pack_format", version.PackFormat);
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            return FunctionEncoding.GetString(buffer.ToArray());
        }

        private static void WriteFormatArray(Utf8JsonWriter writer, string name, McVersionProfile version)
        {
            writer.WriteStartArray(name);
            writer.WriteNumberValue(version.PackFormat);
            writer.WriteNumberValue(version.PackFormatMinor.Value);
            writer.WriteEndArray();
        }

        /// <summary>Creates the function's directory and opens it for writing, truncating any existing file.</summary>
        public static StreamWriter OpenFunction(DatapackLayout layout)
        {
            if (layout == null) throw new ArgumentNullException(nameof(layout));

            string directory = Path.GetDirectoryName(layout.FunctionPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            return new StreamWriter(layout.FunctionPath, append: false, FunctionEncoding);
        }
    }

    /// <summary>
    /// Namespace and path rules for Minecraft resource locations.
    /// </summary>
    /// <remarks>Prefixed to avoid colliding with System.Reflection.ResourceLocation.</remarks>
    public static class McResourceLocation
    {
        /// <summary>Namespaces allow <c>[a-z0-9_.-]</c>.</summary>
        public static string SanitizeNamespace(string value) => Sanitize(value, allowSlash: false);

        /// <summary>Function paths allow <c>[a-z0-9_.-]</c> plus <c>/</c> for subfolders.</summary>
        public static string SanitizePath(string value) => Sanitize(value, allowSlash: true);

        public static bool IsValidNamespace(string value) =>
            !string.IsNullOrEmpty(value) && SanitizeNamespace(value) == value;

        public static bool IsValidPath(string value) =>
            !string.IsNullOrEmpty(value) && SanitizePath(value) == value;

        private static string Sanitize(string value, bool allowSlash)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";

            var sb = new StringBuilder(value.Length);
            foreach (char raw in value.Trim())
            {
                char c = char.ToLowerInvariant(raw);
                bool ok = (c >= 'a' && c <= 'z')
                          || (c >= '0' && c <= '9')
                          || c == '_' || c == '.' || c == '-'
                          || (allowSlash && c == '/');

                // Spaces and Windows path separators are the two things users actually type.
                if (!ok && (c == ' ' || c == '\\'))
                {
                    sb.Append(allowSlash && c == '\\' ? '/' : '_');
                    continue;
                }

                if (ok) sb.Append(c);
            }

            return sb.ToString().Trim('/');
        }
    }
}
