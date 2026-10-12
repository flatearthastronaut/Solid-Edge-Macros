using System;
using System.Reflection;
using System.Threading;
using SketchToModels;

/// <summary>
/// Tests thread and cleanup boundaries without opening or changing CAD files.
/// The filter test registers real Windows OLE filters on this test's STA thread.
/// </summary>
class ComSupportTests
{
    [STAThread] static void Main()
    {
        ComSupport.RequireSta();
        object managed = new object();
        ComSupport.ReleaseOwned(ref managed);
        if (managed != null) throw new Exception("Managed reference not cleared.");
        ComSupport.ReleaseOwned(ref managed); // Null cleanup must be harmless.

        Exception rejection = null;
        var worker = new Thread(() => {
            try { ComSupport.RequireSta(); }
            catch (Exception error) { rejection = error; }
        });
        worker.SetApartmentState(ApartmentState.MTA);
        worker.Start(); worker.Join();
        if (!(rejection is InvalidOperationException))
            throw new Exception("MTA CAD access was not rejected.");

        // Nested registration exercises restoring an existing filter rather
        // than always restoring null. Dispose must be idempotent.
        Type scope = typeof(ComSupport).Assembly.GetType("SketchToModels.MessageFilterScope", true);
        using (var outer = (IDisposable)Activator.CreateInstance(scope, true))
        {
            var inner = (IDisposable)Activator.CreateInstance(scope, true);
            inner.Dispose(); inner.Dispose();
        }
        Console.WriteLine("PASS: STA guard, MTA rejection, null/managed cleanup, nested OLE registration/restoration, repeated Dispose.");
    }
}
