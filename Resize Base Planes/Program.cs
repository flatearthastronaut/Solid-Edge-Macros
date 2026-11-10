using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hammill.Automation;

[assembly: AssemblyTitle("Resize Base Planes")]
[assembly: AssemblyProduct("Resize Base Planes")]
[assembly: AssemblyVersion("1.1.0.0")]
[assembly: AssemblyFileVersion("1.1.0.0")]

namespace ResizeBasePlanes
{
    internal static class Program
    {
        [STAThread]
        internal static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool quiet = Array.Exists(args, a => a.Equals("/quiet", StringComparison.OrdinalIgnoreCase));
            try
            {
                // Match the bundled SDK Automation example: all COM calls run
                // on one STA thread with an OLE busy-call filter. The shared
                // ComScope releases each acquired RCW in reverse order.
                using (new OleMessageFilter())
                using (SolidEdgeSession session = new SolidEdgeSession())
                {
                    double x = SizeInput.Default, y = SizeInput.Default;
                    if (!quiet)
                    {
                        using (SizeDialog dialog = new SizeDialog())
                        {
                            if (dialog.ShowDialog(new WindowOwner(session.Window)) != DialogResult.OK)
                                return 0; // Cancel and Esc never start a CAD command.
                            x = dialog.XValue;
                            y = dialog.YValue;
                        }
                    }
                    session.Apply(x, y);
                }
                // No success message: Enter applies the values and gets out of
                // the way. Errors remain explicit; documents are never saved.
                return 0;
            }
            catch (Exception error)
            {
                string message = "Could not finish resizing the base planes.\r\n\r\n" + error.Message +
                    "\r\n\r\nOpen a part, sheet metal, or assembly document in Solid Edge 2026 and finish any active command. If resizing stopped partway through, some planes may already have changed; running the macro again is safe.";
                if (quiet) Console.Error.WriteLine(message);
                else MessageBox.Show(message, "Resize Base Planes", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 1;
            }
        }
    }

    internal sealed class WindowOwner : IWin32Window
    {
        public IntPtr Handle { get; private set; }
        internal WindowOwner(IntPtr handle) { Handle = handle; }
    }
}
