using System;
using System.IO;
using System.Runtime.InteropServices;
using HoleTableFormatter;
using TurboCallout;
internal static class Inspect
{
 [STAThread] static int Main(string[] args) {
  object app=null,doc=null,sheets=null;
  using(new BusyFilter()) try {
   app=Marshal.GetActiveObject("SolidEdge.Application"); doc=((dynamic)app).ActiveDocument;
   Console.WriteLine("Document: "+((dynamic)doc).FullName);
   var rules=RuleBook.Load(Path.GetFullPath("..\\Turbo Hole Table Callouts\\Custom.xml"));
   sheets=((dynamic)doc).Sheets;
   using(var reader=new NativeHole(doc))
   for(int i=1;i<=(int)((dynamic)sheets).Count;i++) {
    object sheet=null,notes=null;
    try {sheet=((dynamic)sheets).Item(i); if((int)((dynamic)sheet).SectionType!=0)continue;
     notes=((dynamic)sheet).Balloons; Console.WriteLine("Sheet "+((dynamic)sheet).Name+": "+((dynamic)notes).Count);
     for(int j=1;j<=(int)((dynamic)notes).Count;j++) {
      object n=null;
      try {n=((dynamic)notes).Item(j);
       if(args.Length>0&&args[0]=="--alignment") {
        if((int)((dynamic)n).Callout==0)continue;
        double x,y,z;((dynamic)n).GetFreeSpacePoint(out x,out y,out z);
        Console.WriteLine("Callout "+j+": break="+((dynamic)n).BreakLine+" direction="+((dynamic)n).CalloutBalloonBreaklineDirection+
          " right="+((dynamic)n).BreakLineDirection+" align="+((dynamic)n).CalloutHorizontalAlignment+" at="+x+","+y);
        continue;
       }
       if(!TurboCallout.Automation.IsBlank(n))continue;
       var f=reader.Read(n); var d=rules.Match(f);
       Console.WriteLine("Blank "+j+": type="+f.Type+" dia="+f.Diameter+" depth="+f.Depth+" bore="+f.Bore+" bd="+f.BoreDepth+" thread="+f.Thread+" => "+d.Category+" / "+d.Reason);
      }catch(Exception ex){Console.WriteLine("Callout "+j+": "+ex.Message);}
      finally {Com.Release(n);}
     }
    } finally{Com.Release(notes);Com.Release(sheet);}
   }
   return 0;
  }catch(Exception ex){Console.WriteLine(ex);return 1;}
  finally{Com.Release(sheets);Com.Release(doc);Com.Release(app);}
 }
}
