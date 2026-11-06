using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Hammill.AddSketch;
using Hammill.Automation;

// Explicitly run only for live acceptance. It creates its own empty assembly,
// saves only that fixture, closes it, and restores the previously active window.
internal static class LiveSmoke
{
    [STAThread]static int Main()
    {
        object fixture=null,previous=null;
        using(var filter=new OleMessageFilter())
        using(var scope=new ComScope())
        {
            try
            {
                dynamic app=scope.Own(Marshal.GetActiveObject("SolidEdge.Application"));
                dynamic documents=scope.Own(app.Documents);
                if((int)documents.Count>0)previous=scope.Own(app.ActiveDocument);
                fixture=documents.Add("SolidEdge.AssemblyDocument");
                string work=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..",".work","live-"+Guid.NewGuid().ToString("N")));
                Directory.CreateDirectory(work);
                dynamic document=fixture;document.SaveAs(Path.Combine(work,"Add Sketch verification.asm"));
                var planes=SketchEngine.Planes(fixture);
                if(planes.Count==0)throw new Exception("Test assembly has no reference planes.");
                int count=SketchEngine.Add((object)app,fixture,planes[0],2,i=>Console.WriteLine("Created native sketch "+i));
                document.Save();Console.WriteLine("PASS live Solid Edge created "+count+" sketches. Fixture: "+work);return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
            finally
            {
                if(fixture!=null){try{((dynamic)fixture).Close(false,Missing.Value,false);}catch(Exception error){Console.Error.WriteLine("Could not close fixture: "+error.Message);}finally{ComScope.Release(fixture);}}
                if(previous!=null)try{((dynamic)previous).Activate();}catch(Exception error){Console.Error.WriteLine("Could not restore previous window: "+error.Message);}
            }
        }
    }
}
