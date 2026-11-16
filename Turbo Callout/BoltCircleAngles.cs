using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using HoleTableFormatter;

namespace TurboCallout
{
    internal sealed class BoltAnglePlan
    {
        internal double Angle, AxisAngle, HoleAngle, MidAngle;
        internal bool Vertical;
        internal int Sign;
    }

    internal static class BoltCircleAngles
    {
        internal const string DimensionTag="TurboCalloutBoltAngle", AxisTag="TurboCalloutBoltAngleAxis";
        const double Height=.003175;

        internal static double Wrapped(double a)
        { return Math.Atan2(Math.Sin(a),Math.Cos(a)); }

        // Sheet coordinates are used deliberately: the requested axes are the
        // horizontal/vertical drawing axes, even when the model view is rotated.
        // The tolerance only absorbs projection round-off, not displayed rounding.
        internal static BoltAnglePlan Plan(double dx,double dy)
        {
            if(!BoltCircleGeometry.Finite(dx)||!BoltCircleGeometry.Finite(dy))
                throw new InvalidOperationException("Hole position is not finite.");
            double radius=Math.Sqrt(dx*dx+dy*dy);
            if(!BoltCircleGeometry.Finite(radius)||radius<=1e-8)
                throw new InvalidOperationException("Hole coincides with the bolt-circle center.");
            if(Math.Min(Math.Abs(dx),Math.Abs(dy))<=Math.Max(1e-8,radius*1e-7))return null;
            bool vertical=Math.Abs(dy)>Math.Abs(dx)+radius*1e-12; // exact 45-degree ties use horizontal
            int sign=Math.Sign(vertical?dy:dx);
            double axis=vertical?sign*Math.PI/2:sign>0?0:Math.PI,hole=Math.Atan2(dy,dx);
            double delta=Wrapped(hole-axis);
            return new BoltAnglePlan{Angle=Math.Abs(delta),AxisAngle=axis,HoleAngle=hole,
                MidAngle=axis+delta/2,Vertical=vertical,Sign=sign};
        }

        internal static bool Tagged(object item,string name)
        { return (bool)((dynamic)item).IsAttributeSetPresent(name); }

        static void Tag(object item,string name)
        {
            object sets=null,set=null,version=null;
            try
            {
                sets=((dynamic)item).AttributeSets;set=((dynamic)sets).Add(name);
                version=((dynamic)set).Add("Version",2); // SDK seInteger
                ((dynamic)version).Value=1;
            }
            finally{Com.Release(version);Com.Release(set);Com.Release(sets);}
        }

        static object Keep(List<object> held,object item){held.Add(item);return item;}
        static bool Same(object a,object b)
        {return a!=null&&b!=null&&BoltCircleDimensions.Identity(a)==BoltCircleDimensions.Identity(b);}

        internal static object Radial(object marks,object circle,object view,double hx,double hy,List<object> held)
        {
            object found=null;
            for(int i=1;i<=(int)((dynamic)marks).Count;i++)
            {
                object mark=Keep(held,((dynamic)marks).Item(i));
                if(!(bool)((dynamic)mark).TwoPoint)continue;
                object arc=null,reference=null;
                try
                {
                    double x,y,z;bool key;
                    ((IBcCenterMark)mark).GetArcCenter(out arc,out x,out y,out z,out key);
                    if(!key||!Same(arc,circle))continue;
                    ((IBcCenterMark)mark).GetCenter(out reference,out x,out y,out z,out key);
                    if(!key||reference==null)continue;
                    object graphic=Keep(held,((dynamic)reference).Object),parentView=Keep(held,((dynamic)graphic).DrawingView);
                    if(!Same(parentView,view))continue;
                    double sx,sy;((dynamic)graphic).GetCenterPoint(out x,out y);((dynamic)view).ViewToSheet(x,y,out sx,out sy);
                    if(Math.Abs(sx-hx)>1e-8||Math.Abs(sy-hy)>1e-8)continue;
                    if(found!=null)throw new InvalidOperationException("More than one radial projection mark matches the hole.");
                    found=mark;
                }
                finally{Com.Release(reference);Com.Release(arc);}
            }
            if(found==null)throw new InvalidOperationException("The hole's native radial projection mark is missing.");
            return found;
        }

        static List<object> RelatedAngles(object dimensions,object radial,List<object> held)
        {
            var result=new List<object>();
            for(int i=1;i<=(int)((dynamic)dimensions).Count;i++)
            {
                object d=Keep(held,((dynamic)dimensions).Item(i));
                if((int)((dynamic)d).DimensionType==3&&BoltCircleDimensions.RelatedTo(d,radial))result.Add(d);
            }
            return result;
        }

        static double LineDifference(double a,double b)
        {double v=Math.Abs(Wrapped(a-b));return Math.Min(v,Math.Abs(Math.PI-v));}

        internal static double SectorExtent(BoltAnglePlan plan,double cx,double cy,double xmin,double ymin,double xmax,double ymax)
        {
            if(cx<xmin||cx>xmax||cy<ymin||cy>ymax)
                throw new InvalidOperationException("Bolt-circle center is outside the drawing-view bounds.");
            var angles=new List<double>{plan.AxisAngle,plan.HoleAngle};
            foreach(double x in new[]{xmin,xmax})foreach(double y in new[]{ymin,ymax})
            {
                double angle=Math.Atan2(y-cy,x-cx);
                if(Math.Abs(Wrapped(angle-plan.MidAngle))<=plan.Angle/2+1e-10)angles.Add(angle);
            }
            double extent=0;
            foreach(double angle in angles)
            {
                double dx=Math.Cos(angle),dy=Math.Sin(angle);
                double horizontal=Math.Abs(dx)<1e-12?Double.PositiveInfinity:(dx>0?xmax-cx:xmin-cx)/dx;
                double vertical=Math.Abs(dy)<1e-12?Double.PositiveInfinity:(dy>0?ymax-cy:ymin-cy)/dy;
                extent=Math.Max(extent,Math.Min(horizontal,vertical));
            }
            return extent;
        }

        static bool Matches(object d,BoltAnglePlan plan)
        {
            if(!String.IsNullOrEmpty((string)((dynamic)d).OverrideString)||
                Math.Abs((double)((dynamic)d).Value-plan.Angle)>1e-7)return false;
            double a=(double)((dynamic)d).ProjectionLineAngle1,b=(double)((dynamic)d).ProjectionLineAngle2;
            return (LineDifference(a,plan.AxisAngle)<1e-7&&LineDifference(b,plan.HoleAngle)<1e-7)||
                (LineDifference(b,plan.AxisAngle)<1e-7&&LineDifference(a,plan.HoleAngle)<1e-7);
        }

        static bool InSector(object dimension,BoltAnglePlan plan,double cx,double cy)
        {
            // Native angular dimensions expose the text location as the final
            // key point. This verifies the selected half-axes, not just a numeric
            // angle which could also describe the opposite quadrant.
            double x,y,z;int type,handle;
            int count=(int)((dynamic)dimension).KeyPointCount;if(count<1)return false;
            ((IBcDimension)dimension).GetKeyPoint(count-1,out x,out y,out z,out type,out handle);
            return Math.Sqrt((x-cx)*(x-cx)+(y-cy)*(y-cy))>Height&&
                Math.Abs(Wrapped(Math.Atan2(y-cy,x-cx)-plan.MidAngle))<plan.Angle/2+.12;
        }

        static object OtherParent(object d,object radial,List<object> held)
        {
            int count;((IBcDimension)d).GetRelatedCount(out count);
            if(count!=2)throw new InvalidOperationException("Existing angle does not have two line parents.");
            for(int i=0;i<count;i++)
            {
                object item;double x,y,z;bool key;
                ((IBcDimension)d).GetRelated(i,out item,out x,out y,out z,out key);Keep(held,item);
                if(!Same(item,radial))return item;
            }
            throw new InvalidOperationException("Angular axis parent is missing.");
        }

        static bool CenterAt(object axis,double cx,double cy)
        {
            double x,y,z;int type,handle;
            if((int)((dynamic)axis).Type==505332096)
            {
                ((IBcCenterMark)axis).GetKeyPoint(0,out x,out y,out z,out type,out handle);
            }
            else if((int)((dynamic)axis).Type==760091584&&Tagged(axis,AxisTag))
                ((dynamic)axis).GetStartPoint(out x,out y);
            else return false;
            return Math.Abs(x-cx)<1e-8&&Math.Abs(y-cy)<1e-8;
        }

        internal static void Process(object draft,IEnumerable<BoltCircleTarget> targets,RunReport report,Action<string> progress)
        {
            object sheets=null;var seen=new HashSet<long>();
            try
            {
                sheets=((dynamic)draft).Sheets;
                foreach(var target in targets)
                {
                    var held=new List<object>();object created=null,createdAxis=null;
                    string label=target.SheetName+", angle for callout "+target.NoteIndex;
                    try
                    {
                        if(progress!=null)progress("Checking "+label+"...");
                        object sheet=Keep(held,((dynamic)sheets).Item(target.SheetName));
                        object notes=Keep(held,((dynamic)sheet).Balloons),note=Keep(held,((dynamic)notes).Item(target.NoteIndex));
                        object reference;double x,y,z;bool key;
                        ((ICalloutTerminator)note).GetTerminator(out reference,out x,out y,out z,out key);Keep(held,reference);
                        object graphic=Keep(held,((dynamic)reference).Object);
                        ((dynamic)graphic).GetCenterPoint(out x,out y);double hx,hy;
                        ((dynamic)target.View).ViewToSheet(x,y,out hx,out hy);
                        double cx,cy,radius;((dynamic)target.Circle).Circle2d(out cx,out cy,out radius);
                        var plan=Plan(hx-cx,hy-cy);
                        object marks=Keep(held,((dynamic)sheet).CenterMarks),radial=Radial(marks,target.Circle,target.View,hx,hy,held);
                        if(!seen.Add(BoltCircleDimensions.Identity(radial)))continue;
                        object dimensions=Keep(held,((dynamic)sheet).Dimensions);
                        var existing=RelatedAngles(dimensions,radial,held);
                        if(plan==null)
                        {
                            // Only remove this macro's angle when a previously
                            // off-axis hole moves onto an axis. Manual work is kept.
                            foreach(object d in existing.Where(d=>Tagged(d,DimensionTag)))
                            {
                                if(!String.IsNullOrEmpty((string)((dynamic)d).OverrideString))
                                    throw new InvalidOperationException("Existing macro angle has custom overridden text.");
                                object axis=OtherParent(d,radial,held);((dynamic)d).Delete();
                                RemoveUnusedAxis(axis,dimensions,held);report.BcAnglesRemoved++;
                            }
                            report.BcAnglesSkipped++;continue;
                        }
                        if(existing.Count>1)throw new InvalidOperationException("Multiple angles already reference the hole projection.");
                        if(existing.Count==1)
                        {
                            object d=existing[0],axis=OtherParent(d,radial,held);
                            if(!Matches(d,plan)||!CenterAt(axis,cx,cy)||!InSector(d,plan,cx,cy))
                                throw new InvalidOperationException("Existing angle differs from the nearest-axis rule; retained for review.");
                            report.BcAnglesExisting++;continue;
                        }

                        double xmin,ymin,xmax,ymax;((dynamic)target.View).Range(out xmin,out ymin,out xmax,out ymax);
                        // Use only the angular sector's part envelope. The full
                        // view diagonal puts shallow angles unnecessarily far
                        // out, through nearby hole-note text. Retain the native
                        // TrackDistance on reruns so manual adjustments persist.
                        double extent=SectorExtent(plan,cx,cy,xmin,ymin,xmax,ymax);
                        double track=Math.Max(radius,extent)+Height;
                        double axisLength=plan.Vertical?(plan.Sign>0?ymax-cy:cy-ymin):(plan.Sign>0?xmax-cx:cx-xmin);
                        axisLength=Math.Max(axisLength,radius)+2*Height;
                        double ax=cx+Math.Cos(plan.AxisAngle)*axisLength,ay=cy+Math.Sin(plan.AxisAngle)*axisLength;
                        object lines=Keep(held,((dynamic)sheet).Lines2d),relations=Keep(held,((dynamic)sheet).Relations2d);
                        // SE's Angle Between API does not reliably locate the
                        // arm of a CenterMark used as the first parent. A native
                        // center-style Line2d supplies that axis. Its start is
                        // constrained to the actual B.C center and its direction
                        // is a native H/V constraint, never a fixed coordinate.
                        createdAxis=Keep(held,((dynamic)lines).AddBy2Points(cx,cy,ax,ay));
                        Keep(held,((dynamic)relations).AddKeypoint(createdAxis,0,target.Circle,0,true));
                        Keep(held,plan.Vertical?((dynamic)relations).AddVertical(createdAxis,true):((dynamic)relations).AddHorizontal(createdAxis,true));
                        object axisStyle=Keep(held,((dynamic)createdAxis).Style),circleStyle=Keep(held,((dynamic)target.Circle).Style);
                        ((dynamic)axisStyle).DashName=(string)((dynamic)circleStyle).CenterLineType;
                        ((dynamic)axisStyle).Width=(double)((dynamic)circleStyle).LineWidth;
                        ((dynamic)axisStyle).LinearColor=(int)((dynamic)circleStyle).DrivenColor;
                        Tag(createdAxis,AxisTag);
                        double locate=radius+Height;
                        int oldMode=(int)((dynamic)dimensions).AxisMode;
                        try
                        {
                            ((dynamic)dimensions).AxisMode=2; // SDK implied; measure the two actual parents
                            created=Keep(held,((dynamic)dimensions).AddAngleBetweenObjects(createdAxis,ax,ay,0.0,false,
                                radial,cx+Math.Cos(plan.HoleAngle)*locate,cy+Math.Sin(plan.HoleAngle)*locate,0.0,false));
                        }
                        finally{((dynamic)dimensions).AxisMode=oldMode;}
                        object style=Keep(held,((dynamic)created).Style);BoltCircleDimensions.ApplyPresentation(style);
                        ((dynamic)style).Height=Height;((dynamic)style).AngularUnits=3;((dynamic)style).AngularDecimalRoundOff=2;
                        ((dynamic)style).DualDisplay=false;((dynamic)created).DisplayType=1;
                        ((dynamic)created).TrackDistance=track;
                        bool selected=false;
                        // Native direction flags select one of the line sectors.
                        // Verify both the native measurement and physical text
                        // quadrant; never override the displayed numeric value.
                        foreach(bool origin in new[]{false,true})
                        {
                            foreach(bool measure in new[]{false,true})
                            {
                                foreach(bool clockwise in new[]{false,true})
                                {
                                    ((dynamic)created).AngleCompliment=false;((dynamic)created).OriginLineDirection=origin;
                                    ((dynamic)created).MeasureLineDirection=measure;((dynamic)created).AngleClockwise=clockwise;
                                    if(Matches(created,plan)&&InSector(created,plan,cx,cy)){selected=true;break;}
                                }
                                if(selected)break;
                            }
                            if(selected)break;
                        }
                        if(!selected||!BoltCircleDimensions.RelatedTo(created,radial)||!BoltCircleDimensions.RelatedTo(created,createdAxis)||
                            !CenterAt(createdAxis,cx,cy))throw new InvalidOperationException("Native angular value, quadrant, or attachments did not verify.");
                        Tag(created,DimensionTag);report.BcAnglesCreated++;
                        report.Details.Add(label+": "+(plan.Angle*180/Math.PI).ToString("0.####",System.Globalization.CultureInfo.InvariantCulture)+
                            " degrees from the nearest "+(plan.Vertical?"vertical":"horizontal")+" axis.");
                        created=null;createdAxis=null;
                    }
                    catch(Exception error)
                    {
                        try{if(created!=null)((dynamic)created).Delete();if(createdAxis!=null)((dynamic)createdAxis).Delete();}
                        catch(Exception cleanup){report.Errors++;report.Details.Add(label+": angular cleanup needs review: "+cleanup.Message);}
                        report.BcAnglesReview++;report.Review++;report.Details.Add(label+": "+error.Message);
                    }
                    finally{for(int i=held.Count-1;i>=0;i--)Com.Release(held[i]);}
                }
            }
            finally{Com.Release(sheets);}
        }

        static void RemoveUnusedAxis(object axis,object dimensions,List<object> held)
        {
            if(!Tagged(axis,AxisTag))return;
            object relationships=Keep(held,((dynamic)axis).Relationships);
            if((int)((dynamic)relationships).Count>2)return; // preserve any additional user constraints
            for(int i=1;i<=(int)((dynamic)dimensions).Count;i++)
            {object d=Keep(held,((dynamic)dimensions).Item(i));if(BoltCircleDimensions.RelatedTo(d,axis))return;}
            ((dynamic)axis).Delete();
        }
    }
}
