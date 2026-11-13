using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TurboCallout;

internal static class MenuTests
{
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    [STAThread]static int Main()
    {
        try
        {
            Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);
            MainForm form=null;RunReport displayed=null;bool calledDual=false,calledCircles=false,fail=false;int calls=0;
            using(form=new MainForm((dual,circles,progress)=>
            {
                calls++;calledDual=dual;calledCircles=circles;
                Check(!form.ChoicesEnabled,"Choices disabled during automation");
                form.SelectModes(!circles,!dual);
                Check(form.UseDual==dual&&form.IncludeBoltCircles==circles,"Selection cannot change during a run");
                progress("Checking callout...");
                if(fail)throw new InvalidOperationException("Test failure");
                return new RunReport{Filled=2,BoltCirclesEnabled=circles};
            },r=>displayed=r))
            {
                form.ShowInTaskbar=false;form.StartPosition=FormStartPosition.Manual;form.Location=new Point(-32000,-32000);
                form.Show();Application.DoEvents();Directory.CreateDirectory("Inspection/Menu");
                using(var bitmap=new Bitmap(form.Width,form.Height))
                {form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save("Inspection/Menu/Unified-menu.png");}
                foreach(bool circles in new[]{false,true})foreach(bool dual in new[]{false,true})
                {
                    form.Controls.OfType<ChoiceCard>().Single(c=>!c.Units&&c.WithCircles==circles).PerformClick();
                    form.Controls.OfType<ChoiceCard>().Single(c=>c.Units&&c.Title.StartsWith(dual?"Dual":"Inch")).PerformClick();
                    Check(form.IncludeBoltCircles==circles&&form.UseDual==dual,"Card clicks select both mode and units");
                    using(var bitmap=new Bitmap(form.Width,form.Height))
                    {form.DrawToBitmap(bitmap,new Rectangle(Point.Empty,form.Size));bitmap.Save("Inspection/Menu/"+(circles?"With-":"Without-")+(dual?"dual":"inch")+".png");}
                    form.Controls.OfType<Button>().Single(b=>b.Text=="Run Turbo Callout").PerformClick();
                    Check(calledCircles==circles&&calledDual==dual&&displayed.Filled==2,"Run forwards exactly the selected options");
                    Check(form.ChoicesEnabled,"Choices enabled after success");
                    Check(displayed.ToString().Contains("Bolt circles:")==circles,"Report matches the selected mode");
                }
                fail=true;form.RunSelection();
                Check(form.ChoicesEnabled&&displayed.Errors==1&&displayed.Details.Contains("Test failure"),"Failure reported and menu recovers");
                Check(calls==5,"One automation invocation per click");
            }
            Console.WriteLine(checks+" menu checks passed.");return 0;
        }
        catch(Exception e){Console.WriteLine(e);return 1;}
    }
}
