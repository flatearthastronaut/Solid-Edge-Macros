using System;
using System.Text;
using System.Text.RegularExpressions;

namespace DualDimensionToggle
{
    internal static class CalloutFields
    {
        // Single length references documented by Siemens: hole size/depth,
        // counterbore size/depth, countersink size, thread depth, bend radius.
        // Exclude smart depth, complete callouts, thread designations, quantities,
        // angles and arbitrary property text: those can resolve to non-lengths.
        private static readonly Regex LengthField = new Regex(
            @"\A(?<code>%(?:HS|HD|BS|BD|SS|TD|BR))(?<options>/[^{}%|]*)?\z",
            RegexOptions.CultureInvariant);
        private static readonly Regex DualOption = new Regex(@"/DU(?=/|\z)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        internal static string Convert(string source, bool toDual)
        {
            if (String.IsNullOrEmpty(source)) return source;
            StringBuilder result = new StringBuilder(source.Length);
            for (int index = 0; index < source.Length;)
            {
                if (source[index] != '%' || index + 1 == source.Length)
                { result.Append(source[index++]); continue; }
                if (source[index + 1] == '{')
                {
                    // Treat a balanced expression as one token. Never rewrite a
                    // nested field or code inside an unrelated property expression.
                    int start = index, depth = 1;
                    index += 2;
                    while (index < source.Length && depth != 0)
                    {
                        if (source[index] == '{') depth++;
                        else if (source[index] == '}') depth--;
                        index++;
                    }
                    if (depth != 0) throw new FormatException("Unclosed callout property-text expression.");
                    string token = source.Substring(start, index - start);
                    Match match = LengthField.Match(token.Substring(2, token.Length - 3));
                    if (!match.Success) { result.Append(token); continue; }
                    string code = match.Groups["code"].Value;
                    string options = match.Groups["options"].Value;
                    bool hasDual = DualOption.IsMatch(options);
                    if (toDual)
                        result.Append(hasDual ? token : "%{" + code + "/DU" + options + "}");
                    else if (hasDual)
                    {
                        // Remove only the exact DU option; retain round-off and
                        // tolerance options, and their wrapper when still needed.
                        options = DualOption.Replace(options, "");
                        result.Append(options.Length == 0 ? code : "%{" + code + options + "}");
                    }
                    else result.Append(token);
                }
                else if (index + 2 < source.Length && Char.IsLetter(source[index + 1]) && Char.IsLetter(source[index + 2]))
                {
                    // Solid Edge's simple codes are exactly three characters,
                    // so adjacent words (e.g. %BDDEEP) do not form a longer code.
                    string token = source.Substring(index, 3);
                    result.Append(toDual && LengthField.IsMatch(token) ? "%{" + token + "/DU}" : token);
                    index += 3;
                }
                else { result.Append(source[index++]); }
            }
            return result.ToString();
        }

        internal static bool SameValues(string left, string right)
        {
            // v1.5 history may still contain /DU on its inch snapshot. Compare
            // using a common unit format, tolerating only this new transformation
            // while retaining the existing protection against edited literal text.
            return String.Equals(Convert(left, false), Convert(right, false), StringComparison.Ordinal);
        }
    }
}
