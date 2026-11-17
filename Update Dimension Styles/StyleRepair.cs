using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hammill.Automation;
using Hammill.DimensionStyleCycle;

namespace Hammill.StyleRepair
{
    internal static class Repair
    {
        internal static readonly List<string> Retained=new List<string>();
        internal static string[] Required(string path)
        {
            if(new FileInfo(path).Length>65536)throw new InvalidDataException("Cycle list exceeds 64 KB.");
            string text=File.ReadAllText(path);
            StyleSequence.Parse(text); // Validate the same snapshot used below.
            var names=text.Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Select(s=>s.Trim()).Where(s=>s.Length>0 && !s.StartsWith("#") && !s.StartsWith(";") && !s.StartsWith("[")).ToArray();
            if(names.Length==0)throw new InvalidDataException("The cycle list contains no style names.");
            return names;
        }
        internal static Dictionary<string,string> Names(object document)
        {
            var result=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
            using(var scope=new ComScope())
            {
                dynamic styles=scope.Own(((dynamic)document).DimensionStyles);
                for(int i=1;i<=(int)styles.Count;i++)using(var item=new ComScope())
                {
                    dynamic style=item.Own(styles.Item(i));string name=(string)style.Name,key=name.Trim();
                    if(result.ContainsKey(key))throw new InvalidDataException("Ambiguous dimension style name: "+key);
                    result.Add(key,name);
                }
            }
            return result;
        }
        internal static string[] Missing(object document,string[] required)
        {var names=Names(document);return required.Where(n=>!names.ContainsKey(n)).ToArray();}
        internal static string[] AddMissing(object target,object master,string[] required)
        {
            Retained.Clear();
            if((int)((dynamic)target).Type!=2 || (int)((dynamic)master).Type!=2)throw new InvalidOperationException("Both documents must be drafts.");
            var before=Names(target);var missing=required.Where(n=>!before.ContainsKey(n)).ToArray();
            if(missing.Length==0)return missing;
            var source=Names(master);
            var absent=missing.Where(n=>!source.ContainsKey(n)).ToArray();
            if(absent.Length>0)throw new InvalidOperationException("The master is also missing:\r\n"+String.Join("\r\n",absent)+"\r\nNothing was imported.");
            using(var scope=new ComScope())
            {
                dynamic styles=scope.Own(((dynamic)target).DimensionStyles);
                string active=(string)styles.Active;
                try
                {
                    // Native import preserves the complete style, including fields
                    // that vary by SE version. False explicitly protects old styles.
                    ((dynamic)target).ImportStyles2(0,false,master);
                    // Read raw names before normalized validation: old drafts may
                    // spell an existing style with different trailing spaces.
                    // Remove any freshly imported alias rather than leaving an
                    // ambiguous normalized name that breaks Cycle Places.
                    var after=new List<string>();
                    for(int i=1;i<=(int)styles.Count;i++)using(var item=new ComScope())
                    {dynamic style=item.Own(styles.Item(i));after.Add((string)style.Name);}
                    // ImportStyles2 imports a collection. Remove ONLY new styles
                    // outside the cycle list; never remove a pre-existing style.
                    foreach(string name in after)
                        if(!before.Values.Contains(name,StringComparer.Ordinal) && (before.ContainsKey(name.Trim()) || !required.Contains(name.Trim(),StringComparer.OrdinalIgnoreCase)))
                        {
                            try {styles.Remove(name);}
                            catch(System.Runtime.InteropServices.COMException){Retained.Add(name);}
                        }
                    var remaining=Missing(target,required);
                    if(remaining.Length>0)throw new InvalidOperationException("Import did not supply: "+String.Join(", ",remaining));
                    return missing;
                }
                catch(Exception error)
                {throw new InvalidOperationException("Style update was incomplete; review the drawing before saving. "+error.Message,error);}
                finally {styles.Active=active;}
            }
        }
    }
}
