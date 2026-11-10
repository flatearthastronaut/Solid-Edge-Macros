using System;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;

namespace ResizeBasePlanes
{
    internal static class SizeInput
    {
        internal const double Default = 0.40;
        internal static string DefaultText { get { return Default.ToString(".00", CultureInfo.CurrentCulture); } }

        internal static bool TryParse(string text, CultureInfo culture, out double value)
        {
            // A single numeric distance per field, never thousands separators.
            // Accept the local decimal separator and a plain decimal point.
            const NumberStyles style = NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;
            bool parsed = Double.TryParse(text, style, culture, out value) ||
                Double.TryParse(text, style, CultureInfo.InvariantCulture, out value);
            return parsed && value > 0 && !Double.IsNaN(value) && !Double.IsInfinity(value);
        }
    }

    internal sealed class SizeDialog : Form
    {
        internal readonly TextBox XInput = new TextBox();
        internal readonly TextBox YInput = new TextBox();
        internal readonly Button ApplyButton = new Button();
        internal readonly Button DismissButton = new Button();
        internal readonly Label ErrorLabel = new Label();
        internal double XValue { get; private set; }
        internal double YValue { get; private set; }

        internal SizeDialog()
        {
            Text = "Resize Base Planes";
            Font = new Font("Segoe UI", 10f);
            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96, 96);
            ClientSize = new Size(344, 214);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            BackColor = Color.FromArgb(248, 250, 252);
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Macro.Icon"))
                if (stream != null) using (Icon loaded = new Icon(stream)) Icon = (Icon)loaded.Clone();

            var heading = new Label { Text = "Top, Right and Front", AutoSize = true,
                Font = new Font(Font, FontStyle.Bold), Location = new Point(20, 16) };
            var units = new Label { Text = "Sizes use the active document's units.", AutoSize = true,
                ForeColor = Color.FromArgb(88, 101, 115), Location = new Point(20, 43) };
            var xLabel = new Label { Text = "&X", AutoSize = true, Location = new Point(20, 82), TabIndex = 0 };
            var yLabel = new Label { Text = "&Y", AutoSize = true, Location = new Point(184, 82), TabIndex = 2 };
            XInput.SetBounds(44, 77, 112, 30);
            YInput.SetBounds(208, 77, 112, 30);
            XInput.Text = YInput.Text = SizeInput.DefaultText;
            XInput.AccessibleName = "X size";
            YInput.AccessibleName = "Y size";
            XInput.TabIndex = 1;
            YInput.TabIndex = 3;
            XInput.Enter += delegate { XInput.SelectAll(); };
            YInput.Enter += delegate { YInput.SelectAll(); };
            ErrorLabel.SetBounds(20, 114, 304, 26);
            ErrorLabel.ForeColor = Color.FromArgb(175, 38, 38);
            ErrorLabel.AccessibleName = "Input error";

            ApplyButton.Text = "Resize";
            ApplyButton.SetBounds(124, 153, 96, 34);
            ApplyButton.TabIndex = 4;
            ApplyButton.Click += delegate { Submit(); };
            DismissButton.Text = "Cancel";
            DismissButton.SetBounds(228, 153, 96, 34);
            DismissButton.TabIndex = 5;
            DismissButton.DialogResult = DialogResult.Cancel;
            AcceptButton = ApplyButton; // Enter accepts .40/.40 immediately.
            CancelButton = DismissButton; // Escape is always a clean no-op.
            Controls.AddRange(new Control[] { heading, units, xLabel, XInput, yLabel, YInput, ErrorLabel, ApplyButton, DismissButton });
            Shown += delegate { XInput.Focus(); XInput.SelectAll(); };
        }

        internal bool Submit()
        {
            double x, y;
            if (!SizeInput.TryParse(XInput.Text, CultureInfo.CurrentCulture, out x))
                return Reject(XInput, "Enter an X size greater than zero.");
            if (!SizeInput.TryParse(YInput.Text, CultureInfo.CurrentCulture, out y))
                return Reject(YInput, "Enter a Y size greater than zero.");
            XValue = x;
            YValue = y;
            ErrorLabel.Text = "";
            DialogResult = DialogResult.OK;
            return true;
        }

        private bool Reject(TextBox input, string message)
        {
            ErrorLabel.Text = message;
            input.Focus();
            input.SelectAll();
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && Icon != null) { Icon.Dispose(); Icon = null; }
            base.Dispose(disposing);
        }
    }
}
