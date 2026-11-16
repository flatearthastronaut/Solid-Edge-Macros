using System;
using System.Globalization;
using Hammill.Automation;
using Hammill.DimensionStyleCycle;

namespace Hammill.MacroLauncher
{
    internal static class LimitToleranceCycle
    {
        internal const int Nominal = 1, Limits = 4, UnitTolerance = 15, Inches = 5, Millimeters = 3;
        private static readonly decimal[] Presets = { .0005m, .001m, .002m };

        internal static decimal Next(int display, string upper, string lower, int toleranceUnits)
        {
            decimal u, l;
            if (display == Limits && TryInches(upper, toleranceUnits, out u) && TryInches(lower, toleranceUnits, out l))
                for (int i = 0; i < Presets.Length; i++)
                    if (Math.Abs(u - Presets[i]) < .000000001m && Math.Abs(l + Presets[i]) < .000000001m)
                        // Zero denotes native Nominal display, not +/-0 limits.
                        return i + 1 == Presets.Length ? 0m : Presets[i + 1];
            return Presets[0];
        }

        internal static string Number(decimal value) { return value.ToString("0.################", CultureInfo.InvariantCulture); }
        internal static decimal UnitsPerInch(int unit)
        {
            switch (unit) { case 5: return 1; case 3: return 25.4m; case 2: return .0254m; case 4: return 2.54m; case 6: return 1m / 12m; }
            throw new InvalidOperationException("Unsupported tolerance units.");
        }
        internal static bool TryInches(string raw, int units, out decimal result)
        {
            result = 0;
            string text = (raw ?? "").Trim();
            // Limits entered in SE may include a unit label. Never accept an
            // arbitrary note/formula as a number or interpret commas as thousands.
            if (text.EndsWith(" in", StringComparison.OrdinalIgnoreCase)) { units = Inches; text = text.Substring(0, text.Length - 3).Trim(); }
            else if (text.EndsWith(" mm", StringComparison.OrdinalIgnoreCase)) { units = Millimeters; text = text.Substring(0, text.Length - 3).Trim(); }
            decimal value;
            if (!Decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value) &&
                !Decimal.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)) return false;
            try { result = value / UnitsPerInch(units); return true; }
            catch (InvalidOperationException) { return false; }
        }

        internal static decimal Run(object document)
        {
            if (document == null || (int)((dynamic)document).Type != 2)
                throw new InvalidOperationException("Activate a draft and select one existing dimension.");
            using (var scope = new ComScope())
            {
                dynamic selection = scope.Own(((dynamic)document).SelectSet);
                if ((int)selection.Count != 1) throw new InvalidOperationException("Select exactly one dimension, then click Cycle Tolerance.");
                dynamic dimension = scope.Own(selection.Item(1));
                if ((int)dimension.Type != CycleEngine.DimensionObjectType) throw new InvalidOperationException("Select a dimension, not a callout or another drawing object.");
                int kind = (int)dimension.DimensionType;
                if (kind == 3 || kind == 7 || kind == 11) throw new InvalidOperationException("These inch tolerances do not apply to angular dimensions.");
                if (!String.IsNullOrWhiteSpace((string)dimension.OverrideString))
                    throw new InvalidOperationException("This dimension has an overridden value. Remove or review that override before applying calculated limits.");
                dynamic style = scope.Own(dimension.Style);
                int primary = (int)style.PrimaryUnits, secondary = (int)style.SecondaryUnits;
                bool dual = (bool)style.DualDisplay;
                bool inchPrimary = primary == Inches && (!dual || secondary == Millimeters);
                bool inchSecondary = dual && primary == Millimeters && secondary == Inches;
                if (!inchPrimary && !inchSecondary) throw new InvalidOperationException("Use an inch-only or inch/metric dimension style for these inch tolerances.");
                if ((inchPrimary ? (int)style.RoundOffType : (int)style.SecondaryLinearRoundOffType) != 1)
                    throw new InvalidOperationException("Use a decimal inch style before applying these limits.");

                var before = new ToleranceState((object)dimension, (object)style);
                decimal tolerance = Next(before.Display, before.Upper, before.Lower, before.Units);
                int targetDisplay = tolerance == 0m ? Nominal : Limits;
                // Changing style is unnecessary: edit this dimension's local
                // properties, keep its unit order/orientation and all model links.
                var protectedState = new DimensionState((object)dimension,
                    "PrimaryUpperTolerance", "PrimaryLowerTolerance", "SecondaryUpperTolerance", "SecondaryLowerTolerance", "DisplayType");
                try
                {
                    dimension.UseStandardLimitAndFitTolerance = false;
                    style.SecondaryToleranceEquivalent = true;
                    int requiredPrecision = tolerance == .0005m ? 6 : 5; // SDK decimal enum: places + 2.
                    // Precision is part of each limit preset: four inch places for
                    // half-thousandths, three for the other two tolerances.
                    // Dual dimensions use exactly one fewer metric place. Nominal
                    // keeps the current places, including manual/Cycle Places edits.
                    if (tolerance != 0m)
                    {
                        if (inchPrimary) style.PrimaryDecimalRoundOff = requiredPrecision;
                        else style.SecondaryDecimalRoundOff = requiredPrecision;
                        if (dual)
                        {
                            if (inchPrimary) style.SecondaryDecimalRoundOff = requiredPrecision - 1;
                            else style.PrimaryDecimalRoundOff = requiredPrecision - 1;
                        }
                    }

                    // CRITICAL: setting tolerance text directly in Limits mode
                    // does not update SE's numeric limit cache. First use native
                    // UnitTolerance with explicit inch units, then switch to Limits.
                    // This is also used in CapDetailing/Bushing21931.cs and is
                    // regression-tested against actual GetDisplayData limit text.
                    style.PrimaryLinearToleranceUnits = Inches;
                    dimension.DisplayType = UnitTolerance;
                    dimension.PrimaryUpperTolerance = Number(tolerance);
                    dimension.PrimaryLowerTolerance = Number(-tolerance);
                    // On the Nominal step, zero the numeric offsets before hiding
                    // them so old +/- values cannot survive as active native limits.
                    dimension.DisplayType = targetDisplay;
                    protectedState.RestoreAndVerify((object)dimension);
                    decimal u, l;
                    bool precisionCorrect = tolerance == 0m
                        ? (int)style.PrimaryDecimalRoundOff == before.PrimaryPrecision && (int)style.SecondaryDecimalRoundOff == before.SecondaryPrecision
                        : (inchPrimary ? (int)style.PrimaryDecimalRoundOff : (int)style.SecondaryDecimalRoundOff) == requiredPrecision &&
                            (!dual || (inchPrimary ? (int)style.SecondaryDecimalRoundOff : (int)style.PrimaryDecimalRoundOff) == requiredPrecision - 1);
                    if ((int)dimension.DisplayType != targetDisplay || (bool)dimension.UseStandardLimitAndFitTolerance ||
                        (int)style.PrimaryLinearToleranceUnits != Inches || !(bool)style.SecondaryToleranceEquivalent ||
                        !TryInches((string)dimension.PrimaryUpperTolerance, Inches, out u) || u != tolerance ||
                        !TryInches((string)dimension.PrimaryLowerTolerance, Inches, out l) || l != -tolerance ||
                        !precisionCorrect ||
                        (string)style.Name != before.StyleName)
                        throw new InvalidOperationException("Solid Edge did not retain the requested tolerance display.");
                    return tolerance;
                }
                catch (Exception error)
                {
                    string recovery;
                    try { before.Restore((object)dimension, (object)style); recovery = " Original tolerance restored."; }
                    catch (Exception restore) { recovery = " Recovery needs review: " + restore.Message; }
                    throw new InvalidOperationException("Could not cycle the tolerance: " + error.Message + recovery, error);
                }
            }
        }

        private sealed class ToleranceState
        {
            internal readonly int Display, Units;
            internal readonly string Upper, Lower, StyleName;
            private readonly string secondaryUpper, secondaryLower;
            private readonly bool standards, equivalent;
            private readonly bool numericTolerance;
            internal readonly int PrimaryPrecision, SecondaryPrecision;
            private readonly DimensionState original;
            internal ToleranceState(object dim, object styling)
            {
                dynamic d = dim, s = styling;
                Display = (int)d.DisplayType; Units = (int)s.PrimaryLinearToleranceUnits;
                Upper = (string)d.PrimaryUpperTolerance; Lower = (string)d.PrimaryLowerTolerance;
                secondaryUpper = (string)d.SecondaryUpperTolerance; secondaryLower = (string)d.SecondaryLowerTolerance;
                standards = (bool)d.UseStandardLimitAndFitTolerance; equivalent = (bool)s.SecondaryToleranceEquivalent;
                PrimaryPrecision = (int)s.PrimaryDecimalRoundOff; SecondaryPrecision = (int)s.SecondaryDecimalRoundOff;
                StyleName = (string)s.Name; original = new DimensionState(dim);
                decimal value;
                numericTolerance = TryInches(Upper, Units, out value) && TryInches(Lower, Units, out value);
                if ((Display == Limits || Display == UnitTolerance) && !numericTolerance)
                    throw new InvalidOperationException("The existing numeric limits contain unsupported tolerance text. Review them before cycling.");
            }
            internal void Restore(object dim, object styling)
            {
                dynamic d = dim, s = styling;
                d.UseStandardLimitAndFitTolerance = false;
                s.PrimaryLinearToleranceUnits = Units;
                // Restore known numeric offsets even when their original mode was
                // Nominal, so a failed exit does not leave hidden nonzero offsets.
                d.DisplayType = Display == Limits || Display == UnitTolerance || (Display == Nominal && numericTolerance) ? UnitTolerance : Display;
                d.PrimaryUpperTolerance = Upper; d.PrimaryLowerTolerance = Lower;
                s.SecondaryToleranceEquivalent = false;
                d.SecondaryUpperTolerance = secondaryUpper; d.SecondaryLowerTolerance = secondaryLower;
                s.SecondaryToleranceEquivalent = equivalent;
                s.PrimaryDecimalRoundOff = PrimaryPrecision; s.SecondaryDecimalRoundOff = SecondaryPrecision;
                d.DisplayType = Display; d.UseStandardLimitAndFitTolerance = standards;
                original.RestoreAndVerify(dim);
                if ((int)s.PrimaryLinearToleranceUnits != Units || (bool)s.SecondaryToleranceEquivalent != equivalent ||
                    (int)s.PrimaryDecimalRoundOff != PrimaryPrecision || (int)s.SecondaryDecimalRoundOff != SecondaryPrecision ||
                    (bool)d.UseStandardLimitAndFitTolerance != standards)
                    throw new InvalidOperationException("Original tolerance settings were not retained.");
            }
        }
    }
}
