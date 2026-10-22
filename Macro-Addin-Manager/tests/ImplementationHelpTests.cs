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
                manager.Opacity=0;manager.Show();Application.DoEvents();
                var buttons=Descendants(manager).OfType<Button>().Where(b=>b.Name=="ImplementationHelp").ToList();
                Assert(buttons.Count==2 && buttons.All(b=>b.Text=="How to Implement"),"Both editions expose the two implementation guides");
                Assert(buttons.Select(b=>(string)b.Tag).SequenceEqual(new[]{Categories.Macros,Categories.Windows}),"Help buttons attached to correct category frames");
                foreach(var button in buttons)
                {
                    var frame=(GroupBox)button.Parent.Parent;
                    var grid=frame.Controls.OfType<DataGridView>().Single();
                    Assert(button.Dock==DockStyle.Left && button.Left==0 && button.Parent.Dock==DockStyle.Top && button.Parent.Top<grid.Top,"Help stays left beneath the frame title and above its table");
                }
                using(var bitmap=new Bitmap(manager.Width,manager.Height))
                {manager.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine("tests","bin",admin?"admin-help-placement.png":"user-help-placement.png"));}
            }
            foreach(string category in new[]{Categories.Macros,Categories.Windows})using(var guide=new ImplementationHelp(category,l))
            {
                guide.Opacity=0;guide.Show();Application.DoEvents();
                var text=string.Join("\n",Descendants(guide).OfType<Label>().Select(c=>c.Text));
                Assert(text.Contains(l),"Guide identifies actual local install folder");
                var picture=Descendants(guide).OfType<PictureBox>().Single();
                Assert(picture.Image!=null && picture.Image.Width==(category==Categories.Macros?1225:1120),"Guide includes the approved embedded illustration");
                Assert(Math.Abs(picture.Height-picture.Width*(double)picture.Image.Height/picture.Image.Width)<=1,"Detailed picture retains its aspect ratio at normal size");
                Assert(text.Contains(@"User\Documents\Add-ins\"),"Guides use the approved example path");
                if(category==Categories.Macros)
                    Assert(text.Contains("Customize the Ribbon") && text.Contains("Macros") && text.Contains("Add >>") && text.Contains("unversioned .exe") && text.Contains("same ribbon button") && text.Contains("One-time setup"),"Macro guide covers stable ribbon target and one-time migration");
                else Assert(text.Contains("Windows + E") && text.Contains("double-click") && text.Contains("Application") && text.Contains("View tab") && text.Contains("File name extensions"),"Windows guide covers Explorer, extensions and running the EXE");
                using(var bitmap=new Bitmap(guide.Width,guide.Height))
                {guide.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine("tests","bin",category==Categories.Macros?"macro-help-preview.png":"windows-help-preview.png"));}
                guide.Size=guide.MinimumSize;Application.DoEvents();
                Assert(Math.Abs(picture.Height-picture.Width*(double)picture.Image.Height/picture.Image.Width)<=1,"Detailed picture retains its aspect ratio after resize");
                Assert(Descendants(guide).OfType<Panel>().Any(p=>p.AutoScroll && p.VerticalScroll.Visible),"Guide scrolls instead of clipping instructions in a small window");
                Assert(!Descendants(guide).OfType<Panel>().Any(p=>p.AutoScroll && p.HorizontalScroll.Visible),"Instructions wrap without horizontal scrolling");
            }
        });
    }
}
