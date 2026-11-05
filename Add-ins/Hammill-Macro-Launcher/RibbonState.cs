using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;

namespace Hammill.MacroLauncher
{
    internal sealed class RibbonState
    {
        public int Version;
        public string Signature;
        // Deleted macros retain their IDs. A different executable must never inherit an old shortcut.
        public readonly Dictionary<string, int> Ids = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        public static RibbonState Update(string path, MacroCatalog catalog)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            // Two SE instances may start together. Wait briefly for the other instance instead of
            // failing its startup scan; keep the named lock specific to this user's state file.
            string name = @"Local\HammillMacroLauncher-" + MacroCatalog.Hash(Encoding.UTF8.GetBytes(Path.GetFullPath(path).ToLowerInvariant()));
            using (var mutex = new Mutex(false, name))
            {
                bool acquired = false;
                try
                {
                    try { acquired = mutex.WaitOne(5000); } catch (AbandonedMutexException) { acquired = true; }
                    if (!acquired) throw new IOException("Another Solid Edge instance is updating the macro ribbon. Restart this instance after it finishes.");
                    return UpdateLocked(path, catalog);
                }
                finally { if (acquired) mutex.ReleaseMutex(); }
            }
        }

        private static RibbonState UpdateLocked(string path, MacroCatalog catalog)
        {
            using (var gate = new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
            {
                var state = new RibbonState();
                if (File.Exists(path))
                {
                    var root = MacroCatalog.ReadXml(path).Root;
                    if (root == null || root.Name != "ribbon") throw new InvalidDataException("Invalid saved ribbon state.");
                    state.Version = (int)root.Attribute("version"); state.Signature = (string)root.Attribute("signature");
                    foreach (var item in root.Elements("command"))
                    {
                        string key = (string)item.Attribute("key"); int id = (int)item.Attribute("id");
                        if (String.IsNullOrWhiteSpace(key) || id < 100 || state.Ids.ContainsKey(key) || state.Ids.ContainsValue(id)) throw new InvalidDataException("Invalid saved command IDs.");
                        state.Ids.Add(key, id);
                    }
                    if (state.Version < 1) throw new InvalidDataException("Invalid GUI version.");
                }
                int next = state.Ids.Count == 0 ? 100 : checked(state.Ids.Values.Max() + 1);
                foreach (var entry in catalog.Entries.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase))
                {
                    if (!state.Ids.ContainsKey(entry.Key)) { state.Ids.Add(entry.Key, next); next = checked(next + 1); }
                    entry.CommandId = state.Ids[entry.Key];
                }
                string signature = catalog.RibbonSignature();
                if (signature != state.Signature) state.Version = checked(state.Version + 1);
                state.Signature = signature;
                var document = new XDocument(new XElement("ribbon", new XAttribute("version", state.Version), new XAttribute("signature", signature),
                    state.Ids.OrderBy(x => x.Key).Select(x => new XElement("command", new XAttribute("key", x.Key), new XAttribute("id", x.Value)))));
                string temp = path + ".tmp"; document.Save(temp);
                if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
                return state;
            }
        }
    }
}
