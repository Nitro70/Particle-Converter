using ParticleConverter.Minecraft;
using System;
using System.Globalization;
using System.Windows.Data;

namespace ParticleConverter.util
{
    /// <summary>
    /// True when the selected particle carries a colour, which is what the colour-fixing controls
    /// apply to. That is <c>dust</c> and <c>dust_color_transition</c>, not just <c>dust</c> as the
    /// name suggests - the name is kept because the XAML binds to it.
    /// </summary>
    class IsDustConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return IsColored(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return IsColored(value);
        }

        private static bool IsColored(object value)
        {
            if (value is not string id) return false;

            ParticleOptionKind kind = ParticleRegistry.OptionKindOf(id);
            return kind == ParticleOptionKind.Dust
                   || kind == ParticleOptionKind.DustColorTransition
                   || kind == ParticleOptionKind.ColorArgb;
        }
    }
}
