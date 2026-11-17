using System;
using System.Collections.Generic;
using Hammill.Automation;

namespace Hammill.DimensionStyleCycle
{
    // Keep only names and numeric indexes, never document/style COM references.
    // SE's DimensionStyles.Item supports numeric indexes reliably. Validate the
    // indexed name on every click so switching documents or renaming/reordering
    // styles cannot apply a stale index from another document.
    internal sealed class StyleIndex
    {
        private readonly Dictionary<string, List<int>> indexes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        private int count = -1;

        internal object Find(object collection, string requested)
        {
            dynamic styles = collection;
            int currentCount = (int)styles.Count;
            bool rebuilt = false;
            if (count != currentCount) { Rebuild(collection, currentCount); rebuilt = true; }
            for (int attempt = 0; attempt < 2; attempt++)
            {
                List<int> matches;
                if (indexes.TryGetValue(requested.Trim(), out matches) && matches.Count == 1)
                {
                    object result = styles.Item(matches[0]);
                    try
                    {
                        if (String.Equals(((string)((dynamic)result).Name).Trim(), requested.Trim(), StringComparison.OrdinalIgnoreCase))
                        { object owned = result; result = null; return owned; }
                    }
                    finally { ComScope.Release(result); }
                }
                if (rebuilt) break;
                Rebuild(collection, currentCount); rebuilt = true;
            }
            if (!indexes.ContainsKey(requested.Trim())) throw new MissingStyleException(requested);
            throw new InvalidOperationException("The next style, '" + requested + "', has duplicate names in this draft. The dimension was not changed.");
        }

        internal void Invalidate() { count = -1; }

        private void Rebuild(object collection, int currentCount)
        {
            indexes.Clear(); count = -1;
            for (int i = 1; i <= currentCount; i++)
            {
                using (var scope = new ComScope())
                {
                    dynamic style = scope.Own(((dynamic)collection).Item(i));
                    string name = ((string)style.Name).Trim();
                    List<int> matches;
                    if (!indexes.TryGetValue(name, out matches)) indexes.Add(name, matches = new List<int>());
                    matches.Add(i);
                }
            }
            count = currentCount;
        }
    }

    internal sealed class MissingStyleException : InvalidOperationException
    {
        internal MissingStyleException(string name) : base("The next style, '" + name + "', is missing from this draft.") { }
    }
}
