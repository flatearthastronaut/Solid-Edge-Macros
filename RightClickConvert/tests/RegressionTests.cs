using System;
using System.Collections.Generic;
using System.IO;
using SolidEdgeConvert;

internal static class RegressionTests
{
    private static string folder;
    private static string source;
    private static string output;
    private static int passed;

    [STAThread]
    private static int Main()
    {
        folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        source = Path.Combine(folder, "Part & bracket 100% é.PAR");
        output = Path.ChangeExtension(source, ".stp");
        File.WriteAllText(source, "source must never change");
        try
        {
            Test("uppercase extension, spaces, Unicode and same-folder output", delegate {
                Equal(output, Conversion.OutputPath(source));
            });
            Test("draft and missing files rejected before connecting", delegate {
                ExpectFailure(delegate { Conversion.OutputPath(Path.ChangeExtension(source, ".dft")); });
                ExpectFailure(delegate { Conversion.OutputPath(Path.Combine(folder, "missing.par")); });
            });
            Test("successful export restores disabled STEP adapter and closes owned part", delegate {
                FakeSession s = new FakeSession(false);
                Run(s, false);
                Equal("STEP data", File.ReadAllText(output));
                Equal(false, s.Adapter);
                Equal("get,set:True,open,idle,save,close,idle,part-dispose,set:False,session-dispose", String.Join(",", s.Events));
            });
            Test("successful export restores enabled adapter", delegate {
                FakeSession s = new FakeSession(true);
                Run(s, true);
                Equal(true, s.Adapter);
            });
            Test("overwrite refusal never connects and retains old export", delegate {
                File.WriteAllText(output, "old STEP");
                FakeSession s = new FakeSession(false);
                ExpectFailure(delegate { Run(s, false); });
                Equal(0, s.Events.Count);
                Equal("old STEP", File.ReadAllText(output));
            });
            Test("failed SaveAs preserves previous STEP and restores state", delegate {
                FakeSession s = new FakeSession(false) { FailSave = true };
                ExpectFailure(delegate { Run(s, true); });
                Equal("old STEP", File.ReadAllText(output));
                Equal(false, s.Adapter);
                Check(s.PartClosed && s.PartDisposed && s.Disposed, "Cleanup incomplete");
            });
            Test("failed Open restores adapter and releases session", delegate {
                FakeSession s = new FakeSession(false) { FailOpen = true };
                ExpectFailure(delegate { Run(s, true); });
                Equal(false, s.Adapter);
                Check(s.Disposed, "Session not disposed");
            });
            Test("failure while enabling adapter still attempts restoration", delegate {
                FakeSession s = new FakeSession(false) { FailEnable = true };
                ExpectFailure(delegate { Run(s, true); });
                Equal(false, s.Adapter);
                Check(s.Disposed, "Session not disposed");
            });
            Test("close failure still restores translator and preserves old output", delegate {
                FakeSession s = new FakeSession(false) { FailClose = true };
                ExpectFailure(delegate { Run(s, true); });
                Equal(false, s.Adapter);
                Equal("old STEP", File.ReadAllText(output));
                Check(s.PartDisposed && s.Disposed, "References retained");
            });
            Test("restoration failure is reported, not success", delegate {
                FakeSession s = new FakeSession(false) { FailRestore = true };
                ExpectFailure(delegate { Run(s, true); });
                Equal("old STEP", File.ReadAllText(output));
                Check(s.Disposed, "Session not disposed");
            });
            Test("missing and empty translator output are rejected", delegate {
                ExpectFailure(delegate { Run(new FakeSession(false) { MissingOutput = true }, true); });
                ExpectFailure(delegate { Run(new FakeSession(false) { EmptyOutput = true }, true); });
                Equal("old STEP", File.ReadAllText(output));
            });
            Test("concurrent destination creation is not overwritten", delegate {
                File.Delete(output);
                FakeSession s = new FakeSession(false) { OnSave = delegate { File.WriteAllText(output, "other writer"); } };
                ExpectFailure(delegate { Run(s, false); });
                Equal("other writer", File.ReadAllText(output));
            });
            Test("borrowed document is released but never closed", delegate {
                FakeDocument document = new FakeDocument();
                using (IEdgeDocument part = new SolidEdgeDocument(document, false)) { part.SaveAs("unused"); part.CloseIfOwned(); }
                Check(document.Saved && !document.Closed, "Borrowed document was closed");
            });
            Test("owned document closes without saving", delegate {
                FakeDocument document = new FakeDocument();
                using (IEdgeDocument part = new SolidEdgeDocument(document, true)) { part.CloseIfOwned(); }
                Check(document.Closed && !document.SaveOnClose, "Source might be saved");
            });
            Test("open-document lookup reuses a saved part", delegate {
                FakeDocument document = new FakeDocument { FullName = source };
                FakeDocuments documents = new FakeDocuments { Existing = document };
                using (IEdgeDocument part = SolidEdgeSession.OpenDocument(documents, source)) { part.CloseIfOwned(); }
                Check(!documents.OpenCalled && !document.Closed, "Existing part was reopened or closed");
            });
            Test("unsaved edits are rejected without closing or reopening", delegate {
                FakeDocument document = new FakeDocument { FullName = source, Dirty = true };
                FakeDocuments documents = new FakeDocuments { Existing = document };
                ExpectFailure(delegate { SolidEdgeSession.OpenDocument(documents, source); });
                Check(!documents.OpenCalled && !document.Closed, "Dirty document was touched");
            });
            Test("a document opened by the converter is owned", delegate {
                FakeDocument document = new FakeDocument { FullName = source };
                FakeDocuments documents = new FakeDocuments { OpenResult = document };
                using (IEdgeDocument part = SolidEdgeSession.OpenDocument(documents, source)) { part.CloseIfOwned(); }
                Check(documents.OpenCalled && document.Closed && !document.SaveOnClose, "Ownership incorrect");
            });
            Test("redirected and null opens are rejected", delegate {
                FakeDocument other = new FakeDocument { FullName = Path.Combine(folder, "different.par") };
                ExpectFailure(delegate { SolidEdgeSession.OpenDocument(new FakeDocuments { OpenResult = other }, source); });
                Check(!other.Closed && !other.Saved, "Unrelated document was changed");
                ExpectFailure(delegate { SolidEdgeSession.OpenDocument(new FakeDocuments(), source); });
            });
            Test("menu command quotes executable and Explorer argument", delegate {
                Equal("\"C:\\Tools & CAD\\SolidEdgeConvert.exe\" --step \"%1\"", ShellMenu.Command(@"C:\Tools & CAD\SolidEdgeConvert.exe"));
                ExpectFailure(delegate { ShellMenu.Command("relative.exe"); });
                ExpectFailure(delegate { ShellMenu.Command("C:\\bad\"name.exe"); });
            });
            Test("all exports preserve source and remove staging files", delegate {
                Equal("source must never change", File.ReadAllText(source));
                Equal(0, Directory.GetFiles(folder, ".*.stp").Length);
            });
            string draft = Path.Combine(folder, "Drawing & sheet 100% é.DFT");
            string pdf = Path.ChangeExtension(draft, ".pdf");
            File.WriteAllText(draft, "draft must never change");
            Test("PDF accepts uppercase draft extension and preserves full base name", delegate {
                Equal(pdf, Conversion.OutputPath(draft, ConversionFormat.Pdf));
            });
            Test("format and source extension must match", delegate {
                ExpectFailure(delegate { Conversion.OutputPath(source, ConversionFormat.Pdf); });
                ExpectFailure(delegate { Conversion.OutputPath(draft, ConversionFormat.Step); });
                ExpectFailure(delegate { Conversion.OutputPath(Path.Combine(folder, "missing.dft"), ConversionFormat.Pdf); });
                ExpectFailure(delegate { Conversion.OutputPath(draft, (ConversionFormat)99); });
            });
            Test("PDF export uses .pdf SaveAs and never accesses the STEP setting", delegate {
                FakeSession s = new FakeSession(false) { RejectStepAccess = true };
                Conversion.Run(draft, false, delegate { return s; }, delegate { }, ConversionFormat.Pdf);
                Equal(".pdf", s.SavedExtension);
                Equal("PDF data", File.ReadAllText(pdf));
                Equal("open,idle,save,close,idle,part-dispose,session-dispose", String.Join(",", s.Events));
            });
            Test("PDF replacement refusal leaves existing output and never connects", delegate {
                File.WriteAllText(pdf, "old PDF");
                FakeSession s = new FakeSession(false) { RejectStepAccess = true };
                ExpectFailure(delegate { Conversion.Run(draft, false, delegate { return s; }, delegate { }, ConversionFormat.Pdf); });
                Equal(0, s.Events.Count);
                Equal("old PDF", File.ReadAllText(pdf));
            });
            Test("failed PDF SaveAs preserves old output and closes the owned draft", delegate {
                FakeSession s = new FakeSession(false) { RejectStepAccess = true, FailSave = true };
                ExpectFailure(delegate { Conversion.Run(draft, true, delegate { return s; }, delegate { }, ConversionFormat.Pdf); });
                Equal("old PDF", File.ReadAllText(pdf));
                Check(s.PartClosed && s.PartDisposed && s.Disposed, "Draft cleanup incomplete");
            });
            Test("PDF open and empty-output failures preserve previous PDF", delegate {
                foreach (FakeSession s in new[] {
                    new FakeSession(false) { RejectStepAccess = true, FailOpen = true },
                    new FakeSession(false) { RejectStepAccess = true, EmptyOutput = true },
                    new FakeSession(false) { RejectStepAccess = true, MissingOutput = true },
                    new FakeSession(false) { RejectStepAccess = true, FailClose = true } })
                {
                    ExpectFailure(delegate { Conversion.Run(draft, true, delegate { return s; }, delegate { }, ConversionFormat.Pdf); });
                    Equal("old PDF", File.ReadAllText(pdf));
                    Check(s.Disposed, "PDF session not disposed");
                }
            });
            Test("approved PDF replacement publishes the completed export", delegate {
                FakeSession s = new FakeSession(false) { RejectStepAccess = true };
                Conversion.Run(draft, true, delegate { return s; }, delegate { }, ConversionFormat.Pdf);
                Equal("PDF data", File.ReadAllText(pdf));
            });
            Test("saved open drafts stay open and dirty drafts are rejected", delegate {
                FakeDocument document = new FakeDocument { FullName = draft };
                FakeDocuments documents = new FakeDocuments { Existing = document };
                using (IEdgeDocument borrowed = SolidEdgeSession.OpenDocument(documents, draft)) { borrowed.CloseIfOwned(); }
                document.Dirty = true;
                ExpectFailure(delegate { SolidEdgeSession.OpenDocument(documents, draft); });
                Check(!documents.OpenCalled && !document.Closed && !document.Saved, "Existing draft was modified");
            });
            Test("PDF menu command quotes executable and selected draft", delegate {
                Equal("\"C:\\Tools & CAD\\SolidEdgeConvert.exe\" --pdf \"%1\"", ShellMenu.Command(@"C:\Tools & CAD\SolidEdgeConvert.exe", ConversionFormat.Pdf));
            });
            Test("PDF preserves source and removes temporary exports", delegate {
                Equal("draft must never change", File.ReadAllText(draft));
                Equal(0, Directory.GetFiles(folder, ".*.pdf").Length);
            });
            Test("batch converts multiple parts once each and reports progress", delegate {
                string second = Path.Combine(folder, "Second part.par");
                File.WriteAllText(second, "second source");
                int connected = 0, totalSeen = 0;
                List<BatchItem> results = BatchConversion.Run(new[] { source, second, source.ToUpperInvariant() }, ConversionFormat.Step, ExistingOutput.Replace,
                    delegate { connected++; return new FakeSession(false); }, delegate(int n, int total, string message) { totalSeen = total; });
                Equal(2, results.Count); Equal(2, connected); Equal(2, totalSeen);
                Check(results.TrueForAll(x => x.Error == null && !x.Skipped), "Batch did not complete");
            });
            Test("batch continues after a failed file and records its error", delegate {
                string good = Path.Combine(folder, "Good draft.dft");
                File.WriteAllText(good, "good draft");
                int connected = 0;
                List<BatchItem> results = BatchConversion.Run(new[] { draft, good }, ConversionFormat.Pdf, ExistingOutput.Replace,
                    delegate { connected++; return new FakeSession(false) { FailSave = connected == 1, RejectStepAccess = true }; }, delegate { });
                Check(results[0].Error != null && results[1].Error == null, "Failure discarded subsequent files");
                Equal("PDF data", File.ReadAllText(Path.ChangeExtension(good, ".pdf")));
            });
            Test("batch skip policy preserves all existing outputs without connecting", delegate {
                string originalPdf = File.ReadAllText(pdf);
                List<BatchItem> results = BatchConversion.Run(new[] { draft }, ConversionFormat.Pdf, ExistingOutput.Skip,
                    delegate { throw new Exception("Skipped file must not connect"); }, delegate { });
                Check(results[0].Skipped && results[0].Error == null, "Existing output not skipped");
                Equal(originalPdf, File.ReadAllText(pdf));
            });
            Test("batch reports missing and wrong-extension files but converts valid entries", delegate {
                List<BatchItem> results = BatchConversion.Run(new[] { source, Path.Combine(folder, "missing.dft"), draft }, ConversionFormat.Pdf, ExistingOutput.Replace,
                    delegate { return new FakeSession(false) { RejectStepAccess = true }; }, delegate { });
                Check(results[0].Error != null && results[1].Error != null && results[2].Error == null, "Per-file validation failed");
            });
            Test("empty batch rejected and distinct paths survive beyond 100 files", delegate {
                ExpectFailure(delegate { BatchConversion.UniquePaths(new string[0]); });
                List<string> many = new List<string>();
                for (int i = 0; i < 150; i++) many.Add(Path.Combine(folder, "Part " + i + ".par"));
                Equal(150, BatchConversion.UniquePaths(many).Length);
            });
            Test("real Windows Shell selection preserves every path and Unicode", delegate {
                IShellItemArray selection = ShellSelection.Create(new[] { source, draft });
                try
                {
                    string[] paths = ShellSelection.Read(selection);
                    Equal(2, paths.Length); Equal(source, paths[0]); Equal(draft, paths[1]);
                }
                finally { System.Runtime.InteropServices.Marshal.ReleaseComObject(selection); }
            });
            DateTime exportDate = new DateTime(2031, 2, 3);
            string datedPdf = Path.Combine(folder, "Drawing & sheet 100% é 20310203.pdf");
            Test("dated PDF appends exactly one space and an eight-digit date", delegate {
                Equal(datedPdf, Conversion.OutputPath(draft, ConversionFormat.PdfWithDate, exportDate));
                Equal(pdf, Conversion.OutputPath(draft, ConversionFormat.Pdf, exportDate));
                ExpectFailure(delegate { Conversion.OutputPath(source, ConversionFormat.PdfWithDate, exportDate); });
            });
            Test("dated PDF uses Gregorian date regardless of system culture", delegate {
                System.Globalization.CultureInfo previous = System.Threading.Thread.CurrentThread.CurrentCulture;
                try {
                    System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("ar-SA");
                    Equal(datedPdf, Conversion.OutputPath(draft, ConversionFormat.PdfWithDate, exportDate));
                }
                finally { System.Threading.Thread.CurrentThread.CurrentCulture = previous; }
            });
            Test("dated PDF uses local today by default", delegate {
                DateTime today = DateTime.Today;
                Equal(Path.Combine(folder, "Drawing & sheet 100% é " + today.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture) + ".pdf"),
                    Conversion.OutputPath(draft, ConversionFormat.PdfWithDate));
            });
            Test("dated PDF exports as PDF and leaves undated PDF unchanged", delegate {
                File.WriteAllText(pdf, "undated output");
                FakeSession session = new FakeSession(false) { RejectStepAccess = true };
                string result = Conversion.Run(draft, false, delegate { return session; }, delegate { }, ConversionFormat.PdfWithDate, exportDate);
                Equal(datedPdf, result); Equal(".pdf", session.SavedExtension);
                Equal("PDF data", File.ReadAllText(datedPdf)); Equal("undated output", File.ReadAllText(pdf));
            });
            Test("dated batch freezes date for outputs, skips, replacements and results", delegate {
                File.WriteAllText(datedPdf, "existing dated output");
                List<BatchItem> skipped = BatchConversion.Run(new[] { draft }, ConversionFormat.PdfWithDate, ExistingOutput.Skip,
                    delegate { throw new Exception("Existing dated PDF must be skipped"); }, delegate { }, exportDate);
                Check(skipped[0].Skipped && skipped[0].Error == null, "Dated conflict check failed");
                Equal(datedPdf, skipped[0].Output); Equal("existing dated output", File.ReadAllText(datedPdf));
                List<BatchItem> replaced = BatchConversion.Run(new[] { draft, Path.Combine(folder, "Good draft.dft") }, ConversionFormat.PdfWithDate, ExistingOutput.Replace,
                    delegate { return new FakeSession(false) { RejectStepAccess = true }; }, delegate { }, exportDate);
                Check(replaced.TrueForAll(x => x.Error == null && x.Output.EndsWith(" 20310203.pdf")), "Batch dates drifted");
                Equal("PDF data", File.ReadAllText(datedPdf));
            });
            Test("dated PDF failure protects existing export and source", delegate {
                File.WriteAllText(datedPdf, "keep dated PDF");
                ExpectFailure(delegate { Conversion.Run(draft, true, delegate { return new FakeSession(false) { FailSave = true, RejectStepAccess = true }; }, delegate { }, ConversionFormat.PdfWithDate, exportDate); });
                Equal("keep dated PDF", File.ReadAllText(datedPdf));
                Equal("draft must never change", File.ReadAllText(draft));
                Equal(0, Directory.GetFiles(folder, ".*.pdf").Length);
            });
            Test("dated menu quotes paths and selects the dated PDF command", delegate {
                Equal("\"C:\\Tools & CAD\\SolidEdgeConvert.exe\" --pdf-date \"%1\"", ShellMenu.Command(@"C:\Tools & CAD\SolidEdgeConvert.exe", ConversionFormat.PdfWithDate));
            });
            string stepSource = Path.Combine(folder, "Imported.part & é.STP");
            string longStepSource = Path.Combine(folder, "Imported.part & é.STEP");
            string nativePart = Path.ChangeExtension(stepSource, ".par");
            File.WriteAllText(stepSource, "STEP source");
            File.WriteAllText(longStepSource, "STEP source");
            Test("both STEP extensions map to a native part and reject other formats", delegate {
                Equal(nativePart, Conversion.OutputPath(stepSource, ConversionFormat.Part));
                Equal(nativePart, Conversion.OutputPath(longStepSource, ConversionFormat.Part));
                ExpectFailure(delegate { Conversion.OutputPath(draft, ConversionFormat.Part); });
                ExpectFailure(delegate { Conversion.OutputPath(source, ConversionFormat.Part); });
            });
            Test("STEP import uses normal.par and owns only the new part", delegate {
                FakeDocument existing = new FakeDocument { Dirty = true };
                FakeDocument imported = new FakeDocument();
                FakeDocuments documents = new FakeDocuments { Existing = existing, OpenResult = imported };
                using (IEdgeDocument part = SolidEdgeSession.ImportStepPart(documents, stepSource)) { part.CloseIfOwned(); }
                Equal("normal.par", documents.Template); Equal(stepSource, documents.ImportPath);
                Check(!documents.OpenCalled && imported.Closed && !imported.SaveOnClose && !existing.Closed, "Import ownership incorrect");
            });
            Test("already-open STEP import is rejected without saving or closing it", delegate {
                FakeDocument existing = new FakeDocument { Dirty = true };
                ExpectFailure(delegate { SolidEdgeSession.ImportStepPart(new FakeDocuments { Existing = existing, OpenResult = existing }, stepSource); });
                Check(!existing.Closed && !existing.Saved, "Existing document touched");
            });
            Test("invalid template result and null import are rejected safely", delegate {
                FakeDocument assembly = new FakeDocument { Type = 3 };
                ExpectFailure(delegate { SolidEdgeSession.ImportStepPart(new FakeDocuments { OpenResult = assembly }, stepSource); });
                Check(assembly.Closed && !assembly.SaveOnClose && !assembly.Saved, "Rejected import not closed safely");
                ExpectFailure(delegate { SolidEdgeSession.ImportStepPart(new FakeDocuments(), stepSource); });
            });
            Test("STEP import saves native part and restores either translator state", delegate {
                foreach (bool enabled in new[] { false, true }) {
                    FakeSession s = new FakeSession(enabled);
                    Conversion.Run(stepSource, true, delegate { return s; }, delegate { }, ConversionFormat.Part);
                    Equal(".par", s.SavedExtension); Equal("PAR data", File.ReadAllText(nativePart));
                    Equal(enabled, s.Adapter);
                    Check(s.Events.Contains("import") && !s.Events.Contains("open"), "Wrong document opening route");
                    Check(s.PartClosed && s.PartDisposed && s.Disposed, "Import cleanup incomplete");
                }
            });
            Test("failed STEP import or native save preserves prior output and translator", delegate {
                File.WriteAllText(nativePart, "original native part");
                foreach (FakeSession s in new[] { new FakeSession(false) { FailOpen = true }, new FakeSession(false) { FailSave = true }, new FakeSession(false) { EmptyOutput = true } }) {
                    ExpectFailure(delegate { Conversion.Run(stepSource, true, delegate { return s; }, delegate { }, ConversionFormat.Part); });
                    Equal("original native part", File.ReadAllText(nativePart)); Equal(false, s.Adapter);
                    Check(s.Disposed, "Import session retained");
                }
                Equal("STEP source", File.ReadAllText(stepSource)); Equal(0, Directory.GetFiles(folder, ".*.par").Length);
            });
            Test("STEP source collisions cannot overwrite another selection's result", delegate {
                int connected = 0;
                List<BatchItem> results = BatchConversion.Run(new[] { stepSource, longStepSource }, ConversionFormat.Part, ExistingOutput.Replace,
                    delegate { connected++; return new FakeSession(false); }, delegate { });
                Equal(1, connected); Check(results[0].Error == null && results[1].Error != null, "Colliding destinations were accepted");
                Equal("PAR data", File.ReadAllText(nativePart)); Equal("STEP source", File.ReadAllText(longStepSource));
            });
            Test("part menu command quotes selected STEP path", delegate {
                Equal("\"C:\\Tools & CAD\\SolidEdgeConvert.exe\" --part \"%1\"", ShellMenu.Command(@"C:\Tools & CAD\SolidEdgeConvert.exe", ConversionFormat.Part));
            });
            string xt = Path.Combine(folder, "Parasolid & é.X_T");
            string xb = Path.Combine(folder, "Parasolid & é.X_B");
            File.WriteAllText(xt, "text Parasolid"); File.WriteAllText(xb, "binary Parasolid");
            Test("both Parasolid extensions support both native output types", delegate {
                foreach (string input in new[] { xt, xb }) {
                    Equal(Path.ChangeExtension(input, ".par"), Conversion.OutputPath(input, ConversionFormat.ParasolidPart));
                    Equal(Path.ChangeExtension(input, ".asm"), Conversion.OutputPath(input, ConversionFormat.ParasolidAssembly));
                }
                ExpectFailure(delegate { Conversion.OutputPath(stepSource, ConversionFormat.ParasolidPart); });
                ExpectFailure(delegate { Conversion.OutputPath(draft, ConversionFormat.ParasolidAssembly); });
                ExpectFailure(delegate { Conversion.OutputPath(xt, ConversionFormat.Part); });
            });
            Test("Parasolid imports use matching templates and reject wrong native types", delegate {
                foreach (bool assembly in new[] { false, true }) {
                    FakeDocument imported = new FakeDocument { Type = assembly ? 3 : 1 };
                    FakeDocuments documents = new FakeDocuments { OpenResult = imported };
                    string template = assembly ? "normal.asm" : "normal.par";
                    using (IEdgeDocument result = SolidEdgeSession.ImportWithTemplate(documents, xt, template, assembly ? 3 : 1)) { result.CloseIfOwned(); }
                    Equal(template, documents.Template); Check(imported.Closed && !imported.SaveOnClose, "Import left open");
                    FakeDocument wrong = new FakeDocument { Type = assembly ? 1 : 3 };
                    ExpectFailure(delegate { SolidEdgeSession.ImportWithTemplate(new FakeDocuments { OpenResult = wrong }, xb, template, assembly ? 3 : 1); });
                    Check(wrong.Closed && !wrong.Saved, "Wrong type not cleaned up");
                }
            });
            Test("Parasolid conversion saves both formats without accessing STEP settings", delegate {
                foreach (string input in new[] { xt, xb }) foreach (bool assembly in new[] { false, true }) {
                    FakeSession s = new FakeSession(false) { RejectStepAccess = true };
                    ConversionFormat format = assembly ? ConversionFormat.ParasolidAssembly : ConversionFormat.ParasolidPart;
                    string result = Conversion.Run(input, true, delegate { return s; }, delegate { }, format);
                    Equal(assembly ? "ASM data" : "PAR data", File.ReadAllText(result));
                    Check(s.Events.Contains(assembly ? "import-assembly" : "import-parasolid-part"), "Wrong import route");
                    Check(s.PartClosed && s.PartDisposed && s.Disposed, "Parasolid cleanup incomplete");
                    if (assembly) Check(!File.Exists(s.ImportPath) && !Directory.Exists(Path.GetDirectoryName(s.ImportPath)), "Unused copy/folder retained");
                    else Equal(input, s.ImportPath);
                }
            });
            Test("assembly components stay isolated and survive final publication and replacement", delegate {
                string firstFolder = null;
                for (int attempt = 0; attempt < 2; attempt++) {
                    FakeSession s = new FakeSession(false) { RejectStepAccess = true, GenerateComponent = true };
                    Conversion.Run(xt, true, delegate { return s; }, delegate { }, ConversionFormat.ParasolidAssembly);
                    string components = Path.GetDirectoryName(s.ImportPath);
                    Check(components != folder && File.Exists(Path.Combine(components, "component.par")), "Components lost or unisolated");
                    Check(!File.Exists(s.ImportPath), "Input copy retained");
                    if (firstFolder == null) firstFolder = components;
                    else { Check(components != firstFolder, "Replacement reused old components"); Check(File.Exists(Path.Combine(firstFolder, "component.par")), "Old components deleted"); }
                }
            });
            Test("failed Parasolid save preserves existing native output and original input", delegate {
                foreach (ConversionFormat format in new[] { ConversionFormat.ParasolidPart, ConversionFormat.ParasolidAssembly }) {
                    string result = Conversion.OutputPath(xb, format); File.WriteAllText(result, "existing native file");
                    FakeSession s = new FakeSession(false) { RejectStepAccess = true, FailSave = true };
                    ExpectFailure(delegate { Conversion.Run(xb, true, delegate { return s; }, delegate { }, format); });
                    Equal("existing native file", File.ReadAllText(result)); Check(s.PartClosed && s.Disposed, "Failed import not cleaned up");
                }
                Equal("text Parasolid", File.ReadAllText(xt)); Equal("binary Parasolid", File.ReadAllText(xb));
            });
            Test("mixed Parasolid batch prevents same-basename output collisions", delegate {
                foreach (ConversionFormat format in new[] { ConversionFormat.ParasolidPart, ConversionFormat.ParasolidAssembly }) {
                    List<BatchItem> results = BatchConversion.Run(new[] { xt, xb }, format, ExistingOutput.Replace,
                        delegate { return new FakeSession(false) { RejectStepAccess = true }; }, delegate { });
                    Check(results[0].Error == null && results[1].Error != null, "Colliding Parasolid destinations accepted");
                }
            });
            Test("Parasolid menu commands select the requested output type", delegate {
                Equal("\"C:\\Tools & CAD\\SolidEdgeConvert.exe\" --parasolid-part \"%1\"", ShellMenu.Command(@"C:\Tools & CAD\SolidEdgeConvert.exe", ConversionFormat.ParasolidPart));
                Equal("\"C:\\Tools & CAD\\SolidEdgeConvert.exe\" --parasolid-assembly \"%1\"", ShellMenu.Command(@"C:\Tools & CAD\SolidEdgeConvert.exe", ConversionFormat.ParasolidAssembly));
            });
            Test("STEP assembly accepts both extensions and rejects other source formats", delegate {
                Equal(Path.ChangeExtension(stepSource, ".asm"), Conversion.OutputPath(stepSource, ConversionFormat.StepAssembly));
                Equal(Path.ChangeExtension(longStepSource, ".asm"), Conversion.OutputPath(longStepSource, ConversionFormat.StepAssembly));
                ExpectFailure(delegate { Conversion.OutputPath(xt, ConversionFormat.StepAssembly); });
                ExpectFailure(delegate { Conversion.OutputPath(draft, ConversionFormat.StepAssembly); });
            });
            Test("STEP assembly uses normal.asm and protects already-open documents", delegate {
                FakeDocument imported = new FakeDocument { Type = 3 };
                FakeDocuments documents = new FakeDocuments { OpenResult = imported };
                using (IEdgeDocument result = SolidEdgeSession.ImportWithTemplate(documents, stepSource, "normal.asm", 3)) { result.CloseIfOwned(); }
                Equal("normal.asm", documents.Template); Check(imported.Closed && !imported.SaveOnClose, "Assembly ownership incorrect");
                FakeDocument existing = new FakeDocument { Type = 3, Dirty = true };
                ExpectFailure(delegate { SolidEdgeSession.ImportWithTemplate(new FakeDocuments { Existing = existing, OpenResult = existing }, stepSource, "normal.asm", 3); });
                Check(!existing.Saved && !existing.Closed, "Existing assembly touched");
            });
            Test("STEP assembly isolates components and restores enabled or disabled adapter", delegate {
                string previousFolder = null;
                foreach (bool enabled in new[] { false, true }) {
                    FakeSession s = new FakeSession(enabled) { GenerateComponent = true };
                    string result = Conversion.Run(stepSource, true, delegate { return s; }, delegate { }, ConversionFormat.StepAssembly);
                    Equal("ASM data", File.ReadAllText(result)); Equal(enabled, s.Adapter);
                    Check(s.Events.Contains("import-step-assembly") && !s.Events.Contains("open"), "Wrong opening route");
                    Check(s.PartClosed && s.PartDisposed && s.Disposed, "STEP assembly cleanup incomplete");
                    string components = Path.GetDirectoryName(s.ImportPath);
                    Check(components != folder && File.Exists(Path.Combine(components, "component.par")) && !File.Exists(s.ImportPath), "Isolated components/copy incorrect");
                    if (previousFolder != null) Check(components != previousFolder && File.Exists(Path.Combine(previousFolder, "component.par")), "Replacement damaged earlier components");
                    previousFolder = components;
                }
            });
            Test("failed STEP assembly retains old output and restores translator", delegate {
                string result = Conversion.OutputPath(longStepSource, ConversionFormat.StepAssembly);
                File.WriteAllText(result, "existing assembly");
                foreach (FakeSession s in new[] { new FakeSession(false) { FailOpen = true }, new FakeSession(false) { FailSave = true, GenerateComponent = true }, new FakeSession(false) { FailClose = true }, new FakeSession(false) { EmptyOutput = true } }) {
                    ExpectFailure(delegate { Conversion.Run(longStepSource, true, delegate { return s; }, delegate { }, ConversionFormat.StepAssembly); });
                    Equal("existing assembly", File.ReadAllText(result)); Equal(false, s.Adapter);
                    Check(s.Disposed && !File.Exists(s.ImportPath), "Failed STEP import leaked session/input copy");
                    if (s.GenerateComponent) Check(File.Exists(Path.Combine(Path.GetDirectoryName(s.ImportPath), "component.par")), "Recovery component removed");
                }
                Equal("STEP source", File.ReadAllText(longStepSource));
            });
            Test("STEP assembly skip and same-name collision avoid a second import", delegate {
                List<BatchItem> skipped = BatchConversion.Run(new[] { stepSource }, ConversionFormat.StepAssembly, ExistingOutput.Skip,
                    delegate { throw new Exception("Skipped assembly must not connect"); }, delegate { });
                Check(skipped[0].Skipped && skipped[0].Error == null, "Assembly skip failed");
                int connected = 0;
                List<BatchItem> results = BatchConversion.Run(new[] { stepSource, longStepSource }, ConversionFormat.StepAssembly, ExistingOutput.Replace,
                    delegate { connected++; return new FakeSession(false); }, delegate { });
                Equal(1, connected); Check(results[0].Error == null && results[1].Error != null, "STEP assembly collision accepted");
            });
            Test("STEP assembly menu quotes the selected file", delegate {
                Equal("\"C:\\Tools & CAD\\SolidEdgeConvert.exe\" --step-assembly \"%1\"", ShellMenu.Command(@"C:\Tools & CAD\SolidEdgeConvert.exe", ConversionFormat.StepAssembly));
            });
            Console.WriteLine("Passed " + passed + " regression tests.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            // This folder is freshly created by this process under tests/work.
            // Never recurse through a supplied path or remove arbitrary CAD data.
            Directory.Delete(folder, true);
        }
    }

    private static void Run(FakeSession session, bool replace)
    {
        Conversion.Run(source, replace, delegate { return session; }, delegate { });
    }
    private static void Test(string name, Action action) { action(); passed++; Console.WriteLine("PASS " + name); }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static void Equal(object expected, object actual) { Check(Object.Equals(expected, actual), "Expected: " + expected + "; actual: " + actual); }
    private static void ExpectFailure(Action action)
    {
        try { action(); }
        catch { return; }
        throw new Exception("Expected operation to fail.");
    }

    private sealed class FakeSession : IEdgeSession
    {
        internal object Adapter;
        internal bool FailSave, FailOpen, FailClose, FailEnable, FailRestore, MissingOutput, EmptyOutput;
        internal bool PartClosed, PartDisposed, Disposed;
        internal bool RejectStepAccess;
        internal string SavedExtension;
        internal string ImportPath;
        internal bool GenerateComponent;
        internal Action OnSave;
        internal List<string> Events = new List<string>();
        internal FakeSession(bool adapter) { Adapter = adapter; }
        public object StepAdapter
        {
            get { if (RejectStepAccess) throw new Exception("PDF must not read STEP settings"); Events.Add("get"); return Adapter; }
            set
            {
                if (RejectStepAccess) throw new Exception("PDF must not write STEP settings");
                Events.Add("set:" + value);
                Adapter = value;
                if ((bool)value && FailEnable) throw new IOException("Enable failed");
                if (!(bool)value && FailRestore) throw new IOException("Restore failed");
            }
        }
        public IEdgeDocument OpenDocument(string path)
        {
            Events.Add("open");
            if (FailOpen) throw new IOException("Open failed");
            return new FakePart(this);
        }
        public void DoIdle() { Events.Add("idle"); }
        public IEdgeDocument ImportParasolid(string path, bool assembly)
        {
            ImportPath = path;
            Events.Add(assembly ? "import-assembly" : "import-parasolid-part");
            if (FailOpen) throw new IOException("Parasolid import failed");
            if (GenerateComponent) File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), "component.par"), "component");
            return new FakePart(this);
        }
        public IEdgeDocument ImportStepPart(string path)
        {
            Events.Add("import");
            if (FailOpen) throw new IOException("Import or template failed");
            return new FakePart(this);
        }
        public IEdgeDocument ImportStepAssembly(string path)
        {
            ImportPath = path;
            Events.Add("import-step-assembly");
            if (FailOpen) throw new IOException("STEP assembly import failed");
            if (GenerateComponent) File.WriteAllText(Path.Combine(Path.GetDirectoryName(path), "component.par"), "component");
            return new FakePart(this);
        }
        public void Dispose() { Events.Add("session-dispose"); Disposed = true; }
        private sealed class FakePart : IEdgeDocument
        {
            private FakeSession owner;
            internal FakePart(FakeSession owner) { this.owner = owner; }
            public void SaveAs(string path)
            {
                owner.Events.Add("save");
                owner.SavedExtension = Path.GetExtension(path);
                if (!owner.MissingOutput) File.WriteAllText(path, owner.EmptyOutput ? "" : (owner.SavedExtension == ".pdf" ? "PDF data" : owner.SavedExtension == ".par" ? "PAR data" : owner.SavedExtension == ".asm" ? "ASM data" : "STEP data"));
                if (owner.OnSave != null) owner.OnSave();
                if (owner.FailSave) throw new IOException("Save failed after partial write");
            }
            public void CloseIfOwned()
            {
                owner.Events.Add("close"); owner.PartClosed = true;
                if (owner.FailClose) throw new IOException("Close failed");
            }
            public void Dispose() { owner.Events.Add("part-dispose"); owner.PartDisposed = true; }
        }
    }
}

// Public for the runtime binder, just like COM's IDispatch surface.
public sealed class FakeDocument
{
    public int Type = 1;
    public bool Saved, Closed, SaveOnClose;
    public string FullName { get; set; }
    public bool Dirty { get; set; }
    public void SaveAs(string path) { Saved = true; }
    public void Close(bool save) { Closed = true; SaveOnClose = save; }
}

public sealed class FakeDocuments
{
    public FakeDocument Existing, OpenResult;
    public bool OpenCalled;
    public string Template, ImportPath;
    public int Count { get { return Existing == null ? 0 : 1; } }
    public object Item(int index) { return Existing; }
    public object Open(string path) { OpenCalled = true; return OpenResult; }
    public object OpenWithTemplate(string path, string template) { ImportPath = path; Template = template; return OpenResult; }
}
