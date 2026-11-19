using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

// This runner references no macro source or interop DLL. DeploymentTest.ps1
// extracts the actual release ZIP into an isolated directory, then starts this
// process with only the shipped EXE. It must load all standards and generate
// every supported family without a developer checkout or Solid Edge settings.
internal static class DeploymentTests
{
    const BindingFlags Members=BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance;
    static int checks;
    static void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
    static object Get(object value,string name){return value.GetType().GetField(name,Members).GetValue(value);}
    static void Set(object value,string name,object field){value.GetType().GetField(name,Members).SetValue(value,field);}
    static object Call(Type type,string name,params object[] args){return type.GetMethod(name,Members).Invoke(null,args);}
    static object Facts(Type type,object preset)
    {
        object hole=Activator.CreateInstance(type,true);
        string kind=(string)Get(preset,"Kind");
        int code=kind=="Simple"?1:kind=="CounterBore"?2:kind=="CounterDrill"?7:0;
        bool threaded=(int)Get(preset,"Port")!=0&&code==7;
        Set(hole,"Type",threaded?8:code);Set(hole,"Diameter",Get(preset,"Diameter"));
        double depth=(double)Get(preset,"Depth");Set(hole,"Depth",depth>0?(object)depth:null);
        Set(hole,"Bore",code==1?null:Get(preset,"Bore"));
        Set(hole,"BoreDepth",code==1?null:Get(preset,"BoreDepth"));
        Set(hole,"Thread",threaded?Get(preset,"Thread"):"");
        Set(hole,"ThreadDepth",threaded?(object).5:null);
        return hole;
    }
    static void VerifyNote(Type notes,object rules,object facts,bool dual,bool circles)
    {
        object plan=Call(notes,"Build",rules,facts,dual,circles);
        string text=(string)Get(plan,"Text");
        string quantity=circles?"(%QC)PLC'S EQ. SP. AS SHOWN":"TYP(%QC)";
        Check(text.Split('\r').Count(line=>line==quantity)==1,"Packaged note retains mode-specific native quantity");
        Check(text.Contains("%"),"Packaged note retains native field codes");
    }
    static int Main(string[] args)
    {
        try
        {
            var assembly=Assembly.LoadFrom(Path.GetFullPath(args[0]));
            var automation=assembly.GetType("TurboCallout.Automation",true);
            var notes=assembly.GetType("TurboCallout.Notes",true);
            var factsType=assembly.GetType("HoleTableFormatter.HoleFacts",true);
            foreach(string resource in new[]{"TurboCallout.HolePresets.xml","HoleTableFormatter.DowelSeats.xml",
                "HoleTableFormatter.DraftCallout.txt","TurboCallout.AnnotationStandard","TurboCallout.DimensionStandard",
                "TurboCallout.Icon","TurboCallout.Image"})
                using(var stream=assembly.GetManifestResourceStream(resource))
                    Check(stream!=null&&stream.Length>0,"Release contains "+resource);

            // Local files must never replace the validated standard. Exercise
            // absent, stock, malformed, and misleading local settings in turn.
            foreach(string localSettings in new string[]{null,"<CustomSettings/>","not XML",
                "<CustomSettings><SavedHole name='CV Hole- 99mm Plug'/></CustomSettings>"})
            {
                if(localSettings!=null)File.WriteAllText("Custom.xml",localSettings);
                object rules=Call(automation,"LoadRules");
                Check((string)Get(rules,"Source")=="Packaged Turbo Callouts hole definitions","Recognition source is packaged");
                var families=new HashSet<string>();
                foreach(object preset in (IEnumerable)Get(rules,"Presets"))
                {
                    object facts=Facts(factsType,preset);
                    object choice=rules.GetType().GetMethod("Match",Members).Invoke(rules,new[]{facts,(object)true,true});
                    if((int)Get(choice,"Slot")==0)continue; // known ambiguous presets remain for review
                    families.Add((string)Get(choice,"Family"));
                    foreach(bool dual in new[]{false,true})foreach(bool circles in new[]{false,true})
                        VerifyNote(notes,rules,facts,dual,circles);
                }
                foreach(string family in new[]{"Dowel","SHCS counterbore","SAE port","Koenig plug","Spring pin","O-ring face seal"})
                    Check(families.Contains(family),"Standalone release recognizes "+family);
                foreach(int type in new[]{1,4})
                {
                    object hole=Activator.CreateInstance(factsType,true);
                    Set(hole,"Type",type);Set(hole,"Diameter",.201);Set(hole,"Depth",.75);
                    Set(hole,"Thread",type==4?"1/4-20 UNC":"");Set(hole,"ThreadDepth",type==4?(object).5:null);
                    foreach(bool dual in new[]{false,true})foreach(bool circles in new[]{false,true})
                        VerifyNote(notes,rules,hole,dual,circles);
                }
                object unknown=Activator.CreateInstance(factsType,true);
                Set(unknown,"Type",2);Set(unknown,"Diameter",.34123);Set(unknown,"Bore",.53123);Set(unknown,"BoreDepth",.35);
                bool rejected=false;
                try {Call(notes,"Recognize",rules,unknown);}
                catch(TargetInvocationException error)
                {rejected=error.InnerException is InvalidOperationException&&error.InnerException.Message=="No verified standard-hole match. Leave unchanged.";}
                Check(rejected,"Unknown recess remains for review");
            }
            Console.WriteLine(checks+" isolated release checks passed (no Solid Edge connection or drawing changes).");
            return 0;
        }
        catch(Exception error){Console.WriteLine(error);return 1;}
    }
}
