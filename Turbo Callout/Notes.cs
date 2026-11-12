using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using HoleTableFormatter;

namespace TurboCallout
{
    // Recognition is compiled directly from Turbo Hole Table Callouts. This
    // adapter supplies annotation text; it never chooses a family by feature name.
    internal sealed class NotePlan
    {
        internal string Text, Category;
        internal int InchPlaces;
        internal bool Dual;
        internal bool PreserveFormat;
        internal bool FullSizeStacks;
    }

    internal static class Notes
    {
        // Precision belongs to the individual machining value, not the whole
        // hole family: a four-place dowel diameter still has a two-place depth.
        internal static int InchPlaces(Decision choice, HoleFacts hole, string field)
        {
            if (field == "BD" && choice.Family == "O-ring face seal") return 3;
            if (field == "HD" || field == "BD" || field == "TD") return 2;
            if (choice.IsDowel && field == (hole.Type == 1 ? "HS" : "BS")) return 4;
            if (choice.Family == "Spring pin" && field == "HS") return 3;
            // Koenig seats retain their saved three-place limit diameters;
            // their pilot drill and seat depth follow the new two-place rule.
            if (choice.Family == "Koenig plug" && field == "BS") return 3;
            return 2;
        }

        internal static NotePlan Build(RuleBook rules, HoleFacts hole, bool dual)
        {
            Decision choice = Recognize(rules, hole);
            string text;
            if (choice.IsDowel)
            {
                string size = hole.Type == 1 ? "HS" : "BS", depth = hole.Type == 1 ? "%ZH" : "%BD DP.";
                string seed = (hole.Type == 1 ? "" : "%DI%HS DRILL %ZH%RT") +
                    "%DI%{%" + size + "/LT0^0/NU/@4} DIA " + depth;
                text = LinkedCallouts.For(choice, hole, seed);
                if (dual && text != null)
                {
                    string old = LinkedCallouts.PreviousDowelNote(choice, hole, seed);
                    text = text.Replace(LinkedCallouts.DowelFitNote(choice), old.Split(new[] { "%RT" }, StringSplitOptions.None).Last());
                }
            }
            else if (choice.Family == "Koenig plug")
            {
                // Saved inch limits are the manufacturing standard. Keep them
                // associative by expressing their offsets from the modeled seat.
                var matches = rules.Presets.Where(p => p.Family == "Koenig plug" && RuleBook.Near(hole.Bore, p.Bore))
                    .Select(p => Regex.Match(p.Name, @"(\d+)mm").Groups[1].Value).Distinct().ToList();
                if (matches.Count != 1) throw new InvalidOperationException("Ambiguous Koenig seat size.");
                string key = "CV HOLE " + Int32.Parse(matches[0], CultureInfo.InvariantCulture).ToString("00") + "mm";
                double min, max;
                if (!KoenigLimits(key, out min, out max))
                    throw new InvalidOperationException("No approved saved Koenig limits for this size.");
                text = "%DI%HS DRILL %ZH%RT%DI" + Limits("BS", hole.Bore, min, max) +
                    " DIA %BD DP.%RT" + LinkedCallouts.KoenigNote;
            }
            else if (choice.Family == "O-ring face seal")
                text = "%DI%HS DRILL %ZH%RT%DI%BS DIA " + Limits("BD", hole.BoreDepth, .050, .054) + " DP.";
            else if (choice.Family == "Spring pin")
                text = "%DI" + Limits("HS", hole.Diameter, choice.Minimum, choice.Maximum) +
                    " DIA %ZH%RT(FOR 3/8 SPRING PIN)";
            else if (choice.Port != 0)
            {
                // A separate spotface has no native thread-depth field. Use the
                // approved size-specific saved note, as the table macro does.
                // A modeled threaded entrance instead keeps its native %ZT.
                text = "%DI%HS DRILL %ZH%RT#" + choice.Port + " STRAIGHT THR'D O-RING PORT%RT" +
                    "%DI%BS SPOTFACE %BD DP.%RT";
                if (hole.Type == 8 || hole.Type == 5)
                    text += "%TS THR'D %ZT";
                else if (choice.Port == 8)
                {
                    if (!(choice.ThreadDepth > 0)) throw new InvalidOperationException("No unique saved #8 thread-depth standard.");
                    text += "3/4-16 THR'D " + (dual ? (choice.ThreadDepth.Value * 25.4).ToString("0.0", CultureInfo.InvariantCulture) + "[" : "") +
                        choice.ThreadDepth.Value.ToString("0.00", CultureInfo.InvariantCulture) + (dual ? "]" : "") + " DP.";
                }
                else
                {
                    string saved = LinkedCallouts.ReadSaved("SAE Port #" + choice.Port + (dual ? "-M" : ""));
                    text += saved.Split(new[] { "%RT" }, StringSplitOptions.None).Last();
                }
            }
            else if (choice.Category == "Threaded hole")
                // Retain the saved thread fields; the family quantity policy
                // below gives both unit modes the same linked spacing line.
                text = "%DI%HS DRILL %ZH%RT" + LinkedCallouts.ReadSaved(dual ? "HC -THR'D M[I]" : "HC -THR'D");
            else if (choice.Family == "Simple drill")
                // Use the complete requested note, including its model-linked
                // quantity and Smart Depth. The inch and dual entries have
                // different quantity wording, so select the actual saved entry.
                text = LinkedCallouts.ReadSaved(dual ? "HC -DRILL M[I]" : "HC -DRILL");
            else if (choice.Family == "SHCS counterbore")
                // Keep the complete saved note, including the linked quantity
                // and the requested literal (FOR X S.H.C.S.) line. Leave X
                // exactly as saved rather than guessing a screw designation.
                text = LinkedCallouts.ReadSaved(dual ? "HC -C'BORE M[I]" : "HC -C'BORE");
            else throw new InvalidOperationException("No approved callout template for " + choice.Category + ".");
            if (String.IsNullOrWhiteSpace(text)) throw new InvalidOperationException("Saved note could not preserve the model links.");
            // Unlike hole-table cell overrides, native annotations evaluate smart
            // depths directly, so finite/through wording remains model driven.
            // The inherited dowel builder expands them; restore the smart field.
            if (choice.IsDowel && hole.Type == 1)
                text = Regex.Replace(text, @"%\{%HD[^{}]*\} DP\.", "%ZH");
            if (choice.IsDowel && hole.Type == 7)
                text = Regex.Replace(text, @"(?<=DRILL )(?:(?:%\{%HD[^{}]*\} DP\.)|THRU)", "%ZH");
            text = ApplyWordSpacing(ApplyPrecision(FormatFields(text, dual), choice, hole, dual));
            // Native BalloonText uses carriage returns, not the table's %RT.
            return new NotePlan { Text = ApplyQuantity(new[] { text.Replace("%RT", "\r") }, choice, null)[0], Category = choice.Category, InchPlaces = 2, Dual = dual, FullSizeStacks = HasFixedStacks(text) };
        }

        internal static Decision Recognize(RuleBook rules, HoleFacts hole)
        {
            // The table rules deliberately reject unspecified hole families.
            // Supply Greg's drill-note fallback only after special matching,
            // never for a conflicting pin match or another review condition.
            Decision choice = rules.Match(hole);
            if (choice.Slot == 0 && choice.Reason == "No verified standard-hole match. Leave unchanged." && IsPlainDrill(hole))
                choice = new Decision { Category = "Simple drilled hole", Family = "Simple drill", Slot = 1 };
            if (choice.Slot == 0) throw new InvalidOperationException(choice.Reason);
            return choice;
        }

        const string Fields = @"%\{%(?<field>HS|HD|BS|BD|SS|TD)(?<options>[^{}]*)\}|(?<!\{)%(?<field>HS|HD|BS|BD|SS|TD)";

        internal static string ApplyPrecision(string text, Decision choice, HoleFacts hole, bool dual)
        {
            // A single native dual callout has only one secondary round-off.
            // Greg's saved-note fallback keeps that at two inch places and
            // types the higher-precision limits. Inch-only notes can retain
            // native fields throughout because /@ controls their inch values.
            // Existing text already uses primary-unit LT offsets: do not run
            // it through FormatFields, which converts new inch seed offsets.
            return Regex.Replace(text ?? "", Fields, m =>
            {
                string field = m.Groups["field"].Value;
                int places = InchPlaces(choice, hole, field);
                string options = Regex.Replace(m.Groups["options"].Value, @"/(?:DU|NU)(?=/|$)|/@\d+", "", RegexOptions.IgnoreCase);
                if (dual && places > 2)
                {
                    double? value = field == "HS" ? hole.Diameter : field == "BS" ? hole.Bore : field == "BD" ? hole.BoreDepth : null;
                    if (!PositiveFinite(value)) throw new InvalidOperationException("Missing value for fixed precision: " + field);
                    if (options.Length == 0) return FixedValue(value.Value, places);
                    Match limit = Regex.Match(options, @"^/LT(?<hi>[+\-]?[\d.]+)\^(?<lo>[+\-]?[\d.]+)$", RegexOptions.IgnoreCase);
                    if (!limit.Success) throw new InvalidOperationException("Custom tolerance formatting needs review before changing precision: " + field);
                    double upper = value.Value + Double.Parse(limit.Groups["hi"].Value, CultureInfo.InvariantCulture) / 25.4;
                    double lower = value.Value + Double.Parse(limit.Groups["lo"].Value, CultureInfo.InvariantCulture) / 25.4;
                    if (upper <= lower) throw new InvalidOperationException("Invalid limits for fixed precision: " + field);
                    // ST preserves the current stacked limit presentation,
                    // while the values themselves are explicitly fixed text.
                    return "%{/ST" + FixedNumber(upper * 25.4, places - 1, false) + "^" + FixedNumber(lower * 25.4, places - 1, false) +
                        "}[%{/ST" + FixedNumber(upper, places, true) + "^" + FixedNumber(lower, places, true) + "}]";
                }
                return "%{%" + field + (dual ? "/DU" : "") + options + "/@" + (dual ? places - 1 : places) + "}";
            });
        }

        static string FixedValue(double inch, int places)
        { return FixedNumber(inch * 25.4, places - 1, false) + "[" + FixedNumber(inch, places, true) + "]"; }

        static string FixedNumber(double value, int places, bool inch)
        {
            string result = Math.Round(value, places, MidpointRounding.AwayFromZero).ToString("F" + places, CultureInfo.InvariantCulture);
            return inch && result.StartsWith("0.", StringComparison.Ordinal) ? result.Substring(1) : result;
        }

        internal static bool HasTypical(string text)
        { return Regex.IsMatch(text ?? "", @"(?:^|\r|\n|%RT)\s*TYP\b", RegexOptions.IgnoreCase); }

        internal static bool HasFixedStacks(string text)
        { return Regex.IsMatch(text ?? "", @"%\{/ST[+\-]?(?:\d*\.)?\d+\^[+\-]?(?:\d*\.)?\d+\}", RegexOptions.IgnoreCase); }

        internal static string ApplyWordSpacing(string text)
        {
            // Saved machining lines have one separating space. Add one on
            // each side of DRILL and two after DIA by enforcing totals of
            // two/three spaces. Minimums make reruns stable and preserve any
            // wider custom gaps. Treat native field/stack codes as opaque so
            // neither property names nor fixed manufacturing values change.
            // Separate one-sided passes also handle adjacent keywords sharing
            // a gap: DIA DRILL must settle at three spaces, not grow on reruns.
            return RewriteLines(text, line => PadWord(PadWord(PadWord(line, "DRILL", 2, true), "DRILL", 2, false), "DIA", 3, false));
        }

        static string PadWord(string line, string word, int minimum, bool before)
        {
            string token = @"(?<!%)\b(?<word>" + word + @")\b", gap = @"(?<gap> *)";
            return Regex.Replace(line, @"%\{[^{}]*\}|" + (before ? gap + token : token + gap), m =>
            {
                if (!m.Groups["word"].Success) return m.Value;
                string spaces = new string(' ', Math.Max(minimum, m.Groups["gap"].Length));
                return before ? spaces + m.Groups["word"].Value : m.Groups["word"].Value + spaces;
            }, RegexOptions.IgnoreCase);
        }

        internal static bool UsesSpacingQuantity(Decision choice)
        { return choice.Category == "Threaded hole" || choice.Family == "SHCS counterbore"; }

        const string SpacingQuantity = "(%QC)PLC'S EQ. SP. AS SHOWN";
        const string QuantityCount = @"(?:\d+|%QC|%\{%QC(?:/[^{}]*)?\})";
        const string SpacingWords = @"PLC['’]S[ \t]+EQ\.?[ \t]*SP\.?(?:[ \t]+AS[ \t]+SHOWN)?[ \t]*\.?";
        static bool IsSpacingLine(string line)
        { return Regex.IsMatch(line, @"^[ \t]*(?:\([ \t]*" + QuantityCount + @"[ \t]*\)[ \t]*)?" + SpacingWords + @"[ \t]*$", RegexOptions.IgnoreCase); }
        static bool IsTypicalQuantityLine(string line)
        { return Regex.IsMatch(line, @"^[ \t]*TYP\.?[ \t]*\([ \t]*" + QuantityCount + @"[ \t]*\)[ \t]*(?:" + SpacingWords + @")?[ \t]*$", RegexOptions.IgnoreCase); }

        internal static string[] ApplyQuantity(string[] texts, Decision choice, int? quantity)
        {
            // Work across all four native text fields so a lower/prefix/suffix
            // TYP cannot cause a duplicate quantity line in the main text.
            string[] result = (string[])texts.Clone();
            if (!UsesSpacingQuantity(choice))
            {
                if (!texts.Any(HasTypical)) result[0] = EnsureTypical(result[0]);
                return result.Select(t => OmitSingleTypical(t, quantity)).ToArray();
            }

            bool hadQuantity = texts.Any(t => Regex.Split(t ?? "", @"\r\n|\r|\n|%RT").Any(line => IsSpacingLine(line) || IsTypicalQuantityLine(line)));
            // Explicit quantity-one text is still omitted when the model count
            // cannot be read. Never infer a new count from a literal number.
            result = result.Select(t => OmitSingleTypical(t, quantity)).ToArray();
            bool hasSpacing = result.Any(t => Regex.Split(t ?? "", @"\r\n|\r|\n|%RT").Any(IsSpacingLine));
            bool inserted = false;
            for (int field = 0; field < result.Length; field++)
                result[field] = RewriteLines(result[field], line =>
                {
                    bool spacing = IsSpacingLine(line), typical = IsTypicalQuantityLine(line);
                    if (!spacing && !typical) return line;
                    // Prefer the existing spacing-line position, especially
                    // above the counterbore's (FOR X S.H.C.S.) purpose line.
                    if (quantity != 1 && !inserted && (spacing || !hasSpacing))
                    { inserted = true; return SpacingQuantity; }
                    return null;
                });
            if (result.Any(HasTypical))
                throw new InvalidOperationException("Custom TYP wording needs review before replacing its quantity line.");
            if (!inserted && quantity != 1 && (quantity.HasValue || !hadQuantity) && !String.IsNullOrWhiteSpace(result[0]))
                result[0] += (result[0].EndsWith("\r", StringComparison.Ordinal) || result[0].EndsWith("\n", StringComparison.Ordinal) ? "" : "\r") + SpacingQuantity;
            return result;
        }

        // Returning null removes a whole line, including its separator. Other
        // text and line-ending conventions are retained exactly as supplied.
        static string RewriteLines(string text, Func<string, string> rewrite)
        {
            if (String.IsNullOrEmpty(text)) return text;
            string[] parts = Regex.Split(text, @"(\r\n|\r|\n|%RT)");
            var result = new System.Text.StringBuilder(); int previous = -1;
            for (int i = 0; i < parts.Length; i += 2)
            {
                string line = rewrite(parts[i]); if (line == null) continue;
                if (previous >= 0) result.Append(parts[previous + 1]);
                result.Append(line); previous = i;
            }
            return result.ToString();
        }

        internal static string EnsureTypical(string text)
        {
            // Keep an existing TYP line verbatim, including its quantity or
            // punctuation. Used for families other than standard threaded holes
            // and SHCS counterbores, whose quantity policy uses spacing only.
            if (String.IsNullOrWhiteSpace(text) || HasTypical(text)) return text;
            return text + (text.EndsWith("\r", StringComparison.Ordinal) || text.EndsWith("\n", StringComparison.Ordinal) ? "" : "\r") + "TYP(%QC)";
        }

        internal static string OmitSingleTypical(string text, int? quantity)
        {
            if (String.IsNullOrEmpty(text)) return text;
            // TYP and PLC'S EQ. SP. describe the same occurrence quantity.
            // At one occurrence omit their entire lines, including a combined
            // TYP/spacing line. Other note lines and retained endings stay as
            // written, without leaving a blank line where the quantity was.
            string[] parts = Regex.Split(text, @"(\r\n|\r|\n|%RT)");
            var result = new System.Text.StringBuilder(); int previous = -1; bool removed = false;
            for (int i = 0; i < parts.Length; i += 2)
            {
                const string count = @"(?<count>1|%QC|%\{%QC(?:/[^{}]*)?\})";
                Match typ = Regex.Match(parts[i], @"^[ \t]*TYP\.?[ \t]*\([ \t]*" + count + @"[ \t]*\).*$", RegexOptions.IgnoreCase);
                Match spacing = Regex.Match(parts[i], @"^[ \t]*(?:\([ \t]*" + count + @"[ \t]*\)[ \t]*)?PLC['’]S[ \t]+EQ\.?[ \t]*SP\.?(?:[ \t]+AS[ \t]+SHOWN)?[ \t]*\.?[ \t]*$", RegexOptions.IgnoreCase);
                if ((typ.Success && (typ.Groups["count"].Value == "1" || quantity == 1)) ||
                    (spacing.Success && (spacing.Groups["count"].Value == "1" || quantity == 1)))
                { removed = true; continue; }
                if (previous >= 0) result.Append(parts[previous + 1]);
                result.Append(parts[i]); previous = i;
            }
            return removed ? result.ToString() : text;
        }

        static bool IsPlainDrill(HoleFacts hole)
        {
            return hole.Type == 1 && String.IsNullOrWhiteSpace(hole.Thread) &&
                PositiveFinite(hole.Diameter) && (!hole.Depth.HasValue || PositiveFinite(hole.Depth)) &&
                AbsentOrZero(hole.Bore) && AbsentOrZero(hole.BoreDepth) && AbsentOrZero(hole.Sink) &&
                AbsentOrZero(hole.Angle) && AbsentOrZero(hole.ThreadDepth);
        }
        static bool PositiveFinite(double? value)
        { return value.HasValue && value.Value > 0 && !Double.IsInfinity(value.Value); }
        static bool AbsentOrZero(double? value)
        { return !value.HasValue || Math.Abs(value.Value) <= RuleBook.Epsilon; }

        static string Limits(string field, double? nominal, double min, double max)
        {
            if (!nominal.HasValue || max <= min) throw new InvalidDataException("Missing native size or approved fit limits.");
            return "%{%" + field + "/LT" + Number(max - nominal.Value) + "^" + Number(min - nominal.Value) + "}";
        }
        static string Number(double number) { return Math.Round(number, 9).ToString("0.#########", CultureInfo.InvariantCulture); }

        static bool KoenigLimits(string key, out double minimum, out double maximum)
        {
            minimum = maximum = 0;
            try { if (LinkedCallouts.SavedDiameterLimits(key, out minimum, out maximum)) return true; }
            catch (InvalidDataException) { }
            // The 12 mm library entry exists only in dual format. Its bracketed
            // inch limits are authoritative; do not accidentally treat the
            // leading metric pair as inches or derive limits from a nominal.
            string dual;
            try { dual = LinkedCallouts.ReadSaved(key + "-M"); }
            catch (InvalidDataException) { return false; }
            Match pair = Regex.Match(dual, @"%DI\s*[\d.]+/[\d.]+\[(?<upper>[\d.]+)/(?<lower>[\d.]+)\]");
            if (!pair.Success) return false;
            maximum = Double.Parse(pair.Groups["upper"].Value, CultureInfo.InvariantCulture);
            minimum = Double.Parse(pair.Groups["lower"].Value, CultureInfo.InvariantCulture);
            return maximum > minimum;
        }

        internal static string FormatFields(string text, bool dual)
        {
            // LT offsets are expressed in the primary display unit. Convert
            // offsets, not model geometry, when using metric[inch] annotations.
            // ApplyPrecision subsequently assigns primary-unit /@ overrides
            // and converts the exceptional dual limits into fixed text.
            text = Regex.Replace(text, Fields, m =>
            {
                string options = Regex.Replace(m.Groups["options"].Value, @"/(?:DU|NU)(?=/|$)|/@\d+", "", RegexOptions.IgnoreCase);
                if (dual) options = Regex.Replace(options, @"/LT(?<hi>[+\-]?[\d.]+)\^(?<lo>[+\-]?[\d.]+)", t =>
                    "/LT" + Number(Double.Parse(t.Groups["hi"].Value, CultureInfo.InvariantCulture) * 25.4) + "^" +
                    Number(Double.Parse(t.Groups["lo"].Value, CultureInfo.InvariantCulture) * 25.4));
                options = Regex.Replace(options, @"/LT(?<hi>[\d.]+)", "/LT+${hi}");
                options = Regex.Replace(options, @"\^(?<lo>[\d.]+)", "^+${lo}");
                return "%{%" + m.Groups["field"].Value + (dual ? "/DU" : "") + options + "}";
            });
            return text;
        }
    }
}
