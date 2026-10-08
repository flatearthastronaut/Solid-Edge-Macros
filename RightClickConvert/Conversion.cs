using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace SolidEdgeConvert
{
    // These values describe conversion routes, not just output extensions.
    // Part and ParasolidPart both write .par, but require different import paths;
    // Step exports native documents, while StepAssembly imports STEP into .asm.
    // A new route also needs CLI dispatch, menu/COM registration and tests.
    internal enum ConversionFormat { Step, Pdf, PdfWithDate, Part, ParasolidPart, ParasolidAssembly, StepAssembly, ParasolidExport, Stl, PdfSameType, PdfWithDateSameType }

    // These small boundaries let regression tests exercise failure cleanup without
    // starting CAD or touching a user's open documents.
    internal interface IEdgeDocument : IDisposable
    {
        // Closing a document and releasing our COM reference are separate duties:
        // a borrowed user document must stay open even when this wrapper is disposed.
        void SaveAs(string path);
        void CloseIfOwned();
    }

    internal interface IEdgeSession : IDisposable
    {
        object StepAdapter { get; set; }
        object PdfSheetOptions { get; set; }
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
                case ConversionFormat.PdfSameType: return "PDF - All sheets of same type";
                case ConversionFormat.PdfWithDateSameType: return "PDF with Date - All sheets of same type";
                case ConversionFormat.Part: return "Solid Edge Part";
                case ConversionFormat.ParasolidPart: return "Solid Edge Part";
                case ConversionFormat.ParasolidAssembly: return "Solid Edge Assembly";
                case ConversionFormat.StepAssembly: return "Solid Edge Assembly";
                case ConversionFormat.ParasolidExport: return "Parasolid";
                case ConversionFormat.Stl: return "STL";
                default: throw new ArgumentOutOfRangeException("format");
            }
        }

        internal static string OutputPath(string source, ConversionFormat format = ConversionFormat.Step, DateTime? exportDate = null)
        {
            // Shared by overwrite prompts, batch collision checks and conversion.
            // Keep all naming/extension rules here so the previewed destination
            // is exactly the path that receives the completed file. No CAD calls
            // occur here, so invalid source types fail before connecting to COM.
            string name = FormatName(format);
            bool nativeExport = format == ConversionFormat.Step || format == ConversionFormat.ParasolidExport;
            string inputExtension = nativeExport ? ".par or .asm" : ".dft";
            // STL is offered for parts only; do not broaden the assembly menus.
            if (format == ConversionFormat.Stl) inputExtension = ".par";
            bool stepImport = format == ConversionFormat.Part || format == ConversionFormat.StepAssembly;
            if (stepImport) inputExtension = ".stp or .step";
            bool parasolid = format == ConversionFormat.ParasolidPart || format == ConversionFormat.ParasolidAssembly;
            if (parasolid) inputExtension = ".x_t or .x_b";
            if (String.IsNullOrWhiteSpace(source))
                throw new ArgumentException("Select a Solid Edge " + inputExtension + " file.");
            string fullPath = Path.GetFullPath(source);
            string extension = Path.GetExtension(fullPath);
            bool supported = nativeExport
                ? String.Equals(extension, ".par", StringComparison.OrdinalIgnoreCase) || String.Equals(extension, ".asm", StringComparison.OrdinalIgnoreCase)
                : parasolid
                ? String.Equals(extension, ".x_t", StringComparison.OrdinalIgnoreCase) || String.Equals(extension, ".x_b", StringComparison.OrdinalIgnoreCase)
                : stepImport
                ? String.Equals(extension, ".stp", StringComparison.OrdinalIgnoreCase) || String.Equals(extension, ".step", StringComparison.OrdinalIgnoreCase)
                : String.Equals(extension, inputExtension, StringComparison.OrdinalIgnoreCase);
            if (!supported)
                throw new ArgumentException(name + " conversion requires a " + inputExtension + " file.");
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("The selected CAD file could not be found.", fullPath);
            if (format == ConversionFormat.PdfWithDate || format == ConversionFormat.PdfWithDateSameType)
            {
                // Use a Gregorian YYYYMMDD suffix regardless of Windows locale.
                // The caller freezes the local date once for the whole batch.
                return Path.Combine(Path.GetDirectoryName(fullPath), Path.GetFileNameWithoutExtension(fullPath)
                    + " " + (exportDate ?? DateTime.Today).ToString("yyyyMMdd", CultureInfo.InvariantCulture) + ".pdf");
            }
            return Path.ChangeExtension(fullPath, format == ConversionFormat.Step ? ".stp"
                : format == ConversionFormat.ParasolidExport ? ".x_t"
                : format == ConversionFormat.Stl ? ".stl"
                : format == ConversionFormat.Part || format == ConversionFormat.ParasolidPart ? ".par"
                : IsAssembly(format) ? ".asm" : ".pdf");
        }

        internal static bool IsPdf(ConversionFormat format)
        {
            return format == ConversionFormat.Pdf || format == ConversionFormat.PdfWithDate
                || format == ConversionFormat.PdfSameType || format == ConversionFormat.PdfWithDateSameType;
        }

        internal static bool IsAssembly(ConversionFormat format)
        {
            // Means a native assembly OUTPUT requiring persistent component files.
            // Exporting a source .asm to STEP/Parasolid does not meet this condition.
            return format == ConversionFormat.ParasolidAssembly || format == ConversionFormat.StepAssembly;
        }

        internal static string Run(string source, bool replaceExisting,
            Func<IEdgeSession> connect, Action<string> progress, ConversionFormat format = ConversionFormat.Step, DateTime? exportDate = null)
        {
            // One-file transaction: validate, translate to staging, finish CAD
            // cleanup, then publish. Callers must serialize these transactions
            // on an STA thread because Solid Edge/global translator state is shared.
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
            // Preserve the real target extension on the temporary name: Solid
            // Edge selects its export translator from that extension (.stl, etc.).
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
                    // PDF, Parasolid and STL use ordinary SaveAs with the target
                    // extension and must not read/change the STEP translator setting.
                    object previousAdapter = useStepAdapter ? session.StepAdapter : null;
                    // Capture before touching the shared PDF preference. Other
                    // formats must not even read it. Restore the exact COM value,
                    // including sheet-range/visible-sheet modes we do not offer.
                    bool usePdfOptions = IsPdf(format);
                    object previousPdfOptions = usePdfOptions ? session.PdfSheetOptions : null;
                    IEdgeDocument part = null;
                    List<Exception> failures = new List<Exception>();
                    try
                    {
                        // Batch_frm.vb enables seApplicationGlobalSTEPAdapterKey
                        // before Documents.Open / SaveAs. Restore BOTH true and
                        // false values unconditionally, including on COM failures.
                        if (useStepAdapter) session.StepAdapter = true;
                        progress("Opening " + Path.GetFileName(source) + "...");
                        // Imports create native geometry with the selected template.
                        // Native exports (including STL) reuse the ordinary open/
                        // SaveAs path; they need no import template. The wrapper's
                        // ownership flag determines whether cleanup may close it.
                        part = format == ConversionFormat.Part ? session.ImportStepPart(source)
                            : format == ConversionFormat.StepAssembly ? session.ImportStepAssembly(importCopy)
                            : format == ConversionFormat.ParasolidPart ? session.ImportParasolid(source, false)
                            : format == ConversionFormat.ParasolidAssembly ? session.ImportParasolid(importCopy, true)
                            : session.OpenDocument(source);
                        session.DoIdle();
                        progress("Converting to " + formatName + "...");
                        // Apply after opening the draft so the translator sees our
                        // explicit scope. 0 = active sheet; 1 = all of its type.
                        // Do not activate a different sheet or change any other
                        // PDF option (range, sizes, quality, colors, etc.).
                        if (usePdfOptions) session.PdfSheetOptions =
                            format == ConversionFormat.PdfSameType || format == ConversionFormat.PdfWithDateSameType ? 1 : 0;
                        part.SaveAs(temporary);
                        // A successful COM return alone does not prove a file was
                        // written. This checks basic completion; geometry fidelity
                        // belongs in live tests and production acceptance checks.
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
                        if (usePdfOptions)
                        {
                            try { session.PdfSheetOptions = previousPdfOptions; }
                            catch (Exception error) { failures.Add(new IOException("Could not restore the PDF sheet setting. Check Solid Edge.", error)); }
                        }
                    }
                    if (failures.Count > 0)
                        throw new AggregateException("Conversion could not finish.", failures);
                }

                // Do not publish if translation, closing or STEP-state restoration
                // failed. File.Replace protects a previous destination without a
                // delete-then-copy gap; do not fall back to deleting it on failure.
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
