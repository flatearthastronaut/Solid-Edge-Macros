using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Hammill.Automation
{
    // Each standalone tool borrows the user's running session and owns only the
    // references it obtains. Closing a tool never closes any user document.
    internal sealed class SeConnection : IDisposable
    {
        private readonly ComScope scope = new ComScope();
        public dynamic Application, Document;
        public SeConnection(params int[] allowedTypes)
        {
            try
            {
                Application = scope.Own(Marshal.GetActiveObject("SolidEdge.Application"));
                using (var temporary = new ComScope())
                {
                    dynamic documents = temporary.Own(Application.Documents);
                    if ((int)documents.Count == 0) throw new InvalidOperationException("Open the required document in Solid Edge first.");
                }
                Document = scope.Own(Application.ActiveDocument);
                if (!allowedTypes.Contains((int)Document.Type)) throw new InvalidOperationException(allowedTypes.Contains(2) ? "Make a draft the active Solid Edge document first." : "Make an assembly the active Solid Edge document first.");
            }
            catch { Dispose(); throw; }
        }
        public void Dispose() { Document = null; Application = null; scope.Dispose(); }
    }
    internal static class MacroHost
    {
        public static void Run(string title, int[] documentTypes, Func<SeConnection, Form> create)
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                using (var filter = new OleMessageFilter())
                using (var connection = new SeConnection(documentTypes))
                using (var form = create(connection)) System.Windows.Forms.Application.Run(form);
            }
            catch (Exception error) { MessageBox.Show(error.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
        public static void Style(Form form, string title, Size size)
        {
            form.Text = "Hammill " + title; form.Font = new Font("Segoe UI", 10);
            form.AutoScaleMode = AutoScaleMode.Dpi; form.ClientSize = size;
            form.MinimumSize = new Size(size.Width + 16, size.Height + 39);
            form.StartPosition = FormStartPosition.CenterScreen; form.BackColor = Color.White;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Macro.Icon"))
                if (stream != null) { var icon = new Icon(stream); form.Icon = icon; form.Disposed += (s,e) => icon.Dispose(); }
        }
        public static Label Heading(string title)
        { return new Label { Text = "HAMMILL  /  " + title, Dock = DockStyle.Fill, ForeColor = Color.FromArgb(114,148,3), Font = new Font("Segoe UI",17,FontStyle.Bold), AutoEllipsis = true }; }
    }
}
