using System;
using System.Collections.Generic;
using System.Globalization;
using Part = SolidEdge.Part.Interop;
namespace BoltCircleHoles
{
    public sealed class ThreadSpec
    {
        // NominalInches and FullDepthInches are always inches. Pitch is mm/revolution
        // for Metric=true, but threads/inch for UNC. NominalText is the native table
        // key (for example M10), while HoleSize.Screw is the complete UI callout.
        public bool Metric;
        public double NominalInches, Pitch, FullDepthInches;
        public string NominalText;
    }
    public static class ThreadChart
    {
        // Thread_Depth_Charts.pdf, revision DB 9/29/2026. Use the inch columns
        // consistently; do not mix them with independently rounded metric depths.
        public static List<HoleSize> Load(bool metric)
        {
            // Arrays share the PDF's row order; keep all columns aligned when editing.
            // These are published rounded depths, not values recomputed at runtime.
            // HoleSize.Depth stores drill depth to the full-diameter shoulder, which
            // must exceed FullDepthInches and excludes the extra conical drill point.
            string[] names=metric ? new[]{"M3","M3.5","M4","M5","M6","M7","M8","M10","M12","M14","M16","M18","M20"}
                : new[]{"#8","#10","#12","1/4","5/16","3/8","7/16","1/2","9/16","5/8","3/4","7/8","1"};
            double[] diam=metric ? new[]{3.0,3.5,4,5,6,7,8,10,12,14,16,18,20}
                : new[]{.164,.190,.216,.25,.3125,.375,.4375,.5,.5625,.625,.75,.875,1};
            double[] pitch=metric ? new[]{.5,.6,.7,.8,1,1,1.25,1.5,1.75,2,2,2.5,2.5}
                : new[]{32.0,24,24,20,18,16,14,13,12,11,10,9,8};
            double[] full=metric ? new[]{.31,.34,.37,.43,.49,.55,.61,.72,.84,.96,1.08,1.20,1.31}
                : new[]{.38,.41,.45,.50,.60,.69,.79,.88,.97,1.07,1.25,1.44,1.63};
            double[] shoulder=metric ? new[]{.43,.46,.49,.56,.65,.71,.81,.96,1.12,1.28,1.40,1.60,1.71}
                : new[]{.51,.58,.62,.70,.83,.94,1.08,1.19,1.31,1.44,1.65,1.89,2.13};
            var rows=new List<HoleSize>();
            for(int i=0;i<names.Length;i++)
            {
                string callout=names[i]+(metric ? " x " : "-")+pitch[i].ToString("0.##",CultureInfo.InvariantCulture)+(metric ? "" : " UNC");
                rows.Add(new HoleSize {Screw=callout,Depth=shoulder[i],
                    // This is nominal size for validation only. The actual tap drill comes
                    // from Solid Edge's thread table before any hole feature is created.
                    Drill=metric ? diam[i]/25.4 : diam[i],
                    Thread=new ThreadSpec {Metric=metric,NominalInches=metric ? diam[i]/25.4 : diam[i],
                        NominalText=names[i],Pitch=pitch[i],FullDepthInches=full[i]}});
            }
            return rows;
        }
        public static double[][] Centers(double[] start,double[] normal,int count)
        {
            // Pure geometry, with no COM calls. Rotate the original XY vector at each
            // angle instead of repeatedly rotating the last point (avoids accumulated
            // drift). Clone the first point exactly and keep Z constant for every hole.
            // Multiple positions stay on the same support only when its normal is Z;
            // either normal sign is accepted. A single hole needs no circular layout.
            double step=HoleEngine.PatternSpacing(count);
            foreach(double v in start)if(Double.IsNaN(v)||Double.IsInfinity(v))throw new InvalidOperationException("Invalid thread center.");
            double norm=Math.Sqrt(HoleEngine.Dot(normal,normal));
            if(count>1 && (norm<1e-12 || Double.IsNaN(norm) || Double.IsInfinity(norm) || Math.Abs(normal[2])/norm<1-1e-8))
                throw new InvalidOperationException("For equally spaced threads around Z, select a face perpendicular to the base Z axis.");
            if(count>1 && start[0]*start[0]+start[1]*start[1]<1e-16)
                throw new InvalidOperationException("Click away from base Z to set the thread-hole circle.");
            var points=new double[count][];
            points[0]=(double[])start.Clone();
            for(int i=1;i<count;i++)
            {
                double c=Math.Cos(i*step),s=Math.Sin(i*step);
                points[i]=new[]{start[0]*c-start[1]*s,start[0]*s+start[1]*c,start[2]};
            }
            return points;
        }
        public static Part.HoleData AddData(Part.PartDocument part,HoleSize size)
        {
            // Return the owned HoleData before configuring it. The caller records the
            // handle immediately, so a failed table/readback check can delete it during
            // rollback. IgnoreSavedDefaultValues prevents prior interactive hole options
            // from silently changing this macro's chosen definition.
            // Current Solid Edge tables are selected by standard and size. The legacy
            // ThreadDataByDescription setter silently returned all zeros in SE 2026.
            // ISO Metric coarse entries use M10, not M10x1.5; UNC includes its pitch.
            var spec=size.Thread;
            string standard=spec.Metric ? "ISO Metric" : "ANSI Inch";
            string tableSize=spec.Metric ? spec.NominalText : size.Screw;
            RunLog.Write("THREAD.table-request","standard="+standard+" size="+tableSize+" requested="+size.Screw);
            return part.HoleDataCollection.AddEx(Part.FeaturePropertyConstants.igRegularHole,
                standard,spec.Metric ? "Metric" : "UNC",tableSize,spec.Metric ? "6H" : "2B",
                HoleDiameter:spec.NominalInches*.0254,TreatmentType:Part.FeaturePropertyConstants.igTappedHole,
                IgnoreSavedDefaultValues:true);
        }
        public static void Configure(Part.HoleData data,HoleSize size)
        {
            var spec=size.Thread;
            string expected=spec.Metric ? spec.NominalText : size.Screw;
            // AddEx can silently select a table's FIRST entry for an unknown size.
            // Check the returned identity as well as dimensions before using any data.
            RunLog.Write("THREAD.table-result","description="+data.ThreadDescription+" standard="+data.Standard+
                " size="+data.Size+" nominal_m="+RunLog.Value(data.ThreadNominalDiameter)+
                " tap_m="+RunLog.Value(data.ThreadTapDrillDiameter)+" hole_m="+RunLog.Value(data.HoleDiameter));
            if(!String.Equals((data.ThreadDescription ?? "").Trim(),expected,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Solid Edge selected thread '"+data.ThreadDescription+"' instead of '"+expected+"'. Check the installed thread table.");


            data.ThreadDiameterOption=Part.ThreadDiameterOptionConstants.seTapDrillDiameter;
            double drill=data.ThreadTapDrillDiameter;
            double nominal=spec.NominalInches*.0254;
            if(Double.IsNaN(drill)||Double.IsInfinity(drill)||drill<=0||drill>=nominal)
                throw new InvalidOperationException("Solid Edge did not provide a valid tap-drill diameter for "+size.Screw+".");
            if(Math.Abs(data.ThreadNominalDiameter-nominal)>1e-6)
                throw new InvalidOperationException("Solid Edge's thread-table nominal size does not match "+size.Screw+".");
            // HoleDiameter must remain the NOMINAL thread diameter. Setting it to the
            // tap drill makes Solid Edge reselect a different thread or clear its identity.
            data.ThreadDepthMethod=Part.FeaturePropertyConstants.igFinite;
            // Full thread depth is independent of drilling depth: AddFinite receives
            // HoleSize.Depth later. igVBottomDimToFlat makes that depth end at the
            // cylindrical shoulder, with the 120-degree point extending beyond it.
            data.ThreadDepth=spec.FullDepthInches*.0254;
            data.BottomAngle=120.0;
            data.VBottomDimType=Part.FeaturePropertyConstants.igVBottomDimToFlat;
            RunLog.Write("THREAD.settings","description="+data.ThreadDescription+" tap_drill_m="+RunLog.Value(drill)+
                " full_thread_m="+RunLog.Value(data.ThreadDepth)+" shoulder_m="+RunLog.Value(size.Depth.Value*.0254));
        }
    }
}
