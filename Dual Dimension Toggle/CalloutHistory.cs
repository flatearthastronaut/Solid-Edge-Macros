using System;
using System.Text;

namespace DualDimensionToggle
{
    internal sealed class CalloutHistory
    {
        internal readonly string[] Inch = new string[4], Dual = new string[4];
        internal string Encode()
        {
            string result = "1";
            for (int i = 0; i < 4; i++) result += "." + Pack(Inch[i]) + "." + Pack(Dual[i]);
            return result;
        }
        private static string Pack(string value) { return value == null ? "~" : Convert.ToBase64String(Encoding.UTF8.GetBytes(value)); }
        private static string Unpack(string value) { return value == "~" ? null : Encoding.UTF8.GetString(Convert.FromBase64String(value)); }
        internal static CalloutHistory Decode(string text)
        {
            if (text == null) return null;
            string[] parts = text.Split('.');
            if (parts.Length != 9 || parts[0] != "1") throw new InvalidOperationException("Unrecognized saved callout conversion record.");
            CalloutHistory result = new CalloutHistory();
            for (int i = 0; i < 4; i++) { result.Inch[i] = Unpack(parts[1 + 2 * i]); result.Dual[i] = Unpack(parts[2 + 2 * i]); }
            return result;
        }
    }

    internal static class CalloutHistoryStore
    {
        private const string SetName = "DualDimensionToggleCalloutV1", AttributeName = "TextPairs";

        // Persist on the annotation itself so a saved/reopened draft can reverse
        // only the pairs this macro converted. No document paths, sidecar files,
        // session globals or guesses about unbracketed numbers are needed.
        internal static string Read(object note)
        {
            object sets = null, set = null, attribute = null;
            try
            {
                sets = ((dynamic)note).AttributeSets;
                set = Find(sets, SetName, true);
                if (set == null) return null;
                attribute = Find(set, AttributeName, false);
                return attribute == null ? null : (string)((dynamic)attribute).Value;
            }
            finally { Com.Release(ref attribute); Com.Release(ref set); Com.Release(ref sets); }
        }
        internal static void Write(object note, string value)
        {
            object sets = null, set = null, attribute = null;
            try
            {
                sets = ((dynamic)note).AttributeSets;
                set = Find(sets, SetName, true);
                if (set == null && value == null) return;
                if (set == null) set = ((dynamic)sets).Add(SetName);
                attribute = Find(set, AttributeName, false);
                if (value == null)
                {
                    Com.Release(ref attribute);
                    // The set is owned by this macro. Preserve unrelated entries
                    // if another version has added any alongside TextPairs.
                    object existing = null;
                    try { existing = Find(set, AttributeName, false); if (existing != null) ((dynamic)set).Remove(AttributeName); }
                    finally { Com.Release(ref existing); }
                    if ((int)((dynamic)set).Count == 0) ((dynamic)sets).Remove(SetName);
                }
                else
                {
                    // SDK AttributeTypeConstants.seStringUnicode = 64.
                    if (attribute == null) attribute = ((dynamic)set).Add(AttributeName, 64);
                    ((dynamic)attribute).Value = value;
                }
            }
            finally { Com.Release(ref attribute); Com.Release(ref set); Com.Release(ref sets); }
        }
        private static object Find(object collection, string name, bool sets)
        {
            int count = (int)((dynamic)collection).Count;
            for (int i = 1; i <= count; i++)
            {
                object candidate = ((dynamic)collection).Item(i);
                try
                {
                    string found = sets ? (string)((dynamic)candidate).SetName : (string)((dynamic)candidate).Name;
                    if (found == name) { object result = candidate; candidate = null; return result; }
                }
                finally { Com.Release(ref candidate); }
            }
            return null;
        }
    }
}
