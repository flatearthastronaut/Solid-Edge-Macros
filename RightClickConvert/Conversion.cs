using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SolidEdgeConvert
{
    internal enum ConversionFormat { Step, Pdf, PdfWithDate, Part, ParasolidPart, ParasolidAssembly, StepAssembly, ParasolidExport }

    // These small boundaries let regression tests exercise failure cleanup without
    // starting CAD or touching a user's open documents.
    internal interface IEdgeDocument : IDisposable
    {
        void SaveAs(string path);
        void CloseIfOwned();
    }

    internal interface IEdgeSession : IDisposable
    {
        object StepAdapter { get; set; }
        IEdgeDocument OpenDocument(string path);
        IEdgeDocument ImportStepPart(string path);
        IEdgeDocument ImportStepAssembly(string path);
        IEdgeDocument ImportParasolid(string path, bool assembly);
        void DoIdle();
    }

    internal static class Conversion
    {
        internal static string FormatName(ConversionFormat format)
        {
            switch (format)
            {
                case ConversionFormat.Step: return "STEP";
                case ConversionFormat.Pdf: return "PDF";
                case ConversionFormat.PdfWithDate: return "PDF with Date";
                case ConversionFormat.Part: return "Solid Edge Part";
                case ConversionFormat.ParasolidPart: return "Solid Edge Part";
                case ConversionFormat.ParasolidAssembly: return "Solid Edge Assembly";
                case ConversionFormat.StepAssembly: return "Solid Edge Assembly";
                case ConversionFormat.ParasolidExport: return "Parasolid";
                default: throw new ArgumentOutOfRangeException("format");
            }
        }

        internal static string OutputPath(string source, ConversionFormat format = ConversionFormat.Step, DateTime? exportDate = null)
        {
            string name = FormatName(format);
            string inputExtension = format == ConversionFormat.Step || format == ConversionFormat.ParasolidExport ? ".par" : ".dft";
            bool stepImport = format == ConversionFormat.Part || format == ConversionFormat.StepAssembly;
            if (stepImport) inputExtension = ".stp or .step";
            bool parasolid = format == ConversionFormat.ParasolidPart || format == ConversionFormat.ParasolidAssembly;
            if (parasolid) inputExtension = ".x_t or .x_b";
            if (String.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Select a Solid Edge " + inputExtension + " file.");
            string fullPath = Path.GetFullPath(source);
            string extension = Path.GetExtension(fullPath);
            bool supported = parasolid
                ? String.Equals(extension, ".x_t", StringComparison.OrdinalIgnoreCase) || String.Equals(extension, ".x_b", StringComparison.OrdinalIgnoreCase)
                : stepImport
                ? String.Equals(extension, ".stp", StringComparison.OrdinalIgnoreCase) || String.Equals(extension, ".step", StringComparison.OrdinalIgnoreCase)
                : String.Equals(extension, inputExtension, StringComparison.OrdinalIgnoreCase);
            if (!supported)
                throw new ArgumentException(name + " conversion requires a " + inputExtension + " file.");
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("The selected CAD file could not be found.", fullPath);
            if (format == ConversionFormat.PdfWithDate)
            {
                // Use a Gregorian YYYYMMDD suffix regardless of Windows locale.
                // The caller freezes the local date once for the whole batch.
                return Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath)
                    + " " + (exportDate ?? DateTime.Today).ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".pdf");
            }
            return Path.ChangeExtension(fullPath, format == ConversionFormat.Step ? ".stp"
                : format == ConversionFormat.ParasolidExport ? ".x_t"
                : format == ConversionFormat.Part || format == ConversionFormat.ParasolidPart ? ".par"
                : IsAssembly(format) ? ".asm" : ".pdf");
        }

        internal static bool IsAssembly(ConversionFormat format)
        {
            return format == ConversionFormat.ParasolidAssembly || format == ConversionFormat.StepAssembly;
        }

        internal static string Run(string source, bool replaceExisting,
            Func<IEdgeSession> connect, Action<string> progress, ConversionFormat format = ConversionFormat.Step, DateTime? exportDate = null)
        {
            string output = OutputPath(source, format, exportDate);
            string formatName = FormatName(format);
            bool useStepAdapter = format == ConversionFormat.Step || format == ConversionFormat.Part || format == ConversionFormat.StepAssembly;
            source = Path.GetFullPath(source);
            if (File.Exists(output) && !replaceExisting)
                throw new IOException("The " + formatName + " file already exists. Conversion was cancelled.");

            // Export to a unique sibling first. A failed translator must never
            // truncate an existing export or leave a partial file under its name.
            string temporary = Path.Combine(Path.GetDirectoryName(output),
                "." + Path.GetFileNameWithoutExtension(output) + "." + Guid.NewGuid().ToString("N") + Path.GetExtension(output));
            string componentFolder = null, importCopy = null;
            try
            {
                if (IsAssembly(format))
                {
                    // Solid Edge writes component files beside the imported source.
                    // Import a copy in a unique, permanent folder so neither a failed
                    // import nor an approved replacement can overwrite existing parts.
                    // Keep this folder in place: the saved assembly references it.
                    componentFolder = Path.Combine(Path.GetDirectoryName(output),
                        Path.GetFileNameWithoutExtension(output) + " Components " + Guid.NewGuid().ToString("N"));
                    Directory.CreateDirectory(componentFolder);
                    // Assembly SaveAs also writes auxiliary files such as .cfg.
                    // Stage inside the owned folder to keep those beside components.
                    temporary = Path.Combine(componentFolder, Path.GetFileName(temporary));
                    importCopy = Path.Combine(componentFolder, Path.GetFileName(source));
                    File.Copy(source, importCopy, false);
                }
                progress("Connecting to Solid Edge...");
                using (IEdgeSession session = connect())
                {
                    // PDF follows Batch's ordinary SaveAs(.pdf) path. It must
                    // not depend on, read, or change the STEP translator setting.
                    object previousAdapter = useStepAdapter ? session.StepAdapter : null;
                    IEdgeDocument part = null;
                    List<Exception> failures = new List<Exception>();
                    try
                    {
                        // Batch_frm.vb enables seApplicationGlobalSTEPAdapterKey
                        // before Documents.Open / SaveAs. Restore BOTH true and
                        // false values unconditionally, including on COM failures.
                        if (useStepAdapter) session.StepAdapter = true;
                        progress("Opening " + Path.GetFileName(source) + "...");
                        part = format == ConversionFormat.Part ? session.ImportStepPart(source)
                            : format == ConversionFormat.StepAssembly ? session.ImportStepAssembly(importCopy)
                            : format == ConversionFormat.ParasolidPart ? session.ImportParasolid(source, false)
                            : format == ConversionFormat.ParasolidAssembly ? session.ImportParasolid(importCopy, true)
                            : session.OpenDocument(source);
                        session.DoIdle();
                        progress("Converting to " + formatName + "...");
                        part.SaveAs(temporary);
                        if (!File.Exists(temporary) || new FileInfo(temporary).Length == 0)
                            throw new IOException("Solid Edge did not produce a nonempty " + formatName + " file.");
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
                        if (useStepAdapter)
                        {
                            try { session.StepAdapter = previousAdapter; }
                            catch (Exception error) { failures.Add(new IOException("Could not restore the STEP translator setting. Check Solid Edge.", error)); }
                        }
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
                // Delete only our copied input and an empty folder. Retain any
                // generated components, even on failure, because an open document
                // may reference them if CAD cleanup failed. Never recurse/delete
                // another conversion's folder or the components of an older output.
                try
                {
                    if (importCopy != null && File.Exists(importCopy)) File.Delete(importCopy);
                    if (componentFolder != null && Directory.Exists(componentFolder)
                        && Directory.GetFileSystemEntries(componentFolder).Length == 0) Directory.Delete(componentFolder);
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
        }
    }
}
