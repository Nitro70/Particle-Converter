using System;
using System.Collections.Generic;
using System.Linq;

namespace ParticleConverter.Minecraft
{
    /// <summary>
    /// Everything that differs between Minecraft versions when generating a particle datapack.
    /// </summary>
    /// <remarks>
    /// Pack format numbers come from <c>misode/mcmeta</c> (<c>&lt;version&gt;-summary/version.json</c>,
    /// fields <c>data_pack_version</c> / <c>data_pack_version_minor</c>), not from guesswork.
    ///
    /// Two changes drive nearly all of this:
    /// <list type="bullet">
    /// <item>1.20.5 moved particle options from space-separated arguments to SNBT.</item>
    /// <item>1.21 renamed the datapack directory <c>functions</c> to <c>function</c>.</item>
    /// </list>
    /// </remarks>
    public sealed class McVersionProfile
    {
        private McVersionProfile(
            int index,
            string id,
            string displayName,
            int packFormat,
            int? packFormatMinor,
            string functionDirectory,
            bool usesSnbtParticleOptions)
        {
            Index = index;
            Id = id;
            DisplayName = displayName;
            PackFormat = packFormat;
            PackFormatMinor = packFormatMinor;
            FunctionDirectory = functionDirectory;
            UsesSnbtParticleOptions = usesSnbtParticleOptions;
        }

        /// <summary>Position of this version in <see cref="All"/>, and its bit in the particle availability mask.</summary>
        public int Index { get; }

        /// <summary>Stable identifier, e.g. "26.2". Persisted in settings.</summary>
        public string Id { get; }

        /// <summary>Label shown in the version dropdown.</summary>
        public string DisplayName { get; }

        /// <summary>Major pack format, e.g. 107 for 26.2.</summary>
        public int PackFormat { get; }

        /// <summary>
        /// Minor pack format, e.g. 1 for 26.2. Null before 1.21.9, which is when
        /// pack.mcmeta gained the <c>min_format</c>/<c>max_format</c> array form.
        /// </summary>
        public int? PackFormatMinor { get; }

        /// <summary>"function" from 1.21 onwards, "functions" before that.</summary>
        public string FunctionDirectory { get; }

        /// <summary>True from 1.20.5 onwards, when particle options became SNBT.</summary>
        public bool UsesSnbtParticleOptions { get; }

        /// <summary>True from 1.21.9 onwards, when pack.mcmeta gained minor versions.</summary>
        public bool UsesPackFormatRange => PackFormatMinor.HasValue;

        /// <summary>Supported versions, oldest first. Index order is load-bearing: it is the particle mask bit order.</summary>
        public static IReadOnlyList<McVersionProfile> All { get; } = new[]
        {
            new McVersionProfile(0,  "1.16.5",  "1.16.5",            6,   null, "functions", false),
            new McVersionProfile(1,  "1.20.4",  "1.20.2 - 1.20.4",   26,  null, "functions", false),
            new McVersionProfile(2,  "1.20.6",  "1.20.5 - 1.20.6",   41,  null, "functions", true),
            new McVersionProfile(3,  "1.21",    "1.21 - 1.21.3",     48,  null, "function",  true),
            new McVersionProfile(4,  "1.21.4",  "1.21.4",            61,  null, "function",  true),
            new McVersionProfile(5,  "1.21.5",  "1.21.5 - 1.21.7",   71,  null, "function",  true),
            new McVersionProfile(6,  "1.21.8",  "1.21.8",            81,  null, "function",  true),
            new McVersionProfile(7,  "1.21.9",  "1.21.9 - 1.21.10",  88,  0,    "function",  true),
            new McVersionProfile(8,  "1.21.11", "1.21.11",           94,  1,    "function",  true),
            new McVersionProfile(9,  "26.1",    "26.1",              101, 1,    "function",  true),
            new McVersionProfile(10, "26.2",    "26.2 (latest)",     107, 1,    "function",  true),
        };

        public static McVersionProfile Latest => All[All.Count - 1];

        /// <summary>Looks up a profile by <see cref="Id"/>, falling back to <see cref="Latest"/>.</summary>
        public static McVersionProfile ById(string id)
        {
            return All.FirstOrDefault(v => string.Equals(v.Id, id, StringComparison.OrdinalIgnoreCase)) ?? Latest;
        }

        public override string ToString() => DisplayName;
    }
}
