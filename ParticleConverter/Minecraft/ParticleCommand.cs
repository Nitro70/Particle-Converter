using System;
using System.Text;

namespace ParticleConverter.Minecraft
{
    /// <summary>Whether coordinates are world-relative (<c>~</c>) or facing-relative (<c>^</c>).</summary>
    public enum CoordinateMode
    {
        RelativeWorld,
        RelativeLocal,
    }

    /// <summary>The <c>force</c>/<c>normal</c> argument of /particle.</summary>
    public enum ParticleDisplayMode
    {
        Normal,
        Force,
    }

    /// <summary>An 8-bit RGB colour.</summary>
    public readonly struct McColor
    {
        public McColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }
    }

    /// <summary>
    /// Everything needed to turn one pixel into one <c>/particle</c> command.
    /// </summary>
    public sealed class ParticleCommandSettings
    {
        /// <summary>Smallest <c>scale</c> vanilla accepts on a dust particle.</summary>
        public const double MinScale = 0.01;

        /// <summary>
        /// Largest <c>scale</c> vanilla accepts on a dust particle.
        /// </summary>
        /// <remarks>
        /// The original tool capped its input box at 1.0, and the well-known workaround was to
        /// export at 1.0 and find-and-replace the number in a text editor. That worked because
        /// 1.19 had no server-side limit at all (MC-159741). From 1.20.5 the field is validated
        /// by a codec range check, so anything above 4.0 is a parse error and the command simply
        /// does not run. 4.0 is the real ceiling.
        /// </remarks>
        public const double MaxScale = 4.0;

        public McVersionProfile Version { get; set; } = McVersionProfile.Latest;

        /// <summary>Particle id, with or without the <c>minecraft:</c> prefix.</summary>
        public string ParticleId { get; set; } = "dust";

        /// <summary>Dust size. Clamped to <see cref="MinScale"/>..<see cref="MaxScale"/> on write.</summary>
        public double Scale { get; set; } = 1.0;

        /// <summary>Use <see cref="FixedColor"/> for every particle instead of the source pixel's colour.</summary>
        public bool UseFixedColor { get; set; }

        public McColor FixedColor { get; set; } = new McColor(255, 255, 255);

        /// <summary>Destination colour for <c>dust_color_transition</c>.</summary>
        public McColor TransitionToColor { get; set; } = new McColor(255, 255, 255);

        /// <summary>Block state for the <see cref="ParticleOptionKind.BlockState"/> particles.</summary>
        public string BlockState { get; set; } = "minecraft:stone";

        /// <summary>Item id for <c>item</c>.</summary>
        public string Item { get; set; } = "minecraft:stone";

        /// <summary>
        /// Verbatim SNBT for particles whose options cannot be derived from an image, e.g.
        /// <c>{delay:10}</c> for <c>shriek</c>. Written exactly as given, braces included.
        /// </summary>
        public string RawOptions { get; set; } = "";

        public CoordinateMode CoordinateMode { get; set; } = CoordinateMode.RelativeLocal;

        public ParticleDisplayMode DisplayMode { get; set; } = ParticleDisplayMode.Force;

        /// <summary>Target selector for who sees the particle. Empty omits the argument.</summary>
        public string Viewers { get; set; } = "@a";

        public double ClampedScale => Math.Clamp(Scale, MinScale, MaxScale);

        /// <summary>Particle id including the <c>minecraft:</c> namespace.</summary>
        public string QualifiedParticleId
        {
            get
            {
                string id = ParticleId ?? "";
                return id.Contains(':') ? id : "minecraft:" + id;
            }
        }
    }

    /// <summary>
    /// Builds <c>/particle</c> command text for a given Minecraft version.
    /// </summary>
    /// <remarks>
    /// The shape of the command has not changed:
    /// <c>particle &lt;name&gt; &lt;pos&gt; &lt;delta&gt; &lt;speed&gt; &lt;count&gt; [force|normal] [&lt;viewers&gt;]</c>.
    /// What changed in 1.20.5 is how <c>&lt;name&gt;</c> carries its options - space-separated
    /// arguments became SNBT appended to the id.
    /// </remarks>
    public static class ParticleCommand
    {
        /// <summary>Builds the command for a single particle at the given offset.</summary>
        public static string Build(double x, double y, double z, McColor pixelColor, ParticleCommandSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            McColor color = settings.UseFixedColor ? settings.FixedColor : pixelColor;
            string prefix = settings.CoordinateMode == CoordinateMode.RelativeLocal ? "^" : "~";

            var sb = new StringBuilder(96);
            sb.Append("particle ");
            sb.Append(settings.QualifiedParticleId);
            sb.Append(BuildOptions(color, settings));

            sb.Append(' ').Append(prefix).Append(McNumber.Format(x));
            sb.Append(' ').Append(prefix).Append(McNumber.Format(y));
            sb.Append(' ').Append(prefix).Append(McNumber.Format(z));

            // delta x/y/z, speed, count - a still image wants exactly one motionless particle.
            sb.Append(" 0 0 0 0 1 ");
            sb.Append(settings.DisplayMode == ParticleDisplayMode.Force ? "force" : "normal");

            if (!string.IsNullOrWhiteSpace(settings.Viewers))
            {
                sb.Append(' ').Append(settings.Viewers.Trim());
            }

            return sb.ToString();
        }

        /// <summary>
        /// The options that follow the particle id: SNBT from 1.20.5, space-separated arguments before.
        /// Returns an empty string for particles that take none.
        /// </summary>
        public static string BuildOptions(McColor color, ParticleCommandSettings settings)
        {
            ParticleOptionKind kind = ParticleRegistry.OptionKindOf(settings.ParticleId);

            if (kind == ParticleOptionKind.Raw)
            {
                return string.IsNullOrWhiteSpace(settings.RawOptions) ? "" : settings.RawOptions.Trim();
            }

            return settings.Version.UsesSnbtParticleOptions
                ? BuildSnbtOptions(kind, color, settings)
                : BuildLegacyOptions(kind, color, settings);
        }

        private static string BuildSnbtOptions(ParticleOptionKind kind, McColor color, ParticleCommandSettings settings)
        {
            string scale = McNumber.Format(settings.ClampedScale);

            switch (kind)
            {
                case ParticleOptionKind.Dust:
                    return $"{{color:[{Rgb(color)}],scale:{scale}}}";

                case ParticleOptionKind.DustColorTransition:
                    return $"{{from_color:[{Rgb(color)}],to_color:[{Rgb(settings.TransitionToColor)}],scale:{scale}}}";

                case ParticleOptionKind.ColorArgb:
                    return $"{{color:[{Rgb(color)},{McNumber.FormatColorChannel(color.A)}]}}";

                case ParticleOptionKind.BlockState:
                    return $"{{block_state:{Quote(settings.BlockState)}}}";

                case ParticleOptionKind.Item:
                    return $"{{item:{Quote(settings.Item)}}}";

                default:
                    return "";
            }
        }

        /// <summary>
        /// Pre-1.20.5 form. Only the kinds that were actually expressible as positional arguments
        /// are handled; <see cref="ParticleOptionKind.ColorArgb"/> encoded its colour in the delta
        /// and speed arguments back then, which this tool does not attempt to reproduce.
        /// </summary>
        private static string BuildLegacyOptions(ParticleOptionKind kind, McColor color, ParticleCommandSettings settings)
        {
            string scale = McNumber.Format(settings.ClampedScale);

            switch (kind)
            {
                case ParticleOptionKind.Dust:
                    return $" {Rgb(color, " ")} {scale}";

                case ParticleOptionKind.DustColorTransition:
                    return $" {Rgb(color, " ")} {scale} {Rgb(settings.TransitionToColor, " ")}";

                case ParticleOptionKind.BlockState:
                    return " " + settings.BlockState;

                case ParticleOptionKind.Item:
                    return " " + settings.Item;

                default:
                    return "";
            }
        }

        private static string Rgb(McColor c, string separator = ",")
        {
            return string.Join(separator,
                McNumber.FormatColorChannel(c.R),
                McNumber.FormatColorChannel(c.G),
                McNumber.FormatColorChannel(c.B));
        }

        /// <summary>SNBT string literal. Ids never contain quotes, but escape defensively.</summary>
        private static string Quote(string value)
        {
            string safe = (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
            return "\"" + safe + "\"";
        }
    }
}
