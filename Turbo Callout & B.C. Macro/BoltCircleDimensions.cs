using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using HoleTableFormatter;

namespace TurboCallout
{
    // SDK dispatch IDs. Explicit IDispatch outputs avoid late-bound conversion
    // of GetRelated's graphical object. Related/key-point indices are zero based.
    [ComImport,Guid("3f12b760-2211-11ce-9b15-08003601cedf"),InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IBcDimension
    {
        [DispId(39)] void GetRelatedCount(out int count);
        [DispId(40)] void GetRelated(int index,[MarshalAs(UnmanagedType.IDispatch)]out object graphic,out double x,out double y,out double z,out bool keyPoint);
        [DispId(24578)] void GetKeyPoint(int index,out double x,out double y,out double z,out int type,out int handle);
    }
    [ComImport,Guid("4a603c12-e750-11ce-bb40-080036776c02"),InterfaceType(ComInterfaceType.InterfaceIsIDispatch)]
    internal interface IBcNotePosition
    { [DispId(16386)] void GetKeyPoint(int index,out double x,out double y,out double z,out int type,out int handle); }

    internal static class BoltCircleDimensions
    {
        internal static long Identity(object value)
        { IntPtr p=Marshal.GetIUnknownForObject(value);try{return p.ToInt64();}finally{Marshal.Release(p);} }

        // Use the same model facts as note recognition, never the visible note
        // wording or the bolt-circle diameter itself to infer a tolerance class.
        internal static int InchPlaces(Decision choice)
        {
            if(choice==null||choice.Slot==0)throw new InvalidOperationException("Hole type is not verified.");
            if(choice.IsDowel)return 4;
            if(choice.Family=="Spring pin"||choice.Family=="O-ring face seal"||choice.Family=="Koenig plug"||
                choice.Family=="SAE port"||choice.Port>0||choice.Category=="Threaded hole")return 3;
            if(choice.Family=="SHCS counterbore"||choice.Family=="Simple drill")return 2;
            throw new InvalidOperationException("No B.C precision rule for "+choice.Category+".");
        }

        internal static int CommonPlaces(IEnumerable<int> values)
        {
            int[] distinct=values.Distinct().ToArray();
            if(distinct.Length!=1)throw new InvalidOperationException("Associated holes require different B.C decimal places.");
            return distinct[0];
        }

        internal static string WithSuffix(string suffix)
        {
            // Preserve custom suffix wording and avoid a second B.C on reruns.
            return System.Text.RegularExpressions.Regex.IsMatch(suffix??"",@"(?<!\w)B\.C\.?(?!\w)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase)?suffix:
                String.IsNullOrWhiteSpace(suffix)?"B.C":suffix.TrimEnd()+" B.C";
        }

        internal static void Process(object draft,IEnumerable<BoltCircleTarget> targets,RuleBook rules,bool dual,RunReport report,Action<string> progress)
        {
            object sheets=null;
            try
            {
                sheets=((dynamic)draft).Sheets;
                using(var reader=new NativeHole(draft))
                foreach(var group in targets.GroupBy(t=>Identity(t.Circle)))
                {
                    var held=new List<object>();object created=null,dimension=null,style=null;
                    Dictionary<string,object> before=null;string oldSuffix=null;
                    BoltCircleTarget target=group.First();
                    string label=target.SheetName+", B.C for callout "+target.NoteIndex;
                    try
                    {
                        if(progress!=null)progress("Dimensioning "+label+"...");
                        object sheet=((dynamic)sheets).Item(target.SheetName);held.Add(sheet);
                        object notes=((dynamic)sheet).Balloons;held.Add(notes);
                        var associated=new List<object>();var precision=new List<int>();
                        foreach(var t in group)
                        {
                            object note=((dynamic)notes).Item(t.NoteIndex);held.Add(note);associated.Add(note);
                            int? count;precision.Add(InchPlaces(Notes.Recognize(rules,reader.Read(note,out count))));
                        }
                        int places=CommonPlaces(precision);
                        object dimensions=((dynamic)sheet).Dimensions;held.Add(dimensions);
                        var existing=new List<object>();
                        for(int i=1;i<=(int)((dynamic)dimensions).Count;i++)
                        {
                            object item=((dynamic)dimensions).Item(i);held.Add(item);
                            int type=(int)((dynamic)item).DimensionType;
                            if((type==4||type==5)&&RelatedTo(item,target.Circle))existing.Add(item);
                        }
                        if(existing.Count>1)throw new InvalidOperationException("Multiple diameter dimensions already reference this bolt circle.");
                        double cx,cy,r;((dynamic)target.Circle).Circle2d(out cx,out cy,out r);
                        double expected=2*r/(double)((dynamic)target.View).ScaleFactor;
                        if(existing.Count==0)
                        {
                            created=dimension=((dynamic)dimensions).AddRadialDiameter(target.Circle);held.Add(dimension);
                            style=((dynamic)dimension).Style;held.Add(style);
                            ApplyPresentation(style);
                            new UnitStyle(style,new NotePlan{Dual=dual,InchPlaces=places}).Apply(style);
                            Property.Set(style,"DimensionScaleMode",1); // native automatic drawing-view scale
                            ((dynamic)dimension).DisplayType=1; // nominal native value, not inherited limits
                            ((dynamic)dimension).SuffixString="B.C";
                            Place(dimension,associated[0],cx,cy,r,style);
                        }
                        else
                        {
                            dimension=existing[0];style=((dynamic)dimension).Style;held.Add(style);
                            // Existing user dimensions retain their unit mode, metric
                            // precision, tolerances, leader geometry, and text position.
                            // Only the requested inch precision and suffix are changed.
                            if(!String.IsNullOrEmpty((string)((dynamic)dimension).OverrideString))
                                throw new InvalidOperationException("Existing diameter has overridden text; left unchanged.");
                            before=new Dictionary<string,object>();oldSuffix=(string)((dynamic)dimension).SuffixString;
                            string setting=(int)((dynamic)style).PrimaryUnits==5?"PrimaryDecimalRoundOff":
                                (bool)((dynamic)style).DualDisplay&&(int)((dynamic)style).SecondaryUnits==5?"SecondaryDecimalRoundOff":null;
                            if(setting==null)throw new InvalidOperationException("Existing diameter has no inch value; retained for review.");
                            var wanted=new Dictionary<string,object>{{setting,places+2},
                                {setting=="PrimaryDecimalRoundOff"?"RoundOffType":"SecondaryUnitRoundOff",1},
                                {setting=="PrimaryDecimalRoundOff"?"PrimaryTrailingZeros":"SecondaryTrailingZeros",true}};
                            foreach(var p in wanted){before.Add(p.Key,Property.Get(style,p.Key));Property.Set(style,p.Key,p.Value);}
                            foreach(var p in wanted)if(!Property.Equal(Property.Get(style,p.Key),p.Value))
                                throw new InvalidOperationException("Existing dimension precision did not persist.");
                            ((dynamic)dimension).SuffixString=WithSuffix(oldSuffix);
                        }
                        if(!RelatedTo(dimension,target.Circle)||Math.Abs((double)((dynamic)dimension).Value-expected)>1e-7)
                            throw new InvalidOperationException("Native B.C attachment or model diameter did not verify.");
                        if(created!=null)report.BcDimensionsCreated++;
                        else if(before.Any(p=>!Property.Equal(p.Value,Property.Get(style,p.Key)))||oldSuffix!=(string)((dynamic)dimension).SuffixString)
                            report.BcDimensionsUpdated++;
                        else report.BcDimensionsExisting++;
                        report.Details.Add(label+": "+places+"-place inch smart diameter"+(created!=null?" placed near the note.":"; existing placement retained."));
                    }
                    catch(Exception error)
                    {
                        try
                        {
                            if(created!=null)((dynamic)created).Delete();
                            else if(before!=null)
                            {foreach(var p in before)Property.Set(style,p.Key,p.Value);((dynamic)dimension).SuffixString=oldSuffix;}
                        }
                        catch(Exception cleanup){report.Errors++;report.Details.Add(label+": dimension restoration needs review: "+cleanup.Message);}
                        report.Review++;report.BcDimensionsReview++;
                        report.Details.Add(label+": dimension left for review: "+error.Message);
                    }
                    finally{for(int i=held.Count-1;i>=0;i--)Com.Release(held[i]);}
                }
            }
            finally{Com.Release(sheets);}
        }

        internal static bool RelatedTo(object dimension,object circle)
        {
            int count;((IBcDimension)dimension).GetRelatedCount(out count);
            for(int i=0;i<count;i++)
            {
                object reference=null;double x,y,z;bool key;
                try
                {((IBcDimension)dimension).GetRelated(i,out reference,out x,out y,out z,out key);
                    if(reference!=null&&Identity(reference)==Identity(circle))return true;}
                finally{Com.Release(reference);}
            }
            return false;
        }

        static void ApplyPresentation(object style)
        {
            using(var stream=typeof(BoltCircleDimensions).Assembly.GetManifestResourceStream("TurboCallout.DimensionStandard"))
            {
                if(stream==null)throw new InvalidOperationException("Embedded dimension standard is missing.");
                foreach(var p in XDocument.Load(stream).Root.Elements("Property"))
                {
                    string type=(string)p.Attribute("type"),value=(string)p.Attribute("value");
                    object parsed=type=="System.Boolean"?(object)Boolean.Parse(value):type=="System.Double"?
                        (object)Double.Parse(value,CultureInfo.InvariantCulture):type=="System.String"?(object)value:
                        (object)Int32.Parse(value,CultureInfo.InvariantCulture);
                    Property.Set(style,(string)p.Attribute("name"),parsed);
                }
            }
        }

        static void Place(object dimension,object note,double cx,double cy,double radius,object style)
        {
            double x,y,z;int type,handle;
            // Attached callouts return (0,0) from GetFreeSpacePoint/GetVertex.
            // Native key point 1 is the leader elbow, including after filling.
            if(!(bool)((dynamic)note).Leader||!(bool)((dynamic)note).BreakLine||(int)((dynamic)note).KeyPointCount<3)
                throw new InvalidOperationException("Callout has no usable leader elbow for nearby dimension placement.");
            ((IBcNotePosition)note).GetKeyPoint(1,out x,out y,out z,out type,out handle);
            double xmin,ymin,xmax,ymax;((dynamic)note).Range(out xmin,out ymin,out xmax,out ymax);
            double height=(double)((dynamic)style).Height*(double)((dynamic)dimension).TextScale;
            // Outside the entire note's range leaves room for stacked dual text
            // and multiline manufacturing instructions without changing the note.
            double targetY=y>=cy?Math.Max(y,ymax)+3*height:Math.Min(y,ymin)-3*height;
            double dx=x-cx,dy=targetY-cy,distance=Math.Sqrt(dx*dx+dy*dy);
            if(!BoltCircleGeometry.Finite(distance)||distance<=radius+height)
                throw new InvalidOperationException("Callout is too close to the B.C center for a clear outside diameter leader.");
            double angle=Math.Atan2(dy,dx);if(angle<0)angle+=2*Math.PI;
            // For a radial diameter, TrackDistance is the radial angle (radians),
            // NOT a linear distance. BreakDistance is the radial extension past
            // the circumference. These match Greg's native Smart Dimension.
            ((dynamic)dimension).TrackDistance=angle;
            // This is the radial-dimension break mode, not the side of the
            // drawing. Switching it to Left reverses the radial extension.
            // Right matches the manually positioned Smart Dimension on both
            // sides; TrackDistance alone selects the leader's radial direction.
            ((dynamic)dimension).BreakPosition=1;
            ((dynamic)dimension).BreakDistance=distance-radius;
            ((dynamic)dimension).TerminatorPosition=true;
            // Applying the style alone leaves SE's initial cached break length
            // until save/reopen. Set it after the leader exists so the first
            // display and reopened drawing both use the reference 1.5 heights.
            ((dynamic)dimension).LeaderDistance=height*(double)((dynamic)style).LeaderLineLength;
            if(!(bool)((dynamic)dimension).Leader||Math.Abs((double)((dynamic)dimension).TrackDistance-angle)>1e-6||
                Math.Abs((double)((dynamic)dimension).BreakDistance-(distance-radius))>1e-6)
                throw new InvalidOperationException("Native dimension leader placement did not persist.");
            double actualX,actualY,actualZ;int actualType,actualHandle;
            ((IBcDimension)dimension).GetKeyPoint(3,out actualX,out actualY,out actualZ,out actualType,out actualHandle);
            if(Math.Abs(actualY-targetY)>1e-6||Math.Abs(actualX-x)>12*height)
                throw new InvalidOperationException("B.C text did not land beside its associated callout.");
        }
    }
}
