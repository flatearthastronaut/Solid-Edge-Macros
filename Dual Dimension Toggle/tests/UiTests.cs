using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using DualDimensionToggle;

internal static class UiTests
{
    private static int assertions;
    private static void Check(bool success, string message)
    { assertions++; if (!success) throw new Exception(message); }
    private static List<T> Find<T>(Control parent) where T : Control
    {
        List<T> found = new List<T>();
        foreach (Control child in parent.Controls)
        {
            if (child is T) found.Add((T)child);
            found.AddRange(Find<T>(child));
        }
        return found;
    }
    private static void Click(Control control)
    {
        typeof(Control).GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(control, new object[] { EventArgs.Empty });
    }
    private static void Layout(Control control)
    {
        control.PerformLayout();
        foreach (Control child in control.Controls) Layout(child);
    }
    private static void CheckLayout(ConverterForm form)
    {
        Layout(form);
        foreach (Control child in Find<Control>(form))
        {
            Check(child.Parent.ClientRectangle.Contains(child.Bounds), "Control clipped: " + child.GetType().Name + " " + child.Text);
            if (child is RadioButton || child is Label)
                Check(child.Width >= child.PreferredSize.Width, "Label clipped: " + child.Text);
        }
        Check(Find<TextBox>(form)[0].Height >= 80, "Results area stays usable");
    }

    [STAThread]
    public static int Main()
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            // Construct and render our own form; no Solid Edge connection and
            // no screenshot of the user's desktop or another application.
            using (ConverterForm form = new ConverterForm())
            {
                // WinForms omits child controls from DrawToBitmap until the
                // form has been shown. Keep this test window off-screen and
                // transparent while initializing its normal layout lifecycle.
                form.ShowInTaskbar = false;
                form.StartPosition = FormStartPosition.Manual;
                form.Location = new Point(-30000, -30000);
                form.Opacity = 0;
                form.Show();
                CheckLayout(form);
                List<RadioButton> radios = Find<RadioButton>(form);
                List<DimensionPreview> pictures = Find<DimensionPreview>(form);
                Check(radios.Count == 2 && pictures.Count == 2, "Two illustrated choices");
                Check(radios[0].Visible && pictures[0].Visible, "Preview children initialized for rendering");
                Check(radios[0].Checked && !radios[1].Checked, "Default inch direction");
                Check(radios[0].Parent == radios[1].Parent, "Shared radio group");
                Click(pictures[1]);
                Check(radios[1].Checked && !radios[0].Checked, "Clicking dual illustration changes direction exclusively");
                radios[0].Enabled = false;
                Click(pictures[0]);
                Check(radios[1].Checked, "Disabled option cannot be selected through illustration");
                radios[0].Enabled = true;
                Click(pictures[0]);
                Check(radios[0].Checked && !radios[1].Checked, "Clicking inch illustration changes direction exclusively");
                foreach (DimensionPreview picture in pictures)
                    Check(!picture.TabStop && !String.IsNullOrEmpty(picture.AccessibleName), "Examples have accessible descriptions without extra tab stops");
                using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PopupPreview.png"), ImageFormat.Png);
                }
                form.Size = form.MinimumSize;
                CheckLayout(form);
                // Exercise scaling of both layout and the vector control.
                form.Scale(new SizeF(1.5f, 1.5f));
                CheckLayout(form);
                using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
                {
                    form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
                    bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PopupPreviewScaled.png"), ImageFormat.Png);
                }
            }
            Console.WriteLine("PASS: " + assertions + " popup interaction and layout assertions.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
