using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DualDimensionToggle
{
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
            result = source; count = 0; reason = null;
            if (String.IsNullOrEmpty(source)) return true;
            char[] scan = source.ToCharArray();
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
            object style = null;
            try
            {
                string[] before = new string[4], after = new string[4];
                string priorRecord = CalloutHistoryStore.Read(note);
                CalloutHistory history = CalloutHistory.Decode(priorRecord);
                string nextRecord = priorRecord;
                int values = 0;
                for (int field = 0; field < before.Length; field++)
                {
                    before[field] = ReadText(note, field);
                    int changed; string reason;
                    if (!CalloutText.Convert(before[field], toDual, out after[field], out changed, out reason))
                    { report.CalloutsSkipped++; report.Details.Add("Callout " + index + " unchanged: " + reason); return; }
                    if (toDual && history != null && history.Dual[field] != history.Inch[field])
                    {
                        // Only restore a known conversion, never expand other
                        // bare decimal values in an inch-only note. An edited
                        // tracked field requires review rather than stale text.
                        if (before[field] == history.Inch[field])
                        {
                            after[field] = history.Dual[field];
                            string ignored;
                            CalloutText.Convert(history.Dual[field], false, out ignored, out changed, out reason);
                        }
                        else if (before[field] != history.Dual[field])
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
                            if (history.Dual[field] != history.Inch[field] && before[field] != history.Dual[field])
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
                if (!changeStyle) targetStyle = sourceStyle;
                if (!changeStyle && values == 0 && nextRecord == priorRecord) return;
                try
                {
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
            }
            finally { Com.Release(ref style); }
        }
        private static string ReadText(object note, int field)
        {
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
