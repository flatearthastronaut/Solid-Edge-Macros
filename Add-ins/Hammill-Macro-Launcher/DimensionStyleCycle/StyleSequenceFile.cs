using System;
using System.IO;

namespace Hammill.DimensionStyleCycle
{
    internal sealed class StyleSequenceFile
    {
        public readonly string Path;
        private StyleSequence cached;
        private DateTime lastWrite;
        private long length;
        public StyleSequenceFile(string path) { Path = path; }

        public void Initialize(string defaults)
        {
            // User preferences live at the Macro Manager folder root, outside
            // versioned add-in packages. Upgrades must never replace their edits.
            if (File.Exists(Path)) return;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            try { File.Copy(defaults, Path, false); }
            catch (IOException) { if (!File.Exists(Path)) throw; }
        }

        public StyleSequence Get()
        {
            // One cheap metadata check per click; parse only when the local text
            // file changes. No CAD-object caches, timers, processes or polling.
            var info = new FileInfo(Path);
            if (!info.Exists) throw new FileNotFoundException("The dimension-style list is missing: " + Path);
            if (cached != null && info.LastWriteTimeUtc == lastWrite && info.Length == length) return cached;
            var parsed = StyleSequence.Load(Path);
            DateTime stamp = info.LastWriteTimeUtc; long size = info.Length;
            info.Refresh();
            if (info.LastWriteTimeUtc != stamp || info.Length != size)
                throw new IOException("The style list changed while being read. Finish saving it, then click again.");
            cached = parsed; lastWrite = stamp; length = size;
            return cached;
        }
    }
}
