using System;
using System.Collections.Generic;
using Part = SolidEdge.Part.Interop;
using Support = SolidEdge.FrameworkSupport.Interop;
using Geometry = SolidEdge.Geometry.Interop;

namespace BoltCircleHoles
{
    public static class HoleEngine
    {
        public static double[] Dimensions(HoleSize size)
        {
            if(size!=null && size.Unavailable!=null)throw new InvalidOperationException(size.Unavailable);
            if (size == null || !size.Depth.HasValue)
                throw new InvalidOperationException("This screw size has no counterbore depth in the chart. Enter its depth in C'bore Chart.xls and restart the macro.");
            double[] values = { size.Drill * 0.0254, (size.ButtonHole ? size.Drill : size.Bore) * 0.0254, size.Depth.Value * 0.0254 };
            foreach (double value in values)
                if (double.IsNaN(value) || double.IsInfinity(value) || value <= 0)
                    throw new InvalidOperationException("All hole dimensions must be positive finite values.");
            if (!size.ButtonHole && values[1] <= values[0]) throw new InvalidOperationException("The counterbore diameter must exceed the drill diameter.");
            return values;
        }
        public static double[] Vector(Array values)
        {
            int k = values.GetLowerBound(0);
            return new[] { Convert.ToDouble(values.GetValue(k)), Convert.ToDouble(values.GetValue(k+1)), Convert.ToDouble(values.GetValue(k+2)) };
        }
        public static double Dot(double[] a, double[] b) { return a[0]*b[0]+a[1]*b[1]+a[2]*b[2]; }
        public static double[] Difference(double[] a, double[] b) { return new[] {a[0]-b[0],a[1]-b[1],a[2]-b[2]}; }
        public static double[] Intersect(double[] origin, double[] direction, double[] root, double[] normal)
        {
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
            double a=Math.Sqrt(Dot(firstNormal,firstNormal)),b=Math.Sqrt(Dot(secondNormal,secondNormal));
            double alignment=Math.Abs(Dot(firstNormal,secondNormal))/(a*b);
            double distance=Math.Abs(Dot(Difference(secondRoot,firstRoot),firstNormal))/a;
            if(a<1e-12 || b<1e-12 || double.IsNaN(alignment) || double.IsInfinity(alignment) || double.IsNaN(distance) || double.IsInfinity(distance) || alignment<1-1e-8 || distance>1e-6)
                throw new InvalidOperationException("The generated profile plane does not match the selected Create From face. No hole was created.");
        }
        public static double[] BaseZTarget(double[] center,double[] root,double[] normal)
        {
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
            double norm=Math.Sqrt(Dot(normal,normal));
            if(norm<1e-12 || Double.IsNaN(norm) || Math.Abs(normal[2])/norm<1-1e-8)
                throw new InvalidOperationException("A2 holes require a Create From face perpendicular to the base Z axis.");
            double r=Math.Sqrt(clicked[0]*clicked[0]+clicked[1]*clicked[1]);
            if(r<1e-8 || Double.IsNaN(r) || Double.IsInfinity(r))throw new InvalidOperationException("Click away from the Z axis to set the A2 hole's angle.");
            if(radiusInches<=0 || Double.IsNaN(radiusInches) || Double.IsInfinity(radiusInches))throw new InvalidOperationException("Invalid A2 chart radius.");
            double factor=radiusInches*0.0254/r;
            return new[]{clicked[0]*factor,clicked[1]*factor,clicked[2]};
        }
        public static Part.Hole Create(Part.PartDocument part, Part.Model model, object support, double[] center, HoleSize size, bool reverse, int totalHoles)
        {
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
            int setCount=part.ProfileSets.Count, planeCount=part.RefPlanes.Count, dataCount=part.HoleDataCollection.Count;
            Part.RefPlane localPlane = null;
            Part.ProfileSet set = null;
            Part.HoleData data = null;
            Part.Hole hole = null;
            Part.Pattern pattern = null;
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
                AddBaseZConstruction(part,profile,holeCenter,center,u,v);
                int profileStatus=RunLog.Call("PROFILE.End",delegate {return profile.End(Part.ProfileValidationType.igProfileClosed);});
                RunLog.Write("PROFILE.validation","return_code="+profileStatus);
                if (profileStatus!=0) throw new InvalidOperationException("Solid Edge could not validate the hole profile (code "+profileStatus+").");
                var side = Part.FeaturePropertyConstants.igRight;
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
                var expectedType=size.ButtonHole ? Part.FeaturePropertyConstants.igRegularHole : Part.FeaturePropertyConstants.igCounterboreHole;
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
                if(size.ButtonHole) RunLog.Write("HOLEDATA.readback","type="+data.HoleType+" treatment="+data.TreatmentType+" diameter_m="+RunLog.Value(data.HoleDiameter));
                else RunLog.Write("HOLEDATA.readback","type="+data.HoleType+" treatment="+data.TreatmentType+" treatment_numeric="+(int)data.TreatmentType+" drill_m="+RunLog.Value(data.HoleDiameter)+" cbore_m="+RunLog.Value(data.CounterboreDiameter)+" depth_m="+RunLog.Value(data.CounterboreDepth)+" profile_location="+data.CounterboreProfileLocationType);
                if(data.HoleType!=expectedType || data.TreatmentType!=Part.FeaturePropertyConstants.igNone)
                    throw new InvalidOperationException("Solid Edge did not retain the requested hole type/no-thread treatment settings. See HOLEDATA.readback in the log.");
                if(size.ButtonHole) hole=RunLog.Call("HOLE.AddFinite.button",delegate {return model.Holes.AddFinite(profile,side,dims[2],data);});
                else hole=RunLog.Call("HOLE.AddThroughAll",delegate {return model.Holes.AddThroughAll(profile,side,data);});
                object description=null;
                var featureStatus=RunLog.Call("HOLE.get_Status",delegate {return hole.get_Status(out description);});
                RunLog.Write("HOLE.status",featureStatus+" numeric="+(int)featureStatus+" description="+RunLog.Value(description));
                if (featureStatus!=Part.FeatureStatusConstants.igFeatureOK)
                    throw new InvalidOperationException("Solid Edge returned "+featureStatus+" ("+(int)featureStatus+"). "+Convert.ToString(description)+" See View log for the hole inputs and direction.");
                var after=(Geometry.Body)model.Body;
                RunLog.Write("HOLE.volume","before_m3="+RunLog.Value(before)+" after_m3="+RunLog.Value(after.Volume)+" removed_m3="+RunLog.Value(before-after.Volume)+" isSolid="+after.IsSolid);
                if (!after.IsSolid || after.Volume<=0 || before-after.Volume<=Math.Max(1e-16,before*1e-10))
                    throw new InvalidOperationException("The hole did not remove material from a valid solid. Try reversing direction or choosing another center.");
                var actual=(Part.HoleData)hole.HoleData;
                if(size.ButtonHole) RunLog.Write("HOLE.readback","diameter_m="+RunLog.Value(actual.HoleDiameter));
                else RunLog.Write("HOLE.readback","drill_m="+RunLog.Value(actual.HoleDiameter)+" cbore_m="+RunLog.Value(actual.CounterboreDiameter)+" depth_m="+RunLog.Value(actual.CounterboreDepth));
                if (Math.Abs(actual.HoleDiameter-dims[0])>1e-9 || (!size.ButtonHole && (Math.Abs(actual.CounterboreDiameter-dims[1])>1e-9 || Math.Abs(actual.CounterboreDepth-dims[2])>1e-9)))
                    throw new InvalidOperationException("The created hole dimensions do not match the chart.");
                if(size.ButtonHole)
                {
                    RunLog.Write("HOLE.button-readback","extent="+hole.ExtentType+" depth_m="+RunLog.Value(hole.Depth)+" bottom_angle="+RunLog.Value(actual.BottomAngle));
                    if(hole.ExtentType!=Part.FeaturePropertyConstants.igFinite || Math.Abs(hole.Depth-dims[2])>1e-9 || Math.Abs(actual.BottomAngle-120.0)>1e-9)
                        throw new InvalidOperationException("The button-hole blind depth or 120-degree V bottom does not match the chart settings.");
                }
                if(totalHoles>1)
                {
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
                return hole;
            }
            catch(Exception failure)
            {
                RunLog.Error("HOLE.failure-before-cleanup",failure);
                var cleanup = new List<string>();
                // Never delete a pre-existing feature or guess ownership after an ambiguous failed COM call.
                bool safe=true;
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
                if(previousMode!=Part.ModelingModeConstants.seModelingModeOrdered)
                    try {RunLog.Call("MODE.restore."+previousMode,delegate {part.ModelingMode=previousMode;});} catch(Exception ex){Program.Log(ex);}
            }
        }
        static bool RemoveOwned(string name,Action remove,Func<bool> restored,List<string> errors)
        {
            try {RunLog.Call("CLEANUP.delete."+name,remove);return true;}
            catch(Exception ex)
            {
                // A deleted hole can cascade-delete its profile, plane or HoleData.
                // Only ignore a disconnected child when the live owning collection is back to baseline.
                if(ex.HResult==unchecked((int)0x80010108))
                {
                    try {if(restored()){RunLog.Write("CLEANUP.already-removed",name+" collection verified at original count");return true;}}
                    catch(Exception check){RunLog.Error("CLEANUP.verification."+name,check);}
                }
                errors.Add(name+": "+ex.Message);return false;
            }
        }
    }
}









