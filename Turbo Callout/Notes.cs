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
    }

    internal static class Notes
    {
        internal static NotePlan Build(RuleBook rules, HoleFacts hole, bool dual)
        {
            Decision choice = rules.Match(hole);
            // The table rules deliberately reject unspecified hole families.
            // Greg's annotation-only rule supplies a standard drill note for a
            // native simple hole after all special-family matching has run.
            // Accept only the shared engine's ordinary no-match result, never
            // a conflicting pin match or another review condition.
            if (choice.Slot == 0 && choice.Reason == "No verified standard-hole match. Leave unchanged." && IsPlainDrill(hole))
                choice = new Decision { Category = "Simple drilled hole", Family = "Simple drill", Slot = 1 };
            if (choice.Slot == 0) throw new InvalidOperationException(choice.Reason);
            int places = choice.IsDowel ? 4 : choice.Port == 6 || choice.Port == 8 ||
                choice.Family == "SHCS counterbore" ? 2 : 3;
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
                // Retain the saved quantity line as well as the thread fields.
                // Each unit mode has its own wording; %QC stays native so Solid
                // Edge evaluates the count from the attached hole feature.
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
            text = FormatFields(text, dual);
            // Native BalloonText uses carriage returns, not the table's %RT.
            return new NotePlan { Text = text.Replace("%RT", "\r"), Category = choice.Category, InchPlaces = places, Dual = dual };
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
            // Do not use /@ on dual fields: that overrides metric precision.
            text = Regex.Replace(text, @"%\{%(?<field>HS|HD|BS|BD|SS|TD)(?<options>[^{}]*)\}|(?<!\{)%(?<field>HS|HD|BS|BD|SS|TD)", m =>
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
