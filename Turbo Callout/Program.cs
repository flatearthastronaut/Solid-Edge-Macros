using System;
using System.Windows.Forms;
[assembly: System.Reflection.AssemblyTitle("Turbo Callout")]
[assembly: System.Reflection.AssemblyVersion("1.5.0.0")]
namespace TurboCallout
{
    internal static class Program
    {
        [STAThread] static void Main()
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
