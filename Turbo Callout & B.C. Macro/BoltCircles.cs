using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using HoleTableFormatter;

namespace TurboCallout
{
    // Explicit SAFEARRAY(IDispatch) declarations avoid the late-bound runtime's
    // SAFEARRAY(Variant) conversion for feature lists. IDs come from SE's SDK.
    [ComImport,Guid("7f943b18-4eb5-11d2-a479-080036ecf202"),InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IBcPattern
    {
        [DispId(5)] void GetOccurrence(int index,[In,Out,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_DISPATCH)]ref Array features);
        [DispId(45)] void GetCircularPatternData(out int method,out int count,out double angle);
    }
    [ComImport,Guid("a87ec198-e3c9-11ce-9336-080036b65002"),InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IBcHoleGroup
    { [DispId(16398)] void GetInputFeatures([In,Out,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_DISPATCH)]ref Array features); }
    [ComImport,Guid("23c52e80-4698-11ce-b307-0800363a1e02"),InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IBcPart
    { [DispId(24685)] void BindKeyToObject([In,Out,MarshalAs(UnmanagedType.SafeArray,SafeArraySubType=VarEnum.VT_UI1)]ref Array key,[MarshalAs(UnmanagedType.IDispatch)]out object item); }
    [ComImport,Guid("61f55450-5269-11ce-9cfe-08003637fb01"),InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IBcView
    { [DispId(24724)] void ViewOrientation(out double vx,out double vy,out double vz,out double xx,out double xy,out double xz,out int orientation); }
    [ComImport,Guid("7d915053-f4a6-11ce-a941-080036373002"),InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IBcCenterMark
    {
        [DispId(6)] void GetCenter([MarshalAs(UnmanagedType.IDispatch)]out object reference,out double x,out double y,out double z,out bool keyPoint);
        [DispId(8303)] void GetArcCenter([MarshalAs(UnmanagedType.IDispatch)]out object reference,out double x,out double y,out double z,out bool keyPoint);
    }

    internal enum BoltCircleResult { Created, Existing, Skipped, Review }

    internal sealed class BoltCircleTarget
    {
        internal string SheetName;
        internal int NoteIndex;
        internal object Circle, View;
    }

    internal sealed class BoltCircles : IDisposable
    {
        const int HoleType=462094722, CircleType=-2074243498;
        readonly object draft;
        readonly List<object> held=new List<object>();
        readonly Dictionary<long,List<HoleGroup>> groups=new Dictionary<long,List<HoleGroup>>();
        readonly Dictionary<string,List<CircleGraphic>> graphics=new Dictionary<string,List<CircleGraphic>>();
        internal int ProjectionMarksCreated { get; private set; }
        internal int ProjectionMarksUpdated { get; private set; }
        internal readonly List<BoltCircleTarget> DimensionTargets=new List<BoltCircleTarget>();
        sealed class ProjectionConflict : Exception { internal ProjectionConflict(string message):base(message){} }
        sealed class HoleGroup
        {
            internal readonly HashSet<long> Owners=new HashSet<long>();
            internal bool Circular,AllowPair,RegularRequired;
        }
        sealed class CircleGraphic
        {
            internal object Graphic;
            internal long Owner;
            internal CirclePoint Center;
            internal double Radius;
        }
        internal BoltCircles(object draft) { this.draft=draft; }
        object Keep(object value) { held.Add(value);return value; }
        void DimensionTarget(object note,object sheet,object circle,object view)
        {
            // Circle/view RCWs are owned by this engine. Reacquire notes by
            // index later: the caller releases its note and sheet each loop.
            DimensionTargets.Add(new BoltCircleTarget { SheetName=(string)((dynamic)sheet).Name,
                NoteIndex=(int)((dynamic)note).Index,Circle=circle,View=view });
        }
        static long Identity(object value)
        {IntPtr pointer=Marshal.GetIUnknownForObject(value);try{return pointer.ToInt64();}finally{Marshal.Release(pointer);}}
        void AddOwners(HoleGroup group,Array features)
        {
            foreach(object feature in features)
            {Keep(feature);if((int)((dynamic)feature).Type==HoleType)group.Owners.Add(Identity(feature));}
        }
        List<HoleGroup> Groups(object part)
        {
            long id=Identity(part);List<HoleGroup> cached;if(groups.TryGetValue(id,out cached))return cached;
            var result=new List<HoleGroup>();object models=Keep(((dynamic)part).Models);
            for(int m=1;m<=(int)((dynamic)models).Count;m++)
            {
                object model=Keep(((dynamic)models).Item(m)),patterns=Keep(((dynamic)model).Patterns);
                for(int i=1;i<=(int)((dynamic)patterns).Count;i++)
                {
                    object pattern=Keep(((dynamic)patterns).Item(i));var group=new HoleGroup();
                    int total=(int)((dynamic)pattern).NumberOfOccurrences;
                    for(int n=1;n<=total;n++)
                    {Array features=new object[0];((IBcPattern)pattern).GetOccurrence(n,ref features);AddOwners(group,features);}
                    try
                    {
                        int method,count;double angle;((IBcPattern)pattern).GetCircularPatternData(out method,out count,out angle);
                        group.Circular=count>=2&&BoltCircleGeometry.Finite(angle)&&Math.Abs(angle)>1e-8;
                        group.AllowPair=group.Circular&&count==2&&Math.Abs(Math.Abs(angle)-Math.PI)<1e-6;
                    }
                    catch(COMException) { /* Older Ordered patterns expose their definition through Profile. */ }
                    if(!group.Circular)
                    {
                        // SE 2026's Ordered AddByCircularEx patterns can reject
                        // GetCircularPatternData (0x80004021). Their native
                        // CircularPattern2d profile remains authoritative.
                        object profile=Keep(((dynamic)pattern).Profile);
                        if(profile!=null)
                        {
                            object circular=Keep(((dynamic)profile).CircularPatterns2d);
                            if((int)((dynamic)circular).Count==1)
                            {
                                object definition=Keep(((dynamic)circular).Item(1));
                                int count=(int)((dynamic)definition).Count;double angle=(double)((dynamic)definition).AngularSpacing;
                                group.Circular=count>=2&&BoltCircleGeometry.Finite(angle)&&Math.Abs(angle)>1e-8;
                                group.AllowPair=group.Circular&&count==2&&Math.Abs(Math.Abs(angle)-Math.PI)<1e-6;
                            }
                        }
                    }
                    if(group.Owners.Count>1)result.Add(group);
                }
                object userPatterns=Keep(((dynamic)model).UserDefinedPatterns);
                for(int i=1;i<=(int)((dynamic)userPatterns).Count;i++)
                {
                    object pattern=Keep(((dynamic)userPatterns).Item(i));Array features=new object[0];
                    ((IBcHoleGroup)pattern).GetInputFeatures(ref features);
                    var group=new HoleGroup{Circular=true,RegularRequired=true};AddOwners(group,features);
                    // A native multi-profile Hole group is not necessarily a
                    // bolt circle. Require a complete regular circular layout.
                    if(group.Owners.Count>1)result.Add(group);
                }
            }
            groups.Add(id,result);return result;
        }
        object Owner(object graphic,object part)
        {
            string file="";Array key=new byte[0];int size=0;
            ((dynamic)graphic).GetReferenceKey(ref file,ref key,ref size);
            if(!String.Equals(Path.GetFullPath(file),Path.GetFullPath((string)((dynamic)part).FullName),StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Projected hole belongs to a different model component.");
            object edge;((IBcPart)part).BindKeyToObject(ref key,out edge);Keep(edge);
            return Keep(((dynamic)edge).Parent);
        }
        List<CircleGraphic> Circles(object view,object part,object member)
        {
            string key=Identity(view)+"/"+Identity(member);List<CircleGraphic> cached;if(graphics.TryGetValue(key,out cached))return cached;
            var result=new List<CircleGraphic>();object circles=Keep(((dynamic)member).DVCircles2d);
            for(int i=1;i<=(int)((dynamic)circles).Count;i++)
            {
                object circle=Keep(((dynamic)circles).Item(i));
                try
                {
                    object owner=Owner(circle,part);if((int)((dynamic)owner).Type!=HoleType)continue;
                    double x,y;((dynamic)circle).GetCenterPoint(out x,out y);
                    result.Add(new CircleGraphic{Graphic=circle,Owner=Identity(owner),Center=new CirclePoint(x,y),Radius=(double)((dynamic)circle).Radius});
                }
                catch(ArgumentException) { /* Generated/non-model circles have no bindable physical edge. */ }
                catch(COMException) { /* A missing member makes the complete-pattern check below fail safely. */ }
            }
            graphics.Add(key,result);return result;
        }
        internal BoltCircleResult Ensure(object note,object sheet,out string detail)
        {
            detail=null;
            if(!(bool)((dynamic)note).IsTerminatorAttachedToEntity)return BoltCircleResult.Skipped;
            object reference;double tx,ty,tz;bool keypoint;
            ((ICalloutTerminator)note).GetTerminator(out reference,out tx,out ty,out tz,out keypoint);Keep(reference);
            object graphic=Keep(((dynamic)reference).Object);
            if((int)((dynamic)graphic).Type!=CircleType)
            {detail="B.C. skipped: the referenced hole is not a face-on circular edge.";return BoltCircleResult.Skipped;}
            object view=Keep(((dynamic)graphic).DrawingView);
            double vx,vy,vz,xx,xy,xz;int orientation;
            ((IBcView)view).ViewOrientation(out vx,out vy,out vz,out xx,out xy,out xz,out orientation);
            if(orientation<1||orientation>6)
            {detail="B.C. skipped: only face-on orthographic views are supported.";return BoltCircleResult.Skipped;}
            object link=Keep(((dynamic)view).ModelLink),part=Keep(((dynamic)link).ModelDocument);
            ((dynamic)draft).Activate();
            if((int)((dynamic)part).Type!=1)
            {detail="B.C. needs review: the linked model is not a part document.";return BoltCircleResult.Review;}
            object owner=Owner(graphic,part);if((int)((dynamic)owner).Type!=HoleType)return BoltCircleResult.Skipped;
            long ownerId=Identity(owner);var matches=Groups(part).Where(g=>g.Owners.Contains(ownerId)).ToList();
            if(matches.Count==0)return BoltCircleResult.Skipped;
            if(matches.Count!=1)
            {detail="B.C. needs review: the hole belongs to more than one pattern.";return BoltCircleResult.Review;}
            HoleGroup group=matches[0];if(!group.Circular)
            {detail="B.C. skipped: the native pattern is not circular.";return BoltCircleResult.Skipped;}
            object member=Keep(((dynamic)graphic).ModelMember);double radius=(double)((dynamic)graphic).Radius;
            var candidates=Circles(view,part,member).Where(c=>group.Owners.Contains(c.Owner)&&Math.Abs(c.Radius-radius)<=BoltCircleGeometry.Tolerance).ToList();
            var selected=new List<CircleGraphic>();
            foreach(CircleGraphic candidate in candidates)
            {
                var coincident=selected.FirstOrDefault(c=>BoltCircleGeometry.Distance(c.Center,candidate.Center)<=BoltCircleGeometry.Tolerance);
                if(coincident==null)selected.Add(candidate);
                else if(coincident.Owner!=candidate.Owner)
                {detail="B.C. needs review: different pattern holes overlap in this view.";return BoltCircleResult.Review;}
            }
            if(selected.Count!=group.Owners.Count||selected.Select(c=>c.Owner).Distinct().Count()!=group.Owners.Count)
            {detail="B.C. needs review: not all pattern holes have a matching face-on edge in this view.";return BoltCircleResult.Review;}
            var points=selected.Select(c=>c.Center).ToList();CircleFit fit=BoltCircleGeometry.Fit(points,group.AllowPair);
            if(fit==null||(group.RegularRequired&&!BoltCircleGeometry.EquallySpaced(points,fit)))
            {detail="B.C. needs review: the complete hole group does not define one verified circular layout.";return BoltCircleResult.Review;}
            var refs=selected.Select(c=>Keep(((dynamic)view).GetReferenceToGraphicMember2[c.Graphic])).ToList();
            double sx,sy,rx,ry;((dynamic)view).ViewToSheet(fit.X,fit.Y,out sx,out sy);
            ((dynamic)view).ViewToSheet(fit.X+fit.Radius,fit.Y,out rx,out ry);
            double sheetRadius=Math.Sqrt((rx-sx)*(rx-sx)+(ry-sy)*(ry-sy));
            object collection=Keep(((dynamic)sheet).BoltHoleCircles);
            for(int i=1;i<=(int)((dynamic)collection).Count;i++)
            {
                object existing=Keep(((dynamic)collection).Item(i));double x,y,r;((dynamic)existing).Circle2d(out x,out y,out r);
                bool same=Math.Abs(x-sx)<=1e-8&&Math.Abs(y-sy)<=1e-8&&Math.Abs(r-sheetRadius)<=1e-8;
                if(same)
                {
                    bool linked=refs.All(p=>(bool)((dynamic)existing).IsMember[p,0]);
                    if(linked)
                    {
                        try{EnsureProjectionMarks(sheet,view,existing,selected,refs,sx,sy);}
                        catch(ProjectionConflict conflict){detail=conflict.Message;return BoltCircleResult.Review;}
                    }
                    detail=linked?"B.C. already exists ("+selected.Count+" linked holes).":"B.C. needs review: an existing circle occupies this location with different hole references.";
                    if(linked)DimensionTarget(note,sheet,existing,view);
                    return linked?BoltCircleResult.Existing:BoltCircleResult.Review;
                }
            }
            object created=null;int before=(int)((dynamic)collection).Count;
            try
            {
                created=Keep(fit.C<0?((dynamic)collection).AddBoltHoleCircleBy2Points(refs[fit.A],0,refs[fit.B],0):
                    ((dynamic)collection).AddBoltHoleCircleBy3Points(refs[fit.A],0,refs[fit.B],0,refs[fit.C],0));
                foreach(object r in refs)if(!(bool)((dynamic)created).IsMember[r,0])((dynamic)created).AddMember(r,0);
                ApplyStyle(created);
                double x,y,radiusAfter;((dynamic)created).Circle2d(out x,out y,out radiusAfter);
                if(Math.Abs(x-sx)>1e-8||Math.Abs(y-sy)>1e-8||Math.Abs(radiusAfter-sheetRadius)>1e-8||
                    (int)((dynamic)created).Count!=refs.Count||!refs.All(p=>(bool)((dynamic)created).IsMember[p,0]))
                    throw new InvalidOperationException("Native bolt-circle geometry or hole references did not verify.");
                EnsureProjectionMarks(sheet,view,created,selected,refs,sx,sy);
                DimensionTarget(note,sheet,created,view);
                detail="Created native B.C. centerline through "+refs.Count+" pattern holes.";return BoltCircleResult.Created;
            }
            catch(Exception error)
            {
                try{if(created!=null)((dynamic)created).Delete();if((int)((dynamic)collection).Count!=before)throw new Exception("Annotation count did not restore.");}
                catch(Exception rollback){throw new InvalidOperationException("B.C. creation failed: "+error.Message+" Cleanup needs review: "+rollback.Message,error);}
                if(error is ProjectionConflict){detail=error.Message;return BoltCircleResult.Review;}
                throw new InvalidOperationException("B.C. creation rolled back: "+error.Message,error);
            }
        }
        void EnsureProjectionMarks(object sheet,object view,object circle,List<CircleGraphic> selected,List<object> refs,double cx,double cy)
        {
            // SE's BoltHoleCircle API only creates the circular chain line.
            // Greg's manually corrected example uses one native two-point
            // CenterMark per hole: the hole is Object1, the BHC center Object2.
            // Both remain associative; rotating loose sketch lines would not.
            object collection=Keep(((dynamic)sheet).CenterMarks);
            var matches=new List<object>[selected.Count];
            for(int i=0;i<matches.Length;i++)matches[i]=new List<object>();
            for(int i=1;i<=(int)((dynamic)collection).Count;i++)
            {
                object mark=Keep(((dynamic)collection).Item(i)),reference=null;double x,y,z;bool key;
                ((IBcCenterMark)mark).GetCenter(out reference,out x,out y,out z,out key);Keep(reference);
                if(reference==null)continue;
                object graphic;
                try{graphic=Keep(((dynamic)reference).Object);}
                catch(COMException){continue;} // Free-space/non-view center marks.
                catch(Microsoft.CSharp.RuntimeBinder.RuntimeBinderException){continue;}
                if(graphic==null||(int)((dynamic)graphic).Type!=CircleType)continue;
                object markView=Keep(((dynamic)graphic).DrawingView);
                if(Identity(markView)!=Identity(view))continue;
                ((dynamic)graphic).GetCenterPoint(out x,out y);
                for(int j=0;j<selected.Count;j++)
                    if(BoltCircleGeometry.Distance(selected[j].Center,new CirclePoint(x,y))<=BoltCircleGeometry.Tolerance)matches[j].Add(mark);
            }
            // Check every occupied hole before making changes. Reuse the user's
            // matching radial marks; do not overlay or rotate unrelated marks.
            for(int i=0;i<matches.Length;i++)
            {
                if(matches[i].Count==0)continue;
                if(matches[i].Count!=1||!(bool)((dynamic)matches[i][0]).TwoPoint)
                    throw new ProjectionConflict("B.C. needs review: a hole already has a different or duplicate center mark.");
                object arc=null;double x,y,z;bool key;
                ((IBcCenterMark)matches[i][0]).GetArcCenter(out arc,out x,out y,out z,out key);Keep(arc);
                if(arc==null||Identity(arc)!=Identity(circle)||!key)
                    throw new ProjectionConflict("B.C. needs review: an existing hole center mark uses a different radial reference.");
            }
            var added=new List<object>();var changed=new List<Tuple<object,bool,int>>();
            int before=(int)((dynamic)collection).Count;
            try
            {
                for(int i=0;i<selected.Count;i++)
                {
                    object mark;
                    if(matches[i].Count==1)
                    {
                        mark=matches[i][0];object style=Keep(((dynamic)mark).Style);
                        bool auto=(bool)((dynamic)style).AutoCenterProjectionLine;int display=(int)((dynamic)style).ProjectionLineDisplay;
                        if(!auto||display!=3)
                        {
                            changed.Add(Tuple.Create(style,auto,display));
                            ((dynamic)style).AutoCenterProjectionLine=true;((dynamic)style).ProjectionLineDisplay=3;
                        }
                    }
                    else
                    {
                        double x,y;((dynamic)view).ViewToSheet(selected[i].Center.X,selected[i].Center.Y,out x,out y);
                        mark=Keep(((dynamic)collection).Add2PointCenterMark(refs[i],x,y,0.0,true,circle,cx,cy,0.0,true,true));
                        added.Add(mark);ApplyStyle(mark);
                    }
                    VerifyProjectionMark(mark,circle,view,selected[i].Center);
                }
                ProjectionMarksCreated+=added.Count;ProjectionMarksUpdated+=changed.Count;
            }
            catch(Exception error)
            {
                var failures=new List<string>();
                for(int i=added.Count-1;i>=0;i--)try{((dynamic)added[i]).Delete();}catch(Exception e){failures.Add(e.Message);}
                foreach(var saved in changed)try{((dynamic)saved.Item1).AutoCenterProjectionLine=saved.Item2;((dynamic)saved.Item1).ProjectionLineDisplay=saved.Item3;}catch(Exception e){failures.Add(e.Message);}
                if((int)((dynamic)collection).Count!=before)failures.Add("Center-mark count did not restore.");
                throw new InvalidOperationException("Projection marks failed: "+error.Message+(failures.Count>0?" Cleanup needs review: "+String.Join("; ",failures):" Changes rolled back."),error);
            }
        }
        void VerifyProjectionMark(object mark,object circle,object view,CirclePoint center)
        {
            object reference=null,arc=null;double x,y,z,ax,ay,az;bool key,arcKey;
            ((IBcCenterMark)mark).GetCenter(out reference,out x,out y,out z,out key);Keep(reference);
            ((IBcCenterMark)mark).GetArcCenter(out arc,out ax,out ay,out az,out arcKey);Keep(arc);
            object graphic=Keep(((dynamic)reference).Object),markView=Keep(((dynamic)graphic).DrawingView),style=Keep(((dynamic)mark).Style);
            ((dynamic)graphic).GetCenterPoint(out x,out y);
            if(!(bool)((dynamic)mark).TwoPoint||!key||!arcKey||arc==null||Identity(arc)!=Identity(circle)||
                Identity(markView)!=Identity(view)||BoltCircleGeometry.Distance(center,new CirclePoint(x,y))>BoltCircleGeometry.Tolerance||
                !(bool)((dynamic)style).AutoCenterProjectionLine||(int)((dynamic)style).ProjectionLineDisplay!=3)
                throw new InvalidOperationException("Native projection-mark attachments or display did not verify.");
        }
        void ApplyStyle(object circle)
        {
            object style=Keep(((dynamic)circle).Style);
            using(var stream=typeof(BoltCircles).Assembly.GetManifestResourceStream("TurboCallout.AnnotationStandard"))
            {
                if(stream==null)throw new InvalidOperationException("The packaged centerline standard is missing.");
                var category=XDocument.Load(stream).Root.Elements("Category").Single(c=>(string)c.Attribute("name")=="CenterLine");
                string[] wanted={"LineWidth","DrivenColor","CenterLineType","CenterMarkSize","CenterMarkExtension","AutoCenterProjectionLine","ProjectionLineDisplay"};
                foreach(string name in wanted)
                {
                    XElement item=category.Element("Style").Elements("Property").Single(p=>(string)p.Attribute("name")==name);
                    string raw=(string)item.Attribute("value"),type=(string)item.Attribute("type");
                    object value=type=="System.Double"?(object)Double.Parse(raw,CultureInfo.InvariantCulture):type=="System.Boolean"?(object)Boolean.Parse(raw):type=="System.String"?(object)raw:Int32.Parse(raw,CultureInfo.InvariantCulture);
                    if(!Property.Equal(Property.Get(style,name),value))Property.Set(style,name,value);
                    if(!Property.Equal(Property.Get(style,name),value))throw new InvalidOperationException("B.C. style did not persist: "+name);
                }
            }
        }
        public void Dispose()
        {
            try{((dynamic)draft).Activate();}
            finally{for(int i=held.Count-1;i>=0;i--)Com.Release(held[i]);held.Clear();}
        }
    }
}
