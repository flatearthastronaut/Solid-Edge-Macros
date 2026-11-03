using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using CoopManager;

internal static partial class Tests
{
    static void FrameLayoutCases()
    {
        Case("category-frame-layout",(s,l)=>{
            foreach(bool admin in new[]{false,true})using(var manager=new ManagerForm(Path.Combine(l,"layout.xml"),false,admin))
            {
                manager.ShowInTaskbar=false;manager.Opacity=0;manager.Show();Application.DoEvents();
                var entries=Enumerable.Range(1,40).Select(i=>new Entry{Name="Macro "+i.ToString("00"),Category=Categories.Macros,Status="Published"}).Concat(new[]{
                    new Entry{Name="Shared SE add-ins",Category=Categories.Addins,Status="Shared only"},
                    new Entry{Name="SE Macro Manager",Category=Categories.Windows,Status="Published"},
                    new Entry{Name="SolidEdgeConvert",Category=Categories.Windows,Status="Published"}
                }).ToList();
                manager.PopulateEntries(entries);Application.DoEvents();
                var tables=Descendants(manager).OfType<DataGridView>().ToDictionary(g=>g.Parent.Text);
                var addins=tables[Categories.Addins];var windows=tables[Categories.Windows];var macros=tables[Categories.Macros];
                Assert(addins.DisplayedRowCount(false)==1,"Default layout shows the complete add-in row with many macros");
                Assert(windows.DisplayedRowCount(false)==2,"Default layout shows two complete Windows program rows including its help bar");
                int macroHeight=macros.Height,addinHeight=addins.Height,windowsHeight=windows.Height;
                using(var bitmap=new Bitmap(manager.Width,manager.Height))
                {manager.DrawToBitmap(bitmap,new Rectangle(Point.Empty,bitmap.Size));bitmap.Save(Path.Combine("tests","bin",admin?"admin-frame-layout.png":"user-frame-layout.png"));}
                manager.PopulateEntries(new Entry[0]);manager.PopulateEntries(entries);Application.DoEvents();
                Assert(addins.Height==addinHeight && windows.Height==windowsHeight,"Catalog repopulation keeps reserved frame heights");
                manager.Size=manager.MinimumSize;Application.DoEvents();
                Assert(addins.DisplayedRowCount(false)==1 && windows.DisplayedRowCount(false)==2,"Minimum window still shows the reserved complete rows");
                manager.Size=new Size(1240,950);Application.DoEvents();
                Assert(macros.Height>macroHeight && addins.Height==addinHeight && windows.Height==windowsHeight,"Growing the window gives additional space to macros");
                manager.Size=new Size(1140,850);
                entries.Add(new Entry{Name="Another add-in",Category=Categories.Addins});
                entries.Add(new Entry{Name="Another program",Category=Categories.Windows});
                manager.PopulateEntries(entries);Application.DoEvents();
                Assert(addins.DisplayedRowCount(false)==1 && windows.DisplayedRowCount(false)==2,"Additional lower-category entries scroll without shrinking row slots");
                windows.Columns["Package"].Width+=500;Application.DoEvents();
                Assert(windows.DisplayedRowCount(false)==2,"Horizontal scrollbar does not consume a required Windows row");
            }
        });
    }
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
