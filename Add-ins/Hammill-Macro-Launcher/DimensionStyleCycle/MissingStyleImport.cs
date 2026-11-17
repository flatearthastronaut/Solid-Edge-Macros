using System;
using System.IO;
using Hammill.StyleRepair;

namespace Hammill.DimensionStyleCycle
{
    internal static class MissingStyleImport
    {
        internal const string MasterName = "normal (new design).dft";

        // Prefer an administrator-maintained master beside the shared cycle list.
        // The release also includes a clean master so installation works without
        // installing the separate repair macro. Resolve only on a missing style.
        internal static string Resolve(string configuration, string installedFolder)
        {
            string shared = Path.Combine(Path.GetDirectoryName(configuration), MasterName);
            if (File.Exists(shared)) return shared;
            string bundled = Path.Combine(installedFolder, MasterName);
            if (File.Exists(bundled)) return bundled;
            throw new FileNotFoundException("Cannot add the missing dimension style. Place " + MasterName +
                " beside Hammill.DimensionStyles.txt in the shared Available folder, or reinstall Hammill Add-ins. Searched: " + shared + " and " + bundled);
        }

        internal static void Apply(object application, object target, string name, string configuration)
        {
            string path = Resolve(configuration, Path.GetDirectoryName(typeof(MissingStyleImport).Assembly.Location));
            using (var master = new MasterDocument(application, target, path))
                // Reuse the repair macro's non-replacing native import and cleanup.
                // Only the needed name is required: unrelated missing styles in
                // the master cannot block this click. Never save either document.
                Repair.AddMissing(target, master.Document, new[] { name });
        }
    }
}
