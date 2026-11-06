using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using HoleTableFormatter;

internal static class RenderPreview
{
    [STAThread] static void Main(string[] args)
    {
        Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
        XElement xml=XDocument.Load(args[0]).Root;
        var scan=new ScanResult{Document=(string)xml.Attribute("document"),TableCount=(int)xml.Attribute("tables")};
        foreach(XElement row in xml.Elements("Row"))
            scan.Rows.Add(new RowPlan{TableIndex=(int)row.Attribute("table"),RowIndex=(int)row.Attribute("index"),Label=(string)row.Attribute("label"),
                Status=(string)row.Attribute("status"),Category=(string)row.Attribute("category"),Before=(string)row.Element("Before"),After=(string)row.Element("After"),
                Reason=(string)row.Element("Reason"),Eligible=(string)row.Attribute("status")=="Ready",Selected=(string)row.Attribute("status")=="Ready",
                Overridden=(string)row.Attribute("status")=="Custom row preserved",Decision=new Decision{Slot=(int)row.Attribute("slot")}});
        using(var form=new MainForm())
        {
            // Exercise layout in this test process; no external app UI is driven.
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);
            form.Show();Application.DoEvents();
            typeof(MainForm).GetField("result",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(form,scan);
            typeof(MainForm).GetMethod("Fill",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(form,null);
            var summary=(Label)typeof(MainForm).GetField("summary",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);
            summary.Text="19947-01 tombstone.dft — 6 tables, 255 rows. 12 ready; 20 need review; 100 custom rows preserved.";
            var apply=(Button)typeof(MainForm).GetField("applyButton",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(form);apply.Enabled=true;
            form.PerformLayout();Application.DoEvents();
            using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(args[1],ImageFormat.Png);}
        }
    }
}
