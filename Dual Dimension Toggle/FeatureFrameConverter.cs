using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DualDimensionToggle
{
    internal enum FrameTextStatus { Empty, Preserved, Changed, AlreadyTarget, Unsupported }

    internal static class FeatureFrameText
    {
        // Restrict matching to the tolerance compartment, never datum cells
        // (A1, B2, etc.). Modern Solid Edge returns %VB cell separators; the
        // supplied SDK examples also use legacy VB, and the editor accepts |.
        private static readonly Regex Separator = new Regex(@"%VB|\||(?<!%)VB", RegexOptions.CultureInvariant);
        private const string Number = @"(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)";
        // Keep formatting/symbol tokens verbatim. The unprefixed symbols are
        // the legacy diameter/material-condition/projected-tolerance codes.
        // Free text, extra numbers, fractions and expressions fail closed.
        private const string Symbols = @"(?:\s|%[A-Za-z]{2}|DI|MC|LC|SC|PT|[\p{S}\u00d8\u00f8\u24c2\u24c1\u24c8\u24c5])*";
        private static readonly Regex Tolerance = new Regex(
            @"\A(?<prefix>" + Symbols + @")(?<first>" + Number + @")" +
            @"(?:\s*\[\s*(?<inch>" + Number + @")\s*\])?(?<suffix>" + Symbols + @")\z",
            RegexOptions.CultureInvariant);
        private static readonly Regex ProjectionHeight = new Regex(
            @"\A\s*(?:%PT|PT)\s*" + Number + @"\s*\z", RegexOptions.CultureInvariant);

        internal static FrameTextStatus Convert(string row, bool toDual, out string converted, out string reason)
        {
            converted = row;
            reason = null;
            if (String.IsNullOrWhiteSpace(row)) return FrameTextStatus.Empty;
            Match firstSeparator = Separator.Match(row);
            if (!firstSeparator.Success)
            {
                // ProjectedToleranceFrame aliases TertiaryFrame in Solid Edge.
                // A stand-alone projected-zone height is not a tolerance cell.
                if (ProjectionHeight.IsMatch(row)) return FrameTextStatus.Preserved;
                reason = "No recognizable tolerance compartment.";
                return FrameTextStatus.Unsupported;
            }
            int start = firstSeparator.Index + firstSeparator.Length;
            Match nextSeparator = Separator.Match(row, start);
            int length = (nextSeparator.Success ? nextSeparator.Index : row.Length) - start;
            string cell = row.Substring(start, length);
            Match tolerance = Tolerance.Match(cell);
            if (!tolerance.Success)
            {
                reason = "Tolerance is not a single decimal value or a metric[inch] pair.";
                return FrameTextStatus.Unsupported;
            }
            bool dual = tolerance.Groups["inch"].Success;
            if (dual == toDual) return FrameTextStatus.AlreadyTarget;
            string inch = tolerance.Groups[dual ? "inch" : "first"].Value;
            string value = inch;
            if (toDual)
            {
                int point = inch.IndexOf('.');
                int places = point < 0 ? 0 : inch.Length - point - 1;
                decimal inches;
                if (places > 28 || !Decimal.TryParse(inch, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out inches))
                {
                    reason = "Tolerance exceeds supported decimal precision or range.";
                    return FrameTextStatus.Unsupported;
                }
                try
                {
                    // Decimal math avoids binary floating point rounding drift.
                    // Keep the original inch text exactly (including zeros).
                    // User rule: metric has one fewer decimal place; ties round
                    // away from zero, e.g. .025 in -> .64 mm at two places.
                    int metricPlaces = Math.Max(0, places - 1);
                    decimal mm = Decimal.Round(inches * 25.4m, metricPlaces, MidpointRounding.AwayFromZero);
                    string metric = mm.ToString("F" + metricPlaces, CultureInfo.InvariantCulture);
                    if (inch.StartsWith(".", StringComparison.Ordinal) && metric.StartsWith("0.", StringComparison.Ordinal))
                        metric = metric.Substring(1);
                    value = metric + "[" + inch + "]";
                }
                catch (OverflowException)
                {
                    reason = "Converted tolerance exceeds the supported decimal range.";
                    return FrameTextStatus.Unsupported;
                }
            }
            // The bracketed inch value is authoritative when going to single;
            // never reconstruct it from an already rounded metric value.
            string replacement = tolerance.Groups["prefix"].Value + value + tolerance.Groups["suffix"].Value;
            converted = row.Substring(0, start) + replacement + row.Substring(start + length);
            return FrameTextStatus.Changed;
        }
    }

    internal static class FeatureFrameConverter
    {
        private static readonly string[] RowNames = { "primary", "secondary", "tertiary", "quaternary" };

        internal static void ConvertSheet(object sheet, bool toDual, ConversionReport report)
        {
            object frames = null;
            try
            {
                frames = ((dynamic)sheet).FeatureControlFrames;
                int count = (int)((dynamic)frames).Count;
                for (int index = 1; index <= count; index++)
                {
                    report.FramesExamined++;
                    object frame = null;
                    try
                    {
                        frame = ((dynamic)frames).Item(index);
                        ConvertFrame(frame, toDual, index, report);
                    }
                    catch (Exception error)
                    {
                        report.FramesFailed++;
                        report.Details.Add("Feature Control Frame " + index + ": " + error.Message);
                    }
                    finally { Com.Release(ref frame); }
                }
            }
            catch (Exception error)
            {
                report.FramesFailed++;
                report.Details.Add("Could not finish reading Feature Control Frames: " + error.Message);
            }
            finally { Com.Release(ref frames); }
        }

        private static void ConvertFrame(object frame, bool toDual, int index, ConversionReport report)
        {
            string[] before = new string[4];
            string[] after = new string[4];
            int changed = 0, populated = 0;
            // Preflight all rows before editing so an unsupported row cannot
            // leave a stacked frame partly converted. ProjectedToleranceFrame
            // aliases TertiaryFrame; the parser preserves a standalone %PT height
            // there while converting actual tolerance cells in other rows.
            for (int row = 0; row < before.Length; row++)
            {
                before[row] = ReadRow(frame, row);
                string reason;
                FrameTextStatus status = FeatureFrameText.Convert(before[row], toDual, out after[row], out reason);
                if (status == FrameTextStatus.Unsupported)
                {
                    report.FramesSkipped++;
                    report.Details.Add("Feature Control Frame " + index + " unchanged (" + RowNames[row] + " row): " + reason);
                    return;
                }
                if (status == FrameTextStatus.Changed || status == FrameTextStatus.AlreadyTarget) populated++;
                if (status == FrameTextStatus.Changed) changed++;
            }
            if (changed == 0)
            {
                if (populated == 0) report.FramesSkipped++;
                else report.FramesAlreadyTarget++;
                return;
            }
            try
            {
                for (int row = 0; row < before.Length; row++)
                {
                    // Solid Edge can clear projected-height text when the
                    // primary row changes. Compare the current row, not just
                    // the snapshot, and reapply preserved following rows too.
                    if (!String.Equals(ReadRow(frame, row), after[row], StringComparison.Ordinal)) WriteRow(frame, row, after[row]);
                }
                // Read back even untouched rows to catch setters that reject a
                // value silently or unexpectedly alter a neighboring row.
                for (int row = 0; row < before.Length; row++)
                {
                    string observed = ReadRow(frame, row);
                    if (!String.Equals(observed, after[row], StringComparison.Ordinal))
                        throw new InvalidOperationException("Solid Edge did not retain the expected " + RowNames[row] + " row (expected '" + after[row] + "', read '" + observed + "').");
                }
            }
            catch (Exception error)
            {
                bool restored = true;
                for (int row = 0; row < before.Length; row++)
                {
                    try
                    {
                        if (!String.Equals(ReadRow(frame, row), before[row], StringComparison.Ordinal)) WriteRow(frame, row, before[row]);
                    }
                    catch { restored = false; }
                }
                for (int row = 0; row < before.Length; row++)
                {
                    try { if (!String.Equals(ReadRow(frame, row), before[row], StringComparison.Ordinal)) restored = false; }
                    catch { restored = false; }
                }
                throw new InvalidOperationException(error.Message + (restored ? " Original frame text restored." :
                    " Could not restore all original frame text; inspect this frame before saving."), error);
            }
            report.FramesChanged++;
            report.FrameRowsChanged += changed;
        }

        private static string ReadRow(object frame, int row)
        {
            switch (row)
            {
                case 0: return (string)((dynamic)frame).PrimaryFrame;
                case 1: return (string)((dynamic)frame).SecondaryFrame;
                case 2: return (string)((dynamic)frame).TertiaryFrame;
                default: return (string)((dynamic)frame).QuaternaryFrame;
            }
        }
        private static void WriteRow(object frame, int row, string value)
        {
            switch (row)
            {
                case 0: ((dynamic)frame).PrimaryFrame = value; break;
                case 1: ((dynamic)frame).SecondaryFrame = value; break;
                case 2: ((dynamic)frame).TertiaryFrame = value; break;
                default: ((dynamic)frame).QuaternaryFrame = value; break;
            }
        }
    }
}
