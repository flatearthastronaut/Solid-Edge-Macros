using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DualDimensionToggle
{
    internal enum FrameTextStatus { Empty, Preserved, Changed, AlreadyTarget, Unsupported }

    /// <summary>
    /// Rewrites only a frame row's tolerance cell. The symbol cell and datum
    /// cells remain byte-for-byte text copies; their numbers are not lengths to
    /// convert. Unlike free-form callouts, a bare decimal in this known tolerance
    /// cell is explicitly defined by the user as an inch-only tolerance.
    /// </summary>
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
            // Keep the original output until validation succeeds. Empty and
            // projected-height rows are preserved, and Unsupported causes the
            // caller to skip the complete frame before changing any of its rows.
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
            // The first separator ends the geometric-characteristic cell; the
            // second, if present, begins the datums. Limit matching to this slice
            // so datum identifiers such as A1 cannot become conversion candidates.
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
            // Requesting an already displayed mode is a no-op for frame rows.
            // Existing metric values are not recalculated in this branch, unlike
            // explicit callout pairs which support metric-value normalization.
            if (dual == toDual) return FrameTextStatus.AlreadyTarget;
            string inch = tolerance.Groups[dual ? "inch" : "first"].Value;
            string value = inch;
            if (toDual)
            {
                if (!UnitText.TryDual(inch, out value, out reason)) return FrameTextStatus.Unsupported;
            }
            // The bracketed inch value is authoritative when going to single;
            // never reconstruct it from an already rounded metric value.
            string replacement = tolerance.Groups["prefix"].Value + value + tolerance.Groups["suffix"].Value;
            converted = row.Substring(0, start) + replacement + row.Substring(start + length);
            return FrameTextStatus.Changed;
        }
    }

    /// <summary>
    /// Bridges pure frame parsing to the four COM row properties. Validation is
    /// all-or-nothing per frame; applying or restoring rows is best-effort because
    /// Solid Edge exposes individual setters rather than a single atomic update.
    /// </summary>
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
                // Restore from primary to quaternary, the same order as applying
                // edits, so an early setter's clearing of later rows is repaired.
                // Continue through all rows after a failure and then verify every
                // value; a successful setter call alone does not prove recovery.
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
            // Count only validated tolerance changes, not extra setter calls
            // needed to reapply a projected height or another preserved row.
            report.FrameRowsChanged += changed;
        }

        private static string ReadRow(object frame, int row)
        {
            // Row indices are shared with RowNames and the before/after arrays.
            // Do not read ProjectedToleranceFrame as a fifth independent row:
            // Solid Edge exposes it through TertiaryFrame on these annotations.
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
