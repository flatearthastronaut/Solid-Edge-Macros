using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace SolidEdgeConvert
{
    internal sealed class SolidEdgeSession : IEdgeSession
    {
        // Value verified against Batch/Bin/Interop.SolidEdgeFramework.dll:
        // ApplicationGlobalConstants.seApplicationGlobalSTEPAdapterKey = 458.
        // Late binding avoids distributing Siemens interop DLLs with this tool.
        private const int StepAdapterKey = 458;
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
            get
            {
                object value = null;
                ((dynamic)application).GetGlobalParameter(StepAdapterKey, ref value);
                return value;
            }
            set { ((dynamic)application).SetGlobalParameter(StepAdapterKey, value); }
        }

        public IEdgeDocument OpenDocument(string path)
        {
            return OpenDocument(documents, path);
        }

        public IEdgeDocument ImportStepPart(string path)
        {
            return ImportStepPart(documents, path);
        }

        internal static IEdgeDocument ImportStepPart(object documents, string path)
        {
            return ImportWithTemplate(documents, path, "normal.par", 1);
        }

        public IEdgeDocument ImportParasolid(string path, bool assembly)
        {
            return ImportWithTemplate(documents, path, assembly ? "normal.asm" : "normal.par", assembly ? 3 : 1);
        }

        internal static IEdgeDocument ImportWithTemplate(object documents, string path, string template, int documentType)
        {
            // Batch imports with OpenWithTemplate(source, "Normal.par"). Let
            // Solid Edge resolve its configured template instead of hard-coding
            // a workstation/version-specific Program Files path or using Add().
            // A translated document need not have the STEP source's FullName.
            List<object> existing = new List<object>();
            object imported = null;
            bool owned = false;
            try
            {
                int count = ((dynamic)documents).Count;
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
            if (Object.ReferenceEquals(left, right)) return true;
            if (!Marshal.IsComObject(left) || !Marshal.IsComObject(right)) return false;
            IntPtr first = Marshal.GetIUnknownForObject(left);
            IntPtr second = IntPtr.Zero;
            try { second = Marshal.GetIUnknownForObject(right); return first == second; }
            finally { Marshal.Release(first); if (second != IntPtr.Zero) Marshal.Release(second); }
        }

        internal static IEdgeDocument OpenDocument(object documents, string path)
        {
            // Reuse an already-open, saved part or draft without closing it afterwards.
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
        private object document;
        private readonly bool owned;
        internal SolidEdgeDocument(object document, bool owned) { this.document = document; this.owned = owned; }
        public void SaveAs(string path) { ((dynamic)document).SaveAs(path); }
        public void CloseIfOwned() { if (owned) ((dynamic)document).Close(false); }
        public void Dispose() { ComLifetime.Release(ref document); }
    }

    internal static class ComLifetime
    {
        internal static void Release(ref object reference)
        {
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
