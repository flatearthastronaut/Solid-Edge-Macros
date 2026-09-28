using System;
using System.Collections.Generic;
using System.IO;

namespace SolidEdgeConvert
{
    // These small boundaries let regression tests exercise failure cleanup without
    // starting CAD or touching a user's open documents.
    internal interface IPart : IDisposable
    {
        void SaveAs(string path);
        void CloseIfOwned();
    }

    internal interface IEdgeSession : IDisposable
    {
        object StepAdapter { get; set; }
        IPart OpenPart(string path);
        void DoIdle();
    }

    internal static class Conversion
    {
        internal static string OutputPath(string source)
        {
            if (String.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Select a Solid Edge part file (.par).");
            string fullPath = Path.GetFullPath(source);
            if (!String.Equals(Path.GetExtension(fullPath), ".par", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("This version converts only Solid Edge part files (.par) to STEP (.stp).");
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("The selected part file could not be found.", fullPath);
            return Path.ChangeExtension(fullPath, ".stp");
        }

        internal static string Run(string source, bool replaceExisting,
            Func<IEdgeSession> connect, Action<string> progress)
        {
            string output = OutputPath(source);
            source = Path.GetFullPath(source);
            if (File.Exists(output) && !replaceExisting)
                throw new IOException("The STEP file already exists. Conversion was cancelled.");

            // Export to a unique sibling first. A failed translator must never
            // truncate an existing STEP file or leave a partial file under its name.
            string temporary = Path.Combine(Path.GetDirectoryName(output),
                "." + Path.GetFileNameWithoutExtension(output) + "." + Guid.NewGuid().ToString("N") + ".stp");
            try
            {
                progress("Connecting to Solid Edge...");
                using (IEdgeSession session = connect())
                {
                    object previousAdapter = session.StepAdapter;
                    IPart part = null;
                    List<Exception> failures = new List<Exception>();
                    try
                    {
                        // Batch_frm.vb enables seApplicationGlobalSTEPAdapterKey
                        // before Documents.Open / SaveAs. Restore BOTH true and
                        // false values unconditionally, including on COM failures.
                        session.StepAdapter = true;
                        progress("Opening " + Path.GetFileName(source) + "...");
                        part = session.OpenPart(source);
                        session.DoIdle();
                        progress("Converting to STEP...");
                        part.SaveAs(temporary);
                        if (!File.Exists(temporary) || new FileInfo(temporary).Length == 0)
                            throw new IOException("Solid Edge did not produce a nonempty STEP file.");
                    }
                    catch (Exception error) { failures.Add(error); }
                    finally
                    {
                        // Each cleanup operation gets its own attempt. A close
                        // failure must not prevent restoring the global translator.
                        if (part != null)
                        {
                            try { part.CloseIfOwned(); session.DoIdle(); }
                            catch (Exception error) { failures.Add(new IOException("Could not close the converted document. Check Solid Edge.", error)); }
                            finally { part.Dispose(); }
                        }
                        try { session.StepAdapter = previousAdapter; }
                        catch (Exception error) { failures.Add(new IOException("Could not restore the STEP translator setting. Check Solid Edge.", error)); }
                    }
                    if (failures.Count > 0)
                        throw new AggregateException("Conversion could not finish.", failures);
                }

                progress("Saving " + Path.GetFileName(output) + "...");
                if (replaceExisting && File.Exists(output))
                    File.Replace(temporary, output, null);
                else
                    // Move deliberately fails if another program creates the
                    // destination after our initial check; no silent overwrite.
                    File.Move(temporary, output);
                return output;
            }
            finally
            {
                // Preserve the original diagnostic if removing a failed export
                // is impossible (e.g. a disconnected network share).
                try { if (File.Exists(temporary)) File.Delete(temporary); }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
