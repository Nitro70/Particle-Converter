using System;
using System.Collections.Generic;
using System.Linq;

namespace ParticleConverter.Minecraft
{
    /// <summary>
    /// The particle ids Minecraft knows about, per version.
    /// </summary>
    /// <remarks>
    /// The data lives in ParticleRegistry.Generated.cs, built from the misode/mcmeta registry
    /// dumps. Regenerate it with tools/gen_registry.ps1 when adding a version.
    ///
    /// This replaces the 66 hardcoded 1.16 ids that used to live in data/Particles.xaml, which
    /// still offered <c>barrier</c> (removed in 1.18) and was missing everything added since.
    /// </remarks>
    public static partial class ParticleRegistry
    {
        /// <summary>Every particle id known to any supported version, alphabetically.</summary>
        public static IEnumerable<ParticleDefinition> All => Definitions;

        /// <summary>The particle ids that exist in <paramref name="version"/>, alphabetically.</summary>
        public static IReadOnlyList<ParticleDefinition> ForVersion(McVersionProfile version)
        {
            if (version == null) throw new ArgumentNullException(nameof(version));
            return Definitions.Where(d => d.ExistsIn(version)).ToList();
        }

        public static bool TryGet(string id, out ParticleDefinition definition)
        {
            string bare = StripNamespace(id);
            foreach (ParticleDefinition d in Definitions)
            {
                if (string.Equals(d.Id, bare, StringComparison.OrdinalIgnoreCase))
                {
                    definition = d;
                    return true;
                }
            }

            definition = default;
            return false;
        }

        /// <summary>
        /// What options <paramref name="id"/> needs. Unknown ids are treated as taking none,
        /// so a particle added after this build still produces a runnable command.
        /// </summary>
        public static ParticleOptionKind OptionKindOf(string id)
        {
            return TryGet(id, out ParticleDefinition d) ? d.Options : ParticleOptionKind.None;
        }

        /// <summary>True if the particle exists in the given version. Unknown ids are assumed valid.</summary>
        public static bool ExistsIn(string id, McVersionProfile version)
        {
            return !TryGet(id, out ParticleDefinition d) || d.ExistsIn(version);
        }

        private static string StripNamespace(string id)
        {
            if (string.IsNullOrEmpty(id)) return id;
            int colon = id.IndexOf(':');
            return colon >= 0 ? id.Substring(colon + 1) : id;
        }
    }
}
