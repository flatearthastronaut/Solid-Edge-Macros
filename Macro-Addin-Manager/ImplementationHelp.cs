using System;
using System.Drawing;
using System.Windows.Forms;

namespace CoopManager
{
    // Both editions embed the same approved artwork and use this single offline guide.
    // PNG resources keep the standalone application independent of SVG renderers.
    internal sealed class ImplementationHelp : Form
    {
        readonly Bitmap illustration;
        readonly Font titleFont=new Font("Segoe UI",18,FontStyle.Bold);
        readonly Font stepFont=new Font("Segoe UI",11,FontStyle.Bold);
        internal ImplementationHelp(string category,string localFolder)
        {
            bool macro=category==Categories.Macros;
            Text="How to Implement — "+category;Size=new Size(1180,900);MinimumSize=new Size(780,640);
            StartPosition=FormStartPosition.CenterParent;ShowInTaskbar=false;Font=new Font("Segoe UI",10);
            BackColor=Color.White;
            var layout=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=3,Padding=new Padding(18)};
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute,48));layout.RowStyles.Add(new RowStyle(SizeType.Percent,100));layout.RowStyles.Add(new RowStyle(SizeType.Absolute,44));Controls.Add(layout);
            layout.Controls.Add(new Label{Text=macro?"Add a macro to the Solid Edge ribbon":"Run a Windows program",Font=titleFont,Dock=DockStyle.Fill},0,0);
            var scroll=new Panel{Dock=DockStyle.Fill,AutoScroll=true};layout.Controls.Add(scroll,0,1);
            var body=new TableLayoutPanel{ColumnCount=1,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,Dock=DockStyle.Top,Padding=new Padding(0,0,12,10)};
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));scroll.Controls.Add(body);
            AddText(body,"Start by using Install / update selected in the user manager. Your installed programs are in:\r\n"+localFolder,false);
            if(macro)
            {
                AddText(body,"Choose the stable ribbon target — no version number.",true);
                AddText(body,@"Use the Stable ribbon target shown in the manager's details. Example: User\Documents\Add-ins\MyMacro\MyMacro.exe"+"\r\nThe manager refreshes this copy when you update. Your ribbon button keeps using the same path.",false);
            }
            illustration=LoadIllustration(macro);
            var picture=new PictureBox{Name="ImplementationIllustration",Image=illustration,SizeMode=PictureBoxSizeMode.Zoom,Dock=DockStyle.Top,Margin=new Padding(0,10,0,0)};
            body.Controls.Add(picture);
            AddText(body,macro?"Solid Edge 2019 Draft example supplied by the user. Names, folders and ribbon labels may differ in your installation.":"Windows 10 File Explorer illustration with example files. Your folder contents may differ.",false);
            if(macro)
            {
                // Numbering follows the red annotations in the supplied screenshot.
                Step(body,"1. Customize the ribbon","Open the document environment where you want the command (Draft, Part or Assembly). Right-click the ribbon, choose Customize the Ribbon, and select the Ribbon tab.");
                Step(body,"2. Choose Macros","Set Choose commands from to Macros, as circled in the picture.");
                Step(body,"3. Browse to the folder","Click Browse and go to the folder containing your Stable ribbon target. For ZIP or folder packages, use the unversioned package folder too.");
                Step(body,"4. Select and add","Select the unversioned .exe in the left list. Choose the destination tab and group on the right, click Add >>, then save your customization.");
                Step(body,"After updates","Keep using the same ribbon button. The manager refreshes the stable copy, keeps the latest numbered release for reference, and removes older releases. Open the document the macro needs before running it.");
                AddText(body,"One-time setup for existing buttons: if your old ribbon entry points to a versioned filename or folder, replace it with the Stable ribbon target. Buttons already using that unversioned path stay valid. Keep supporting files with the EXE.",false);
                AddText(body,"Reference: Siemens — First Steps to Automation, Part 2.\r\nhttps://blogs.sw.siemens.com/solidedge/First-Steps-to-Automation-Part-2/",false);
            }
            else
            {
                Step(body,"1. Open your local Add-ins folder",@"Press Windows + E to open File Explorer. Go to the Local folder shown above (example: User\Documents\Add-ins\), then open the program's folder if present.");
                Step(body,"2. Show file extensions","In Windows 10 File Explorer, select the View tab and check File name extensions to show .exe at the end of application filenames.");
                Step(body,"3. Run the .exe","Find the program's .exe (Type: Application) and double-click it. Windows runs the program directly; you do not need to add it to the Solid Edge ribbon.");
                Step(body,"Keep the package together","Run the installed copy, not the ZIP on the network share. Leave DLLs, templates and other supporting files beside it. Programs that automate Solid Edge may still need Solid Edge running.");
                AddText(body,"After an update, open the newest installed package. If you made a Windows shortcut and its target moved, update the shortcut to the new .exe.",false);
                AddText(body,"Reference: Microsoft — Common file name extensions in Windows.\r\nhttps://support.microsoft.com/en-us/windows/experience/storage-filemanagement/common-file-name-extensions-in-windows",false);
            }
            // Preserve the full image aspect ratio instead of shrinking a detailed screenshot
            // into a fixed-height banner. The page scrolls vertically at smaller sizes/DPI.
            EventHandler resizePicture=delegate{
                int height=Math.Max(1,(int)Math.Round(picture.ClientSize.Width*(double)illustration.Height/illustration.Width));
                if(picture.Height!=height)picture.Height=height;
            };
            picture.SizeChanged+=resizePicture;
            // Adding the picture can complete its first width layout before the handler is
            // attached; initialize explicitly as well as following subsequent resizes.
            resizePicture(picture,EventArgs.Empty);
            body.SizeChanged+=delegate{foreach(Control child in body.Controls)if(child is Label)child.MaximumSize=new Size(Math.Max(100,body.ClientSize.Width-24),0);};
            var close=new Button{Text="Close",DialogResult=DialogResult.OK,Anchor=AnchorStyles.Right,Size=new Size(105,32)};
            layout.Controls.Add(close,0,2);AcceptButton=close;CancelButton=close;
        }
        void AddText(TableLayoutPanel body,string text,bool heading)
        {
            var label=new Label{Text=text,AutoSize=true,MaximumSize=new Size(1100,0),Margin=new Padding(0,heading?14:5,0,4),Dock=DockStyle.Top};
            if(heading)label.Font=stepFont;
            body.Controls.Add(label);
        }
        void Step(TableLayoutPanel body,string title,string text){AddText(body,title,true);AddText(body,text,false);}
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if(disposing){if(illustration!=null)illustration.Dispose();titleFont.Dispose();stepFont.Dispose();}
        }
        internal static Bitmap LoadIllustration(bool macro)
        {
            string name=macro?"MacroRibbonHelp.png":"WindowsExplorerHelp.png";
            using(var stream=typeof(ImplementationHelp).Assembly.GetManifestResourceStream(name))
            {
                if(stream==null)throw new InvalidOperationException("Missing embedded help illustration: "+name);
                // Detach the bitmap from its resource stream before closing that stream.
                using(var original=Image.FromStream(stream))return new Bitmap(original);
            }
        }
    }
}
