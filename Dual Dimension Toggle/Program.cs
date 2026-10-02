using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Dual Dimension Toggle")]
[assembly: AssemblyProduct("Dual Dimension Toggle")]
[assembly: AssemblyDescription("Switch draft dimensions and Feature Control Frame tolerances between inch-only and dual units.")]
[assembly: AssemblyVersion("1.9.0.0")]

namespace DualDimensionToggle
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ConverterForm());
        }
    }

    internal sealed class ConverterForm : Form
    {
        private readonly Icon applicationIcon;
        private readonly RadioButton toInch = new RadioButton { Text = "Dual dimensioned to inch only", Checked = true, AutoSize = true };
        private readonly RadioButton toDual = new RadioButton { Text = "Inch only to dual dimensioned", AutoSize = true };
        private readonly Button convert = new Button { Text = "Convert active sheet", AutoSize = true, Padding = new Padding(10, 4, 10, 4) };
        private readonly TextBox results = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill };

        internal ConverterForm()
        {
            Text = "Dual Dimension Toggle v1.9";
            // The same multi-resolution icon is embedded as a Windows resource
            // for Explorer and a managed resource for the window/taskbar.
            using (System.IO.Stream iconStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("DualDimensionToggle.ico"))
            using (Icon embeddedIcon = new Icon(iconStream))
                applicationIcon = (Icon)embeddedIcon.Clone();
            Icon = applicationIcon;
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(760, 570);
            MinimumSize = new Size(740, 550);
            StartPosition = FormStartPosition.CenterScreen;
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 6 };
            for (int i = 0; i < 5; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label { Text = "Convert dimensions, Feature Control Frames, and callouts", AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 0, 0, 12) });
            layout.Controls.Add(new Label { Text = "Active sheet only. Dimension styles keep their decimal places and orientation.\r\nFrames: .001 <-> .03[.001]. Callouts: styles, dual-unit fields, and metric[inch] pairs.", AutoSize = true, Margin = new Padding(0, 0, 0, 12) });
            TableLayoutPanel choices = new TableLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = Padding.Empty };
            choices.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            choices.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            choices.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            choices.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            AddChoice(choices, toInch, false, 0);
            AddChoice(choices, toDual, true, 1);
            layout.Controls.Add(choices);
            layout.Controls.Add(new Label { Text = "Activate the desired sheet in Solid Edge, then click Convert.\r\nAngular dimensions are unchanged. Review and save the draft afterward.", AutoSize = true, Margin = new Padding(0, 12, 0, 12) });
            convert.Margin = new Padding(0, 0, 0, 14);
            layout.Controls.Add(convert);
            layout.Controls.Add(results);
            Controls.Add(layout);
            convert.Click += ConvertClick;
        }

        private static void AddChoice(TableLayoutPanel choices, RadioButton radio, bool dual, int row)
        {
            // Both radios share this parent, preserving WinForms' exclusive
            // selection and arrow-key navigation. The adjacent example is also
            // clickable, but is not an extra stop in the keyboard tab order.
            radio.Anchor = AnchorStyles.Left;
            radio.Margin = new Padding(0, 0, 14, 0);
            DimensionPreview picture = new DimensionPreview(dual) { Dock = DockStyle.Fill, Margin = Padding.Empty, Cursor = Cursors.Hand };
            picture.Click += delegate { if (radio.Enabled) { radio.Checked = true; radio.Focus(); } };
            choices.Controls.Add(radio, 0, row);
            choices.Controls.Add(picture, 1, row);
        }

        protected override void Dispose(bool disposing)
        {
            try { base.Dispose(disposing); }
            finally { if (disposing && applicationIcon != null) applicationIcon.Dispose(); }
        }

        private void ConvertClick(object sender, EventArgs args)
        {
            convert.Enabled = toInch.Enabled = toDual.Enabled = false;
            UseWaitCursor = true;
            results.Text = "Converting dimensions on the active sheet...";
            results.Refresh();
            try
            {
                // All automation stays on the WinForms STA, as required by the
                // SDK. No background COM calls or reentrant DoEvents loops.
                using (new OleMessageFilter()) results.Text = Converter.Run(toDual.Checked).ToString();
            }
            catch (Exception error)
            {
                results.Text = "Conversion could not complete.\r\n" + error.Message +
                    "\r\nIf no draft is active, open it and select the desired sheet. " +
                    "If Solid Edge is busy, finish its current command and retry.\r\n" +
                    "Review the active sheet before saving; any completed changes remain unsaved.";
            }
            finally { UseWaitCursor = false; convert.Enabled = toInch.Enabled = toDual.Enabled = true; }
        }
    }
}
