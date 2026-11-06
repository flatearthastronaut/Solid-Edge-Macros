using System;
using System.Collections.Generic;
using System.Reflection;
using Hammill.Automation;

namespace Hammill.AddSketch
{
    internal sealed class PlaneChoice
    {
        public int Index; public string Name;
        public override string ToString(){return Name;}
    }
    internal static class SketchEngine
    {
        public static List<PlaneChoice> Planes(object assembly)
        {
            var result=new List<PlaneChoice>();
            using(var scope=new ComScope())
            {
                dynamic planes=scope.Own(((dynamic)assembly).AsmRefPlanes);
                int count=(int)planes.Count;
                for(int i=1;i<=count;i++)using(var item=new ComScope())
                {dynamic plane=item.Own(planes.Item(i));result.Add(new PlaneChoice{Index=i,Name=(string)plane.Name});}
            }
            return result;
        }
        public static int Add(object application,object assembly,PlaneChoice choice,int count,Action<int> progress)
        {
            if(choice == null)throw new ArgumentException("Select a reference plane.");
            if(count<1 || count>100)throw new ArgumentOutOfRangeException("count","Choose between 1 and 100 sketches.");
            dynamic app=application;bool previous=(bool)app.DelayCompute;int created=0;
            using(var scope=new ComScope())
            {
                dynamic planes=scope.Own(((dynamic)assembly).AsmRefPlanes);
                dynamic plane=scope.Own(planes.Item(choice.Index));
                if((string)plane.Name != choice.Name)throw new InvalidOperationException("Reference planes changed. Reload the list before adding sketches.");
                dynamic layouts=scope.Own(((dynamic)assembly).Layouts);
                try
                {
                    app.DelayCompute=true;
                    for(int i=0;i<count;i++)using(var item=new ComScope())
                    {
                        int before=(int)layouts.Count;
                        object status;
                        // Preserve the original tool's omitted ReturnExisting
                        // argument. Explicit False can reject an existing layout
                        // on that plane. Check Count so reuse never looks like a
                        // successfully created new sketch.
                        object layout=layouts.Add(plane,Missing.Value,out status);
                        item.Own(layout);
                        if(layout == null || (int)layouts.Count != before+1)
                            throw new InvalidOperationException("Solid Edge did not create a new sketch on this plane (status " + Convert.ToString(status) + ").");
                        created++;progress(created);
                    }
                    return created;
                }
                catch(Exception error){throw new InvalidOperationException(created+" of "+count+" sketches added. "+error.Message,error);}
                finally{app.DelayCompute=previous;}
            }
        }
    }
}
