using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Hammill O-Ring Finder")]
[assembly: AssemblyCompany("Hammill")]
[assembly: AssemblyProduct("Hammill Macros")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

namespace Hammill.ORingFinder
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            SolidEdgeSession session = null;
            try
            {
                string table = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Oring_table.txt");
                if (args.Length != 0)
                {
                    if (args.Length != 2 || args[0] != "--table") throw new ArgumentException("Usage: O-Ring Finder.exe [--table \"full path to Oring_table.txt\"]");
                    table = Path.GetFullPath(args[1]);
                }
                using (var filter = new OleMessageFilter())
                using (var form = new FinderForm(table, () => {
                    if (session == null) session = new SolidEdgeSession();
                    return session.Scan();
                }, SolidEdgeSession.Apply)) Application.Run(form);
            }
            catch (Exception error) { MessageBox.Show(error.Message, "Hammill O-Ring Finder", MessageBoxButtons.OK, MessageBoxIcon.Error); }
            finally { if (session != null) session.Dispose(); }
        }
    }

    internal sealed class FinderForm : Form
    {
        private readonly Func<List<RingPart>> scan;
        private readonly Action<RingPart, RingSize, bool> apply;
        private readonly ListBox parts = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false };
        private readonly TextBox tablePath = new TextBox { Dock = DockStyle.Fill, ReadOnly = true };
        private readonly Label detail = new Label { Dock = DockStyle.Fill, AutoEllipsis = true };
        private readonly Label status = new Label { Dock = DockStyle.Fill, AutoEllipsis = true };
        private readonly RadioButton suggested = new RadioButton { AutoSize = true, Checked = true };
        private readonly RadioButton smaller = new RadioButton { AutoSize = true };
        private readonly RadioButton low = new RadioButton { Text = "< 1,200 psi", AutoSize = true, Checked = true };
        private readonly RadioButton high = new RadioButton { Text = "> 1,200 psi (P4300)", AutoSize = true };
        private readonly TextBox properties = new TextBox { Dock = DockStyle.Fill, Multiline = true, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White };
        private readonly Button save = new Button { Text = "Apply && save part", AutoSize = true, Enabled = false };
        private readonly Button reload = new Button { Text = "Reload table / assembly", AutoSize = true };
        private List<RingPart> owned = new List<RingPart>();
        private RingTable table;
        private RingMatch match;
        private readonly Icon ownedIcon;
        internal string StatusText { get { return status.Text; } }
        internal bool CanSave { get { return save.Enabled; } }

        internal FinderForm(string path, Func<List<RingPart>> scanParts, Action<RingPart, RingSize, bool> savePart)
        {
            scan = scanParts; apply = savePart;
            Text = "Hammill O-Ring Finder"; StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10f); AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(920, 600); MinimumSize = new Size(900, 620);
            BackColor = Color.White;
            using (Stream resource = Assembly.GetExecutingAssembly().GetManifestResourceStream("ORingFinder.Icon"))
                if (resource != null) { ownedIcon = new Icon(resource); Icon = ownedIcon; }
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(18) };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 62));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            var heading = new Label { Text = "HAMMILL  /  O-Ring Finder", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(114,148,3), Font = new Font("Segoe UI", 19f, FontStyle.Bold) };
            layout.Controls.Add(heading, 0, 0);
            var source = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3 };
            source.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72)); source.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); source.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 116));
            source.Controls.Add(new Label { Text = "Table file", AutoSize = true, Padding = new Padding(0,5,0,0) },0,0);
            tablePath.Text = path; source.Controls.Add(tablePath,1,0);
            var browse = new Button { Text = "Choose table…", Dock = DockStyle.Top, AutoSize = true };
            browse.Click += (s,e) => { using (var dialog = new OpenFileDialog { Filter = "O-ring tables (*.txt)|*.txt|All files (*.*)|*.*", FileName = tablePath.Text, CheckFileExists = true })
                if (dialog.ShowDialog(this) == DialogResult.OK) { tablePath.Text = dialog.FileName; ReloadData(); } };
            source.Controls.Add(browse,2,0); layout.Controls.Add(source,0,1);

            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34)); body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66));
            var listGroup = new GroupBox { Text = "Assembly O-ring parts", Dock = DockStyle.Fill, Padding = new Padding(10,20,10,10) };
            listGroup.Controls.Add(parts); body.Controls.Add(listGroup,0,0);
            var review = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(14,0,0,0) };
            review.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); review.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            review.RowStyles.Add(new RowStyle(SizeType.Absolute, 38)); review.RowStyles.Add(new RowStyle(SizeType.Absolute, 58)); review.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
            review.Controls.Add(detail,0,0); review.Controls.Add(suggested,0,1); review.Controls.Add(smaller,0,2);
            var pressure = new GroupBox { Text = "Pressure option", Dock = DockStyle.Fill };
            var pressureOptions = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(6,0,0,0) };
            pressureOptions.Controls.Add(low); pressureOptions.Controls.Add(high); pressure.Controls.Add(pressureOptions); review.Controls.Add(pressure,0,3);
            review.Controls.Add(properties,0,4); body.Controls.Add(review,1,0); layout.Controls.Add(body,0,2);
            status.Padding = new Padding(0,8,0,0); layout.Controls.Add(status,0,3);
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft };
            var close = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.Cancel };
            buttons.Controls.Add(close); buttons.Controls.Add(save); buttons.Controls.Add(reload); layout.Controls.Add(buttons,0,4);
            Controls.Add(layout); CancelButton = close;
            parts.SelectedIndexChanged += (s,e) => Review();
            suggested.CheckedChanged += (s,e) => PreviewProperties(); smaller.CheckedChanged += (s,e) => PreviewProperties();
            high.CheckedChanged += (s,e) => PreviewProperties();
            reload.Click += (s,e) => ReloadData(); save.Click += (s,e) => SaveSelected();
            Shown += (s,e) => ReloadData();
        }
        internal void ReloadData()
        {
            save.Enabled = false; match = null; table = null;
            parts.Items.Clear(); foreach (var part in owned) part.Dispose(); owned.Clear();
            try
            {
                table = RingTable.Load(tablePath.Text); // Validate before connecting to CAD.
                owned = scan(); parts.Items.AddRange(owned.Cast<object>().ToArray());
                status.Text = owned.Count == 0 ? "No top-level O_ or O- part occurrences found in the active assembly." : owned.Count + " unique O-ring part(s). Select a part to review its size.";
                if (owned.Count != 0) parts.SelectedIndex = 0; else Review();
            }
            catch (Exception error) { status.Text = error.Message; Review(); }
        }
        private void Review()
        {
            match = null; save.Enabled = false; suggested.Text = "Suggested: —"; smaller.Text = "Next smaller: —";
            suggested.Enabled = false; smaller.Enabled = false; properties.Clear(); detail.Text = "Select a part to review. All dimensions are inches.";
            var part = parts.SelectedItem as RingPart;
            if (part == null || table == null) return;
            try
            {
                if (part.Error != null) throw new InvalidDataException(part.Error);
                match = table.Match(part.Diameter, part.GrooveWidth);
                detail.Text = part.Name + "\r\n" + part.FileName + "\r\n\r\nPart ID: " + F(match.PartDiameter) + " in     WD: " + F(part.GrooveWidth) + " in\r\nNominal O-ring width: " + match.WidthLabel + " in\r\nDifference = (1 − ring ID / part ID) × 100.";
                suggested.Text = Choice("Suggested", match.Suggested); suggested.Enabled = true; suggested.Checked = true;
                smaller.Text = match.Smaller == null ? "Next smaller: none in this width group" : Choice("Next smaller", match.Smaller); smaller.Enabled = match.Smaller != null;
                low.Checked = true; save.Enabled = true; PreviewProperties();
            }
            catch (Exception error) { detail.Text = part.Name + "\r\n" + part.FileName + "\r\n\r\n" + error.Message; }
        }
        private static string F(decimal value) { return value.ToString("0.####", CultureInfo.InvariantCulture); }
        private string Choice(string label, RingSize size)
        { return label + ": " + size.Number + "     ID " + F(size.InsideDiameter) + " in     Difference " + F(match.Difference(size)) + "%"; }
        private void PreviewProperties()
        {
            if (match == null) return;
            var size = smaller.Checked ? match.Smaller : match.Suggested;
            if (size == null) return;
            properties.Text = "drawing number:  " + RingTable.DrawingNumber(size, high.Checked) + "\r\ndave_b:  " + RingTable.StockNumber(size, high.Checked) + "\r\nApply saves this part, including any other unsaved edits.";
        }
        private static string MatchKey(RingMatch value)
        { return F(value.PartDiameter) + "/" + value.WidthLabel + "/" + value.Suggested.Number + "/" + F(value.Suggested.InsideDiameter) + "/" + (value.Smaller == null ? "" : value.Smaller.Number + "/" + F(value.Smaller.InsideDiameter)); }
        internal void SaveSelected()
        {
            if (!save.Enabled || match == null) return;
            var part = (RingPart)parts.SelectedItem;
            try
            {
                // Refresh data immediately before writing. If a table edit or CAD
                // edit changes the displayed choice, require a fresh review click.
                var fresh = RingTable.Load(tablePath.Text);
                SolidEdgeSession.ReadDimensions(part);
                var current = fresh.Match(part.Diameter, part.GrooveWidth);
                table = fresh;
                if (MatchKey(current) != MatchKey(match)) { Review(); status.Text = "The dimensions or table changed. Review the updated sizes, then apply again."; return; }
                RingSize selected = smaller.Checked ? current.Smaller : current.Suggested;
                apply(part, selected, high.Checked);
                status.Text = "Saved " + part.Name + " — " + RingTable.DrawingNumber(selected, high.Checked);
            }
            catch (Exception error) { save.Enabled = false; status.Text = error.Message + " Reload to try again."; }
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing) { foreach (var part in owned) part.Dispose(); owned.Clear(); if (ownedIcon != null) ownedIcon.Dispose(); }
            base.Dispose(disposing);
        }
    }
}
