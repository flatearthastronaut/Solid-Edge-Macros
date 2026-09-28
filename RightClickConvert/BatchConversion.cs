using System;
using System.Collections.Generic;
using System.IO;

namespace SolidEdgeConvert
{
    internal enum ExistingOutput { Skip, Replace }
    internal sealed class BatchItem
    {
        internal string Source, Output, Error;
        internal bool Skipped;
    }

    internal static class BatchConversion
    {
        internal static string[] UniquePaths(IEnumerable<string> paths)
        {
            List<string> result = new List<string>();
            HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string path in paths)
            {
                string full = Path.GetFullPath(path);
                if (seen.Add(full)) result.Add(full);
            }
            if (result.Count == 0) throw new ArgumentException("Select at least one CAD file.");
            return result.ToArray();
        }

        // One STA worker invokes every conversion in order. Each existing
        // single-file transaction retains its own cleanup/translator restoration.
        // A bad file is recorded without discarding the rest of the selection.
        internal static List<BatchItem> Run(string[] sources, ConversionFormat format, ExistingOutput existing,
            Func<IEdgeSession> connect, Action<int, int, string> progress, DateTime? exportDate = null)
        {
            sources = UniquePaths(sources);
            // One date for conflict checks, export paths, and the final results,
            // even if a long conversion or overwrite prompt crosses midnight.
            DateTime batchDate = exportDate ?? DateTime.Today;
            List<BatchItem> results = new List<BatchItem>();
            for (int index = 0; index < sources.Length; index++)
            {
                BatchItem item = new BatchItem { Source = sources[index] };
                results.Add(item);
                try
                {
                    item.Output = Conversion.OutputPath(item.Source, format, batchDate);
                    // Check immediately before each conversion, not only when
                    // the batch starts, so newly created outputs are protected.
                    if (File.Exists(item.Output) && existing == ExistingOutput.Skip)
                    {
                        item.Skipped = true;
                        progress(index + 1, sources.Length, "Skipped existing " + Path.GetFileName(item.Output));
                        continue;
                    }
                    int number = index + 1;
                    Conversion.Run(item.Source, existing == ExistingOutput.Replace, connect,
                        delegate(string text) { progress(number, sources.Length, text); }, format, batchDate);
                }
                catch (Exception error) { item.Error = ErrorText(error); }
            }
            return results;
        }

        private static string ErrorText(Exception error)
        {
            AggregateException aggregate = error as AggregateException;
            if (aggregate == null) return error.Message;
            List<string> messages = new List<string>();
            foreach (Exception inner in aggregate.Flatten().InnerExceptions) messages.Add(inner.Message);
            return String.Join("; ", messages);
        }
    }
}
