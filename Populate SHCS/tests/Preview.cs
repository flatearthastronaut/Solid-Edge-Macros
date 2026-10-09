using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
class Preview
{
    [STAThread]static void Main()
    {
        Application.EnableVisualStyles();
        var assembly=Assembly.LoadFrom(Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","Compiled Executables","PopulateSHCS-v1.0.exe")));
        using(var form=(Form)Activator.CreateInstance(assembly.GetType("PopulateSHCS.MainWindow"))){
            form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-10000,-10000);form.ShowInTaskbar=false;form.Show();Application.DoEvents();form.PerformLayout();
            using(var bitmap=new Bitmap(form.Width,form.Height)){form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"artifacts"));bitmap.Save(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"artifacts","preview.png"));}
        }
    }
}
