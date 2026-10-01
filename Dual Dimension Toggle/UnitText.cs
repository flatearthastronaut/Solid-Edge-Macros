using System;
using System.Globalization;

namespace DualDimensionToggle
{
    internal static class UnitText
    {
        // Shared by frame tolerances and callout literals: use decimal arithmetic
        // and preserve the original inch spelling, signs and trailing zeros.
        internal static bool TryDual(string inch, out string value, out string reason)
        {
            value = inch;
            reason = null;
            int point = inch.IndexOf('.');
            int places = point < 0 ? 0 : inch.Length - point - 1;
            decimal inches;
            if (places > 28 || !Decimal.TryParse(inch, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out inches))
            { reason = "Value exceeds supported decimal precision or range."; return false; }
            try
            {
                int metricPlaces = Math.Max(0, places - 1);
                decimal mm = Decimal.Round(inches * 25.4m, metricPlaces, MidpointRounding.AwayFromZero);
                string metric = mm.ToString("F" + metricPlaces, CultureInfo.InvariantCulture);
                string magnitude = inch.TrimStart('+', '-');
                if (magnitude.StartsWith(".", StringComparison.Ordinal))
                {
                    if (metric.StartsWith("0.", StringComparison.Ordinal)) metric = metric.Substring(1);
                    else if (metric.StartsWith("-0.", StringComparison.Ordinal)) metric = "-" + metric.Substring(2);
                }
                if (inch.StartsWith("+", StringComparison.Ordinal)) metric = "+" + metric;
                value = metric + "[" + inch + "]";
                return true;
            }
            catch (OverflowException) { reason = "Converted value exceeds the supported decimal range."; return false; }
        }
    }
}
