using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Hammill.Automation;
using Hammill.AddSketch;
internal static class InspectPlanes
{
    [STAThread]static int Main()
    {
        using(new OleMessageFilter())using(var scope=new ComScope())try
        {
            dynamic app=scope.Own(Marshal.GetActiveObject("SolidEdge.Application"));
            dynamic doc=scope.Own(app.ActiveDocument);
            Console.WriteLine("Active document: "+doc.Name+" (type "+doc.Type+")");
            dynamic layouts=scope.Own(doc.Layouts);int before=(int)layouts.Count;bool delay=(bool)app.DelayCompute;
            var scan=SketchEngine.Planes((object)doc);
            Console.WriteLine(scan.Choices.Count+" readable named planes; "+scan.Notices.Count+" skipped entries.");
            foreach(var plane in scan.Choices)Console.WriteLine(plane.Index+": "+plane.Name);
            if(scan.Choices.Count==0)throw new Exception("No usable planes in the active assembly.");
            Application.EnableVisualStyles();
            using(var form=new SketchForm((object)app,(object)doc))
            {
                form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);form.Show();Application.DoEvents();
                var choices=form.Controls[0].Controls.OfType<ComboBox>().Single();
                if(choices.Items.Count!=scan.Choices.Count || choices.SelectedItem==null)throw new Exception("Live UI did not load its native plane choices.");
                string work=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..",".work"));Directory.CreateDirectory(work);
                using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save(Path.Combine(work,"active-planes.png"));}
                form.Hide();Console.WriteLine("PASS live window loaded planes; preview: "+Path.Combine(work,"active-planes.png"));
            }
            if((int)layouts.Count!=before || (bool)app.DelayCompute!=delay)throw new Exception("Read-only scan changed assembly state.");
            Console.WriteLine("PASS read-only scan: layout count and compute state unchanged; nothing created or saved.");
            return 0;
        }
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
