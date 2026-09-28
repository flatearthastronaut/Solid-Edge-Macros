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
                using (IPart part = new SolidEdgePart(document, false)) { part.SaveAs("unused"); part.CloseIfOwned(); }
                Check(document.Saved && !document.Closed, "Borrowed document was closed");
            });
            Test("owned document closes without saving", delegate {
                FakeDocument document = new FakeDocument();
                using (IPart part = new SolidEdgePart(document, true)) { part.CloseIfOwned(); }
                Check(document.Closed && !document.SaveOnClose, "Source might be saved");
            });
            Test("open-document lookup reuses a saved part", delegate {
                FakeDocument document = new FakeDocument { FullName = source };
                FakeDocuments documents = new FakeDocuments { Existing = document };
                using (IPart part = SolidEdgeSession.OpenPart(documents, source)) { part.CloseIfOwned(); }
                Check(!documents.OpenCalled && !document.Closed, "Existing part was reopened or closed");
            });
            Test("unsaved edits are rejected without closing or reopening", delegate {
                FakeDocument document = new FakeDocument { FullName = source, Dirty = true };
                FakeDocuments documents = new FakeDocuments { Existing = document };
                ExpectFailure(delegate { SolidEdgeSession.OpenPart(documents, source); });
                Check(!documents.OpenCalled && !document.Closed, "Dirty document was touched");
            });
            Test("a document opened by the converter is owned", delegate {
                FakeDocument document = new FakeDocument { FullName = source };
                FakeDocuments documents = new FakeDocuments { OpenResult = document };
                using (IPart part = SolidEdgeSession.OpenPart(documents, source)) { part.CloseIfOwned(); }
                Check(documents.OpenCalled && document.Closed && !document.SaveOnClose, "Ownership incorrect");
            });
            Test("redirected and null opens are rejected", delegate {
                FakeDocument other = new FakeDocument { FullName = Path.Combine(folder, "different.par") };
                ExpectFailure(delegate { SolidEdgeSession.OpenPart(new FakeDocuments { OpenResult = other }, source); });
                Check(!other.Closed && !other.Saved, "Unrelated document was changed");
                ExpectFailure(delegate { SolidEdgeSession.OpenPart(new FakeDocuments(), source); });
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
        internal Action OnSave;
        internal List<string> Events = new List<string>();
        internal FakeSession(bool adapter) { Adapter = adapter; }
        public object StepAdapter
        {
            get { Events.Add("get"); return Adapter; }
            set
            {
                Events.Add("set:" + value);
                Adapter = value;
                if ((bool)value && FailEnable) throw new IOException("Enable failed");
                if (!(bool)value && FailRestore) throw new IOException("Restore failed");
            }
        }
        public IPart OpenPart(string path)
        {
            Events.Add("open");
            if (FailOpen) throw new IOException("Open failed");
            return new FakePart(this);
        }
        public void DoIdle() { Events.Add("idle"); }
        public void Dispose() { Events.Add("session-dispose"); Disposed = true; }
        private sealed class FakePart : IPart
        {
            private FakeSession owner;
            internal FakePart(FakeSession owner) { this.owner = owner; }
            public void SaveAs(string path)
            {
                owner.Events.Add("save");
                if (!owner.MissingOutput) File.WriteAllText(path, owner.EmptyOutput ? "" : "STEP data");
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
