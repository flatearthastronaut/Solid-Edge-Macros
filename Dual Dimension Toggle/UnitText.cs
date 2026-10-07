using System;
using System.Globalization;

namespace DualDimensionToggle
{
    /// <summary>
    /// Formats one previously identified inch literal as millimeters[inches].
    /// Callers decide whether text is a length; this helper does not recognize
    /// callout fields, tolerance compartments, quantities, or thread sizes.
    /// </summary>
    internal static class UnitText
    {
        // Shared by frame tolerances and callout literals: use decimal arithmetic
        // and preserve the original inch spelling, signs and trailing zeros.
        internal static bool TryDual(string inch, out string value, out string reason)
        {
            value = inch;
            reason = null;
            // Precision comes from the spelling, not the parsed magnitude:
            // .001 and .0010 have different requested metric decimal counts.
            // Callers supply a non-null literal with period decimal notation.
            int point = inch.IndexOf('.');
            int places = point < 0 ? 0 : inch.Length - point - 1;
            decimal inches;
            if (places > 28 || !Decimal.TryParse(inch, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out inches))
            { reason = "Value exceeds supported decimal precision or range."; return false; }
            try
            {
                // User rule: one fewer DECIMAL PLACE (not significant digit).
                // Decimal arithmetic avoids binary floating-point midpoint
                // surprises; AwayFromZero gives ordinary half-up rounding for
                // positive tolerances and the symmetric rule for negative values.
                // Example: .0005 inch -> .013[.0005], retaining the inch spelling.
                int metricPlaces = Math.Max(0, places - 1);
                decimal mm = Decimal.Round(inches * 25.4m, metricPlaces, MidpointRounding.AwayFromZero);
                string metric = mm.ToString("F" + metricPlaces, CultureInfo.InvariantCulture);
                string magnitude = inch.TrimStart('+', '-');
                // Match the source's leading-zero convention only when the
                // metric magnitude is below one. Keep a written '+' explicit.
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
