using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DraftDimensionStyles
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
        internal int Places;
        internal bool Dual;
        internal bool Vertical;
        internal string Key(bool dual)
        {
            return Places.ToString(CultureInfo.InvariantCulture) + ":" + dual + ":" + Vertical;
        }
        internal static StyleName Parse(string name)
        {
            if (name == null) return null;
            Match match = Pattern.Match(name);
            int places;
            if (!match.Success || !Int32.TryParse(match.Groups[1].Value, out places)) return null;
            return new StyleName { Places = places, Dual = match.Groups[2].Success, Vertical = match.Groups[3].Success };
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
