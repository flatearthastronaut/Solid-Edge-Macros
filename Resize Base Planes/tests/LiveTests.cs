using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Hammill.Automation;
using ResizeBasePlanes;

internal static class LiveTests
{
    [STAThread]
    public static int Main()
    {
        try
        {
            using (new OleMessageFilter())
            using (ComScope scope = new ComScope())
            {
                dynamic app = scope.Own(Marshal.GetActiveObject("SolidEdge.Application"));
                dynamic documents = scope.Own(app.Documents);
                object original = (int)documents.Count > 0 ? scope.Own(app.ActiveDocument) : null;
                Console.WriteLine("Solid Edge " + app.Version);
                try
                {
                    foreach (string kind in new[] { "Assembly", "Part", "SheetMetal" })
                    using (ComScope local = new ComScope())
                    {
                        dynamic document = local.Own(documents.Add("SolidEdge." + kind + "Document"));
                        try
                        {
                            document.Activate();
                            using (SolidEdgeSession session = new SolidEdgeSession())
                            {
                                session.Apply(.65, 1.20);
                                Verify(app, document, local, kind, .65, 1.20);
                                session.Apply(.40, .40);
                                session.Apply(.40, .40);
                                Verify(app, document, local, kind, .40, .40);
                            }
                            Console.WriteLine("PASS " + kind + ": unequal sizes, defaults, repeat run, all six field readbacks.");
                        }
                        finally
                        {
                            app.AbortCommand(false);
                            document.Close(false);
                        }
                    }
                }
                finally { if (original != null) ((dynamic)original).Activate(); }
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Verify(dynamic app, dynamic document, ComScope scope, string kind, double x, double y)
    {
        dynamic planes = scope.Own(kind == "Assembly" ? document.AsmRefPlanes : document.RefPlanes);
        dynamic select = scope.Own(document.SelectSet);
        dynamic units = scope.Own(document.UnitsOfMeasure);
        double expectedX = Convert.ToDouble(units.ParseUnit(1, x.ToString(CultureInfo.CurrentCulture)));
        double expectedY = Convert.ToDouble(units.ParseUnit(1, y.ToString(CultureInfo.CurrentCulture)));
        for (int i = 1; i <= 3; i++)
        {
            dynamic plane = scope.Own(planes.Item(i));
            select.RemoveAll(); select.Add(plane);
            try
            {
                app.StartCommand(40074);
                string[] fields;
                Stopwatch timer = Stopwatch.StartNew();
                while ((fields = NativeSizeReader.Read(new IntPtr((int)app.hWnd))) == null)
                {
                    if (timer.ElapsedMilliseconds > 2500) throw new Exception("No verification fields.");
                    Thread.Sleep(50);
                }
                double actualX = Convert.ToDouble(units.ParseUnit(1, fields[0]));
                double actualY = Convert.ToDouble(units.ParseUnit(1, fields[1]));
                if (Math.Abs(actualX - expectedX) > 1e-9 || Math.Abs(actualY - expectedY) > 1e-9)
                    throw new Exception(kind + " " + plane.Name + " readback mismatch: " + fields[0] + ", " + fields[1]);
                Console.WriteLine(kind + " " + plane.Name + ": X=" + fields[0] + " Y=" + fields[1]);
            }
            finally { app.AbortCommand(false); select.RemoveAll(); }
        }
    }
}
