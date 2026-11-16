using System;
using System.IO;
using System.Text;

namespace Hammill.DimensionStyleCycle
{
    internal sealed class StyleSequenceFile
    {
        private readonly Func<string> resolvePath;
        private StyleSequence cached;
        private string cachedText;
        public string Path { get { return resolvePath(); } }
        public StyleSequenceFile(string path) : this(() => path) { }
        internal StyleSequenceFile(Func<string> path) { resolvePath = path; }

        public StyleSequence Get()
        {
            string path = Path, text;
            try { text = ReadText(path); }
            catch (InvalidDataException error) { throw new InvalidDataException("Invalid shared dimension-style list:\r\n" + path + "\r\n" + error.Message, error); }
            catch (IOException error)
            {
                throw new IOException("Cannot read the shared dimension-style list:\r\n" + path +
                    "\r\nCheck Macro Manager's Available folder and the shared file, then try again. " + error.Message, error);
            }
            catch (UnauthorizedAccessException error)
            {
                throw new UnauthorizedAccessException("Read access is required for the shared dimension-style list:\r\n" + path, error);
            }
            // One small read per click detects same-length edits even when
            // timestamps are retained or network metadata is cached. Reuse the
            // parse for identical contents, never a stale/local fallback list.
            if (cached != null && String.Equals(text, cachedText, StringComparison.Ordinal)) return cached;
            StyleSequence parsed;
            try { parsed = StyleSequence.Parse(text); }
            catch (InvalidDataException error) { throw new InvalidDataException("Invalid shared dimension-style list:\r\n" + path + "\r\n" + error.Message, error); }
            cached = parsed; cachedText = text;
            return cached;
        }

        private static string ReadText(string path)
        {
            // Lock out in-place writers during this bounded read. Share Delete
            // lets editors atomically replace the file; an open handle sees one
            // complete version, never a mixture. Nothing writes to the share.
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
            {
                const int maximum = 65536;
                if (stream.Length > maximum) throw new InvalidDataException("Hammill.DimensionStyles.txt is too large (maximum 64 KB).");
                var bytes = new byte[maximum + 1]; int count = 0, read;
                while (count < bytes.Length && (read = stream.Read(bytes, count, bytes.Length - count)) != 0) count += read;
                if (count > maximum) throw new InvalidDataException("Hammill.DimensionStyles.txt is too large (maximum 64 KB).");
                using (var memory = new MemoryStream(bytes, 0, count))
                using (var reader = new StreamReader(memory, Encoding.UTF8, true)) return reader.ReadToEnd();
            }
        }
    }
}
