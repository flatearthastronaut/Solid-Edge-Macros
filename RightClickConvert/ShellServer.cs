using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SolidEdgeConvert
{
    // Out-of-process IExecuteCommand receives the complete selection in one
    // call. Unlike a legacy %1 command, it neither launches once per file nor
    // drops filenames at a command-line length or 100-item menu limit.
    [ComVisible(true), ClassInterface(ClassInterfaceType.None)]
    public abstract class SelectionCommand : IExecuteCommand, IObjectWithSelection
    {
        private string[] paths;
        private readonly ConversionFormat format;
        internal SelectionCommand(ConversionFormat format) { this.format = format; ShellServer.Touch(); }
        // Copy filesystem paths immediately. The queued CAD work should not hold
        // Explorer's selection COM objects alive for the duration of a conversion.
        public void SetSelection(IShellItemArray selection) { paths = ShellSelection.Read(selection); ShellServer.Touch(); }
        public void GetSelection(ref Guid iid, out IntPtr selection)
        {
            selection = IntPtr.Zero;
            if (paths == null) throw new InvalidOperationException("No files were selected.");
            IShellItemArray array = ShellSelection.Create(paths);
            IntPtr unknown = Marshal.GetIUnknownForObject(array);
            // QueryInterface transfers one native reference to the caller. Release
            // only our temporary IUnknown and array acquisitions here, not 'selection'.
            try { Marshal.ThrowExceptionForHR(Marshal.QueryInterface(unknown, ref iid, out selection)); }
            finally { Marshal.Release(unknown); Marshal.ReleaseComObject(array); }
        }
        public void Execute()
        {
            if (paths == null) throw new InvalidOperationException("No files were selected.");
            ShellServer.Submit(paths, format);
            paths = null;
        }
        public void SetKeyState(uint keys) { ShellServer.Touch(); }
        public void SetParameters(string parameters) { }
        public void SetPosition(ShellPoint point) { }
        public void SetShowWindow(int show) { }
        public void SetNoShowUI(bool noUI) { }
        public void SetDirectory(string directory) { }
    }

    // Distinct COM classes identify the requested route without parsing filenames
    // or menu captions. Their GUIDs must match ShellMenu's DelegateExecute values.
    [ComVisible(true), Guid(ShellMenu.StepClass), ClassInterface(ClassInterfaceType.None)]
    public sealed class StepSelectionCommand : SelectionCommand
    { public StepSelectionCommand() : base(ConversionFormat.Step) { } }

    [ComVisible(true), Guid(ShellMenu.PdfClass), ClassInterface(ClassInterfaceType.None)]
    public sealed class PdfSelectionCommand : SelectionCommand
    { public PdfSelectionCommand() : base(ConversionFormat.Pdf) { } }

    [ComVisible(true), Guid(ShellMenu.DatedPdfClass), ClassInterface(ClassInterfaceType.None)]
    public sealed class DatedPdfSelectionCommand : SelectionCommand
    { public DatedPdfSelectionCommand() : base(ConversionFormat.PdfWithDate) { } }

    [ComVisible(true), Guid(ShellMenu.PartClass), ClassInterface(ClassInterfaceType.None)]
    public sealed class PartSelectionCommand : SelectionCommand
    { public PartSelectionCommand() : base(ConversionFormat.Part) { } }

    [ComVisible(true), Guid(ShellMenu.ParasolidPartClass), ClassInterface(ClassInterfaceType.None)]
    public sealed class ParasolidPartSelectionCommand : SelectionCommand
    { public ParasolidPartSelectionCommand() : base(ConversionFormat.ParasolidPart) { } }

    [ComVisible(true), Guid(ShellMenu.ParasolidAssemblyClass), ClassInterface(ClassInterfaceType.None)]
    public sealed class ParasolidAssemblySelectionCommand : SelectionCommand
    { public ParasolidAssemblySelectionCommand() : base(ConversionFormat.ParasolidAssembly) { } }

    [ComVisible(true), Guid(ShellMenu.StepAssemblyClass), ClassInterface(ClassInterfaceType.None)]
    public sealed class StepAssemblySelectionCommand : SelectionCommand
    { public StepAssemblySelectionCommand() : base(ConversionFormat.StepAssembly) { } }

    [ComVisible(true), Guid(ShellMenu.ParasolidExportClass), ClassInterface(ClassInterfaceType.None)]
    public sealed class ParasolidExportSelectionCommand : SelectionCommand
    { public ParasolidExportSelectionCommand() : base(ConversionFormat.ParasolidExport) { } }

    [ComVisible(true), Guid(ShellMenu.StlClass), ClassInterface(ClassInterfaceType.None)]
    public sealed class StlSelectionCommand : SelectionCommand
    { public StlSelectionCommand() : base(ConversionFormat.Stl) { } }

    internal sealed class ShellServer : ApplicationContext
    {
        private static ShellServer current;
        private readonly Control dispatcher = new Control();
        private readonly Timer idle = new Timer();
        private readonly Queue<Action> jobs = new Queue<Action>();
        private readonly RegistrationServices registration = new RegistrationServices();
        private readonly List<int> cookies = new List<int>();
        private DateTime lastActivity = DateTime.UtcNow;
        private bool busy;

        internal ShellServer()
        {
            Application.OleRequired();
            IntPtr handle = dispatcher.Handle; // creates an STA message target
            current = this;
            try
            {
                cookies.Add(registration.RegisterTypeForComClients(typeof(StepSelectionCommand), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse));
                cookies.Add(registration.RegisterTypeForComClients(typeof(PdfSelectionCommand), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse));
                cookies.Add(registration.RegisterTypeForComClients(typeof(DatedPdfSelectionCommand), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse));
                cookies.Add(registration.RegisterTypeForComClients(typeof(PartSelectionCommand), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse));
                cookies.Add(registration.RegisterTypeForComClients(typeof(ParasolidPartSelectionCommand), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse));
                cookies.Add(registration.RegisterTypeForComClients(typeof(ParasolidAssemblySelectionCommand), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse));
                cookies.Add(registration.RegisterTypeForComClients(typeof(StepAssemblySelectionCommand), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse));
                cookies.Add(registration.RegisterTypeForComClients(typeof(ParasolidExportSelectionCommand), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse));
                cookies.Add(registration.RegisterTypeForComClients(typeof(StlSelectionCommand), RegistrationClassContext.LocalServer, RegistrationConnectionType.MultipleUse));
                // A small idle grace period lets Explorer finish using its proxy.
                // No CAD work runs on this thread, and busy batches cannot expire.
                idle.Interval = 1000;
                idle.Tick += delegate
                {
                    if (!busy && jobs.Count == 0 && (DateTime.UtcNow - lastActivity).TotalSeconds > 60) ExitThread();
                };
                idle.Start();
            }
            catch { Dispose(); throw; }
        }

        internal static void Touch() { if (current != null) current.lastActivity = DateTime.UtcNow; }
        internal static void Submit(string[] paths, ConversionFormat format)
        {
            if (current == null) throw new InvalidOperationException("The Explorer conversion server is unavailable.");
            ShellServer host = current;
            Touch();
            // Return to Explorer immediately; show prompts only from the posted
            // callback. Additional Explorer requests are queued on the same STA.
            host.jobs.Enqueue(delegate { Program.RunBatch(paths, format); });
            host.dispatcher.BeginInvoke((Action)host.ProcessNext);
        }

        private void ProcessNext()
        {
            // RunBatch shows a modal form that continues pumping UI messages.
            // Explorer can queue another request during that time; this guard
            // prevents the nested message loop from starting a concurrent batch.
            if (busy || jobs.Count == 0) return;
            busy = true;
            try { jobs.Dequeue()(); }
            catch (Exception error) { MessageBox.Show(Program.Describe(error), "Solid Edge Convert", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { busy = false; Touch(); }
            if (jobs.Count > 0) dispatcher.BeginInvoke((Action)ProcessNext);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Cookies revoke precisely the factories registered by this host.
                // The list also supports cleanup after partial constructor failure.
                idle.Stop(); idle.Dispose();
                foreach (int cookie in cookies) registration.UnregisterTypeForComClients(cookie);
                cookies.Clear();
                dispatcher.Dispose();
                if (current == this) current = null;
            }
            base.Dispose(disposing);
        }
    }
}
