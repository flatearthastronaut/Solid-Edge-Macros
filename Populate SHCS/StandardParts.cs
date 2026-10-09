using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace PopulateSHCS
{
    public static class StandardParts
    {
        public static string DefaultFolder()
        {
            using(var root=Registry.CurrentUser.OpenSubKey(@"Software\Siemens\Standard Parts")){
                if(root!=null)foreach(var version in root.GetSubKeyNames().OrderByDescending(n=>n))using(var key=root.OpenSubKey(version)){
                    var path=Convert.ToString(key.GetValue("SMAPFolder"));if(!String.IsNullOrWhiteSpace(path))return Path.Combine(path,"Standard Parts");
                }
            }
            return "";
        }
        public static List<CatalogPart> Index(string folder,List<Size> sizes,Action<string> progress,Func<bool> cancel)
        {
            if(!Directory.Exists(folder))throw new DirectoryNotFoundException("The Standard Parts folder is unavailable: "+folder);
            var result=new List<CatalogPart>();var stack=new Stack<string>();stack.Push(folder);
            while(stack.Count>0){
                if(cancel())throw new OperationCanceledException();var dir=stack.Pop();progress("Reading Standard Parts: "+Path.GetFileName(dir));
                foreach(var file in Directory.EnumerateFiles(dir,"Screw_ANSI_B18_3_A*.par")){
                    var p=Rules.ParsePart(file,sizes);if(p!=null)result.Add(p);
                }
                // Do not traverse junctions or silently ignore access errors. A partial
                // index could choose an unnecessarily long screw and misstate availability.
                foreach(var sub in Directory.EnumerateDirectories(dir))if((File.GetAttributes(sub)&FileAttributes.ReparsePoint)==0)stack.Push(sub);
            }
            return result;
        }
        public static string SelectAndGenerate(string seed,object application)
        {
            // Public installed Standard Parts COM interface. Its generic query function
            // intentionally excludes internal catalog tables; do not bypass that boundary.
            // The selector is the supported fallback for a size not already generated.
            using(var scope=new ComScope()){
                dynamic pf=scope.Keep(Activator.CreateInstance(Type.GetTypeFromProgID("CADTeam.SMAP.PFCOM.PFCOM",true)));
                try{
                    int error=pf.LoadSettings("");if(error!=0 || !(bool)pf.Init(false))throw new InvalidOperationException("Standard Parts could not load its configured catalog.");
                    int id=pf.SelectPart(seed??"",false);if(id<=0)return null;
                    object cad=application;string file=pf.GeneratePart(id,true,ref cad,false);
                    if(String.IsNullOrWhiteSpace(file) || !File.Exists(file))throw new InvalidOperationException("Standard Parts did not return an available generated part: "+pf.LastErrorDescription);
                    return file;
                }finally{pf.Dispose();}
            }
        }
    }
}
