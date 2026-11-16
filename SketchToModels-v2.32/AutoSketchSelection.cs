using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace SketchToModels
{
    /// <summary>
    /// Optional graphical source pick AFTER Project to Sketch/Wireframe Chain
    /// is active. Preselection in v2.33 was ignored. Direct IncludeEdge tests
    /// created geometry but failed the source-update check, so production uses
    /// the same native graphical command as the operator. It never accepts the
    /// projection or finishes the feature. The operator must review the preview.
    /// </summary>
    public sealed class AutoSketchSelection
    {
        readonly int sourceIndex;
        readonly IOutlinePicker picker;
        bool attempted;
        public bool Enabled { get; private set; }
        public string Message { get; private set; }
        public AutoSketchSelection(int copiedSketchIndex,bool enabled)
            : this(copiedSketchIndex,enabled,new NativeOutlinePicker()) {}
        public AutoSketchSelection(int copiedSketchIndex,bool enabled,IOutlinePicker outlinePicker)
        {sourceIndex=copiedSketchIndex;Enabled=enabled;picker=outlinePicker;Message="";}

        // Keep the launch hook separate from the pick: changing SelectSet before
        // StartCommand does not simulate the edge click required by this command.
        public void Prepare(dynamic part,int expectedSketchCount)
        {if(Enabled)Message="Waiting for Project to Sketch / Wireframe Chain before picking the outline.";}

        public void PickAfterChain(dynamic part,int expectedSketchCount)
        {
            if(!Enabled || attempted)return;
            attempted=true;
            try {
                if((int)part.Application.GetActiveCommand()!=50011)
                    throw new InvalidOperationException("Project to Sketch is not active.");
                if(sourceIndex<1 || sourceIndex>expectedSketchCount || (int)part.Sketches.Count!=expectedSketchCount)
                    throw new InvalidOperationException("The copied source sketch cannot be identified safely.");
                dynamic source=part.Sketches.Item(sourceIndex);
                dynamic target=part.ActiveSketch;
                if((int)source.Type!=1689979564 || target==null || (int)target.Type!=1584866912 || (int)target.Parent.Type!=-1521484909)
                    throw new InvalidOperationException("Enter the revolve feature sketch, not the source sketch.");
                picker.Pick((object)part,(object)source.Profile);
                Message="Clicked the copied outline with Wireframe Chain. Check the highlighted preview and accept. If no chain is selected, use Select manually.";
            }
            catch(Exception error) {
                Enabled=false;
                Message="Automatic pick unavailable: "+error.Message+" Select the outline manually.";
            }
        }
        public void UseManual(dynamic part)
        {
            Enabled=false;attempted=true;
            Message="Manual mode: pick the original outline and accept.";
            part.SelectSet.RemoveAll();
        }
    }
    public interface IOutlinePicker {void Pick(object document,object sourceProfile);}

    /// <summary>
    /// Finds a real point on a non-construction source curve and converts its
    /// model coordinates through the CURRENT view. No hardcoded screen points,
    /// keyboard shortcuts, offset geometry or background CAD COM calls are used.
    /// Only one verified drawing-window click is sent, after the native dropdown
    /// worker has finished. The instruction form is hidden during this call.
    /// </summary>
    public sealed class NativeOutlinePicker : IOutlinePicker
    {
        [StructLayout(LayoutKind.Sequential)] public struct Point {public int X,Y;}
        [StructLayout(LayoutKind.Sequential)] struct Rect {public int Left,Top,Right,Bottom;}
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr hwnd,out Rect rect);
        [DllImport("user32.dll")] static extern bool ClientToScreen(IntPtr hwnd,ref Point point);
        [DllImport("user32.dll")] static extern IntPtr WindowFromPoint(Point point);
        [DllImport("user32.dll")] static extern bool IsChild(IntPtr parent,IntPtr child);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr hwnd);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint pid);
        [DllImport("user32.dll")] static extern bool SetCursorPos(int x,int y);
        [DllImport("user32.dll")] static extern bool GetCursorPos(out Point point);
        [DllImport("user32.dll")] static extern IntPtr SetThreadDpiAwarenessContext(IntPtr context);
        [DllImport("user32.dll")] static extern IntPtr SendMessageTimeout(IntPtr hwnd,uint message,IntPtr w,IntPtr l,uint flags,uint timeout,out UIntPtr result);

        public void Pick(object document,object sourceProfile)
        {
            // Solid Edge's device coordinates are physical pixels. Temporarily
            // use the same units for Win32 hit-testing on scaled monitors, then
            // restore WinForms' prior DPI context before showing instructions.
            IntPtr previous=SetThreadDpiAwarenessContext(new IntPtr(-4));
            if(previous==IntPtr.Zero)throw new InvalidOperationException("Cannot establish physical-pixel coordinates for the graphical pick.");
            try {PickCore(document,sourceProfile);}
            finally {SetThreadDpiAwarenessContext(previous);}
        }
        void PickCore(object document,object sourceProfile)
        {
            dynamic part=document,source=sourceProfile,app=part.Application;
            if(!String.Equals((string)app.ActiveDocument.FullName,(string)part.FullName,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Return to the part tab first.");
            app.Activate();
            dynamic window=app.ActiveWindow,view=window.View;
            IntPtr drawing=new IntPtr(Convert.ToInt64(window.DrawHwnd));
            uint pid=Convert.ToUInt32(app.ProcessID);
            // Force the source curve body to exist before mapping its elements.
            if((int)source.CurveBody.Curves.Count==0)throw new InvalidOperationException("The source sketch has no curves.");
            object[] collections={source.Lines2d,source.Arcs2d,source.Circles2d,source.Ellipses2d,source.EllipticalArcs2d,source.BSplineCurves2d};
            int inspected=0;
            foreach(dynamic elements in collections) {
                for(int i=1;i<=(int)elements.Count;i++) {
                    if(++inspected>128)throw new InvalidOperationException("No clear visible outline point was found in the first 128 elements. Zoom in and select manually.");
                    object element=elements.Item(i);
                    if((bool)source.IsConstructionElement(element))continue;
                    int count=0;object mapped=null;
                    source.MapGeometryToCurves(element,ref count,ref mapped);
                    var curves=mapped as Array;
                    if(curves==null || curves.Length!=count)continue;
                    foreach(dynamic curve in curves) {
                        double lo=0,hi=0;curve.GetParamExtents(ref lo,ref hi);
                        if(!Finite(lo) || !Finite(hi) || hi<=lo)continue;
                        // Interior samples avoid endpoint ambiguity. If a point
                        // is covered by another window, try another source point.
                        foreach(double fraction in new[]{.5,.37,.63}) {
                            Array parameter=new[]{lo+(hi-lo)*fraction},coordinates=new double[3];
                            curve.GetPointAtParam(1,ref parameter,ref coordinates);
                            if(coordinates==null || coordinates.Length!=3)continue;
                            int lower=coordinates.GetLowerBound(0);
                            double[] xyz={Convert.ToDouble(coordinates.GetValue(lower)),Convert.ToDouble(coordinates.GetValue(lower+1)),Convert.ToDouble(coordinates.GetValue(lower+2))};
                            if(xyz.Length!=3 || !Finite(xyz[0]) || !Finite(xyz[1]) || !Finite(xyz[2]))continue;
                            // Avoid picking the predefined part-Z axis instead
                            // of an outline that happens to run along that axis.
                            if(Math.Sqrt(xyz[0]*xyz[0]+xyz[1]*xyz[1])<1e-7)continue;
                            int x=0,y=0;view.TransformModelToDC(xyz[0],xyz[1],xyz[2],ref x,ref y);
                            if(!CanClick(drawing,pid,x,y))continue;
                            // Recheck the native command immediately before the
                            // click; never click after the user changed commands.
                            if((int)app.GetActiveCommand()!=50011)throw new InvalidOperationException("The active command changed.");
                            Click(drawing,pid,x,y);return;
                        }
                    }
                }
            }
            throw new InvalidOperationException("No unobstructed outline point is visible. Zoom to the sketch and select manually.");
        }
        static bool Finite(double value){return !Double.IsNaN(value) && !Double.IsInfinity(value);}
        public static bool CanClick(IntPtr drawing,uint expectedPid,int x,int y)
        {
            uint pid;Rect bounds;
            if(drawing==IntPtr.Zero || !IsWindowVisible(drawing) || GetWindowThreadProcessId(drawing,out pid)==0 || pid!=expectedPid || !GetClientRect(drawing,out bounds))return false;
            if(x<bounds.Left+4 || y<bounds.Top+4 || x>=bounds.Right-4 || y>=bounds.Bottom-4 || x>32767 || y>32767)return false;
            Point screen=new Point{X=x,Y=y};if(!ClientToScreen(drawing,ref screen))return false;
            IntPtr hit=WindowFromPoint(screen);
            return hit==drawing || IsChild(drawing,hit);
        }
        public static void Click(IntPtr drawing,uint expectedPid,int x,int y)
        {
            if(!CanClick(drawing,expectedPid,x,y))throw new InvalidOperationException("The graphics window is obscured or the pick is outside it.");
            Point old,screen=new Point{X=x,Y=y};bool restore=GetCursorPos(out old);
            if(!ClientToScreen(drawing,ref screen) || !SetCursorPos(screen.X,screen.Y))throw new InvalidOperationException("Cannot position the pointer on the outline.");
            IntPtr position=new IntPtr((y<<16)|(x&0xffff));
            bool down=false;
            try {
                Send(drawing,0x200,IntPtr.Zero,position); // WM_MOUSEMOVE: update native locate.
                down=true;Send(drawing,0x201,new IntPtr(1),position); // WM_LBUTTONDOWN.
                Send(drawing,0x202,IntPtr.Zero,position);down=false; // WM_LBUTTONUP.
            }
            finally {
                if(down)try{Send(drawing,0x202,IntPtr.Zero,position);}catch{}
                Point current;
                if(restore && GetCursorPos(out current) && current.X==screen.X && current.Y==screen.Y)SetCursorPos(old.X,old.Y);
            }
        }
        static void Send(IntPtr hwnd,uint message,IntPtr w,IntPtr l)
        {
            UIntPtr result;
            if(SendMessageTimeout(hwnd,message,w,l,2,1500,out result)==IntPtr.Zero)
                throw new InvalidOperationException("Solid Edge did not respond to the graphical pick. No second pick was sent.");
        }
    }
}
