using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
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
        foreach(XElement table in xml.Elements("Table"))scan.Tables.Add(new TableInfo{Index=(int)table.Attribute("index"),TitleText=(string)table.Element("Title")});
        foreach(XElement row in xml.Elements("Row"))
            scan.Rows.Add(new RowPlan{TableIndex=(int)row.Attribute("table"),RowIndex=(int)row.Attribute("index"),Label=(string)row.Attribute("label"),
                Status=(string)row.Attribute("status"),Category=(string)row.Attribute("category"),Before=(string)row.Element("Before"),After=(string)row.Element("After"),
                Assignment=(string)row.Element("Assignment"),SeparateLinked=(bool?)row.Attribute("separateLinked")??false,
                Reason=(string)row.Element("Reason"),Eligible=(string)row.Attribute("status")=="Ready",Selected=(string)row.Attribute("status")=="Ready",
                Overridden=(string)row.Attribute("status")=="Custom row preserved",Decision=new Decision{Slot=(int)row.Attribute("slot")}});
        foreach(TableInfo table in scan.Tables)
        using(var form=new TableForm(scan,table,index=>0))
        {
            // Render this test process offscreen; do not drive an external app.
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);
            form.Show();form.PerformLayout();Application.DoEvents();
            using(var bitmap=new Bitmap(form.Width,form.Height))
            {form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(args[1],"table-"+table.Index+"-preview.png"),ImageFormat.Png);}
        }
    }
}
