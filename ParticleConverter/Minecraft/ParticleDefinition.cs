namespace ParticleConverter.Minecraft
{
    /// <summary>
    /// What extra data a particle needs after its id, e.g. <c>dust{color:[1,0,0],scale:1}</c>.
    /// </summary>
    public enum ParticleOptionKind
    {
        /// <summary>Takes no options, or only optional ones that we leave at their defaults.</summary>
        None,

        /// <summary><c>{color:[r,g,b],scale:s}</c>. This is the one that carries an image.</summary>
        Dust,

        /// <summary><c>{from_color:[r,g,b],to_color:[r,g,b],scale:s}</c>.</summary>
        DustColorTransition,

        /// <summary><c>{color:[r,g,b,a]}</c> - entity_effect, flash, tinted_leaves.</summary>
        ColorArgb,

        /// <summary><c>{block_state:"minecraft:stone"}</c>.</summary>
        BlockState,

        /// <summary><c>{item:"minecraft:stone"}</c>.</summary>
        Item,

        /// <summary>
        /// Needs options this tool cannot derive from an image (vibration destinations, geyser
        /// impulses, shriek delays). The user supplies the SNBT verbatim.
        /// </summary>
        Raw,
    }

    /// <summary>
    /// A particle id, what options it takes, and which Minecraft versions it exists in.
    /// </summary>
    public readonly struct ParticleDefinition
    {
        private readonly int _versionMask;

        public ParticleDefinition(string id, ParticleOptionKind options, int versionMask)
        {
            Id = id;
            Options = options;
            _versionMask = versionMask;
        }

        /// <summary>Particle id without the <c>minecraft:</c> namespace, e.g. "dust".</summary>
        public string Id { get; }

        public ParticleOptionKind Options { get; }

        /// <summary>True if this particle exists in the given version.</summary>
        public bool ExistsIn(McVersionProfile version)
        {
            return version != null && (_versionMask & (1 << version.Index)) != 0;
        }

        public override string ToString() => Id;
    }
}
