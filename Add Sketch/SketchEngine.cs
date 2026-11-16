using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using Hammill.Automation;

namespace Hammill.AddSketch
{
    internal sealed class PlaneChoice
    {
        public int Index; public string Name;
        public override string ToString(){return Name;}
    }
    internal sealed class PlaneScan
    {
        public readonly List<PlaneChoice> Choices=new List<PlaneChoice>();
        public readonly List<string> Notices=new List<string>();
    }
    internal static class SketchEngine
    {
        public static PlaneScan Planes(object assembly)
        {
            var result=new PlaneScan();
            using(var scope=new ComScope())
            {
                dynamic planes=scope.Own(((dynamic)assembly).AsmRefPlanes);
                int count=(int)planes.Count;
                for(int i=1;i<=count;i++)using(var item=new ComScope())
                {
                    // Real assemblies can retain collection slots whose Name
                    // getter is empty or returns E_FAIL. One such slot must not
                    // discard usable planes read earlier or later in the list.
                    // Keep the native index, not the filtered dropdown position.
                    try
                    {
                        dynamic plane=item.Own(planes.Item(i));
                        if(plane==null){result.Notices.Add("Plane "+i+": no reference-plane object.");continue;}
                        string name=(string)plane.Name;
                        if(String.IsNullOrWhiteSpace(name)){result.Notices.Add("Plane "+i+": no readable name.");continue;}
                        result.Choices.Add(new PlaneChoice{Index=i,Name=name});
                    }
                    catch(COMException error){result.Notices.Add("Plane "+i+": unable to read (0x"+error.ErrorCode.ToString("X8")+").");}
                    catch(InvalidComObjectException){result.Notices.Add("Plane "+i+": reference is no longer available.");}
                }
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
                dynamic plane;
                try
                {
                    plane=scope.Own(planes.Item(choice.Index));
                    if(plane==null || (string)plane.Name != choice.Name)throw new InvalidOperationException("Reference planes changed. Reload the list before adding sketches.");
                }
                catch(COMException error){throw new InvalidOperationException("The selected reference plane is no longer readable. Reload planes and select an available plane.",error);}
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
