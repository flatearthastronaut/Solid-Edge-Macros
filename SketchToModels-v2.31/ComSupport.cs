using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace SketchToModels
{
    /// <summary>
    /// COM lifetime boundary based on the SDK Automation and toolbar samples.
    /// CAD calls belong on the UI's STA thread; workers receive only HWNDs.
    /// </summary>
    public static class ComSupport
    {
        public static void RequireSta()
        {
            if (Thread.CurrentThread.GetApartmentState() != ApartmentState.STA)
                throw new InvalidOperationException("Solid Edge automation must run on the STA user-interface thread.");
        }

        /// <summary>
        /// Attach to the existing session. Do not silently start another instance.
        /// Translate only MK_E_UNAVAILABLE; preserve other errors for diagnosis.
        /// </summary>
        public static object ConnectRunning()
        {
            RequireSta();
            try { return Marshal.GetActiveObject("SolidEdge.Application"); }
            catch (COMException error)
            {
                if (error.ErrorCode == unchecked((int)0x800401E3))
                    throw new InvalidOperationException("Open Solid Edge and your saved assembly before running this macro.", error);
                throw;
            }
        }

        /// <summary>
        /// Drop one owned COM reference after its last consumer finishes.
        /// Never loop to zero or call FinalReleaseComObject: the SDK add-in sample
        /// warns that force-releasing shared wrappers can break other consumers.
        /// This does not close documents, save them, or quit Solid Edge.
        /// </summary>
        public static void ReleaseOwned(ref object reference)
        {
            object value = reference;
            reference = null;
            if (value == null || !Marshal.IsComObject(value)) return;
            try { Marshal.ReleaseComObject(value); }
            catch (InvalidComObjectException) { /* Already disconnected/released. */ }
            catch (COMException) { /* Do not hide the original operation's error. */ }
        }
    }

    /// <summary>
    /// Register on the owning STA thread and restore the previous filter on exit.
    /// Check HRESULT and retain the filter throughout the UI message loop.
    /// </summary>
    internal sealed class MessageFilterScope : IDisposable
    {
        private readonly int ownerThread;
        private readonly BusyFilter filter;
        private IMessageFilter previous;
        private bool disposed;

        public MessageFilterScope()
        {
            ComSupport.RequireSta();
            ownerThread = Thread.CurrentThread.ManagedThreadId;
            filter = new BusyFilter();
            Marshal.ThrowExceptionForHR(BusyFilter.CoRegisterMessageFilter(filter, out previous));
        }

        public void Dispose()
        {
            if (disposed) return;
            if (Thread.CurrentThread.ManagedThreadId != ownerThread)
                throw new InvalidOperationException("Restore the OLE filter on its registering thread.");
            IMessageFilter removed;
            Marshal.ThrowExceptionForHR(BusyFilter.CoRegisterMessageFilter(previous, out removed));
            disposed = true;
            previous = null;
            GC.KeepAlive(filter);
        }
    }
}
