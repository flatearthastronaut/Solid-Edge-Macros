using System;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CoopManager
{
    // This is a shared, administrator-editable configuration, not a program release.
    // Seed it from the launcher package once; later publications must not replace
    // the shop's live edits with the default list included in a newer build.
    internal static class SupportingFiles
    {
        internal const string Styles = "Hammill.DimensionStyles.txt";
        internal static bool UsesStyles(Package package)
        {
            return package.Key == "hammillmacrolauncher" &&
                ReleaseName.Compare(package.Release, ReleaseName.Parse("Launcher-v0.6", false)) >= 0;
        }

        static bool IsSeed(string relative)
        {
            string normalized = relative.Replace('/', '\\');
            return !Files.ExcludedRelativeFile(normalized) &&
                normalized.EndsWith("Shared Configuration\\" + Styles, StringComparison.OrdinalIgnoreCase) &&
                (normalized.Equals("Shared Configuration\\" + Styles, StringComparison.OrdinalIgnoreCase) ||
                 normalized.EndsWith("\\Shared Configuration\\" + Styles, StringComparison.OrdinalIgnoreCase));
        }

        static void BoundedCopy(Stream input, string destination)
        {
            // Match the launcher's bound. Reject bad packages before any shared mutation.
            using (var output = new MemoryStream())
            {
                var buffer = new byte[4096]; int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (output.Length + read > 65536) throw new IOException(Styles + " exceeds 64 KB.");
                    output.Write(buffer, 0, read);
                }
                if (output.Length == 0) throw new IOException(Styles + " is empty.");
                File.WriteAllBytes(destination, output.ToArray());
            }
        }

        internal static void StageSeed(Package package, string destination)
        {
            if (package.Kind == "ZIP")
            {
                using (var archive = ZipFile.OpenRead(package.Path))
                {
                    var matches = archive.Entries.Where(e => IsSeed(e.FullName)).ToList();
                    if (matches.Count != 1) throw new IOException("Launcher package must contain exactly one Shared Configuration\\" + Styles + ". Rebuild the launcher package.");
                    using (var input = matches[0].Open()) BoundedCopy(input, destination);
                }
            }
            else
            {
                var matches = Catalog.SourceFiles(package).Where(f => IsSeed(f.Key)).ToList();
                if (matches.Count != 1) throw new IOException("Launcher package must contain exactly one Shared Configuration\\" + Styles + ". Rebuild the launcher package.");
                using (var input = File.OpenRead(matches[0].Value)) BoundedCopy(input, destination);
            }
        }

        internal static bool MissingShared(Package package, string shared)
        { return UsesStyles(package) && !File.Exists(Files.Child(shared, Styles)); }

        internal static void StageLocal(Package package, string shared, string stage)
        {
            if (!UsesStyles(package)) return;
            string source = Files.Child(shared, Styles);
            if (!File.Exists(source)) throw new IOException("The shared " + Styles + " is missing. Publish Hammill Macro Launcher with Admin Manager v1.16 or later first.");
            // This snapshot belongs in the user's configured Local folder, not a
            // hard-coded Documents path or a numbered launcher directory. It is
            // included in the normal install journal, backup and rollback.
            Files.Copy(source, Files.Child(stage, Styles));
        }

        internal static bool LocalDiffers(Package package, string shared, string local)
        {
            if (!UsesStyles(package)) return false;
            string source = Files.Child(shared, Styles), target = Files.Child(local, Styles);
            return !File.Exists(source) || !File.Exists(target) || Files.Hash(source) != Files.Hash(target);
        }
    }
}
