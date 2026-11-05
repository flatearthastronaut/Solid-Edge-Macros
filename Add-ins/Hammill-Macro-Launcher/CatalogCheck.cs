using System;
using System.IO;

namespace Hammill.MacroLauncher
{
    // Read-only diagnostic. Passing explicit paths supports tests without reading a user's settings.
    internal static class CatalogCheck
    {
        private static int Main(string[] args)
        {
            try
            {
                string settings = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Coop", "SEMacroManager", "settings.xml");
                string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                for (int i = 0; i < args.Length; i++)
                {
                    if (i + 1 == args.Length) throw new ArgumentException("Expected --settings PATH or --documents PATH.");
                    if (args[i] == "--settings") settings = args[++i];
                    else if (args[i] == "--documents") documents = args[++i];
                    else throw new ArgumentException("Unknown option: " + args[i]);
                }
                var result = MacroCatalog.Scan(settings, documents);
                Console.WriteLine("Macro folder: " + result.Folder);
                foreach (var macro in result.Entries) Console.WriteLine(macro.Name + " | " + macro.Environments + " | " + macro.Path);
                foreach (string notice in result.Notices) Console.WriteLine("NOTICE: " + notice);
                return result.Notices.Count == 0 ? 0 : 2;
            }
            catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        }
    }
}
