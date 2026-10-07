using System;
using System.Collections.Generic;
using Part = SolidEdge.Part.Interop;
using Support = SolidEdge.FrameworkSupport.Interop;
using Geometry = SolidEdge.Geometry.Interop;

namespace BoltCircleHoles
{
    public static class HoleEngine
    {
        // Geometry helpers use base-CSYS XYZ and meters, except parameters explicitly
        // named Inches. Angles sent to pattern APIs are radians; HoleData.BottomAngle
        // uses degrees. Keep those API conventions separate from the inch-based UI.
        public static double[] Dimensions(HoleSize size)
        {
            // Result slots: drill/nominal diameter, counterbore diameter (or unused
            // regular-hole placeholder), and counterbore/shoulder depth. This checks
            // the chart before any model edits; ThreadChart later selects the tap drill.
            if(size!=null && size.Unavailable!=null)throw new InvalidOperationException(size.Unavailable);
            if (size == null || !size.Depth.HasValue)
                throw new InvalidOperationException("This screw size has no counterbore depth in the chart. Enter its depth in C'bore Chart.xls and restart the macro.");
            double[] values = { size.Drill * 0.0254, (size.ButtonHole || size.Thread!=null ? size.Drill : size.Bore) * 0.0254, size.Depth.Value * 0.0254 };
            foreach (double value in values)
                if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                    throw new InvalidOperationException("All hole dimensions must be positive finite values.");
            if (!size.ButtonHole && size.Thread==null && values[1] <= values[0]) throw new InvalidOperationException("The counterbore diameter must exceed the drill diameter.");
            if(size.Thread!=null && (size.Thread.FullDepthInches<=0 || size.Thread.FullDepthInches>=size.Depth.Value))
                throw new InvalidOperationException("Thread depth must be positive and shorter than drill shoulder depth.");
            return values;
        }
        public static double[] Vector(Array values)
        {
            // COM SAFEARRAYs need not start at zero. Normalize the three coordinates
            // once at the boundary so subsequent vector math can use ordinary arrays.
            int k = values.GetLowerBound(0);
            return new[] { Convert.ToDouble(values.GetValue(k)), Convert.ToDouble(values.GetValue(k+1)), Convert.ToDouble(values.GetValue(k+2)) };
        }
        public static double Dot(double[] a, double[] b) { return a[0]*b[0]+a[1]*b[1]+a[2]*b[2]; }
        public static double[] Difference(double[] a, double[] b) { return new[] {a[0]-b[0],a[1]-b[1],a[2]-b[2]}; }
        public static double[] Intersect(double[] origin, double[] direction, double[] root, double[] normal)
        {
            // Solve normal dot (origin + t*direction - root) = 0. The parallelism
            // threshold is scaled by both vector lengths, so normalization is not
            // required and nearly edge-on views cannot produce enormous false centers.
            double denominator = Dot(direction, normal);
            if (Math.Abs(denominator) < 1e-10 * Math.Sqrt(Dot(direction,direction)*Dot(normal,normal)))
                throw new InvalidOperationException("The support is edge-on. Rotate the view and click the center again.");
            double t = Dot(Difference(root,origin),normal)/denominator;
            var point = new[] {origin[0]+t*direction[0],origin[1]+t*direction[1],origin[2]+t*direction[2]};
            foreach (double v in point) if (double.IsNaN(v) || double.IsInfinity(v)) throw new InvalidOperationException("Cannot resolve the hole center on this support.");
            return point;
        }
        static double[] OnProfile(Part.Profile profile, double u, double v)
        {
            double x=0,y=0,z=0; profile.Convert2DCoordinate(u,v,out x,out y,out z); return new[] {x,y,z};
        }
        public static void RequireCoincidentPlanes(double[] firstRoot,double[] firstNormal,double[] secondRoot,double[] secondNormal)
        {
            // Opposite normals still describe the same plane. Test absolute alignment
            // and normal distance rather than equal roots: roots may differ tangentially.
            // Reject offsets above one micron before generating a hole at the wrong Z.
            double a=Math.Sqrt(Dot(firstNormal,firstNormal)),b=Math.Sqrt(Dot(secondNormal,secondNormal));
            double alignment=Math.Abs(Dot(firstNormal,secondNormal))/(a*b);
            double distance=Math.Abs(Dot(Difference(secondRoot,firstRoot),firstNormal))/a;
            if(a<1e-12 || b<1e-12 || double.IsNaN(alignment) || double.IsInfinity(alignment) || double.IsNaN(distance) || double.IsInfinity(distance) || alignment<1-1e-8 || distance>1e-6)
                throw new InvalidOperationException("The generated profile plane does not match the selected Create From face. No hole was created.");
        }
        public static double[] BaseZTarget(double[] center,double[] root,double[] normal)
        {
            // Substitute (0,0,z) into the support-plane equation to find its base-Z
            // intersection. A plane containing the entire axis uses the nearest point
            // at the hole's Z; a parallel displaced plane has no valid in-plane endpoint.
            double length=Math.Sqrt(Dot(normal,normal));
            if(double.IsNaN(length) || double.IsInfinity(length) || length<1e-12)
                throw new InvalidOperationException("Cannot determine the hole profile plane.");
            double planeOffset=Dot(root,normal);
            double z;
            if(Math.Abs(normal[2])/length>1e-10) z=planeOffset/normal[2];
            else
            {
                if(Math.Abs(planeOffset)/length>1e-6)
                    throw new InvalidOperationException("This profile plane is parallel to the base Z axis and does not contain it. A construction line to that axis cannot lie in this profile.");
                z=center[2]; // The plane contains Z: use the closest point on the axis.
            }
            if(double.IsNaN(z) || double.IsInfinity(z)) throw new InvalidOperationException("Cannot locate the base Z axis in this profile.");
            return new[]{0.0,0.0,z};
        }
        static void AddBaseZConstruction(Part.PartDocument part,Part.Profile profile,Part.Hole2d holeCenter,double[] center,double u,double v)
        {
            // Convert through the actual profile basis, not an assumed XY orientation.
            // Tie the hole endpoint by coincidence and fix only the axis endpoint;
            // the native length/angle dimensions can then drive the hole position.
            // Thread selections intentionally bypass this complete construction path.
            var origin=OnProfile(profile,0,0);
            var a=Difference(OnProfile(profile,1,0),origin);
            var b=Difference(OnProfile(profile,0,1),origin);
            var normal=new[]{a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};
            var target=BaseZTarget(center,origin,normal);
            double tu=0,tv=0;
            profile.Convert3DCoordinate(target[0],target[1],target[2],out tu,out tv);
            var mapped=OnProfile(profile,tu,tv);
            var mismatch=Difference(mapped,target);
            if(Dot(mismatch,mismatch)>1e-12) throw new InvalidOperationException("The Z-axis endpoint did not map into the hole profile.");
            RunLog.Write("CONSTRUCTION.base-Z","hole_xyz_m="+RunLog.Value(center)+" axis_xyz_m="+RunLog.Value(target)+" axis_uv_m="+RunLog.Value(new[]{tu,tv}));
            if((tu-u)*(tu-u)+(tv-v)*(tv-v)<1e-16)
            {
                RunLog.Write("CONSTRUCTION.skipped","The hole center is already on the base Z axis; no zero-length line is added.");
                return;
            }
            var line=RunLog.Call("CONSTRUCTION.line",delegate {return profile.Lines2d.AddBy2Points(u,v,tu,tv);});
            RunLog.Call("CONSTRUCTION.mark",delegate {if(!profile.IsConstructionElement(line))profile.ToggleConstruction(line);});
            if(!profile.IsConstructionElement(line)) throw new InvalidOperationException("Solid Edge did not mark the radial line as construction geometry.");
            // Find the actual center keypoint rather than assume a Hole2d keypoint index.
            int centerIndex=-1;
            for(int i=0;i<holeCenter.KeyPointCount;i++)
            {
                double x=0,y=0,z=0; SolidEdge.Framework.Interop.KeyPointType type; int handle;
                holeCenter.GetKeyPoint(i,out x,out y,out z,out type,out handle);
                if(Math.Abs(x-u)<1e-8 && Math.Abs(y-v)<1e-8) {centerIndex=i;break;}
            }
            if(centerIndex<0) throw new InvalidOperationException("Cannot find the hole-center keypoint for the construction line.");
            RunLog.Call("CONSTRUCTION.tie-to-hole",delegate {profile.Relations2d.AddKeypoint(line,0,holeCenter,centerIndex);});
            RunLog.Call("CONSTRUCTION.fix-axis-end",delegate {profile.Relations2d.AddKeypointFix(line,1);});
            AddPositionDimensions(part,profile,line,u,v,tu,tv,normal);
            RunLog.Write("CONSTRUCTION.complete","Construction line starts at the hole center and ends at base CSYS Z. Hole endpoint is coincident; axis endpoint is fixed in profile coordinates.");
        }
        static Part.RefPlane FindBaseRightPlane(Part.PartDocument part)
        {
            // Identify the base YZ plane geometrically; never assume a plane index or locale.
            for(int i=1;i<=Math.Min(3,part.RefPlanes.Count);i++)
            {
                var plane=part.RefPlanes.Item(i);
                Array root=new double[3],normal=new double[3];
                plane.GetRootPoint(ref root);plane.GetNormal(ref normal);
                var r=Vector(root);var n=Vector(normal);
                double length=Math.Sqrt(Dot(n,n));
                if(length>1e-12 && Math.Abs(n[0])/length>1-1e-8 && Math.Abs(r[0])<1e-6)
                    return plane;
            }
            throw new InvalidOperationException("Cannot identify the part's base Right (YZ) reference plane.");
        }
        static void AddPositionDimensions(Part.PartDocument part,Part.Profile profile,Support.Line2d line,
            double u,double v,double tu,double tv,double[] normal)
        {
            var right=RunLog.Call("DIMENSION.find-Right",delegate {return FindBaseRightPlane(part);});
            // ProjectRefPlane supports only orthogonal planes. Do not substitute a free line
            // that merely looks like a reference to the actual Right plane.
            if(Math.Abs(normal[0])/Math.Sqrt(Dot(normal,normal))>1e-8)
                throw new InvalidOperationException("An angular profile dimension to the Right plane requires a Create From face perpendicular to that plane. Choose a perpendicular face; no hole was created.");
            var reference=RunLog.Call("DIMENSION.project-Right",delegate {return profile.ProjectRefPlane(right) as Support.Line2d;});
            if(reference==null) throw new InvalidOperationException("Solid Edge did not return a line for the projected Right reference plane.");
            // ProjectRefPlane returns reference geometry. Solid Edge rejects ToggleConstruction
            // on this object (0x80040223); keep the projected reference in its native state.
            RunLog.Write("DIMENSION.reference","Using the projected Right plane directly; no construction conversion.");
            var dimensions=(Support.Dimensions)profile.Dimensions;
            RunLog.Call("DIMENSION.driving-default",delegate {dimensions.Constraint=true;});
            double radius=Math.Sqrt((u-tu)*(u-tu)+(v-tv)*(v-tv));
            var length=RunLog.Call("DIMENSION.length",delegate {return dimensions.AddLength(line);});
            RunLog.Call("DIMENSION.length-driving",delegate {length.Constraint=true;});
            RunLog.Call("DIMENSION.length-placement",delegate {length.TrackDistance=Math.Max(0.003,radius*0.15);});
            if(!length.Constraint || Math.Abs(length.Value-radius)>1e-7)
                throw new InvalidOperationException("The construction-line length dimension did not match the picked hole center.");
            // Locate the reference on the positive direction of its intersection with the
            // profile (towards +Y when possible), and the radius towards the picked hole.
            var axis=OnProfile(profile,tu,tv);
            var direction=new[]{0.0,normal[2],-normal[1]};
            if(direction[1]<-1e-10 || (Math.Abs(direction[1])<1e-10 && direction[2]<0))
                for(int i=0;i<3;i++)direction[i]=-direction[i];
            double magnitude=Math.Sqrt(Dot(direction,direction));
            double ru=0,rv=0;
            profile.Convert3DCoordinate(axis[0],axis[1]+direction[1]*radius/magnitude,
                axis[2]+direction[2]*radius/magnitude,out ru,out rv);
            var angle=RunLog.Call("DIMENSION.angle-to-Right",delegate {return dimensions.AddAngleBetweenObjects(
                reference,ru,rv,0,false,line,(u+tu)*0.5,(v+tv)*0.5,0,false);});
            RunLog.Call("DIMENSION.angle-driving",delegate {angle.Constraint=true;});
            RunLog.Call("DIMENSION.angle-placement",delegate {angle.TrackDistance=Math.Max(0.003,radius*0.6);});
            if(!angle.Constraint || double.IsNaN(angle.Value) || double.IsInfinity(angle.Value))
                throw new InvalidOperationException("Solid Edge did not retain the angular dimension to the Right reference plane.");
            double sx=0,sy=0,ex=0,ey=0;
            line.GetStartPoint(out sx,out sy);line.GetEndPoint(out ex,out ey);
            if(Math.Abs(sx-u)>1e-7 || Math.Abs(sy-v)>1e-7 || Math.Abs(ex-tu)>1e-7 || Math.Abs(ey-tv)>1e-7)
                throw new InvalidOperationException("Adding the position dimensions moved the construction line. No hole was created.");
            RunLog.Write("DIMENSION.complete","length_in="+RunLog.Value(length.Value/0.0254)+
                " angle_deg="+RunLog.Value(angle.Value*180/Math.PI)+" driving_length="+length.Constraint+
                " driving_angle="+angle.Constraint+" reference=base Right plane");
        }
        public static double PatternSpacing(int count)
        {
            // Quantity includes the clicked seed: six holes means 60-degree increments,
            // not six copies plus a seed. Count=1 is valid and creates no extra positions.
            if(count<1 || count>999)throw new ArgumentOutOfRangeException("count","Enter a whole number of holes from 1 to 999.");
            return 2*Math.PI/count;
        }
        static Part.RefPlane FindBaseXYPlane(Part.PartDocument part)
        {
            for(int i=1;i<=Math.Min(3,part.RefPlanes.Count);i++)
            {
                var plane=part.RefPlanes.Item(i);
                Array root=new double[3],normal=new double[3];
                plane.GetRootPoint(ref root);plane.GetNormal(ref normal);
                var r=Vector(root);var n=Vector(normal);double length=Math.Sqrt(Dot(n,n));
                if(length>1e-12 && Math.Abs(n[2])/length>1-1e-8 && Math.Abs(r[2])<1e-6)return plane;
            }
            throw new InvalidOperationException("Cannot identify the base XY plane for a pattern around Z.");
        }
        public static double[] A2Center(double[] clicked,double[] normal,double radiusInches)
        {
            // A2 uses the click only for azimuth. Scale XY to the chart's Z radius and
            // preserve the support elevation; do not move the point along the face normal.
            double norm=Math.Sqrt(Dot(normal,normal));
            if(norm<1e-12 || Double.IsNaN(norm) || Math.Abs(normal[2])/norm<1-1e-8)
                throw new InvalidOperationException("A2 holes require a Create From face perpendicular to the base Z axis.");
            double r=Math.Sqrt(clicked[0]*clicked[0]+clicked[1]*clicked[1]);
            if(r<1e-8 || Double.IsNaN(r) || Double.IsInfinity(r))throw new InvalidOperationException("Click away from the Z axis to set the A2 hole's angle.");
            if(radiusInches<=0 || Double.IsNaN(radiusInches) || Double.IsInfinity(radiusInches))throw new InvalidOperationException("Invalid A2 chart radius.");
            double factor=radiusInches*0.0254/r;
            return new[]{clicked[0]*factor,clicked[1]*factor,clicked[2]};
        }
        public static object Create(Part.PartDocument part, Part.Model model, object support, double[] center, HoleSize size, bool reverse, int totalHoles)
        {
            // Transaction-like sequence: validate -> create owned support/profile/data
            // -> cut seed -> group threads or pattern other holes -> verify -> hide aids.
            // Success leaves the document unsaved. Failure removes only objects owned
            // by this attempt, in dependency order; collection baselines detect ambiguous
            // COM calls that modified the document without returning an ownership handle.
            // Return type is object because a multi-position thread Hole is represented
            // by UserDefinedPattern, while a single position returns the seed Hole.
            RunLog.Write("HOLE.request","size="+size+" center_m="+RunLog.Value(center)+" reverse="+reverse+" support="+(support is Geometry.Face ? "Face" : "RefPlane"));
            if(size!=null && size.Unavailable!=null)throw new InvalidOperationException(size.Unavailable);
            if(size!=null && size.Radius.HasValue)
            {
                Array supportNormal=new double[3];
                var selectedFace=support as Geometry.Face;
                if(selectedFace!=null)((Geometry.Plane)selectedFace.Geometry).GetNormalVector(ref supportNormal);
                else ((Part.RefPlane)support).GetNormal(ref supportNormal);
                var clicked=center;
                center=A2Center(clicked,Vector(supportNormal),size.Radius.Value);
                RunLog.Write("A2.center","adapter="+size.Adapter+" radius_in="+RunLog.Value(size.Radius)+
                    " clicked_m="+RunLog.Value(clicked)+" resolved_m="+RunLog.Value(center));
            }
            double angularSpacing=PatternSpacing(totalHoles);
            if(totalHoles>1 && center[0]*center[0]+center[1]*center[1]<1e-16) throw new InvalidOperationException("Choose a hole center away from the Z axis for a circular pattern.");
            double[] dims = Dimensions(size);
            RunLog.Write("HOLE.dimensions","inches="+RunLog.Value(new[]{size.Drill,size.Bore,size.Depth.Value})+" meters="+RunLog.Value(dims));
            var body = (Geometry.Body)model.Body;
            if (!body.IsSolid || body.Volume <= 0) throw new InvalidOperationException("Select a solid body for the hole.");
            if (part.ReadOnly) throw new InvalidOperationException("The part is read-only. Obtain write access before creating a hole.");
            double before = body.Volume;
            int holeCount = model.Holes.Count;
            // Snapshot counts before any creation, not midway through the operation.
            // Rollback uses these only to verify cascade deletion/ownership, never as
            // instructions to delete arbitrary collection members by index.
            int setCount=part.ProfileSets.Count, planeCount=part.RefPlanes.Count, dataCount=part.HoleDataCollection.Count;
            Part.RefPlane localPlane = null;
            Part.ProfileSet set = null;
            Part.HoleData data = null;
            Part.Hole hole = null;
            Part.Pattern pattern = null;
            Part.UserDefinedPattern threadGroup=null;
            int threadGroupCount=model.UserDefinedPatterns.Count;
            double[][] threadCenters=null;
            int patternCount=model.Patterns.Count;
            var previousMode = part.ModelingMode;
            RunLog.Write("HOLE.before","part="+part.Name+" mode="+previousMode+" volume_m3="+RunLog.Value(before)+" holes="+holeCount+" profileSets="+setCount+" refPlanes="+planeCount+" holeData="+dataCount);
            try
            {
                if(previousMode!=Part.ModelingModeConstants.seModelingModeOrdered)
                    RunLog.Call("MODE.ordered",delegate {part.ModelingMode = Part.ModelingModeConstants.seModelingModeOrdered;});
                object profileSupport = support;
                var face = support as Geometry.Face;
                if (face != null)
                {
                    // Give each hole an explicit face-linked plane. A command-local plane can
                    // resolve at the origin when another hole/profile already exists (v0.5 log).
                    localPlane = RunLog.Call("PLANE.face-linked-explicit-zero-offset",delegate {return part.RefPlanes.AddParallelByDistance(face,0,Part.ReferenceElementConstants.igNormalSide,Local:false);});
                    Array faceRoot=new double[3],faceNormal=new double[3],planeRoot=new double[3],planeNormal=new double[3];
                    var surface=(Geometry.Plane)face.Geometry;
                    surface.GetRootPoint(ref faceRoot);surface.GetNormalVector(ref faceNormal);
                    localPlane.GetRootPoint(ref planeRoot);localPlane.GetNormal(ref planeNormal);
                    RunLog.Write("PLANE.verify","face_root_m="+RunLog.Value(faceRoot)+" face_normal="+RunLog.Value(faceNormal)+" plane_root_m="+RunLog.Value(planeRoot)+" plane_normal="+RunLog.Value(planeNormal));
                    RequireCoincidentPlanes(Vector(faceRoot),Vector(faceNormal),Vector(planeRoot),Vector(planeNormal));
                    profileSupport = localPlane;
                }
                set = RunLog.Call("PROFILESET.add",delegate {return part.ProfileSets.Add();});
                var profile = RunLog.Call("PROFILE.add",delegate {return set.Profiles.Add(profileSupport);});
                double u=0,v=0;
                profile.Convert3DCoordinate(center[0],center[1],center[2],out u,out v);
                var projected = OnProfile(profile,u,v);
                var error = Difference(projected,center);
                RunLog.Write("PROFILE.center","uv_m="+RunLog.Value(new[]{u,v})+" roundtrip_xyz_m="+RunLog.Value(projected)+" error_m="+RunLog.Value(error));
                if (Dot(error,error)>1e-12) throw new InvalidOperationException("The center did not map onto the Create From support.");
                var holeCenter=RunLog.Call("PROFILE.Holes2d.Add",delegate {return profile.Holes2d.Add(u,v);});
                if(size.Thread==null)AddBaseZConstruction(part,profile,holeCenter,center,u,v);
                else
                {
                    // Profile.End splits disconnected hole centers into separate profiles.
                    // Keep them in one ProfileSet, then use the native multi-profile Hole
                    // grouping operation after creating the seed. This produces one Hole
                    // entry in Pathfinder, not separate Hole features or a circular Pattern.
                    var origin=OnProfile(profile,0,0);
                    var a=Difference(OnProfile(profile,1,0),origin);var b=Difference(OnProfile(profile,0,1),origin);
                    var normal=new[]{a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};
                    threadCenters=ThreadChart.Centers(center,normal,totalHoles);
                    for(int i=1;i<threadCenters.Length;i++)
                    {
                        var point=threadCenters[i];double pu,pv;
                        profile.Convert3DCoordinate(point[0],point[1],point[2],out pu,out pv);
                        var delta=Difference(OnProfile(profile,pu,pv),point);
                        if(Dot(delta,delta)>1e-12)throw new InvalidOperationException("A thread center did not map to its support.");
                        profile.Holes2d.Add(pu,pv);
                        RunLog.Write("THREAD.center","index="+(i+1)+" xyz_m="+RunLog.Value(point));
                    }
                    RunLog.Write("THREAD.layout","holes="+totalHoles+" angular_spacing_deg="+RunLog.Value(360.0/totalHoles)+" dimensions=none pattern_feature=none");
                }
                int profileStatus=RunLog.Call("PROFILE.End",delegate {return profile.End(Part.ProfileValidationType.igProfileClosed);});
                RunLog.Write("PROFILE.validation","return_code="+profileStatus);
                if (profileStatus!=0) throw new InvalidOperationException("Solid Edge could not validate the hole profile (code "+profileStatus+").");
                var side = Part.FeaturePropertyConstants.igRight;
                // igRight follows the profile normal. For a selected face, use its
                // topological outward normal (which can oppose the surface normal) to
                // cut into material by default; the user's reverse option flips once.
                if (face != null)
                {
                    // Face normals account for face orientation, unlike the underlying surface normal.
                    Array points = center, guesses = new double[2], deviations = new double[1], parameters = new double[2], flags = new int[1];
                    RunLog.Call("FACE.GetParamAtPoint",delegate {face.GetParamAtPoint(1,ref points,ref guesses,ref deviations,ref parameters,ref flags);});
                    RunLog.Write("FACE.parameters","uv="+RunLog.Value(parameters)+" flags="+RunLog.Value(flags)+" deviations="+RunLog.Value(deviations));
                    Array normals = new double[3]; RunLog.Call("FACE.GetNormal",delegate {face.GetNormal(1,ref parameters,ref normals);});
                    var outward = Vector(normals);
                    if (Dot(outward,outward)<1e-16) throw new InvalidOperationException("Cannot determine the face's cutting direction.");
                    var origin=OnProfile(profile,0,0); var a=Difference(OnProfile(profile,1,0),origin); var b=Difference(OnProfile(profile,0,1),origin);
                    var positive = new[] {a[1]*b[2]-a[2]*b[1],a[2]*b[0]-a[0]*b[2],a[0]*b[1]-a[1]*b[0]};
                    RunLog.Write("DIRECTION.normals","face="+RunLog.Value(outward)+" profile_positive="+RunLog.Value(positive)+" dot="+RunLog.Value(Dot(positive,outward)));
                    side = Dot(positive,outward)>0 ? Part.FeaturePropertyConstants.igLeft : Part.FeaturePropertyConstants.igRight;
                }
                if (reverse) side = side==Part.FeaturePropertyConstants.igRight ? Part.FeaturePropertyConstants.igLeft : Part.FeaturePropertyConstants.igRight;
                RunLog.Write("DIRECTION.chosen",side+" numeric="+(int)side+" reversed="+reverse);
                var expectedType=size.ButtonHole || size.Thread!=null ? Part.FeaturePropertyConstants.igRegularHole : Part.FeaturePropertyConstants.igCounterboreHole;
                var expectedTreatment=size.Thread!=null ? Part.FeaturePropertyConstants.igTappedHole : Part.FeaturePropertyConstants.igNone;
                if(size.Thread!=null)
                {
                    data=RunLog.Call("HOLEDATA.thread-add",delegate {return ThreadChart.AddData(part,size);});
                    RunLog.Call("HOLEDATA.thread-configure",delegate {ThreadChart.Configure(data,size);});
                    dims[0]=data.HoleDiameter; // Native nominal thread diameter; tap-drill option controls cutting size.
                }
                else
                if(size.ButtonHole)
                    data=RunLog.Call("HOLEDATA.button",delegate {return part.HoleDataCollection.Add(expectedType,dims[0],
                        BottomAngle:120.0,VBottomDimType:Part.FeaturePropertyConstants.igVBottomDimToFlat,TreatmentType:Part.FeaturePropertyConstants.igNone,IgnoreSavedDefaultValues:true);});
                else
                data=RunLog.Call("HOLEDATA.add",delegate {return part.HoleDataCollection.Add(Part.FeaturePropertyConstants.igCounterboreHole,dims[0],
                    CounterboreDiameter:dims[1],CounterboreDepth:dims[2],BottomAngle:0.0,
                    // HoleData accepts igNone, igTappedHole or igTaperedHole.
                    // igTreatmentOff belongs to other feature treatments and is not valid here.
                    TreatmentType:Part.FeaturePropertyConstants.igNone,
                    CounterboreProfileLocationType:Part.FeaturePropertyConstants.igCounterboreProfileIsAtTop,
                    IgnoreSavedDefaultValues:true);});
                if(size.ButtonHole || size.Thread!=null) RunLog.Write("HOLEDATA.readback","type="+data.HoleType+" treatment="+data.TreatmentType+" diameter_m="+RunLog.Value(data.HoleDiameter));
                else RunLog.Write("HOLEDATA.readback","type="+data.HoleType+" treatment="+data.TreatmentType+" treatment_numeric="+(int)data.TreatmentType+" drill_m="+RunLog.Value(data.HoleDiameter)+" cbore_m="+RunLog.Value(data.CounterboreDiameter)+" depth_m="+RunLog.Value(data.CounterboreDepth)+" profile_location="+data.CounterboreProfileLocationType);
                if(data.HoleType!=expectedType || data.TreatmentType!=expectedTreatment)
                    throw new InvalidOperationException("Solid Edge did not retain the requested hole type/treatment settings. See HOLEDATA.readback in the log.");
                if(size.ButtonHole || size.Thread!=null) hole=RunLog.Call(size.Thread!=null ? "HOLE.AddFinite.threads" : "HOLE.AddFinite.button",delegate {return model.Holes.AddFinite(profile,side,dims[2],data);});
                else hole=RunLog.Call("HOLE.AddThroughAll",delegate {return model.Holes.AddThroughAll(profile,side,data);});
                object description=null;
                var featureStatus=RunLog.Call("HOLE.get_Status",delegate {return hole.get_Status(out description);});
                RunLog.Write("HOLE.status",featureStatus+" numeric="+(int)featureStatus+" description="+RunLog.Value(description));
                if (featureStatus!=Part.FeatureStatusConstants.igFeatureOK)
                    throw new InvalidOperationException("Solid Edge returned "+featureStatus+" ("+(int)featureStatus+"). "+Convert.ToString(description)+" See View log for the hole inputs and direction.");
                var after=(Geometry.Body)model.Body;
                // A successful COM return is not enough: require OK feature status,
                // a valid remaining solid, measurable material removal, and dimension
                // readback. The volume threshold combines an absolute and relative floor.
                RunLog.Write("HOLE.volume","before_m3="+RunLog.Value(before)+" after_m3="+RunLog.Value(after.Volume)+" removed_m3="+RunLog.Value(before-after.Volume)+" isSolid="+after.IsSolid);
                if (!after.IsSolid || after.Volume<=0 || before-after.Volume<=Math.Max(1e-16,before*1e-10))
                    throw new InvalidOperationException("The hole did not remove material from a valid solid. Try reversing direction or choosing another center.");
                var actual=(Part.HoleData)hole.HoleData;
                if(size.ButtonHole || size.Thread!=null) RunLog.Write("HOLE.readback","diameter_m="+RunLog.Value(actual.HoleDiameter));
                else RunLog.Write("HOLE.readback","drill_m="+RunLog.Value(actual.HoleDiameter)+" cbore_m="+RunLog.Value(actual.CounterboreDiameter)+" depth_m="+RunLog.Value(actual.CounterboreDepth));
                if (Math.Abs(actual.HoleDiameter-dims[0])>1e-9 || (!size.ButtonHole && size.Thread==null && (Math.Abs(actual.CounterboreDiameter-dims[1])>1e-9 || Math.Abs(actual.CounterboreDepth-dims[2])>1e-9)))
                    throw new InvalidOperationException("The created hole dimensions do not match the chart.");
                if(size.ButtonHole)
                {
                    RunLog.Write("HOLE.button-readback","extent="+hole.ExtentType+" depth_m="+RunLog.Value(hole.Depth)+" bottom_angle="+RunLog.Value(actual.BottomAngle));
                    if(hole.ExtentType!=Part.FeaturePropertyConstants.igFinite || Math.Abs(hole.Depth-dims[2])>1e-9 || Math.Abs(actual.BottomAngle-120.0)>1e-9)
                        throw new InvalidOperationException("The button-hole blind depth or 120-degree V bottom does not match the chart settings.");
                }
                if(size.Thread!=null)
                {
                    RunLog.Write("THREAD.readback","description="+actual.ThreadDescription+" treatment="+actual.TreatmentType+
                        " depth_method="+actual.ThreadDepthMethod+" thread_depth_m="+RunLog.Value(actual.ThreadDepth)+" shoulder_m="+RunLog.Value(hole.Depth));
                    if(actual.TreatmentType!=Part.FeaturePropertyConstants.igTappedHole || actual.ThreadDepthMethod!=Part.FeaturePropertyConstants.igFinite ||
                        Math.Abs(actual.ThreadDepth-size.Thread.FullDepthInches*.0254)>1e-9 || hole.ExtentType!=Part.FeaturePropertyConstants.igFinite ||
                        Math.Abs(hole.Depth-dims[2])>1e-9 || Math.Abs(actual.BottomAngle-120.0)>1e-9)
                        throw new InvalidOperationException("Thread treatment or depths do not match the selected chart row.");
                    if(set.Profiles.Count!=totalHoles)
                        throw new InvalidOperationException("The thread profile count does not match the requested quantity.");
                    if(totalHoles>1)
                    {
                        Array profiles=new object[totalHoles];
                        for(int i=1;i<=totalHoles;i++)profiles.SetValue(set.Profiles.Item(i),i-1);
                        double seedVolume=((Geometry.Body)model.Body).Volume;
                        // SE represents an Ordered multi-position Hole internally as a
                        // UserDefinedPattern. AddByProfiles consumes the seed into ONE
                        // native Hole entry (Holes.Count returns to baseline); it does not
                        // create a separate circular Pattern or independently editable holes.
                        threadGroup=RunLog.Call("THREAD.group.AddByProfiles",delegate {
                            return model.UserDefinedPatterns.AddByProfiles(totalHoles,profiles,hole);});
                        object groupDescription=null;
                        if(threadGroup.get_Status(out groupDescription)!=Part.FeatureStatusConstants.igFeatureOK)
                            throw new InvalidOperationException("Solid Edge could not create the complete Hole group: "+Convert.ToString(groupDescription));
                        int occurrences=0,features=0;
                        threadGroup.GetNumberOfOccurrences(out occurrences,out features);
                        if(occurrences!=totalHoles)
                            throw new InvalidOperationException("The Hole group did not retain every requested position.");
                        var groupedBody=(Geometry.Body)model.Body;
                        if(!groupedBody.IsSolid || groupedBody.Volume<=0 || seedVolume-groupedBody.Volume<=Math.Max(1e-16,seedVolume*1e-10))
                            throw new InvalidOperationException("The Hole group did not remove additional material from a valid solid.");
                        RunLog.Write("THREAD.group","entry="+threadGroup.EdgebarName+" occurrences="+occurrences+
                            " seed_volume_m3="+RunLog.Value(seedVolume)+" final_volume_m3="+RunLog.Value(groupedBody.Volume));
                    }
                    for(int i=1;i<=set.Profiles.Count;i++)
                    {
                        var member=set.Profiles.Item(i);
                        RemoveThreadDimensions(member);member.Visible=false;
                    }
                    object finalDescription=null;
                    var finalStatus=threadGroup==null ? hole.get_Status(out finalDescription) : threadGroup.get_Status(out finalDescription);
                    if(finalStatus!=Part.FeatureStatusConstants.igFeatureOK)
                        throw new InvalidOperationException("The Hole feature failed after removing automatic dimensions.");
                    if(model.Patterns.Count!=patternCount || model.Holes.Count!=holeCount+(totalHoles==1 ? 1 : 0) ||
                        model.UserDefinedPatterns.Count!=threadGroupCount+(totalHoles>1 ? 1 : 0))
                        throw new InvalidOperationException("Unexpected thread-hole or pattern-feature count.");
                }
                if(totalHoles>1 && size.Thread==null)
                {
                    // Counterbores and A2 button holes retain the requested editable
                    // native circular pattern. Threads use explicit chart-driven centers
                    // in one Hole group above, without a separate circular Pattern.
                    var patternPlane=RunLog.Call("PATTERN.base-XY",delegate {return FindBaseXYPlane(part);});
                    Array features=new object[]{hole};Array axisPoint=new double[]{0,0,0};
                    double seedVolume=after.Volume;
                    RunLog.Write("PATTERN.request","total_holes="+totalHoles+" spacing_deg="+RunLog.Value(360.0/totalHoles)+" axis=base Z axis_point_m=[0,0,0]");
                    pattern=RunLog.Call("PATTERN.AddByCircularEx",delegate {return model.Patterns.AddByCircularEx(
                        1,features,patternPlane,totalHoles,angularSpacing,axisPoint,
                        Support.PatternOffsetTypeConstants.sePatternFixedOffset,
                        Part.PatternCurveAnchorSideConstants.sePatternCurveLeftSide,false,
                        Part.PatternTypeConstants.seSmartPattern);});
                    object patternDescription=null;
                    var patternStatus=RunLog.Call("PATTERN.status",delegate {return pattern.get_Status(out patternDescription);});
                    RunLog.Write("PATTERN.status-result",patternStatus+" description="+RunLog.Value(patternDescription));
                    if(patternStatus!=Part.FeatureStatusConstants.igFeatureOK)
                        throw new InvalidOperationException("Solid Edge could not create all patterned holes: "+patternStatus+" "+Convert.ToString(patternDescription));
                    // GetCircularPatternData is unsupported for the pattern returned by
                    // AddByCircularEx in the observed SE 2026 Ordered session (0x80004021).
                    // Creation already received the count/spacing; retain the validated feature.
                    RunLog.Write("PATTERN.inputs-confirmed","requested_total="+totalHoles+
                        " supplied_spacing_rad="+RunLog.Value(angularSpacing)+" feature_status=OK");
                    var patternedBody=(Geometry.Body)model.Body;
                    RunLog.Write("PATTERN.volume","seed_m3="+RunLog.Value(seedVolume)+" patterned_m3="+RunLog.Value(patternedBody.Volume));
                    if(!patternedBody.IsSolid || patternedBody.Volume<=0 || seedVolume-patternedBody.Volume<=Math.Max(1e-16,seedVolume*1e-10))
                        throw new InvalidOperationException("The circular pattern did not remove additional material from a valid solid.");
                    RunLog.Write("PATTERN.success","Created "+totalHoles+" holes total, including the original hole.");
                }
                profile.Visible=false;
                if(localPlane!=null) localPlane.Visible=false;
                RunLog.Write("HOLE.success","Created and validated. Part left unsaved.");
                return threadGroup!=null ? (object)threadGroup : hole;
            }
            catch(Exception failure)
            {
                RunLog.Error("HOLE.failure-before-cleanup",failure);
                var cleanup = new List<string>();
                // Never delete a pre-existing feature or guess ownership after an ambiguous failed COM call.
                bool safe=true;
                if(threadGroup!=null) safe=RemoveOwned("thread Hole group",delegate {threadGroup.Delete();},delegate {return model.UserDefinedPatterns.Count==threadGroupCount;},cleanup);
                else if(model.UserDefinedPatterns.Count!=threadGroupCount){cleanup.Add("A Hole group was added without an ownership handle.");safe=false;}
                if(pattern!=null) safe=RemoveOwned("pattern",delegate {pattern.Delete();},delegate {return model.Patterns.Count==patternCount;},cleanup);
                else if(model.Patterns.Count!=patternCount){cleanup.Add("The pattern collection changed without returning a feature handle.");safe=false;}
                if(safe && hole!=null) safe=RemoveOwned("hole",delegate {hole.Delete();},delegate {return model.Holes.Count==holeCount;},cleanup);
                else if(safe && model.Holes.Count!=holeCount) { cleanup.Add("The hole collection changed without returning a feature handle.");safe=false; }
                if(safe && set!=null) safe=RemoveOwned("profile set",delegate {set.Delete();},delegate {return part.ProfileSets.Count==setCount;},cleanup);
                if(safe && localPlane!=null) safe=RemoveOwned("local plane",delegate {localPlane.Delete();},delegate {return part.RefPlanes.Count==planeCount;},cleanup);
                if(safe && data!=null) safe=RemoveOwned("hole data",delegate {data.Delete();},delegate {return part.HoleDataCollection.Count==dataCount;},cleanup);
                RunLog.Write("CLEANUP.complete","verified="+safe+" issues="+String.Join("; ",cleanup));
                if(cleanup.Count>0) throw new InvalidOperationException(failure.Message+" Partial work may remain. Inspect the part before trying again. Cleanup: "+String.Join("; ",cleanup),failure);
                throw;
            }
            finally
            {
                // Restore the user's modeling mode even after failure. Restoration
                // errors are logged separately so they do not hide the original error.
                if(previousMode!=Part.ModelingModeConstants.seModelingModeOrdered)
                    try {RunLog.Call("MODE.restore."+previousMode,delegate {part.ModelingMode=previousMode;});} catch(Exception ex){Program.Log(ex);}
            }
        }
        static void RemoveThreadDimensions(Part.Profile profile)
        {
            // SE creates positioning dimensions while consuming a hole profile, even if
            // none were requested. Remove only dimensions on this newly owned profile.
            var dimensions=(Support.Dimensions)profile.Dimensions;
            int count=dimensions.Count;
            for(int i=count;i>=1;i--)dimensions.Item(i).Delete();
            if(dimensions.Count!=0)throw new InvalidOperationException("Could not remove automatic thread positioning dimensions.");
            RunLog.Write("THREAD.dimensions-removed","count="+count+" remaining=0");
        }
        static bool RemoveOwned(string name,Action remove,Func<bool> restored,List<string> errors)
        {
            // Returning false tells the caller to stop deleting lower-level dependencies.
            // A stale COM handle alone is not proof of successful removal: require the
            // corresponding live collection to match the saved baseline before ignoring it.
            try {RunLog.Call("CLEANUP.delete."+name,remove);return true;}
            catch(Exception ex)
            {
                // A deleted hole can cascade-delete its profile, plane or HoleData.
                // Only ignore a disconnected child when the live owning collection is back to baseline.
                if(ex.HResult==unchecked((int)0x80010108) || ex.HResult==unchecked((int)0x800401FD) || ex.HResult==unchecked((int)0x800401FB))
                {
                    try {if(restored()){RunLog.Write("CLEANUP.already-removed",name+" collection verified at original count");return true;}}
                    catch(Exception check){RunLog.Error("CLEANUP.verification."+name,check);}
                }
                errors.Add(name+": "+ex.Message);return false;
            }
        }
    }
}
