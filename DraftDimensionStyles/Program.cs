using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

[assembly: AssemblyTitle("Draft Dimension Styles")]
[assembly: AssemblyVersion("1.0.0.0")]

namespace DraftDimensionStyles
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
        private readonly RadioButton toInch = new RadioButton { Text = "Dual dimensioned to inch only", Checked = true, AutoSize = true };
        private readonly RadioButton toDual = new RadioButton { Text = "Inch only to dual dimensioned", AutoSize = true };
        private readonly Button convert = new Button { Text = "Convert active sheet", AutoSize = true, Padding = new Padding(10, 4, 10, 4) };
        private readonly TextBox results = new TextBox { Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Both, WordWrap = false, Dock = DockStyle.Fill };

        internal ConverterForm()
        {
            Text = "Draft Dimension Styles v1.0";
            Font = new Font("Segoe UI", 10);
            AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(690, 480);
            MinimumSize = new Size(610, 430);
            StartPosition = FormStartPosition.CenterScreen;
            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 7 };
            for (int i = 0; i < 6; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.Controls.Add(new Label { Text = "Change dimension styles on the active draft sheet", AutoSize = true, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(0, 0, 0, 12) });
            layout.Controls.Add(new Label { Text = "Matches the decimal-place count and vertical orientation in your style names.\r\nExample: 2 3 place m[i]  <->  1 3 place", AutoSize = true, Margin = new Padding(0, 0, 0, 12) });
            layout.Controls.Add(toInch);
            layout.Controls.Add(toDual);
            layout.Controls.Add(new Label { Text = "Activate the desired sheet in Solid Edge, then click Convert.\r\nMissing matches and other styles are skipped. Review and save the draft afterward.", AutoSize = true, Margin = new Padding(0, 12, 0, 12) });
            convert.Margin = new Padding(0, 0, 0, 14);
            layout.Controls.Add(convert);
            layout.Controls.Add(results);
            Controls.Add(layout);
            convert.Click += ConvertClick;
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
