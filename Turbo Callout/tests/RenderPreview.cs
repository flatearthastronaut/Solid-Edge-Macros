using System;
using System.Drawing;
using System.Windows.Forms;
using TurboCallout;
internal static class RenderPreview
{
    [STAThread] static void Main()
    {
        Application.EnableVisualStyles();
        using(var form=new MainForm())
        {
            form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);
            form.Show();Application.DoEvents();
            using(var bitmap=new Bitmap(form.Width,form.Height))
            {form.DrawToBitmap(bitmap,new Rectangle(0,0,form.Width,form.Height));bitmap.Save("Inspection\\window.png");}
        }
    }
}
