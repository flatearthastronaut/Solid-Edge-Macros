using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ResizeBasePlanes;

internal static class Tests
{
    private static int checks;
    private static void Check(bool ok, string message) { checks++; if (!ok) throw new Exception(message); }
    private static void Near(double a, double b, string message) { Check(Math.Abs(a - b) < 1e-9, message); }
    private static void Throws(Action action, string message)
    { bool failed = false; try { action(); } catch { failed = true; } Check(failed, message); }

    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Parser();
            Engine();
            Dialog(args[0]);
            Icons(args[0]);
            Console.WriteLine("PASS: " + checks + " regression assertions.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }

    private static void Parser()
    {
        double value;
        CultureInfo en = CultureInfo.GetCultureInfo("en-US"), de = CultureInfo.GetCultureInfo("de-DE");
        Check(SizeInput.TryParse(".40", en, out value), "Default .40 accepted"); Near(value, .4, "Default is .4");
        Check(SizeInput.TryParse(" 1.250 ", en, out value), "Whitespace and decimal precision accepted"); Near(value, 1.25, "Custom numeric value");
        Check(SizeInput.TryParse(",75", de, out value), "Local decimal separator accepted"); Near(value, .75, "Localized value");
        Check(SizeInput.TryParse(".75", de, out value), "Point accepted in decimal-comma locale"); Near(value, .75, "Point isn't interpreted as grouping");
        foreach (string bad in new[] { "", " ", "0", "-.4", "NaN", "Infinity", "1,000", "abc", ".4in", "1e309" })
            Check(!SizeInput.TryParse(bad, en, out value), "Reject invalid input: " + bad);
    }

    private static void Engine()
    {
        FakePlane[] planes = { new FakePlane(5, 7), new FakePlane(.1, .2), new FakePlane(.4, .9), new FakePlane(8, 9) };
        ResizeEngine.Apply(planes, .4, .9);
        for (int i = 0; i < 3; i++) { Near(planes[i].X, .4, "Every base X"); Near(planes[i].Y, .9, "Every base Y"); }
        Near(planes[3].X, 8, "Non-base X untouched"); Near(planes[3].Y, 9, "Non-base Y untouched");
        int calls = planes[0].Moves + planes[1].Moves + planes[2].Moves;
        ResizeEngine.Apply(planes, .4, .9);
        Check(calls == planes[0].Moves + planes[1].Moves + planes[2].Moves, "Repeated run does not move boundaries");
        Check(planes[1].LastDelta < 0, "Small planes use negative displacement to enlarge");
        ResizeEngine.Apply(planes, 1.25, .25);
        for (int i = 0; i < 3; i++) { Near(planes[i].X, 1.25, "Custom X applied independently"); Near(planes[i].Y, .25, "Custom Y applied independently"); }
        FakePlane[] rounded = { new FakePlane(.4, .4), new FakePlane(.4, .4), new FakePlane(.4, .4) };
        foreach (FakePlane p in rounded) p.Resolution = .001;
        ResizeEngine.Apply(rounded, .4004, .4004);
        Check(rounded[0].Moves == 0 && rounded[1].Moves == 0 && rounded[2].Moves == 0, "No accumulated drift below displayed precision");
        Throws(() => ResizeEngine.Apply(planes, 0, .4), "Reject zero before changing planes");
        Throws(() => ResizeEngine.Apply(planes, Double.NaN, .4), "Reject NaN");
        Throws(() => ResizeEngine.Apply(planes, .4, Double.PositiveInfinity), "Reject infinity");
        Throws(() => ResizeEngine.Apply(new IPlane[0], .4, .4), "Reject missing planes");
        planes[0].IgnoreMoves = true;
        Throws(() => ResizeEngine.Apply(planes, 7, 6), "A failed CAD write must fail readback verification");
    }

    private static SizeDialog NewVisibleDialog()
    {
        var form = new SizeDialog();
        // Render only our own app, off-screen, following this repository's UI
        // test convention. No Solid Edge session is contacted by these tests.
        form.StartPosition = FormStartPosition.Manual;
        form.Location = new Point(-30000, -30000);
        form.Opacity = 0;
        form.Show();
        Application.DoEvents();
        return form;
    }

    private static void Key(SizeDialog form, Keys key)
    {
        typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(form, new object[] { key });
    }

    private static void Dialog(string project)
    {
        using (SizeDialog form = NewVisibleDialog())
        {
            Check(form.XInput.Text == SizeInput.DefaultText && form.YInput.Text == SizeInput.DefaultText, "Both fields default to .40");
            Check(form.AcceptButton == form.ApplyButton && form.CancelButton == form.DismissButton, "Enter/Escape wired to expected buttons");
            Check(form.XInput.SelectionLength == form.XInput.TextLength, "Default X selected for quick replacement");
            Check(form.XInput.TabIndex < form.YInput.TabIndex && form.YInput.TabIndex < form.ApplyButton.TabIndex, "Tab order X, Y, Resize");
            Layout(form);
            Render(form, Path.Combine(project, "Assets", "Popup-Preview.png"));
            Key(form, Keys.Enter);
            Check(form.DialogResult == DialogResult.OK, "Enter accepts defaults");
            Near(form.XValue, .4, "Enter default X"); Near(form.YValue, .4, "Enter default Y");
        }
        using (SizeDialog form = NewVisibleDialog())
        {
            form.XInput.Text = ".65";
            form.YInput.Text = "1.20";
            form.ApplyButton.PerformClick();
            Check(form.DialogResult == DialogResult.OK, "Resize button submits");
            Near(form.XValue, .65, "Custom popup X"); Near(form.YValue, 1.2, "Custom popup Y");
        }
        using (SizeDialog form = NewVisibleDialog())
        {
            form.XInput.Text = "0";
            Key(form, Keys.Enter);
            Check(form.DialogResult == DialogResult.None && form.Visible, "Invalid X keeps dialog open");
            Check(form.ErrorLabel.Text.Contains("X"), "X error identified");
            form.XInput.Text = ".4"; form.YInput.Text = "bad";
            Key(form, Keys.Enter);
            Check(form.DialogResult == DialogResult.None && form.ErrorLabel.Text.Contains("Y"), "Invalid Y kept open");
            form.Scale(new SizeF(1.5f, 1.5f)); Layout(form);
            Render(form, Path.Combine(project, "tests", "bin", "Popup-Scaled.png"));
            Key(form, Keys.Escape);
            Check(form.DialogResult == DialogResult.Cancel, "Escape cancels even invalid input");
        }
        using (SizeDialog form = NewVisibleDialog())
        {
            form.DismissButton.PerformClick();
            Check(form.DialogResult == DialogResult.Cancel, "Cancel button exits");
        }
        using (SizeDialog form = NewVisibleDialog())
        { Check(form.XInput.Text == SizeInput.DefaultText && form.YInput.Text == SizeInput.DefaultText, "Every new invocation resets to .40"); }
    }

    private static void Layout(SizeDialog form)
    {
        form.PerformLayout();
        foreach (Control child in form.Controls)
        {
            Check(form.ClientRectangle.Contains(child.Bounds), "Control stays inside popup: " + child.Name + child.Text);
            if (child is Label && ((Label)child).AutoSize) Check(child.Width >= child.PreferredSize.Width, "Label fits");
        }
    }
    private static void Render(Form form, string path)
    {
        using (Bitmap bitmap = new Bitmap(form.Width, form.Height))
        { form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(path, ImageFormat.Png); }
    }
    private static void Icons(string project)
    {
        foreach (string name in new[] { "1-Three-Planes", "2-Fit-Frame", "3-XY-Size" })
        {
            string path = Path.Combine(project, "Assets", name + ".ico");
            using (BinaryReader reader = new BinaryReader(File.OpenRead(path)))
            {
                Check(reader.ReadUInt16() == 0 && reader.ReadUInt16() == 1, "Valid ICO header");
                Check(reader.ReadUInt16() == 7, "Seven icon resolutions");
                foreach (int size in new[] { 16, 24, 32, 48, 64, 128, 256 })
                {
                    Check(reader.ReadByte() == (size == 256 ? 0 : size), "Correct ICO width");
                    Check(reader.ReadByte() == (size == 256 ? 0 : size), "Correct ICO height");
                    reader.ReadBytes(6);
                    int length = reader.ReadInt32(), offset = reader.ReadInt32();
                    long next = reader.BaseStream.Position;
                    reader.BaseStream.Position = offset;
                    using (MemoryStream png = new MemoryStream(reader.ReadBytes(length)))
                    using (Bitmap frame = new Bitmap(png))
                    {
                        Check(frame.Width == size && frame.Height == size, "Frame decodes at expected size");
                        Check(frame.GetPixel(0, 0).A == 0, "Transparent frame background");
                    }
                    reader.BaseStream.Position = next;
                }
            }
        }
    }

    private sealed class FakePlane : IPlane
    {
        internal double X, Y, Resolution, LastDelta;
        internal int Moves;
        internal bool IgnoreMoves;
        internal FakePlane(double x, double y) { X = x; Y = y; }
        public string Name { get { return "Test plane"; } }
        public AxisReading[] Read() { return new[] { new AxisReading(X, Resolution), new AxisReading(Y, Resolution) }; }
        public void MoveBoundary(bool x, double delta)
        { Moves++; LastDelta = delta; if (!IgnoreMoves) { if (x) X -= 2 * delta; else Y -= 2 * delta; } }
    }
}
