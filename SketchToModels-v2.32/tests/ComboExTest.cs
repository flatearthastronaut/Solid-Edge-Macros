using System;using System.Diagnostics;using System.IO;using System.Runtime.InteropServices;using System.Windows.Forms;using SketchToModels;
class ExTest{
[DllImport("comctl32.dll")]static extern bool InitCommonControlsEx(ref Init i);
struct Init{public int size,flags;}
[DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr CreateWindowEx(int ex,string cls,string text,int style,int x,int y,int w,int h,IntPtr parent,IntPtr menu,IntPtr instance,IntPtr param);
[DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern IntPtr SendMessage(IntPtr h,int m,IntPtr w,IntPtr l);
[StructLayout(LayoutKind.Sequential)]struct Item{public uint mask;public IntPtr index,text;public int max,image,selected,overlay,indent;public IntPtr data;}
[STAThread]static int Main(string[] args){
if(args.Length==0){
string file=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"combo-host.txt");if(File.Exists(file))File.Delete(file);
var start=new ProcessStartInfo(Application.ExecutablePath,"host"){WindowStyle=ProcessWindowStyle.Hidden};
using(var p=Process.Start(start)){try{
for(int i=0;i<100&&!File.Exists(file);i++)System.Threading.Thread.Sleep(50);
IntPtr root=new IntPtr(long.Parse(File.ReadAllText(file)));
bool ok=NativePlaneChoice.Select(root,p.Id,()=>false);
Console.WriteLine(ok?"PASS: separate-process native ComboBoxEx selection.":"FAIL: native ComboBoxEx selection.");return ok?0:1;
}finally{p.Kill();}}}
Init init=new Init{size=8,flags=0x200};InitCommonControlsEx(ref init);
using(var f=new Form())using(var floating=new Form()){
f.Shown+=delegate{
floating.Show(f);
IntPtr combo=CreateWindowEx(0,"ComboBoxEx32","",unchecked((int)0x50000003),10,10,250,200,floating.Handle,new IntPtr(3082),IntPtr.Zero,IntPtr.Zero);
if(combo==IntPtr.Zero)throw new Exception("Create failed");
string[] labels={"Coincident Plane","Feature's Plane","Parallel Plane"};
for(int n=0;n<labels.Length;n++){IntPtr text=Marshal.StringToHGlobalUni(labels[n]);IntPtr mem=Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Item)));try{Marshal.StructureToPtr(new Item{mask=1,index=new IntPtr(n),text=text},mem,false);SendMessage(combo,0x40b,IntPtr.Zero,mem);}finally{Marshal.FreeHGlobal(mem);Marshal.FreeHGlobal(text);}}
SendMessage(combo,0x14e,IntPtr.Zero,IntPtr.Zero);
File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"combo-host.txt"),f.Handle.ToInt64().ToString());
};Application.Run(f);}return 0;
}}
