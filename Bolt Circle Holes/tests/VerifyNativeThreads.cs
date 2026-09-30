using System;
using System.Runtime.InteropServices;
using Part=SolidEdge.Part.Interop;
using SE=SolidEdge.Framework.Interop;
using Geometry=SolidEdge.Geometry.Interop;
using BoltCircleHoles;
class VerifyNativeThreads
{
    [STAThread] static int Main()
    {
        Part.PartDocument part=null;
        try
        {
            var app=(SE.Application)Marshal.GetActiveObject("SolidEdge.Application");
            part=(Part.PartDocument)app.Documents.Add("SolidEdge.PartDocument");
            part.ModelingMode=Part.ModelingModeConstants.seModelingModeOrdered;
            foreach(bool metric in new[]{false,true})foreach(var row in ThreadChart.Load(metric))
            {
                Part.HoleData data=null;
                try
                {
                    data=ThreadChart.AddData(part,row);ThreadChart.Configure(data,row);
                    if(Math.Abs(data.ThreadDepth-row.Thread.FullDepthInches*.0254)>1e-9)throw new Exception("Depth mismatch");
                    Console.WriteLine("PASS TABLE {0} tap_in={1:F6} thread_in={2:F3}",row.Screw,data.ThreadTapDrillDiameter/.0254,data.ThreadDepth/.0254);
                }
                finally{if(data!=null)data.Delete();}
            }
            Part.RefPlane plane=null;
            for(int i=1;i<=3;i++)
            {
                var p=part.RefPlanes.Item(i);Array n=new double[3];p.GetNormal(ref n);
                if(Math.Abs(HoleEngine.Vector(n)[2])>.99){plane=p;break;}
            }
            if(plane==null)throw new Exception("No XY plane");
            var profile=part.ProfileSets.Add().Profiles.Add(plane);
            profile.Circles2d.AddByCenterRadius(0,0,.1);
            if(profile.End(Part.ProfileValidationType.igProfileClosed)!=0)throw new Exception("Base profile failed");
            Array profiles=new object[]{profile};
            var model=part.Models.AddFiniteExtrudedProtrusion(1,ref profiles,Part.FeaturePropertyConstants.igSymmetric,.15);
            foreach(bool metric in new[]{false,true})
            {
                var size=ThreadChart.Load(metric)[metric ? 7 : 3];
                int before=model.Holes.Count,groups=model.UserDefinedPatterns.Count,geometryCount=model.HoleGeometries.Count;
                double beforeVolume=((Geometry.Body)model.Body).Volume;
                var group=(Part.UserDefinedPattern)HoleEngine.Create(part,model,plane,new[]{metric ? .065 : .04,0.0,0.0},size,false,6);
                object desc=null;
                if(group.get_Status(out desc)!=Part.FeatureStatusConstants.igFeatureOK)throw new Exception("Hole group status failed");
                if(model.Holes.Count!=before || model.UserDefinedPatterns.Count!=groups+1 || model.Patterns.Count!=0)
                    throw new Exception("Expected one native Hole group, no separate holes or circular Pattern");
                int count=0,features=0;group.GetNumberOfOccurrences(out count,out features);
                if(count!=6 || model.HoleGeometries.Count!=geometryCount+6)throw new Exception("Not all six physical holes were created");
                var set=part.ProfileSets.Item(part.ProfileSets.Count);
                if(set.Profiles.Count!=6)throw new Exception("Incorrect profile count");
                double drill=0;
                for(int i=1;i<=6;i++)
                {
                    var member=set.Profiles.Item(i);
                    if(member.Dimensions.Count!=0 || member.Holes2d.Count!=1)throw new Exception("Unexpected profile geometry/dimensions");
                    double u,v;member.Holes2d.Item(1).GetCenterPoint(out u,out v);
                    double x,y,z;member.Convert2DCoordinate(u,v,out x,out y,out z);
                    double radius=metric ? .065 : .04;
                    if(Math.Abs(Math.Sqrt(x*x+y*y)-radius)>1e-9 || Math.Abs(z)>1e-9)throw new Exception("Center moved");
                    var created=(Part.HoleData)model.HoleGeometries.Item(geometryCount+i).HoleData;
                    string expected=metric ? size.Thread.NominalText : size.Screw;
                    if(created.ThreadDescription!=expected)throw new Exception("Thread identity changed");
                    drill=created.ThreadTapDrillDiameter;
                }
                // The scratch cylinder contains each entire drill + point, so analytical
                // volume proves all six holes were cut, not just six saved sketch positions.
                double r=drill/2;
                double expectedVolume=6*Math.PI*r*r*(size.Depth.Value*.0254+r/Math.Tan(Math.PI/3)/3);
                double removed=beforeVolume-((Geometry.Body)model.Body).Volume;
                if(Math.Abs(removed-expectedVolume)>expectedVolume*1e-6)throw new Exception("Six-hole volume mismatch");
                Console.WriteLine("PASS GEOMETRY {0}: six physical holes in ONE {1}; zero dimensions and circular patterns",size.Screw,group.EdgebarName);
            }
            int oldHoles=model.Holes.Count,oldGroups=model.UserDefinedPatterns.Count,oldSets=part.ProfileSets.Count,oldData=part.HoleDataCollection.Count;
            double oldVolume=((Geometry.Body)model.Body).Volume;
            bool rejected=false;
            try{HoleEngine.Create(part,model,plane,new[]{.2,0.0,0.0},ThreadChart.Load(false)[3],false,6);}
            catch(InvalidOperationException){rejected=true;}
            if(!rejected || model.Holes.Count!=oldHoles || model.UserDefinedPatterns.Count!=oldGroups || part.ProfileSets.Count!=oldSets ||
                part.HoleDataCollection.Count!=oldData || Math.Abs(((Geometry.Body)model.Body).Volume-oldVolume)>1e-12)
                throw new Exception("Failed layout did not cleanly roll back");
            Console.WriteLine("PASS ROLLBACK: off-body layout rejected; existing groups and body preserved");
            return 0;
        }
        catch(Exception ex){Console.WriteLine(ex);return 1;}
        finally{if(part!=null){part.Close(false);Console.WriteLine("Scratch part closed without saving.");}}
    }
}
