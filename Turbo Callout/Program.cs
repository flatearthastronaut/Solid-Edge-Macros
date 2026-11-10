using System;
using System.Drawing;
using System.Windows.Forms;
using HoleTableFormatter;

[assembly: System.Reflection.AssemblyTitle("Turbo Callout")]
[assembly: System.Reflection.AssemblyVersion("0.4.0.0")]

namespace TurboCallout
{
    internal sealed class MainForm : Form
    {
        readonly Button inch = new Button(), dual = new Button();
        readonly Label status = new Label();
        readonly TextBox results = new TextBox();
        bool running;
        internal MainForm()
        {
            Text = "Turbo Callout v0.4"; StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10); ClientSize = new Size(414, 164);
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
            using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream("TurboCallout.Icon")) Icon = new Icon(stream);
            var title = new Label { Text = "Fill blank hole callouts using:", AutoSize = true, Location = new Point(20, 19) };
            inch.Text = "Inch only"; inch.SetBounds(20, 53, 178, 42);
            dual.Text = "Dual — metric[inch]"; dual.SetBounds(214, 53, 180, 42);
            status.Text = "Scans all working sheets in the open draft."; status.SetBounds(20, 111, 376, 44);
            results.Multiline = true; results.ReadOnly = true; results.ScrollBars = ScrollBars.Vertical; results.SetBounds(20, 161, 374, 228);
            results.Visible = false;
            Controls.AddRange(new Control[] { title, inch, dual, status, results });
            inch.Click += delegate { Run(false); }; dual.Click += delegate { Run(true); };
            FormClosing += delegate(object sender, FormClosingEventArgs e) { if (running) e.Cancel = true; };
        }
        void Run(bool useDual)
        {
            running = true; inch.Enabled = dual.Enabled = false; UseWaitCursor = true;
            try
            {
                using (new BusyFilter())
                {
                    RunReport report = Automation.Run(useDual, text => { status.Text = text; status.Refresh(); });
                    status.Text = report.Filled + " filled; " + report.AlignedExisting + " aligned; " + report.Review + " need review; " + report.Errors + " errors.";
                    results.Text = report.ToString();
                }
            }
            catch (Exception error) { status.Text = "No further changes made."; results.Text = error.Message; }
            finally
            {
                running = false; inch.Enabled = dual.Enabled = true; UseWaitCursor = false;
                ClientSize = new Size(414, 410); results.Visible = true;
            }
        }
    }
    internal static class Program
    {
        [STAThread] static void Main()
        {
            Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
