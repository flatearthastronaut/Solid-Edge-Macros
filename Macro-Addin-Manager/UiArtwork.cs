using System;
using System.Drawing;
using System.Runtime.InteropServices;
namespace CoopManager
{
    internal static class UiArtwork
    {
        [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
        struct StockIcon { public uint Size; public IntPtr Handle; public int ImageIndex,IconIndex; [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Path; }
        [DllImport("shell32.dll",CharSet=CharSet.Unicode)]static extern int SHGetStockIconInfo(uint id,uint flags,ref StockIcon info);
        [DllImport("user32.dll")]static extern bool DestroyIcon(IntPtr handle);
        // Render simple UI symbols as bitmaps, avoiding missing-font/emoji glyphs on Windows buttons.
        public static Bitmap Lock(bool locked)
        {
            var bitmap=new Bitmap(26,26);
            using(var g=Graphics.FromImage(bitmap))
            using(var pen=new Pen(locked?Color.FromArgb(35,65,95):Color.FromArgb(25,125,80),2.5f))
            using(var body=new SolidBrush(pen.Color))
            {
                g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                if(locked){g.DrawArc(pen,7,3,12,14,180,180);g.DrawLine(pen,7,10,7,13);g.DrawLine(pen,19,10,19,13);}
                else{g.DrawArc(pen,10,1,12,14,180,180);g.DrawLine(pen,10,8,10,13);g.DrawLine(pen,22,8,22,10);}
                g.FillRectangle(body,5,12,17,12);g.FillEllipse(Brushes.White,11,15,5,5);g.FillRectangle(Brushes.White,12,18,3,4);
            }
            return bitmap;
        }
        public static Bitmap Folder(bool network)
        {
            var folder=Stock(3);
            if(network)using(var badge=Stock(9))using(var graphics=Graphics.FromImage(folder))graphics.DrawImage(badge,new Rectangle(15,15,17,17));
            return folder;
        }
        static Bitmap Stock(uint id)
        {
            var info=new StockIcon{Size=(uint)Marshal.SizeOf(typeof(StockIcon))};
            try
            {
                // Windows stock network-drive and folder artwork; no mapped-drive access is needed.
                int result=SHGetStockIconInfo(id,0x100,ref info);
                if(result==0 && info.Handle!=IntPtr.Zero)using(var icon=Icon.FromHandle(info.Handle))using(var image=icon.ToBitmap())return new Bitmap(image,32,32);
            }
            finally{if(info.Handle!=IntPtr.Zero)DestroyIcon(info.Handle);}
            using(var fallback=SystemIcons.Application.ToBitmap())return new Bitmap(fallback,32,32);
        }
    }
}
