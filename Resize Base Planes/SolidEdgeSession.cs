using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Hammill.Automation;

namespace ResizeBasePlanes
{
    internal sealed class SolidEdgeSession : IDisposable
    {
        private readonly ComScope scope = new ComScope();
        private readonly dynamic app;
        private readonly dynamic units;
        private readonly dynamic selection;
        private readonly IPlane[] planes = new IPlane[3];
        internal IntPtr Window { get; private set; }

        internal SolidEdgeSession()
        {
            try
            {
                app = scope.Own(Marshal.GetActiveObject("SolidEdge.Application"));
                dynamic documents = scope.Own(app.Documents);
                if ((int)documents.Count == 0) throw new InvalidOperationException("Open a model document first.");
                dynamic document = scope.Own(app.ActiveDocument);
                int type = (int)document.Type;
                bool assembly = type == 3 || type == 7 || type == 10;
                if (!assembly && type != 1 && type != 4 && type != 8 && type != 9)
                    throw new InvalidOperationException("The active document must be a part, sheet metal, or assembly.");
                units = scope.Own(document.UnitsOfMeasure);
                selection = scope.Own(document.SelectSet);
                dynamic collection = scope.Own(assembly ? document.AsmRefPlanes : document.RefPlanes);
                if ((int)collection.Count < 3) throw new InvalidOperationException("The document has fewer than three base planes.");
                Window = new IntPtr((int)app.hWnd);
                for (int i = 0; i < 3; i++)
                {
                    dynamic plane = scope.Own(collection.Item(i + 1));
                    if (!(bool)plane.Global) throw new InvalidOperationException("An expected base plane is not a global reference plane.");
                    planes[i] = new Plane(this, (object)plane);
                }
            }
            catch { scope.Dispose(); throw; }
        }

        internal void Apply(double x, double y)
        {
            // Convert the popup's document-unit values to the API's meters.
            double targetX = Parse(x.ToString("G17", CultureInfo.CurrentCulture));
            double targetY = Parse(y.ToString("G17", CultureInfo.CurrentCulture));
            if (!(targetX > 0) || !(targetY > 0) || Double.IsInfinity(targetX) || Double.IsInfinity(targetY))
                throw new InvalidOperationException("Solid Edge could not convert the requested sizes.");
            app.AbortCommand(false);
            ResizeEngine.Apply(planes, targetX, targetY);
        }

        private double Parse(string value) { return Convert.ToDouble(units.ParseUnit(1, value)); }

        private AxisReading Reading(string text)
        {
            double value = Parse(text);
            if (!(value > 0) || Double.IsInfinity(value))
                throw new InvalidOperationException("Solid Edge returned an invalid plane size.");
            Match decimalPlaces = Regex.Match(text, @"[.,](\d+)");
            double resolution = decimalPlaces.Success
                ? Parse(Math.Pow(10, -decimalPlaces.Groups[1].Length).ToString("F" + decimalPlaces.Groups[1].Length, CultureInfo.CurrentCulture))
                : 0;
            return new AxisReading(value, resolution);
        }

        private AxisReading[] Read(object plane)
        {
            // 40074 is the Resize Plane command in the installed 2026 version.
            // Read the native controls rather than UIA's cached floating-pane
            // tree. This is read-only UI access: all edits use the Solid Edge API.
            selection.RemoveAll();
            selection.Add(plane);
            try
            {
                app.StartCommand(40074);
                Stopwatch clock = Stopwatch.StartNew();
                do
                {
                    string[] values = NativeSizeReader.Read(Window);
                    if (values != null) return new[] { Reading(values[0]), Reading(values[1]) };
                    Thread.Sleep(50);
                } while (clock.ElapsedMilliseconds < 2500);
                throw new InvalidOperationException("Could not read the Resize Plane X/Y fields. A visible Solid Edge 2026 window is required.");
            }
            finally
            {
                // DoIdle is deliberately omitted: it can block while this
                // interactive command is active. Abort closes only our command.
                try { app.AbortCommand(false); }
                finally { selection.RemoveAll(); }
            }
        }

        public void Dispose() { scope.Dispose(); }

        private sealed class Plane : IPlane
        {
            private readonly SolidEdgeSession session;
            private readonly dynamic plane;
            internal Plane(SolidEdgeSession session, object plane) { this.session = session; this.plane = plane; }
            public string Name { get { return (string)plane.Name; } }
            public AxisReading[] Read() { return session.Read((object)plane); }
            public void MoveBoundary(bool alongX, double delta) { plane.ResizeRefPlaneByDirDist(alongX, 0, delta); }
        }
    }

    internal static class NativeSizeReader
    {
        private delegate bool EnumWindowProc(IntPtr window, IntPtr param);
        [DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowProc callback, IntPtr param);
        [DllImport("user32.dll")] private static extern bool EnumChildWindows(IntPtr parent, EnumWindowProc callback, IntPtr param);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        [DllImport("user32.dll")] private static extern int GetDlgCtrlID(IntPtr window);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder text, int capacity);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern IntPtr SendMessageTimeout(IntPtr window, uint message, IntPtr wParam, StringBuilder text, uint flags, uint timeout, out IntPtr result);

        internal static string[] Read(IntPtr root)
        {
            string[] result = ReadChildren(root);
            if (result != null) return result;
            uint process;
            GetWindowThreadProcessId(root, out process);
            // A floating command pane is a top-level window. Keep both fields
            // within one window and restrict enumeration to this SE process.
            EnumWindows(delegate(IntPtr window, IntPtr unused) {
                uint candidate;
                GetWindowThreadProcessId(window, out candidate);
                if (candidate == process && IsWindowVisible(window)) result = ReadChildren(window);
                return result == null;
            }, IntPtr.Zero);
            return result;
        }

        private static string[] ReadChildren(IntPtr parent)
        {
            string x = null, y = null;
            EnumChildWindows(parent, delegate(IntPtr window, IntPtr unused) {
                int id = GetDlgCtrlID(window);
                if ((id != 1997 && id != 1998) || !IsWindowVisible(window)) return true;
                StringBuilder name = new StringBuilder(128);
                GetClassName(window, name, name.Capacity);
                if (name.ToString().IndexOf("ComboBox", StringComparison.OrdinalIgnoreCase) < 0) return true;
                StringBuilder text = new StringBuilder(256);
                IntPtr ignored;
                // WM_GETTEXT only, with an explicit timeout for a busy UI.
                if (SendMessageTimeout(window, 0x000D, new IntPtr(text.Capacity), text, 2, 500, out ignored) != IntPtr.Zero)
                {
                    if (id == 1997) x = text.ToString(); else y = text.ToString();
                }
                return String.IsNullOrWhiteSpace(x) || String.IsNullOrWhiteSpace(y);
            }, IntPtr.Zero);
            return String.IsNullOrWhiteSpace(x) || String.IsNullOrWhiteSpace(y) ? null : new[] { x, y };
        }
    }
}
