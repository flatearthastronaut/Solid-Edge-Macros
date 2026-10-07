using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DualDimensionToggle
{
    /// <summary>
    /// Pure text conversion for explicit metric[inch] pairs, including slash-
    /// separated limits. Never interprets a bare decimal as an inch length.
    /// It is deliberately independent of COM so parsing and preservation rules
    /// can be tested without opening or changing a Solid Edge document.
    /// </summary>
    internal static class CalloutText
    {
        private const string Number = @"[+-]?(?:[0-9]+(?:\.[0-9]+)?|\.[0-9]+)";
        private const string List = Number + @"(?:[ \t]*/[ \t]*" + Number + @")*";
        private static readonly Regex Values = new Regex(
            @"(?<![\w.,/\[+\-])(?<metric>" + List + @")[ \t]*\[[ \t]*(?<inch>" + List + @")[ \t]*\](?![\w,/\]+\-]|\.[0-9])",
            RegexOptions.CultureInvariant);
        private static readonly Regex Numbers = new Regex(Number, RegexOptions.CultureInvariant);
        private static readonly Regex Symbol = new Regex(@"%[A-Za-z]{2}", RegexOptions.CultureInvariant);
        private static readonly Regex NumericBracket = new Regex(@"\[[ \t]*[+\-.0-9]", RegexOptions.CultureInvariant);
        private static readonly Regex AngleOnlyPattern = new Regex(@"\A\s*" + Number + @"\s*(?:%DG|\u00b0|DEG(?:REES)?)\s*\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal static bool IsAngleOnly(string text) { return text != null && AngleOnlyPattern.IsMatch(text); }

        internal static bool Convert(string source, bool toDual, out string result, out int count, out string reason)
        {
            // Build the complete replacement before publishing result. On false,
            // the caller must discard all outputs except reason and leave the
            // entire note alone; count may describe work preceding the failure.
            result = source; count = 0; reason = null;
            if (String.IsNullOrEmpty(source)) return true;
            char[] scan = source.ToCharArray();
            // Replace excluded spans with spaces instead of deleting them.
            // Match offsets therefore remain valid against the original source,
            // which is used for copying all untouched characters into the result.
            // Mask property expressions before scanning numbers. Work on raw
            // BalloonText, never evaluated BalloonDisplayedText, so holes and
            // other linked model fields remain associative. Nested fields and
            // format options such as /@3 and /ST+.001^-.002 remain byte-exact.
            for (int i = 0; i < source.Length - 1; i++)
            {
                if (source[i] != '%' || source[i + 1] != '{') continue;
                int start = i, depth = 1;
                i += 2;
                while (i < source.Length && depth > 0)
                {
                    if (source[i] == '{') depth++;
                    else if (source[i] == '}') depth--;
                    scan[i++] = ' ';
                }
                if (depth != 0) { reason = "Unclosed property-text expression."; return false; }
                scan[start] = scan[start + 1] = ' ';
                i--;
            }
            // Simple codes such as %DI may touch a literal without whitespace.
            // Masking codes permits that length to match without editing codes.
            string masked = new string(scan);
            foreach (Match code in Symbol.Matches(masked))
                for (int i = code.Index; i < code.Index + code.Length; i++) scan[i] = ' ';
            masked = new string(scan);
            MatchCollection values = Values.Matches(masked);
            StringBuilder output = new StringBuilder();
            int cursor = 0;
            foreach (Match match in values)
            {
                // Masked text could make numbers on either side of a property
                // appear contiguous. Reject such a span instead of deleting the
                // embedded reference. Both lists must also have equal arity:
                // 6.50/6.35[.256/.250] is two values, not one arithmetic fraction.
                if (source.Substring(match.Index, match.Length).IndexOf('%') >= 0)
                { reason = "A numeric pair overlaps a symbol or property expression."; return false; }
                if (Numbers.Matches(match.Groups["metric"].Value).Count != Numbers.Matches(match.Groups["inch"].Value).Count)
                { reason = "Metric/inch lists have different numbers of values."; return false; }
                for (int i = match.Index; i < match.Index + match.Length; i++) scan[i] = ' ';
                string inches = match.Groups["inch"].Value;
                string replacement = inches;
                if (toDual)
                {
                    // Only explicitly bracketed pairs are candidates. Ordinary
                    // decimals, quantities, threads and angles are never inferred.
                    StringBuilder metric = new StringBuilder();
                    int position = 0;
                    foreach (Match number in Numbers.Matches(inches))
                    {
                        string dual;
                        if (!UnitText.TryDual(number.Value, out dual, out reason)) return false;
                        metric.Append(inches.Substring(position, number.Index - position));
                        metric.Append(dual.Substring(0, dual.IndexOf('[')));
                        position = number.Index + number.Length;
                    }
                    metric.Append(inches.Substring(position));
                    replacement = metric + "[" + inches + "]";
                }
                output.Append(source.Substring(cursor, match.Index - cursor));
                output.Append(replacement);
                cursor = match.Index + match.Length;
                if (source.Substring(match.Index, match.Length) != replacement) count += Numbers.Matches(inches).Count;
            }
            // An unrecognized numeric bracket must not lead to partially
            // converted text or a style-only conversion of this same note.
            if (NumericBracket.IsMatch(new string(scan)))
            { reason = "Unsupported numeric bracket expression."; count = 0; return false; }
            output.Append(source.Substring(cursor));
            result = output.ToString();
            return true;
        }

    }

    /// <summary>
    /// Coordinates style selection, literal pairs, /DU fields, and persistent
    /// history for active-sheet callouts. Each note is preflighted before edits;
    /// a failed write triggers best-effort recovery of that note, while earlier
    /// successful notes stay changed and unsaved for the user to review.
    /// </summary>
    internal static class CalloutConverter
    {
        internal static void ConvertSheet(object sheet, StyleMap map, bool toDual, ConversionReport report)
        {
            object items = null;
            try
            {
                items = ((dynamic)sheet).Balloons;
                int count = (int)((dynamic)items).Count;
                for (int index = 1; index <= count; index++)
                {
                    object note = null;
                    try
                    {
                        note = ((dynamic)items).Item(index);
                        // The SDK represents callouts as Balloons with Callout=1.
                        // Ordinary item balloons and parts-list labels are not notes.
                        if ((int)((dynamic)note).Callout == 0) continue;
                        report.CalloutsExamined++;
                        if ((bool)((dynamic)note).DisplayByItemNumber || (bool)((dynamic)note).LinkToPartsList)
                        { report.CalloutsSkipped++; continue; }
                        ConvertNote(note, map, toDual, index, report);
                    }
                    catch (Exception error)
                    { report.CalloutsFailed++; report.Details.Add("Callout " + index + ": " + error.Message); }
                    finally { Com.Release(ref note); }
                }
            }
            catch (Exception error)
            { report.CalloutsFailed++; report.Details.Add("Could not finish reading callouts: " + error.Message); }
            finally { Com.Release(ref items); }
        }

        private static void ConvertNote(object note, StyleMap map, bool toDual, int index, ConversionReport report)
        {
            // Order matters: snapshot and validate literals/history; prepare
            // field formatting; resolve the target style; then write and verify.
            // No mutation occurs during preflight, including on a stale record.
            object style = null;
            try
            {
                string[] before = new string[4], after = new string[4];
                string priorRecord = CalloutHistoryStore.Read(note);
                CalloutHistory history = CalloutHistory.Decode(priorRecord);
                string nextRecord = priorRecord;
                // Keep the exact prior serialized string for rollback, including
                // an empty record. Decode treats that empty value as no history,
                // allowing the current explicit pairs to seed a valid new record.
                int values = 0;
                for (int field = 0; field < before.Length; field++)
                {
                    before[field] = ReadText(note, field);
                    int changed; string reason;
                    if (!CalloutText.Convert(before[field], toDual, out after[field], out changed, out reason))
                    { report.CalloutsSkipped++; report.Details.Add("Callout " + index + " unchanged: " + reason); return; }
                    if (toDual && history != null && !CalloutFields.SameValues(history.Dual[field], history.Inch[field]))
                    {
                        // Only restore a known conversion, never expand other
                        // bare decimal values in an inch-only note. An edited
                        // tracked field requires review rather than stale text.
                        if (CalloutFields.SameValues(before[field], history.Inch[field]))
                        {
                            after[field] = history.Dual[field];
                            string ignored;
                            CalloutText.Convert(history.Dual[field], false, out ignored, out changed, out reason);
                        }
                        else if (!CalloutFields.SameValues(before[field], history.Dual[field]))
                        {
                            report.CalloutsSkipped++;
                            report.Details.Add("Callout " + index + " unchanged: text containing a saved pair was edited; review the note before restoring dual values.");
                            return;
                        }
                    }
                    values += changed;
                }
                if (!toDual && values > 0)
                {
                    // Do not replace an older record if a tracked text field
                    // was edited. This avoids silently losing known pairs.
                    if (history != null)
                        for (int field = 0; field < before.Length; field++)
                            if (!CalloutFields.SameValues(history.Dual[field], history.Inch[field]) && !CalloutFields.SameValues(before[field], history.Dual[field]))
                            {
                                report.CalloutsSkipped++;
                                report.Details.Add("Callout " + index + " unchanged: existing saved pairs need review before another text conversion.");
                                return;
                            }
                    CalloutHistory saved = new CalloutHistory();
                    for (int field = 0; field < before.Length; field++)
                    {
                        saved.Inch[field] = after[field];
                        int ignored; string reason;
                        if (!CalloutText.Convert(before[field], true, out saved.Dual[field], out ignored, out reason))
                        { report.CalloutsSkipped++; report.Details.Add("Callout " + index + " unchanged: " + reason); return; }
                    }
                    nextRecord = saved.Encode();
                }
                else if (toDual && history != null) nextRecord = null;
                // Apply field formatting after restoring any saved literals.
                // History remains dedicated to literal pairs; linked-only notes
                // need no metadata, and old records remain compatible.
                bool textChanged = false;
                int formattedFields = 0;
                for (int field = 0; field < after.Length; field++)
                {
                    after[field] = CalloutFields.Convert(after[field], toDual);
                    if (after[field] != before[field]) textChanged = true;
                    if (CalloutFields.Convert(before[field], toDual) != before[field]) formattedFields++;
                }
                if (CalloutText.IsAngleOnly(before[0]) && String.IsNullOrWhiteSpace(before[1]) && String.IsNullOrWhiteSpace(before[2]) && String.IsNullOrWhiteSpace(before[3]))
                { report.CalloutsSkipped++; return; }
                style = ((dynamic)note).Style;
                string sourceStyle = (string)((dynamic)style).Name, targetStyle;
                MatchStatus status = map.Resolve(sourceStyle, toDual, out targetStyle);
                if (status == MatchStatus.Missing || status == MatchStatus.Ambiguous)
                {
                    report.CalloutsSkipped++;
                    report.Details.Add("Callout " + index + " unchanged: " + (status == MatchStatus.Missing ? "no" : "multiple") + " matching styles for '" + sourceStyle + "'.");
                    return;
                }
                bool changeStyle = status == MatchStatus.Convert;
                // A custom/unrecognized style remains as-is, but explicit pairs
                // and supported field codes can still be changed. AlreadyTarget
                // also must not suppress text repairs after a previous style-only
                // conversion. Missing/ambiguous recognized counterparts skip above.
                if (!changeStyle) targetStyle = sourceStyle;
                if (!changeStyle && !textChanged && nextRecord == priorRecord) return;
                try
                {
                    // Save the restoration data before removing bracketed pairs.
                    // If metadata cannot be written, do not start style/text edits.
                    // Verify all three components afterward: COM setters can return
                    // successfully while ignoring or altering the requested value.
                    if (nextRecord != priorRecord) CalloutHistoryStore.Write(note, nextRecord);
                    if (changeStyle) ((dynamic)style).Name = targetStyle;
                    // Reapply raw fields after the style assignment if needed;
                    // preserve model expressions instead of baking displayed text.
                    for (int field = 0; field < before.Length; field++)
                        if (!String.Equals(ReadText(note, field), after[field], StringComparison.Ordinal)) WriteText(note, field, after[field]);
                    if (!String.Equals((string)((dynamic)style).Name, targetStyle, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Requested callout style was not retained.");
                    for (int field = 0; field < before.Length; field++)
                        if (!String.Equals(ReadText(note, field), after[field], StringComparison.Ordinal)) throw new InvalidOperationException("Requested callout text was not retained.");
                    if (CalloutHistoryStore.Read(note) != nextRecord) throw new InvalidOperationException("Callout conversion record was not retained.");
                }
                catch (Exception error)
                {
                    // Restore style first because selecting a style may affect
                    // raw text, then restore the four fields and original record.
                    // Attempt each component even if another fails. This is not a
                    // document-wide undo transaction; report incomplete recovery.
                    bool restored = true;
                    try { if ((string)((dynamic)style).Name != sourceStyle) ((dynamic)style).Name = sourceStyle; }
                    catch { restored = false; }
                    for (int field = 0; field < before.Length; field++)
                    {
                        try { if (ReadText(note, field) != before[field]) WriteText(note, field, before[field]); }
                        catch { restored = false; }
                    }
                    try
                    {
                        if ((string)((dynamic)style).Name != sourceStyle) restored = false;
                        for (int field = 0; field < before.Length; field++) if (ReadText(note, field) != before[field]) restored = false;
                    }
                    catch { restored = false; }
                    try
                    {
                        if (CalloutHistoryStore.Read(note) != priorRecord) CalloutHistoryStore.Write(note, priorRecord);
                        if (CalloutHistoryStore.Read(note) != priorRecord) restored = false;
                    }
                    catch { restored = false; }
                    throw new InvalidOperationException(error.Message + (restored ? " Original callout style and text restored." : " Could not restore the entire callout; inspect it before saving."), error);
                }
                report.CalloutsChanged++;
                if (changeStyle) report.CalloutStylesChanged++;
                report.CalloutValuesChanged += values;
                report.CalloutFieldsChanged += formattedFields;
            }
            finally { Com.Release(ref style); }
        }
        private static string ReadText(object note, int field)
        {
            // Keep this mapping synchronized with WriteText and CalloutHistory.
            // Read raw text only: displayed text would replace model references
            // with fixed values and lose their connection to the model.
            switch (field)
            {
                case 0: return (string)((dynamic)note).BalloonText;
                case 1: return (string)((dynamic)note).BalloonTextLower;
                case 2: return (string)((dynamic)note).BalloonTextPrefix;
                default: return (string)((dynamic)note).BalloonTextSuffix;
            }
        }
        private static void WriteText(object note, int field, string value)
        {
            switch (field)
            {
                case 0: ((dynamic)note).BalloonText = value; break;
                case 1: ((dynamic)note).BalloonTextLower = value; break;
                case 2: ((dynamic)note).BalloonTextPrefix = value; break;
                default: ((dynamic)note).BalloonTextSuffix = value; break;
            }
        }
    }
}
