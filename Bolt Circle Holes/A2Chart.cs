using System;
using System.Collections.Generic;
namespace BoltCircleHoles
{
    public static class A2Chart
    {
        // A2 Adapter Chart.pdf, 2/27/2026: Z is center-to-mounting-hole radius in inches.
        // Button Hole Dia / Dp describe the drive button, NOT the mounting counterbore.
        public static List<HoleSize> Load(List<HoleSize> standard)
        {
            string[] adapters={"A2-5","A2-6","A2-8","A2-11","A2-15","A2-20","A2-28"};
            string[] screws={"M10","M12","M16","M20","M24","M24","M30"};
            double?[] radii={2.063,2.625,3.375,4.625,null,null,null};
            double?[] buttons={0.64,0.77,0.96,1.15,1.40,1.65,null};
            double?[] depths={0.25,0.25,0.31,0.38,0.38,0.38,null};
            var result=new List<HoleSize>();
            for(int i=0;i<adapters.Length;i++)
            {
                HoleSize match=null;
                foreach(var row in standard)
                    if(row.Screw.StartsWith(screws[i]+" ",StringComparison.OrdinalIgnoreCase))
                    {
                        if(match!=null)throw new InvalidOperationException("Ambiguous counterbore chart screw size: "+screws[i]);
                        match=row;
                    }
                var item=new HoleSize {Adapter=adapters[i],Screw=match==null?screws[i]:match.Screw,
                    Drill=match==null?0:match.Drill,Bore=match==null?0:match.Bore,Depth=match==null?null:match.Depth,Radius=radii[i]};
                if(!radii[i].HasValue)item.Unavailable="The A2 chart does not specify a Z radius for "+adapters[i]+".";
                if(match==null)item.Unavailable=(item.Unavailable??"")+" No "+screws[i]+" counterbore is listed in the size chart.";
                result.Add(item);
                var button=new HoleSize {Adapter=adapters[i],ButtonHole=true,Screw="Button Hole",
                    Drill=buttons[i]??0,Bore=0,Depth=depths[i],Radius=radii[i]};
                if(!radii[i].HasValue)button.Unavailable="The A2 chart does not specify a Z radius for "+adapters[i]+".";
                if(!buttons[i].HasValue || !depths[i].HasValue)button.Unavailable=(button.Unavailable??"")+" Button-hole diameter/depth are missing.";
                if(i>=4)button.Unavailable+=" Chart note: button hole is 15 degrees from mounting holes.";
                result.Add(button);
            }
            return result;
        }
    }
}

