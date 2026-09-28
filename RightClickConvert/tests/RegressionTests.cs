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
            Console.WriteLine("Passed " + passed + " regression tests.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            // This folder is freshly created by this process under tests/work.
            // Never recurse through a supplied path or remove arbitrary CAD data.
            foreach (string file in Directory.GetFiles(folder)) File.Delete(file);
            Directory.Delete(folder);
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
        public void Dispose() { Events.Add("session-dispose"); Disposed = true; }
        private sealed class FakePart : IEdgeDocument
        {
            private FakeSession owner;
            internal FakePart(FakeSession owner) { this.owner = owner; }
            public void SaveAs(string path)
            {
                owner.Events.Add("save");
                owner.SavedExtension = Path.GetExtension(path);
                if (!owner.MissingOutput) File.WriteAllText(path, owner.EmptyOutput ? "" : (owner.SavedExtension == ".pdf" ? "PDF data" : "STEP data"));
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
    public int Count { get { return Existing == null ? 0 : 1; } }
    public object Item(int index) { return Existing; }
    public object Open(string path) { OpenCalled = true; return OpenResult; }
}
