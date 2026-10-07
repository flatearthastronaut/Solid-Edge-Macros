using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
namespace BoltCircleHoles
{
    // Small scalable section views. Draw on demand and dispose pens immediately;
    // no image files or persistent GDI handles are needed by the portable macro.
    public sealed class HoleSymbol : Control
    {
        string kind="Counterbore";
        public string Kind {get{return kind;} set{kind=value;AccessibleName=value+" hole section";Invalidate();}}
        public HoleSymbol(){DoubleBuffered=true;TabStop=false;Size=new Size(42,28);}
        protected override void OnPaint(PaintEventArgs e)
        {
            // Draw in a fixed 42x28 coordinate space and scale to the current control.
            // These are explanatory section symbols, not dimensioned previews: the
            // chosen chart values appear in the text summary and drive the model itself.
            base.OnPaint(e);
            e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
            e.Graphics.ScaleTransform(Width/42f,Height/28f);
            using(var edge=new Pen(kind=="Thread" ? Color.SlateGray : Color.FromArgb(42,88,123),1.5f))
            using(var axis=new Pen(Color.LightSlateGray,0.7f))
            {
                axis.DashStyle=DashStyle.Dash;
                e.Graphics.DrawLine(axis,21,0,21,28);
                e.Graphics.DrawLine(edge,2,3,9,3);e.Graphics.DrawLine(edge,33,3,40,3);
                if(kind=="Counterbore")
                {
                    e.Graphics.DrawLines(edge,new[]{new Point(9,3),new Point(9,13),new Point(15,13),new Point(15,26)});
                    e.Graphics.DrawLines(edge,new[]{new Point(33,3),new Point(33,13),new Point(27,13),new Point(27,26)});
                }
                else
                {
                    // The 120-degree included angle gives a shallow drill point.
                    e.Graphics.DrawLines(edge,new[]{new Point(9,3),new Point(9,18),new Point(21,25),new Point(33,18),new Point(33,3)});
                    if(kind=="Thread")for(int y=6;y<=15;y+=4)
                    {
                        e.Graphics.DrawLine(edge,7,y+2,12,y);
                        e.Graphics.DrawLine(edge,30,y+2,35,y);
                    }
                }
            }
        }
    }
}
