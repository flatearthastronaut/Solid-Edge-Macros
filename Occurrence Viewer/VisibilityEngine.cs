using System;
using System.Collections.Generic;
using Hammill.Automation;

namespace Hammill.OccurrenceViewer
{
    internal sealed class VisibilityResult
    {
        public int TopLevel,Nested,Matched,Changed;
        public readonly List<string> Errors=new List<string>();
        public override string ToString(){return TopLevel+" top-level / "+Nested+" nested occurrences. "+Matched+" matched; "+Changed+" changed."+(Errors.Count==0?"":"\r\n"+String.Join("\r\n",Errors));}
    }
    internal static class VisibilityEngine
    {
        public static readonly string[] Filters={"All","Reference","Assembly reports (BOM)","Drawing views","Physical properties","Higher level","Interference reports","User-defined quantity","Selectable"};
        private sealed class Target { public object VisibleObject; public bool Current; }
        internal static bool Matches(object occurrence,int filter,bool expected)
        {
            dynamic item=occurrence;bool value;
            switch(filter)
            {
                case 0:return true;
                case 1:value=(bool)item.DisplayAsReference;break;
                case 2:value=(bool)item.IncludeInBom;break;
                case 3:value=(bool)item.DisplayInDrawings;break;
                case 4:value=(bool)item.IncludeInPhysicalProperties;break;
                case 5:value=(bool)item.DisplayInSubAssembly;break;
                case 6:value=(bool)item.IncludeInInterference;break;
                case 7:value=Convert.ToDouble(item.Quantity)!=1.0;break;
                case 8:value=(bool)item.Locatable;break;
                default:throw new ArgumentOutOfRangeException("filter");
            }
            return value==expected;
        }
        public static VisibilityResult Run(object application,object assembly,int filter,bool expected,bool includeNested,bool? visibility)
        {
            if(filter<0 || filter>=Filters.Length)throw new ArgumentOutOfRangeException("filter");
            var result=new VisibilityResult();var targets=new List<Target>();
            using(var scope=new ComScope())
            {
                // Read matching properties once before changing any visibility.
                // SubOccurrence.Visible is the instance in this top assembly;
                // ThisAsOccurrence is only used to inspect its property flags.
                dynamic occurrences=scope.Own(((dynamic)assembly).Occurrences);
                Collect(occurrences,false,0,includeNested,filter,expected,scope,targets,result);
                if(!visibility.HasValue)return result;
                dynamic app=application;bool delay=(bool)app.DelayCompute;
                try
                {
                    app.DelayCompute=true;
                    foreach(var target in targets)
                    {
                        if(target.Current==visibility.Value)continue;
                        try{((dynamic)target.VisibleObject).Visible=visibility.Value;result.Changed++;}
                        catch(Exception error){result.Errors.Add("Could not change an occurrence: "+error.Message);}
                    }
                }
                finally{app.DelayCompute=delay;}
            }
            return result;
        }
        private static void Collect(object collection,bool nested,int depth,bool recurse,int filter,bool expected,ComScope lifetime,List<Target> targets,VisibilityResult result)
        {
            if(depth>128)throw new InvalidOperationException("Assembly nesting exceeds 128 levels.");
            dynamic items=collection;int count=(int)items.Count;
            for(int i=1;i<=count;i++)
            {
                dynamic item=lifetime.Own(items.Item(i));
                if(nested)result.Nested++;else result.TopLevel++;
                try
                {
                    using(var properties=new ComScope())
                    {
                        object occurrence=nested ? properties.Own(item.ThisAsOccurrence) : item;
                        if(Matches(occurrence,filter,expected))
                        {result.Matched++;targets.Add(new Target{VisibleObject=item,Current=(bool)item.Visible});}
                    }
                }
                catch(Exception error){result.Errors.Add("Cannot inspect "+(nested?"nested":"top-level")+" occurrence "+i+": "+error.Message);}
                if(recurse)
                {
                    try
                    {
                        if((bool)item.Subassembly)
                        using(var children=new ComScope())
                        {dynamic sub=children.Own(item.SubOccurrences);Collect(sub,true,depth+1,true,filter,expected,lifetime,targets,result);}
                    }
                    catch(Exception error){result.Errors.Add("Cannot traverse subassembly "+i+": "+error.Message);}
                }
            }
        }
    }
}
