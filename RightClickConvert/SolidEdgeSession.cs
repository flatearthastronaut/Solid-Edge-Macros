using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SolidEdgeConvert
{
    internal sealed class SolidEdgeSession : IEdgeSession
    {
        // Owns only the COM references acquired by this session, not the lifetime
        // of the Solid Edge process. Never share these wrappers between workers;
        // construction, use and disposal all belong on the same STA thread.
        // Value verified against Batch/Bin/Interop.SolidEdgeFramework.dll:
        // ApplicationGlobalConstants.seApplicationGlobalSTEPAdapterKey = 458.
        // Late binding avoids distributing Siemens interop DLLs with this tool.
        private const int StepAdapterKey = 458;
        // seApplicationGlobalDraftSaveAsPDFSheetOptions. The API's AllSheets
        // value (1) corresponds to the UI's "All sheets of same type" option.
        private const int PdfSheetOptionsKey = 172;
        private object application;
        private object documents;

        internal SolidEdgeSession()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                throw new InvalidOperationException("Solid Edge automation requires an STA thread.");
            try
            {
                bool started = false;
                try { application = Marshal.GetActiveObject("SolidEdge.Application"); }
                catch (COMException error)
                {
                    // As in the SDK Automation example, start only when there
                    // is no running server. Do not hide other connection errors.
                    if (error.ErrorCode != unchecked((int)0x800401E3)) throw;
                    Type type = Type.GetTypeFromProgID("SolidEdge.Application");
                    if (type == null) throw new InvalidOperationException("Solid Edge is not installed or its automation server is not registered.");
                    application = Activator.CreateInstance(type);
                    started = true;
                }
                if (started) ((dynamic)application).Visible = true;
                documents = ((dynamic)application).Documents;
            }
            catch { Dispose(); throw; }
        }

        public object StepAdapter
        {
            // Preserve the returned COM value as an object rather than coercing
            // it to bool. Conversion.Run restores the exact captured value after
            // STEP work; unrelated formats never access this global preference.
            get
            {
                object value = null;
                ((dynamic)application).GetGlobalParameter(StepAdapterKey, ref value);
                return value;
            }
            set { ((dynamic)application).SetGlobalParameter(StepAdapterKey, value); }
        }

        public object PdfSheetOptions
        {
            get
            {
                object value = null;
                ((dynamic)application).GetGlobalParameter(PdfSheetOptionsKey, ref value);
                return value;
            }
            set { ((dynamic)application).SetGlobalParameter(PdfSheetOptionsKey, value); }
        }

        public IEdgeDocument OpenDocument(string path)
        {
            return OpenDocument(documents, path);
        }

        public IEdgeDocument OpenDraftCopyWithoutGrindStock(string path, string copyPath)
        {
            return OpenDraftCopyWithoutGrindStock(documents, path, copyPath);
        }

        internal static IEdgeDocument OpenDraftCopyWithoutGrindStock(object documents, string path, string copyPath)
        {
            string activeName;
            // SaveCopyAs preserves the source identity and avoids file-lock issues
            // when a saved draft is already open. OpenDocument rejects dirty sources.
            using (SolidEdgeDocument source = (SolidEdgeDocument)OpenDocument(documents, path))
            {
                try
                {
                    activeName = source.ActiveSheetName();
                    source.SaveDraftCopy(copyPath);
                }
                finally { source.CloseIfOwned(); }
            }
            SolidEdgeDocument copy = (SolidEdgeDocument)OpenDocument(documents, copyPath);
            try
            {
                if (!copy.IsOwned) throw new IOException("The temporary draft is already open; cannot safely remove annotations.");
                copy.ActivateSheet(activeName);
                copy.RemoveGrindStock();
                return copy;
            }
            catch
            {
                try { copy.CloseIfOwned(); }
                finally { copy.Dispose(); }
                throw;
            }
        }

        public IEdgeDocument ImportStepPart(string path)
        {
            return ImportStepPart(documents, path);
        }

        internal static IEdgeDocument ImportStepPart(object documents, string path)
        {
            return ImportWithTemplate(documents, path, "normal.par", 1);
        }

        public IEdgeDocument ImportStepAssembly(string path)
        {
            return ImportWithTemplate(documents, path, "normal.asm", 3);
        }

        public IEdgeDocument ImportParasolid(string path, bool assembly)
        {
            return ImportWithTemplate(documents, path, assembly ? "normal.asm" : "normal.par", assembly ? 3 : 1);
        }

        internal static IEdgeDocument ImportWithTemplate(object documents, string path, string template, int documentType)
        {
            // Batch imports with OpenWithTemplate and Normal.par/Normal.asm. Let
            // Solid Edge resolve its configured template instead of hard-coding
            // a workstation/version-specific Program Files path or using Add().
            // A translated document need not have the STEP source's FullName.
            List<object> existing = new List<object>();
            object imported = null;
            bool owned = false;
            try
            {
                int count = ((dynamic)documents).Count;
                // Snapshot identity before importing. Solid Edge can return an
                // existing document, which this operation must not save or close.
                // Each Item acquisition is released in the finally block below.
                for (int index = 1; index <= count; index++) existing.Add(((dynamic)documents).Item(index));
                imported = ((dynamic)documents).OpenWithTemplate(path, template);
                if (imported == null) throw new IOException("Solid Edge did not return an imported document. Check the source file and " + template + " template.");
                foreach (object open in existing)
                    if (SameDocument(open, imported))
                        throw new IOException("Solid Edge returned an already-open document for this file. Close that document and retry so " + template + " can be applied to a fresh import.");
                owned = true;
                // Framework DocumentTypeConstants: part = 1, assembly = 3.
                if ((int)((dynamic)imported).Type != documentType)
                    throw new IOException("The import did not produce the requested document type using " + template + ".");
                IEdgeDocument result = new SolidEdgeDocument(imported, true);
                imported = null;
                return result;
            }
            finally
            {
                try
                {
                    // Close only a newly created rejected import; never an object
                    // from the pre-import document snapshot, even if it is dirty.
                    if (imported != null && owned) ((dynamic)imported).Close(false);
                }
                finally
                {
                    ComLifetime.Release(ref imported);
                    for (int index = existing.Count - 1; index >= 0; index--)
                    {
                        object open = existing[index];
                        ComLifetime.Release(ref open);
                    }
                }
            }
        }

        private static bool SameDocument(object left, object right)
        {
            // Two managed wrappers may refer to the same native COM document.
            // Canonical IUnknown identity detects that case; GetIUnknownForObject
            // adds a reference, so both acquired pointers need matching releases.
            if (Object.ReferenceEquals(left, right)) return true;
            if (!Marshal.IsComObject(left) || !Marshal.IsComObject(right)) return false;
            IntPtr first = Marshal.GetIUnknownForObject(left);
            IntPtr second = IntPtr.Zero;
            try { second = Marshal.GetIUnknownForObject(right); return first == second; }
            finally { Marshal.Release(first); if (second != IntPtr.Zero) Marshal.Release(second); }
        }

        internal static IEdgeDocument OpenDocument(object documents, string path)
        {
            // Reuse an already-open, saved part, assembly or draft without closing it afterwards.
            // Reject unsaved changes so a right-click export represents the file
            // selected in Explorer, not an unexpected in-memory revision.
            int count = ((dynamic)documents).Count;
            for (int index = 1; index <= count; index++)
            {
                object candidate = null;
                try
                {
                    candidate = ((dynamic)documents).Item(index);
                    string name = ((dynamic)candidate).FullName;
                    if (!String.IsNullOrEmpty(name) && String.Equals(Path.GetFullPath(name), path, StringComparison.OrdinalIgnoreCase))
                    {
                        if ((bool)((dynamic)candidate).Dirty)
                            throw new InvalidOperationException("This document has unsaved changes in Solid Edge. Save it, then convert it again.");
                        IEdgeDocument part = new SolidEdgeDocument(candidate, false);
                        candidate = null; // ownership of this COM reference transfers
                        return part;
                    }
                }
                finally { ComLifetime.Release(ref candidate); }
            }
            object opened = null;
            try
            {
                opened = ((dynamic)documents).Open(path);
                if (opened == null) throw new IOException("Solid Edge did not return the requested document.");
                string openedName = ((dynamic)opened).FullName;
                if (String.IsNullOrEmpty(openedName) || !String.Equals(Path.GetFullPath(openedName), path, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("Solid Edge opened a different document. Check the selected file and try again.");
                IEdgeDocument part = new SolidEdgeDocument(opened, true);
                opened = null;
                return part;
            }
            finally { ComLifetime.Release(ref opened); }
        }

        public void DoIdle() { ((dynamic)application).DoIdle(); }

        public void Dispose()
        {
            ComLifetime.Release(ref documents);
            // Like Batch, leave Solid Edge running. Never quit a user's session.
            ComLifetime.Release(ref application);
        }
    }

    internal sealed class SolidEdgeDocument : IEdgeDocument
    {
        // 'owned' grants permission to close, not permission to save the source.
        // SaveAs receives the staging destination; Close(false) discards incidental
        // document changes. Dispose releases our reference in either ownership mode.
        private object document;
        private readonly bool owned;
        internal SolidEdgeDocument(object document, bool owned) { this.document = document; this.owned = owned; }
        internal bool IsOwned { get { return owned; } }
        internal string ActiveSheetName()
        {
            object sheet = ((dynamic)document).ActiveSheet;
            try { return (string)((dynamic)sheet).Name; }
            finally { ComLifetime.Release(ref sheet); }
        }
        internal void SaveDraftCopy(string path) { ((dynamic)document).SaveCopyAs(path); }
        internal void ActivateSheet(string name)
        {
            object sheets = null, sheet = null;
            try { sheets = ((dynamic)document).Sheets; sheet = ((dynamic)sheets).Item(name); ((dynamic)sheet).Activate(); }
            finally { ComLifetime.Release(ref sheet); ComLifetime.Release(ref sheets); }
        }
        internal void RemoveGrindStock()
        {
            if (!owned) throw new InvalidOperationException("Cannot filter a borrowed draft.");
            GrindStockFilter.Remove(document);
        }
        public void SaveAs(string path) { ((dynamic)document).SaveAs(path); }
        public void CloseIfOwned() { if (owned) ((dynamic)document).Close(false); }
        public void Dispose() { ComLifetime.Release(ref document); }
    }

    internal static class GrindStockFilter
    {
        internal const int Red = 255; // Solid Edge COLORREF: RGB(255, 0, 0).

        internal static void Remove(object draft)
        {
            object sheets = null;
            try
            {
                sheets = ((dynamic)draft).Sheets;
                // Include background sheets because their graphics can appear on
                // the active sheet. No sheets are activated or deleted here.
                for (int i = 1; i <= (int)((dynamic)sheets).Count; i++)
                {
                    object sheet = ((dynamic)sheets).Item(i);
                    try { CleanSheet(sheet, 0); }
                    finally { ComLifetime.Release(ref sheet); }
                }
            }
            finally { ComLifetime.Release(ref sheets); }
        }

        private static void CleanSheet(object sheet, int depth)
        {
            CleanAnnotations(sheet, depth);
            object views = null;
            try
            {
                views = ((dynamic)sheet).DrawingViews;
                for (int i = 1; i <= (int)((dynamic)views).Count; i++)
                {
                    object view = null, viewSheet = null;
                    try
                    {
                        view = ((dynamic)views).Item(i); viewSheet = ((dynamic)view).Sheet;
                        CleanSheet(viewSheet, depth + 1);
                    }
                    finally { ComLifetime.Release(ref viewSheet); ComLifetime.Release(ref view); }
                }
            }
            finally { ComLifetime.Release(ref views); }
        }

        internal static void CleanAnnotations(object container, int depth)
        {
            if (depth > 64) throw new IOException("Unexpectedly deep annotation nesting; PDF was not exported.");
            object groups = null, frames = null, symbols = null;
            try
            {
                groups = ((dynamic)container).Groups;
                // Groups themselves are preserved. Recursively examine their
                // annotations; never delete a group based on its name or color.
                for (int i = (int)((dynamic)groups).Count; i >= 1; i--)
                {
                    object group = ((dynamic)groups).Item(i);
                    try
                    {
                        CleanAnnotations(group, depth + 1);
                    }
                    finally { ComLifetime.Release(ref group); }
                }
                symbols = ((dynamic)container).Symbols;
                // Iterate backwards because deletion shifts later collection items.
                // Delete only the placed occurrence, never open/edit its source file
                // or shared embedded document. Other instances/symbols remain intact.
                for (int i = (int)((dynamic)symbols).Count; i >= 1; i--)
                {
                    object symbol = ((dynamic)symbols).Item(i);
                    try
                    {
                        // The UI's symbol filename is SourceDoc. Name is an
                        // autogenerated occurrence ID (e.g. Symbol2d 42674).
                        // SourceDoc may include an OLE prefix before its path;
                        // compare only the final filename, never a substring.
                        string name = (string)((dynamic)symbol).SourceDoc;
                        if (MatchesSymbol(name)) ((dynamic)symbol).Delete();
                    }
                    finally { ComLifetime.Release(ref symbol); }
                }
                frames = ((dynamic)container).FeatureControlFrames;
                for (int i = (int)((dynamic)frames).Count; i >= 1; i--)
                {
                    object frame = null, style = null;
                    try
                    {
                        frame = ((dynamic)frames).Item(i); style = ((dynamic)frame).Style;
                        // FCF color is exposed through its dimension style, not
                        // a Color property. The sample's two red +.006 frames use
                        // DrivenColor=255. Read it; do not modify shared styles.
                        if (Convert.ToInt32(((dynamic)style).DrivenColor) == Red) ((dynamic)frame).Delete();
                    }
                    finally { ComLifetime.Release(ref style); ComLifetime.Release(ref frame); }
                }
            }
            finally { ComLifetime.Release(ref frames); ComLifetime.Release(ref symbols); ComLifetime.Release(ref groups); }
        }

        internal static bool MatchesSymbol(string name)
        {
            return !String.IsNullOrEmpty(name) && String.Equals(Path.GetFileName(name), "gsnote.dft", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class ComLifetime
    {
        internal static void Release(ref object reference)
        {
            // Clear the caller's slot first so another cleanup attempt is harmless.
            // Managed fakes used by regression tests require no COM release.
            object value = reference;
            reference = null;
            if (value == null || !Marshal.IsComObject(value)) return;
            // Release only our acquisition, never force a shared RCW to zero.
            // This follows the lifetime guidance in the SDK add-in samples.
            try { Marshal.ReleaseComObject(value); }
            catch (InvalidComObjectException) { }
            catch (COMException) { }
        }
    }

    [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleMessageFilter
    {
        [PreserveSig] int HandleInComingCall(int callType, IntPtr caller, int tickCount, IntPtr info);
        [PreserveSig] int RetryRejectedCall(IntPtr callee, int tickCount, int rejectType);
        [PreserveSig] int MessagePending(IntPtr callee, int tickCount, int pendingType);
    }

    // Based on the SDK Automation/OleMessageFilter example, with bounded retries
    // and restoration of the previous filter. All CAD calls stay on this STA.
    internal sealed class OleMessageFilter : IOleMessageFilter, IDisposable
    {
        private IOleMessageFilter previous;
        internal OleMessageFilter() { Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(this, out previous)); }
        public int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info) { return 0; }
        public int RetryRejectedCall(IntPtr callee, int ticks, int rejectType)
        {
            // 2 = SERVERCALL_RETRYLATER: retry after 250 ms, for at most 30 s.
            // -1 cancels a rejected call. This bounds busy-call retries, not the
            // duration of an export that Solid Edge has already accepted.
            return rejectType == 2 && ticks < 30000 ? 250 : -1;
        }
        public int MessagePending(IntPtr callee, int ticks, int pendingType) { return 2; }
        public void Dispose()
        {
            IOleMessageFilter removed;
            Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(previous, out removed));
            previous = null;
            GC.KeepAlive(this);
        }
        [DllImport("ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter filter, out IOleMessageFilter previous);
    }
}
