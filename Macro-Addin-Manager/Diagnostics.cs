using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace CoopManager
{
    internal static class AppInfo
    {
        public const string Version = "1.11";
#if ADMIN
        public static readonly bool Admin = true;
#else
        public static readonly bool Admin = false;
#endif
        public static string SettingsFolder { get { return Admin ? "SEMacroManagerAdmin" : "SEMacroManager"; } }
        public static string DefaultBuilds { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),"Codex","Solid-Edge-Macros"); } }
        public const string DefaultSource = @"H:\Coop\internal\Macros and Add-ins";
        public static string SourceSetting(string saved)
        {
            // Migrate only the old shipped default; leave deliberately selected custom folders alone.
            return string.IsNullOrWhiteSpace(saved) || saved.Trim().TrimEnd('\\').Equals(@"H:\solid_edge\Coop\internal\Macros and Add-ins", StringComparison.OrdinalIgnoreCase)
                ? DefaultSource : saved;
        }
    }
    internal static class Diagnostics
    {
        static readonly object gate = new object();
        static string directory, session;
        static int part;
        static long maxBytes = 5 * 1024 * 1024;
        public static string LastError { get; private set; }
        public static string LogDirectory { get { return directory; } }
        public static string CurrentPath { get { return directory == null ? null : Path.Combine(directory, session + "-" + part.ToString("D3") + ".log"); } }

        public static void Initialize(string folder, long sizeLimit = 5 * 1024 * 1024)
        {
            lock (gate)
            {
                directory = folder; part = 0; maxBytes = sizeLimit; LastError = null;
                session = "SEManager-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff") + "-" + Guid.NewGuid().ToString("N").Substring(0,8);
                using (var process = Process.GetCurrentProcess())
                    Info("SESSION START version=" + AppInfo.Version + " process=" + process.Id + " OS=" + Environment.OSVersion + " CLR=" + Environment.Version + " x64=" + Environment.Is64BitProcess);
            }
        }
        public static void Info(string message) { Write("INFO", message, null); }
        public static void Error(string message, Exception error) { Write("ERROR", message, error); }
        public static void Write(string level, string message, Exception error)
        {
            lock (gate)
            {
                if (directory == null) return; // Unit tests may deliberately run without persistent diagnostics.
                try
                {
                    Directory.CreateDirectory(directory);
                    if (File.Exists(CurrentPath) && new FileInfo(CurrentPath).Length >= maxBytes) part++;
                    string text = DateTimeOffset.Now.ToString("o") + " [" + level + "] [thread " + System.Threading.Thread.CurrentThread.ManagedThreadId + "] " + message + Environment.NewLine;
                    if (error != null) text += error.ToString() + Environment.NewLine;
                    // Append and close each time so logs survive a crash and can be read while the app runs.
                    File.AppendAllText(CurrentPath, text, Encoding.UTF8);
                    LastError = null;
                    // Keep at most 30 bounded log parts across sessions. Never touch unrelated files.
                    foreach (var old in new DirectoryInfo(directory).GetFiles("SEManager-*.log").OrderByDescending(f => f.LastWriteTimeUtc).ThenByDescending(f => f.Name).Skip(30))
                        if (!old.FullName.Equals(CurrentPath, StringComparison.OrdinalIgnoreCase)) old.Delete();
                }
                catch (Exception ex)
                {
                    // A full/read-only log disk must not abort an installation or its rollback.
                    LastError = ex.Message; Trace.WriteLine("Diagnostic log unavailable: " + ex);
                }
            }
        }
        public static void Export(string destination)
        {
            lock (gate)
            {
                if (directory == null || !Directory.Exists(directory)) throw new IOException("No diagnostic logs are available.");
                string full = Path.GetFullPath(destination);
                if (Files.Inside(directory, full)) throw new IOException("Save the exported ZIP outside the logs folder.");
                string temporary = full + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    using (var archive = ZipFile.Open(temporary, ZipArchiveMode.Create))
                        foreach (string file in Directory.GetFiles(directory, "SEManager-*.log"))
                            archive.CreateEntryFromFile(file, Path.GetFileName(file), CompressionLevel.Optimal);
                    if (File.Exists(full)) File.Replace(temporary, full, null); else File.Move(temporary, full);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
        }
    }
}
