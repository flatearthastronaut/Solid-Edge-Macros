using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using HoleTableFormatter;

// Approved unified menu: both modes use one shared callout rule engine.
namespace TurboCallout
{
    internal static class Palette
    {
        internal static readonly Color Navy=Color.FromArgb(12,35,67), Teal=Color.FromArgb(0,120,133),
            Ink=Color.FromArgb(30,48,66), Muted=Color.FromArgb(94,111,128), Border=Color.FromArgb(210,221,229),
            Selected=Color.FromArgb(239,249,250), Gold=Color.FromArgb(255,208,47);
        internal static GraphicsPath Round(RectangleF r,float radius)
        {
            var path=new GraphicsPath();float d=radius*2;
            path.AddArc(r.X,r.Y,d,d,180,90);path.AddArc(r.Right-d,r.Y,d,d,270,90);
            path.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);path.AddArc(r.X,r.Bottom-d,d,d,90,90);path.CloseFigure();return path;
        }
        internal static void Text(Graphics g,string text,float size,FontStyle style,Color color,float x,float y)
        {using(var f=new Font("Segoe UI",size,style))using(var b=new SolidBrush(color))g.DrawString(text,f,b,x,y);}
    }

    internal sealed class ChoiceCard : Button
    {
        internal bool Selected, WithCircles, Dual, Units;
        internal string Title, Description;
        internal ChoiceCard()
        {
            SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);
            FlatStyle=FlatStyle.Flat;FlatAppearance.BorderSize=0;BackColor=Color.White;Cursor=Cursors.Hand;
            AccessibleRole=AccessibleRole.RadioButton;TabStop=true;
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.Clear(Parent.BackColor);
            using(var path=Palette.Round(new RectangleF(1,1,Width-3,Height-3),10))
            using(var fill=new SolidBrush(Selected?Palette.Selected:Color.White))
            using(var border=new Pen(Selected?Palette.Teal:Palette.Border,Selected?2:1))
            {g.FillPath(fill,path);g.DrawPath(border,path);}
            float ry=Units?Height/2f:25;
            using(var pen=new Pen(Selected?Palette.Teal:Palette.Muted,1.5f))g.DrawEllipse(pen,18,ry-7,14,14);
            if(Selected)using(var brush=new SolidBrush(Palette.Teal))g.FillEllipse(brush,22,ry-3,6,6);
            Palette.Text(g,Title,Units?10.5f:12,FontStyle.Bold,Palette.Ink,42,Units?14:14);
            if(!Units)
            {
                Diagram(g);
                Palette.Text(g,Description,9.5f,FontStyle.Regular,Palette.Muted,18,183);
                if(WithCircles)Palette.Text(g,"Face-on orthographic views",9,FontStyle.Regular,Palette.Muted,18,203);
            }
            if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(g,new Rectangle(7,7,Width-15,Height-15));
        }
        void Diagram(Graphics g)
        {
            // The rectangular plate distinguishes callouts-only work from the
            // circular pattern. Each illustration shows its quantity wording.
            var state=g.Save();g.TranslateTransform(14,52);
            const float cx=70,cy=67,r=33;
            using(var outline=new Pen(Color.FromArgb(115,136,154),1.35f))
            using(var hole=new Pen(Palette.Navy,1.65f))
            using(var center=new Pen(Palette.Teal,1.1f))
            using(var leader=new Pen(Palette.Teal,1.6f))
            using(var white=new SolidBrush(Color.White))
            {
                if(WithCircles)
                {g.FillEllipse(white,cx-49,cy-49,98,98);g.DrawEllipse(outline,cx-49,cy-49,98,98);}
                else
                {g.FillRectangle(white,cx-49,cy-37,98,74);g.DrawRectangle(outline,cx-49,cy-37,98,74);}
                for(int i=0;i<6;i++)
                {
                    double a=(i*60-30)*Math.PI/180;
                    float x=WithCircles?cx+r*(float)Math.Cos(a):cx+(i%3-1)*26;
                    float y=WithCircles?cy+r*(float)Math.Sin(a):cy+(i/3==0?-18:18);
                    g.DrawEllipse(hole,x-5.5f,y-5.5f,11,11);
                    if(WithCircles)
                    {
                        float ux=(float)Math.Cos(a),uy=(float)Math.Sin(a);
                        g.DrawLine(center,x-10*ux,y-10*uy,x+10*ux,y+10*uy);
                    }
                }
                if(WithCircles)
                {
                    center.DashPattern=new float[]{8,3,1,3};g.DrawEllipse(center,cx-r,cy-r,2*r,2*r);
                    center.DashStyle=DashStyle.Solid;g.DrawLine(center,cx-4,cy,cx+4,cy);g.DrawLine(center,cx,cy-4,cx,cy+4);
                }
                Arrow(g,leader,WithCircles?new PointF(102,47):new PointF(100,45),new PointF(136,16),new PointF(236,16));
                Palette.Text(g,Dual?"Ø6.4 [.25] DRILL":"Ø.25 DRILL",9.3f,FontStyle.Regular,Palette.Teal,138,-2);
                Palette.Text(g,WithCircles?"(6)PLC'S EQ. SP. AS SHOWN":"TYP(6)",8.5f,FontStyle.Regular,Palette.Teal,6,112);
                if(WithCircles)
                {
                    Arrow(g,leader,new PointF(98,85),new PointF(139,94),new PointF(151,94));
                    Palette.Text(g,Dual?"Ø63.5 [2.50] B.C":"Ø2.50 B.C",9.3f,FontStyle.Regular,Palette.Teal,153,85);
                }
            }
            g.Restore(state);
        }
        static void Arrow(Graphics g,Pen p,PointF tip,PointF elbow,PointF end)
        {
            g.DrawLines(p,new[]{tip,elbow,end});double a=Math.Atan2(elbow.Y-tip.Y,elbow.X-tip.X);
            var tail=new PointF(tip.X+7*(float)Math.Cos(a),tip.Y+7*(float)Math.Sin(a));
            using(var b=new SolidBrush(p.Color))g.FillPolygon(b,new[]{tip,
                new PointF(tail.X-2*(float)Math.Sin(a),tail.Y+2*(float)Math.Cos(a)),
                new PointF(tail.X+2*(float)Math.Sin(a),tail.Y-2*(float)Math.Cos(a))});
        }
    }

    internal sealed class MainForm : Form
    {
        readonly ChoiceCard without=new ChoiceCard(),with=new ChoiceCard(),inch=new ChoiceCard(),dual=new ChoiceCard();
        readonly Button run=new Button();
        readonly Image logo;
        readonly Func<bool,bool,Action<string>,RunReport> runner;
        readonly Action<RunReport> reportViewer;
        bool running;
        string statusText="Hole notes and bolt-circle detailing";
        internal bool IncludeBoltCircles {get{return with.Selected;}}
        internal bool UseDual {get{return dual.Selected;}}
        internal MainForm() : this((useDual,circles,progress)=>Automation.Run(useDual,progress,circles),null) {}
        internal MainForm(Func<bool,bool,Action<string>,RunReport> runAction,Action<RunReport> showReport)
        {
            runner=runAction;reportViewer=showReport;
            AutoScaleMode=AutoScaleMode.None;ClientSize=new Size(624,552);BackColor=Color.FromArgb(249,251,253);
            Font=new Font("Segoe UI",10);Text="Turbo Callout v1.5";
            FormBorderStyle=FormBorderStyle.FixedDialog;MaximizeBox=false;MinimizeBox=false;StartPosition=FormStartPosition.CenterScreen;
            using(var s=typeof(MainForm).Assembly.GetManifestResourceStream("TurboCallout.Icon"))Icon=new Icon(s);
            using(var s=typeof(MainForm).Assembly.GetManifestResourceStream("TurboCallout.Image"))using(var loaded=Image.FromStream(s))logo=new Bitmap(loaded);
            without.WithCircles=false;without.Title="Without bolt circles";without.Description="Fill and update hole callouts.";
            with.WithCircles=true;with.Title="With bolt circles";with.Description="Callouts, centerlines, and B.C dimensions.";
            without.SetBounds(24,142,280,232);with.SetBounds(320,142,280,232);
            inch.Units=dual.Units=true;inch.Title="Inch only";dual.Title="Dual  (metric [inch])";
            inch.SetBounds(24,414,280,50);dual.SetBounds(320,414,280,50);
            without.AccessibleName=without.Title;with.AccessibleName=with.Title;inch.AccessibleName=inch.Title;dual.AccessibleName=dual.Title;
            without.Click+=delegate{SelectModes(false,UseDual);};with.Click+=delegate{SelectModes(true,UseDual);};
            inch.Click+=delegate{SelectModes(IncludeBoltCircles,false);};dual.Click+=delegate{SelectModes(IncludeBoltCircles,true);};
            run.Text="Run Turbo Callout";run.SetBounds(414,490,186,42);run.BackColor=Palette.Gold;run.ForeColor=Palette.Navy;
            run.Font=new Font("Segoe UI",10.5f,FontStyle.Bold);run.FlatStyle=FlatStyle.Flat;run.FlatAppearance.BorderSize=0;run.Cursor=Cursors.Hand;
            run.Click+=delegate{RunSelection();};
            var cancel=new Button {Text="Cancel",DialogResult=DialogResult.Cancel,FlatStyle=FlatStyle.Flat,ForeColor=Palette.Muted,BackColor=BackColor};
            cancel.FlatAppearance.BorderSize=0;cancel.SetBounds(24,490,94,42);cancel.Click+=delegate{Close();};CancelButton=cancel;AcceptButton=run;
            Controls.AddRange(new Control[]{without,with,inch,dual,run,cancel});SelectModes(true,false);
            FormClosing+=delegate(object sender,FormClosingEventArgs e){if(running)e.Cancel=true;};
        }
        internal void SelectModes(bool circles,bool useDual)
        {
            if(running)return;
            without.Selected=!circles;with.Selected=circles;inch.Selected=!useDual;dual.Selected=useDual;
            without.Dual=with.Dual=useDual;
            foreach(var c in new[]{without,with,inch,dual})c.Invalidate();
        }
        internal bool ChoicesEnabled {get{return without.Enabled&&with.Enabled&&inch.Enabled&&dual.Enabled&&run.Enabled;}}
        internal void RunSelection()
        {
            if(running)return;
            bool circles=IncludeBoltCircles,useDual=UseDual;RunReport report;
            running=true;UseWaitCursor=true;
            foreach(Control c in Controls)if(c is Button)c.Enabled=false;
            try
            {
                using(new BusyFilter())report=runner(useDual,circles,text=>{statusText=text;Invalidate();Update();});
            }
            catch(Exception error)
            {report=new RunReport{Errors=1,BoltCirclesEnabled=circles};report.Details.Add(error.Message);}
            finally
            {
                running=false;UseWaitCursor=false;
                foreach(Control c in Controls)if(c is Button)c.Enabled=true;
            }
            statusText="Finished: "+report.Filled+" filled, "+report.UpdatedExisting+" updated, "+report.Review+" for review, "+report.Errors+" errors.";
            Invalidate();
            if(reportViewer!=null){reportViewer(report);return;}
            using(var results=new Form())
            {
                results.Text="Turbo Callout results";results.ClientSize=new Size(620,440);results.Font=Font;
                results.StartPosition=FormStartPosition.CenterParent;results.MinimizeBox=false;results.Icon=Icon;
                var text=new TextBox{Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,Text=report.ToString(),
                    BackColor=Color.White,BorderStyle=BorderStyle.FixedSingle};text.SetBounds(20,20,580,350);
                text.Anchor=AnchorStyles.Top|AnchorStyles.Bottom|AnchorStyles.Left|AnchorStyles.Right;
                var close=new Button{Text="Close",DialogResult=DialogResult.OK};close.SetBounds(490,386,110,36);
                close.Anchor=AnchorStyles.Bottom|AnchorStyles.Right;results.Controls.AddRange(new Control[]{text,close});
                results.AcceptButton=close;results.CancelButton=close;results.ShowDialog(this);
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);Graphics g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;
            using(var b=new SolidBrush(Palette.Navy))g.FillRectangle(b,0,0,ClientSize.Width,92);
            g.DrawImage(logo,24,20,52,52);
            Palette.Text(g,"Turbo Callout",21,FontStyle.Bold,Color.White,90,17);
            using(var statusFont=new Font("Segoe UI",10))TextRenderer.DrawText(g,statusText,statusFont,new Rectangle(92,55,500,25),Color.FromArgb(199,213,228),TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
            Palette.Text(g,"What would you like to add?",11,FontStyle.Bold,Palette.Ink,24,111);
            Palette.Text(g,"Dimension format",11,FontStyle.Bold,Palette.Ink,24,386);
            using(var p=new Pen(Palette.Border))g.DrawLine(p,24,480,600,480);
        }
        protected override void Dispose(bool disposing){if(disposing)logo.Dispose();base.Dispose(disposing);}
    }

}
