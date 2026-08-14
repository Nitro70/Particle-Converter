using System.Globalization;

namespace ParticleConverter.Minecraft
{
    /// <summary>
    /// Number formatting for command text.
    /// </summary>
    /// <remarks>
    /// Always invariant culture: a machine with a comma decimal separator would otherwise emit
    /// <c>~0,5</c>, which Minecraft rejects.
    ///
    /// Never the "R" round-trip specifier either, which the original code used. "R" switches to
    /// exponential notation for small magnitudes - a coordinate of <c>1E-07</c> is a parse error
    /// in game. The fixed-point patterns below cannot produce an exponent.
    /// </remarks>
    public static class McNumber
    {
        /// <summary>Coordinates and scales. Seven decimals is well past what a particle position resolves.</summary>
        public static string Format(double value) => Clean(value.ToString("0.#######", CultureInfo.InvariantCulture));

        /// <summary>Colour components in 0..1. Four decimals distinguishes all 256 byte levels.</summary>
        public static string FormatColor(double value) => Clean(value.ToString("0.####", CultureInfo.InvariantCulture));

        /// <summary>Converts a 0..255 byte channel to its 0..1 command representation.</summary>
        public static string FormatColorChannel(byte channel) => FormatColor(channel / 255.0);

        /// <summary>Rounding a tiny negative toward zero yields "-0"; emit plain "0" instead.</summary>
        private static string Clean(string formatted) => formatted == "-0" ? "0" : formatted;
    }
}
