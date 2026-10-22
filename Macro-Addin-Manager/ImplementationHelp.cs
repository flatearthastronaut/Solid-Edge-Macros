using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace CoopManager
{
    // Shared, offline help. The diagrams are drawn from vector primitives at runtime, so both
    // standalone EXEs carry the same instructions without loose assets or an internet dependency.
    internal sealed class ImplementationHelp : Form
    {
        readonly Bitmap illustration;
        readonly Font titleFont=new Font("Segoe UI",18,FontStyle.Bold);
        readonly Font stepFont=new Font("Segoe UI",11,FontStyle.Bold);
        internal ImplementationHelp(string category,string localFolder)
        {
            bool macro=category==Categories.Macros;
            Text="How to Implement — "+category;Size=new Size(960,800);MinimumSize=new Size(780,640);
            StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;Font=new Font("Segoe UI",10);
            BackColor=Color.White;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(18)};
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));Controls.Add(layout);
            layout.Controls.Add(new Label{Text=macro?"Add a macro to the Solid Edge ribbon":"Run a Windows program",Font=titleFont,Dock=DockStyle.Fill},0,0);
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,0,1);
            var body=new TableLayoutPanel{ColumnCount=1,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top,Padding=new Padding(0,0,12,10)};
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroll.Controls.Add(body);
            AddText(body,"Start by using Install / update selected in the user manager. Your installed programs are in:\r\n"+localFolder,false);
            illustration=DrawIllustration(macro);
            body.Controls.Add(new PictureBox{Name="ImplementationIllustration",Image=illustration,SizeMode=PictureBoxSizeMode.Zoom,Dock=DockStyle.Top,Height=280,Margin=new Padding(0,10,0,0)});
            AddText(body,"Illustration — example names and layout; labels can vary by Solid Edge or Windows version.",false);
            if(macro)
            {
                Step(body,"1. Open ribbon customization","Open the Solid Edge document environment where you will use the macro (Part, Assembly, or Draft). Right-click a ribbon command and choose Customize the Ribbon. Select the Ribbon tab.");
                Step(body,"2. Choose the installed macro","Set Choose commands from to Macros. Click Browse and select the macro's .exe in your local install folder. For a ZIP package, open its extracted package folder to find the .exe.");
                Step(body,"3. Add the command","Select the macro in the left list. On the right, select the destination tab and group, then click Add >>. Rename the command or adjust its icon/text if desired. Save or close the Customize dialog.");
                Step(body,"4. Run it — and check after updates","Click the new ribbon command with the document the macro needs open. Repeat customization for other environments as needed. If an update changes the executable's name or folder, remove the old ribbon entry and add the new .exe.");
                AddText(body,"Use the installed local .exe. Keep its supporting files together in the package folder.",false);
                AddText(body,"Reference: Siemens — First Steps to Automation, Part 2; QAT versus the Radial Menu.\r\nhttps://blogs.sw.siemens.com/solidedge/First-Steps-to-Automation-Part-2/",false);
            }
            else
            {
                Step(body,"1. Open the local folder","Press Windows + E to open File Explorer. Go to the Local folder shown in the user manager (shown above), then open the program's package folder if it has one.");
                Step(body,"2. Double-click the program","Find the program's .exe (Type: Application) and double-click it. Windows runs the program directly; you do not need to add it to the Solid Edge ribbon.");
                Step(body,"3. Keep the package together","Run the installed copy, not the ZIP on the network share. Leave DLLs, templates, and other supporting files beside it. Programs that automate Solid Edge may still need Solid Edge running.");
                AddText(body,"After an update, open the newest installed package. If you made a Windows shortcut and its target moved, update the shortcut to the new .exe.",false);
            }
            // Labels wrap to the current viewport width; the whole page scrolls at smaller sizes/DPI.
            body.SizeChanged+=delegate{foreach(Control child in body.Controls)if(child is Label)child.MaximumSize=new Size(Math.Max(100,body.ClientSize.Width-24),0);};
            var close=new Button{Text="Close",DialogResult=DialogResult.OK,Anchor=AnchorStyles.Right,Size=new Size(105,32)};
            layout.Controls.Add(close,0,2);AcceptButton=close;CancelButton=close;
        }
        void AddText(TableLayoutPanel body,string text,bool heading)
        {
            var label=new Label{Text=text,AutoSize=true,MaximumSize=new Size(850,0),Margin=new Padding(0,heading?14:5,0,4),Dock=DockStyle.Top};
            if(heading)label.Font=stepFont;
            body.Controls.Add(label);
        }
        void Step(TableLayoutPanel body,string title,string text){AddText(body,title,true);AddText(body,text,false);}
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(disposing){illustration.Dispose();titleFont.Dispose();stepFont.Dispose();}
        }
        static readonly Color Ink=Color.FromArgb(27,45,65),Blue=Color.FromArgb(25,102,177),Pale=Color.FromArgb(231,242,253);
        static void TextAt(Graphics g,string text,float x,float y,float width,float height,float size,bool bold)
        {
            using(var font=new Font("Segoe UI",size,bold?FontStyle.Bold:FontStyle.Regular))using(var brush=new SolidBrush(Ink))
                g.DrawString(text,font,brush,new RectangleF(x,y,width,height));
        }
        static void Box(Graphics g,Rectangle rect,Color fill)
        {using(var brush=new SolidBrush(fill))g.FillRectangle(brush,rect);using(var pen=new Pen(Color.FromArgb(180,198,216)))g.DrawRectangle(pen,rect);}
        static void Arrow(Graphics g,int x1,int y1,int x2,int y2)
        {using(var pen=new Pen(Blue,4)){pen.EndCap=LineCap.ArrowAnchor;g.DrawLine(pen,x1,y1,x2,y2);}}
        internal static Bitmap DrawIllustration(bool macro)
        {
            var bitmap=new Bitmap(960,290);
            using(var g=Graphics.FromImage(bitmap))
            {
                g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Color.White);
                if(macro)
                {
                    Box(g,new Rectangle(8,8,940,272),Color.FromArgb(247,249,252));
                    Box(g,new Rectangle(8,8,940,38),Pale);TextAt(g,"Solid Edge  /  Customize  /  Ribbon",24,14,880,30,14,true);
                    TextAt(g,"Choose commands from:",28,60,350,25,12,false);
                    Box(g,new Rectangle(28,90,300,34),Color.White);TextAt(g,"Macros                         v",40,94,280,28,12,true);
                    Box(g,new Rectangle(28,140,300,44),Pale);TextAt(g,"MyMacro.exe",42,150,270,30,13,true);
                    Box(g,new Rectangle(28,207,130,36),Color.White);TextAt(g,"Browse...",47,212,110,30,12,true);
                    Box(g,new Rectangle(382,143,136,42),Pale);TextAt(g,"Add >>",412,150,105,30,13,true);
                    Arrow(g,335,164,375,164);Arrow(g,527,164,575,164);
                    TextAt(g,"Select a tab and group:",594,60,330,28,12,false);
                    Box(g,new Rectangle(590,90,330,153),Color.White);
                    TextAt(g,"Home\n    Select",604,100,290,65,13,false);
                    Box(g,new Rectangle(635,167,263,42),Pale);TextAt(g,"MyMacro",651,174,225,30,13,true);
                    TextAt(g,"1  Browse to the local .exe",28,251,340,25,11,true);
                    TextAt(g,"2  Add it to a ribbon group",590,251,340,25,11,true);
                }
                else
                {
                    Box(g,new Rectangle(8,8,650,272),Color.FromArgb(247,249,252));
                    Box(g,new Rectangle(8,8,650,38),Pale);TextAt(g,"File Explorer",24,14,610,30,14,true);
                    Box(g,new Rectangle(24,60,615,35),Color.White);TextAt(g,"Documents  >  Add-ins  >  MyProgram-v2.0",35,65,590,28,12,false);
                    TextAt(g,"Name",35,110,360,25,12,true);TextAt(g,"Type",442,110,170,25,12,true);
                    Box(g,new Rectangle(24,143,615,40),Pale);TextAt(g,"MyProgram.exe",43,150,360,30,13,true);TextAt(g,"Application",442,150,180,30,12,true);
                    TextAt(g,"helper.dll",43,196,350,25,12,false);TextAt(g,"template.txt",43,229,350,25,12,false);
                    Arrow(g,666,163,730,163);
                    Box(g,new Rectangle(741,84,205,151),Color.White);Box(g,new Rectangle(741,84,205,35),Pale);
                    TextAt(g,"My Program",758,90,180,28,12,true);TextAt(g,"Program is\nrunning",763,144,170,70,15,true);
                    TextAt(g,"Double-click the .exe",687,246,270,30,12,true);
                }
            }
            return bitmap;
        }
    }
}
