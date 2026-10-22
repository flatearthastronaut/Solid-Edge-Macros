using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CoopManager;

internal static partial class Tests
{
    static void ImplementationHelpCases()
    {
        Case("implementation-help",(s,l)=>{
            foreach(bool admin in new[]{false,true})using(var manager=new ManagerForm(Path.Combine(l,"settings.xml"),false,admin))
            {
                var buttons=Descendants(manager).OfType<Button>().Where(b=>b.Name=="ImplementationHelp").ToList();
                Assert(buttons.Count==2 && buttons.All(b=>b.Text=="How to Implement"),"Both editions expose the two implementation guides");
                Assert(buttons.Select(b=>(string)b.Tag).SequenceEqual(new[]{Categories.Macros,Categories.Windows}),"Help buttons attached to correct category frames");
            }
            foreach(string category in new[]{Categories.Macros,Categories.Windows})using(var guide=new ImplementationHelp(category,l))
            {
                guide.Opacity=0;guide.Show();Application.DoEvents();
                var text=string.Join("\n",Descendants(guide).OfType<Label>().Select(c=>c.Text));
                Assert(text.Contains(l),"Guide identifies actual local install folder");
                var picture=Descendants(guide).OfType<PictureBox>().Single();
                Assert(picture.Image!=null && picture.Image.Width==960,"Guide includes an offline illustration");
                if(category==Categories.Macros)
                    Assert(text.Contains("Customize the Ribbon") && text.Contains("Macros") && text.Contains("Add >>") && text.Contains("unversioned .exe") && text.Contains("same ribbon button") && text.Contains("One-time setup"),"Macro guide covers stable ribbon target and one-time migration");
                else Assert(text.Contains("Windows + E") && text.Contains("double-click") && text.Contains("Application"),"Windows guide covers Explorer and running the EXE");
                using(var bitmap=new Bitmap(guide.Width,guide.Height))
                {guide.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine("tests","bin",category==Categories.Macros?"macro-help-preview.png":"windows-help-preview.png"));}
                guide.Size=guide.MinimumSize;Application.DoEvents();
                Assert(Descendants(guide).OfType<Panel>().Any(p=>p.AutoScroll && p.VerticalScroll.Visible),"Guide scrolls instead of clipping instructions in a small window");
            }
        });
    }
}
