using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Hammill.DimensionStyleCycle
{
    // Configuration is the user's ordered list, not a guess based on dimension
    // geometry. A vertical dimension can use a horizontal-text style and vice versa.
    internal sealed class StyleSequence
    {
        private readonly Dictionary<string, List<string>> groups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] Families = { "Inch", "Inch-Metric", "Vertical Inch", "Vertical Inch-Metric" };

        public static StyleSequence Load(string path)
        {
            if (!File.Exists(path)) throw new InvalidOperationException("The dimension-style list is missing: " + path);
            if (new FileInfo(path).Length > 65536) throw new InvalidDataException("Hammill.DimensionStyles.txt is too large.");
            return Parse(File.ReadAllText(path));
        }

        internal static StyleSequence Parse(string text)
        {
            var result = new StyleSequence();
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            List<string> current = null;
            int lineNumber = 0;
            using (var reader = new StringReader(text))
            {
                string raw;
                while ((raw = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;
                    if (line.StartsWith("["))
                    {
                        if (!line.EndsWith("]")) throw Invalid(lineNumber, "Incomplete section heading.");
                        string family = line.Substring(1, line.Length - 2).Trim();
                        if (!Families.Contains(family, StringComparer.OrdinalIgnoreCase)) throw Invalid(lineNumber, "Unknown family: " + family);
                        if (result.groups.ContainsKey(family)) throw Invalid(lineNumber, "Duplicate family: " + family);
                        result.groups.Add(family, current = new List<string>());
                    }
                    else
                    {
                        if (current == null) throw Invalid(lineNumber, "Put a family heading before its style names.");
                        if (line.Any(Char.IsControl) || !used.Add(line)) throw Invalid(lineNumber, "Invalid or duplicate style: " + line);
                        current.Add(line);
                    }
                }
            }
            if (result.groups.Count == 0) throw new InvalidDataException("Hammill.DimensionStyles.txt does not contain a style family.");
            return result;
        }

        private static Exception Invalid(int line, string message)
        { return new InvalidDataException("Hammill.DimensionStyles.txt, line " + line + ": " + message); }

        public string Next(string source)
        {
            foreach (var group in groups)
            {
                int position = group.Value.FindIndex(s => String.Equals(s, source.Trim(), StringComparison.OrdinalIgnoreCase));
                if (position < 0) continue;
                if (group.Value.Count < 2) throw new InvalidOperationException("Add at least two styles to [" + group.Key + "] in Hammill.DimensionStyles.txt.");
                return group.Value[(position + 1) % group.Value.Count];
            }
            throw new InvalidOperationException("The selected style, '" + source + "', is not listed in Hammill.DimensionStyles.txt. Add it to the appropriate family first.");
        }
    }
}
