using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DualDimensionToggle
{
    internal enum MatchStatus { Convert, AlreadyTarget, Unrecognized, Missing, Ambiguous }

    internal sealed class StyleName
    {
        // The leading integer is a sorting group, not a unit or precision.
        // Anchor the entire name so custom/fraction styles cannot be mistaken
        // for one of the user's decimal styles. Preserve vertical orientation.
        private static readonly Regex Pattern = new Regex(
            @"^\s*\d+\s+(\d+)\s+place(?:\s+(m\s*\[\s*i\s*\]))?(\s+\(vert\))?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        // The draft uses Vert- 2 PLC and Vert - 2PLC m[i]. Treat the
        // optional hyphen and its spacing as naming variations, preserving the
        // precision and the catalog's exact target spelling. Still require a
        // separator after Vert to avoid accepting unrelated custom names.
        private static readonly Regex VertPattern = new Regex(
            @"^\s*Vert(?:\s*-\s*|\s+)(\d+)\s*PLC(?:\s+(m\s*\[\s*i\s*\]))?\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        internal int Places;
        internal bool Dual;
        internal bool Vertical;
        private bool vertPrefix;
        internal string Key(bool dual)
        {
            // Keep the two vertical naming families separate when a draft has
            // both. Vert 3PLC must pair with Vert 3 PLC, not 3 3 place (vert).
            return Places.ToString(CultureInfo.InvariantCulture) + ":" + dual + ":" + Vertical + ":" + vertPrefix;
        }
        internal static StyleName Parse(string name)
        {
            if (name == null) return null;
            Match match = Pattern.Match(name);
            bool prefix = !match.Success;
            if (prefix) match = VertPattern.Match(name);
            int places;
            if (!match.Success || !Int32.TryParse(match.Groups[1].Value, out places)) return null;
            return new StyleName { Places = places, Dual = match.Groups[2].Success,
                Vertical = prefix || match.Groups[3].Success, vertPrefix = prefix };
        }
    }

    internal sealed class StyleMap
    {
        private readonly Dictionary<string, List<string>> names = new Dictionary<string, List<string>>();
        internal StyleMap(IEnumerable<string> available)
        {
            foreach (string name in available)
            {
                StyleName parsed = StyleName.Parse(name);
                if (parsed == null) continue;
                string key = parsed.Key(parsed.Dual);
                List<string> matches;
                if (!names.TryGetValue(key, out matches)) names.Add(key, matches = new List<string>());
                if (!matches.Contains(name)) matches.Add(name);
            }
        }
        internal MatchStatus Resolve(string source, bool toDual, out string target)
        {
            target = null;
            StyleName parsed = StyleName.Parse(source);
            if (parsed == null) return MatchStatus.Unrecognized;
            if (parsed.Dual == toDual) return MatchStatus.AlreadyTarget;
            List<string> matches;
            if (!names.TryGetValue(parsed.Key(toDual), out matches)) return MatchStatus.Missing;
            if (matches.Count != 1) return MatchStatus.Ambiguous;
            target = matches[0]; // Use the document's exact spelling and grouping number.
            return MatchStatus.Convert;
        }
    }
}
