using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;

partial class CapDetailTool {
 static string NativeEdgeKey(object edge){var line=edge as D.DVLine2d;return line!=null?line.get_Key(true):((D.DVArc2d)edge).get_Key(true);}
 static void ProbeNativeKeys19679(){LoadCap();foreach(D.DVLine2d edge in csection.DVLines2d){F.Reference reference=null;object target=null;try{reference=edge.Reference;target=reference.Object;string a=NativeEdgeKey(edge),b=NativeEdgeKey(target);Console.WriteLine("native="+a+" referenced="+b+" COM="+SameNativeObject(edge,target));CRequire(!string.IsNullOrEmpty(a)&&a==b,"Reference key mismatch");}finally{Release(target);Release(reference);Release(edge);}}Console.WriteLine("PASS: all section edges retain their native parent-relative key through Reference.Object.");}
 static void InspectDimensionLinks19679(){LoadCap();var dims=(S.Dimensions)csh.Dimensions;foreach(int i in new[]{6,13,15}){var d=dims.Item(i);int count;d.GetRelatedCount(out count);Console.WriteLine("DIM "+i+" related="+count);for(int j=0;j<count;j++){object o=null;double x,y,z;bool k;d.GetRelated(j,out o,out x,out y,out z,out k);Console.WriteLine("PARENT "+j+" "+x+","+y+" key="+k+" ref="+(o is F.Reference));var r=o as F.Reference;if(r!=null){object target=r.Object;Console.WriteLine("TARGET line="+(target is D.DVLine2d)+" arc="+(target is D.DVArc2d));if(target is D.DVLine2d)Console.WriteLine(Scalars(target,"Draft","DVLine2d","Line"));Release(target);}Release(o);}}}
 static void Release(object o){if(o!=null&&Marshal.IsComObject(o))Marshal.ReleaseComObject(o);}
 static bool SameNativeObject(object a,object b){IntPtr pa=Marshal.GetIUnknownForObject(a),pb=Marshal.GetIUnknownForObject(b);try{return pa==pb;}finally{Marshal.Release(pa);Marshal.Release(pb);}}
 static void VerifyParents(S.Dimension dimension,object[] expected){
  int count;dimension.GetRelatedCount(out count);CRequire(count==expected.Length,"Unexpected native parent count");var seen=new bool[count];
  // GetRelated is zero-based. Reference.Object creates another COM identity for
  // the same edge. Siemens documents Key(true) as the recursive parent-relative
  // native identity; a read-only round-trip test checked every section edge.
  // Require both the owning view and that native key, not geometric coincidence.
  for(int i=0;i<count;i++){object related=null,target=null;D.DrawingView owner=null;try{double x,y,z;bool key;dimension.GetRelated(i,out related,out x,out y,out z,out key);var reference=related as F.Reference;CRequire(reference!=null,"Dimension parent is not a model reference");target=reference.Object;
   var line=target as D.DVLine2d;var arc=target as D.DVArc2d;CRequire(line!=null||arc!=null,"Dimension parent is not a projected edge");owner=line!=null?line.DrawingView:arc.DrawingView;CRequire(SameNativeObject(owner,csection),"Dimension parent belongs to another view");string nativeKey=NativeEdgeKey(target);CRequire(!string.IsNullOrEmpty(nativeKey),"Empty native edge key");bool found=false;for(int j=0;j<count;j++)if(!seen[j]&&nativeKey==NativeEdgeKey(expected[j])){seen[j]=true;found=true;break;}CRequire(found,"Replacement points to an unintended model edge");
  }finally{Release(owner);Release(target);Release(related);}}
  Console.WriteLine("Verified "+count+" exact model-edge parents for "+dimension.GetComment());
 }
 static void ValidateCandidate(S.Dimension old,S.Dimension candidate,object[] expected){try{CopyDimensionContent(old,candidate);VerifyParents(candidate,expected);}catch{candidate.Delete();throw;}}
 static void CopyDimensionContent(S.Dimension old,S.Dimension fresh){
  // Presentation and engineering data are independent of placement. Keep both.
  fresh.Style=old.Style;fresh.PrefixString=old.PrefixString;fresh.SuffixString=old.SuffixString;fresh.SuperfixString=old.SuperfixString;fresh.SubfixString=old.SubfixString;fresh.SubfixString2=old.SubfixString2;
  if(old.DisplayType==S.DimDispTypeConstants.igDimDisplayTypeLimits){double tolerance;string raw=old.PrimaryUpperTolerance.Replace(" in","").Trim();CRequire(double.TryParse(raw,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out tolerance),"Tolerance is not numeric");fresh.Style.PrimaryLinearToleranceUnits=S.DimLinearUnitConstants.igDimStyleLinearInches;fresh.DisplayType=S.DimDispTypeConstants.igDimDisplayTypeUnitTolerance;fresh.PrimaryUpperTolerance=tolerance.ToString("F5",System.Globalization.CultureInfo.InvariantCulture);fresh.PrimaryLowerTolerance=(-tolerance).ToString("F5",System.Globalization.CultureInfo.InvariantCulture);}
  fresh.DisplayType=old.DisplayType;fresh.Style.PrimaryDecimalRoundOff=old.Style.PrimaryDecimalRoundOff;fresh.Style.SecondaryDecimalRoundOff=old.Style.SecondaryDecimalRoundOff;fresh.SetComment(old.GetComment());
  CRequire(Math.Abs(fresh.Value-old.Value)<1e-9&&Math.Abs(fresh.SecondValue-old.SecondValue)<1e-9&&fresh.StatusOfDimension==S.DimStatusConstants.seDimStatusDriven,"Replacement dimension is not equivalent");
 }
 static S.Dimension NewAxial(S.Dimension old,double r,double z1,double z2){
  var a=Rad(csection,r,z1);var b=Rad(csection,r,z2);var ea=CEdge(csection,a);var eb=CEdge(csection,b);
  try{var d=((S.Dimensions)csh.Dimensions).AddDistanceBetweenObjects(ea.Reference,a[0],a[1],0,true,eb.Reference,b[0],b[1],0,true);d.MeasurementAxisEx=S.DimAxisModeConstants.igDimAxisModeDefault;d.MeasurementAxisDirection=false;ValidateCandidate(old,d,new object[]{ea,eb});return d;}
  finally{Release(ea);Release(eb);}
 }
 static S.Dimension NewChamfer(S.Dimension old,double desiredX,double desiredY){
  double setback=old.Value,angle=old.SecondValue;D.DVLine2d chosen=null;double best=double.MaxValue;
  foreach(D.DVLine2d l in csection.DVLines2d){double a,b,c,d;l.GetStartPoint(out a,out b);l.GetEndPoint(out c,out d);csection.ViewToSheet(a,b,out a,out b);csection.ViewToSheet(c,d,out c,out d);double dx=Math.Abs(c-a)/csection.ScaleFactor,dy=Math.Abs(d-b)/csection.ScaleFactor;bool match=Math.Abs(dx-setback)<1e-7&&Math.Abs(dy-dx*Math.Tan(angle))<1e-7;double gap=Math.Pow((a+c)/2-desiredX,2)+Math.Pow((b+d)/2-desiredY,2);if(match&&gap<best){Release(chosen);chosen=l;best=gap;}else Release(l);}
  CRequire(chosen!=null,"Chamfer edge not found");double x,y,u,v;chosen.GetStartPoint(out x,out y);chosen.GetEndPoint(out u,out v);csection.ViewToSheet(x,y,out x,out y);csection.ViewToSheet(u,v,out u,out v);
  try{foreach(D.DVLine2d a in csection.DVLines2d){try{double p,q,r,s;a.GetStartPoint(out p,out q);a.GetEndPoint(out r,out s);csection.ViewToSheet(p,q,out p,out q);csection.ViewToSheet(r,s,out r,out s);bool axis=Math.Abs(angle-Math.PI/6)<1e-6?Math.Abs(p-r)<1e-8:Math.Abs(q-s)<1e-8;double gap=Math.Min(Math.Min(Math.Abs(p-x)+Math.Abs(q-y),Math.Abs(r-x)+Math.Abs(s-y)),Math.Min(Math.Abs(p-u)+Math.Abs(q-v),Math.Abs(r-u)+Math.Abs(s-v)));if(!axis||gap>1e-7)continue;
   var dim=((S.Dimensions)csh.Dimensions).AddChamfer(a.Reference,(p+r)/2,(q+s)/2,0,false,chosen.Reference,(x+u)/2,(y+v)/2,0,false,S.DimChamferModeConstants.igDimChamferModeParallel);ValidateCandidate(old,dim,new object[]{a,chosen});return dim;
  }finally{Release(a);}}throw new Exception("Chamfer adjoining edge not found");}finally{Release(chosen);}
 }
 static S.Dimension NewUpperRadius(S.Dimension old){foreach(D.DVArc2d arc in csection.DVArcs2d){try{double x,y;arc.GetCenterPoint(out x,out y);csection.ViewToSheet(x,y,out x,out y);if(Math.Abs(arc.Radius-old.Value)>1e-8||y<.19)continue;var dim=((S.Dimensions)csh.Dimensions).AddRadius(arc.Reference);ValidateCandidate(old,dim,new object[]{arc});return dim;}finally{Release(arc);}}throw new Exception("Upper radius not found");}
 static S.Dimension ByRole(S.Dimensions dims,int role){string wanted="CAP19679-"+role;foreach(S.Dimension d in dims){if(d.GetComment()==wanted)return d;Release(d);}throw new Exception("Missing placement role "+role);}
 static void PutLinear(S.Dimension d,double x,double y){d.SetKeyPoint(d.KeyPointCount-1,x,y,0);}
 static double RadialTerminatorAngle(S.Dimension d){double x,y,z,cx,cy;F.KeyPointType t;int h;d.GetKeyPoint(0,out x,out y,out z,out t,out h);cface.GetOrigin(out cx,out cy);return Math.Atan2(y-cy,x-cx);}
 static void SetRadialDirection(S.Dimension d,double angle){
  // A center arc uses its own parameter origin and interval, which can differ
  // from a model circle after view regeneration. Calibrate that native parameter
  // from two positions; retain the existing dimension and circle association.
  d.TrackDistance=0;double a=RadialTerminatorAngle(d);d.TrackDistance=1;double step=RadialTerminatorAngle(d)-a;
  while(step>Math.PI)step-=2*Math.PI;while(step< -Math.PI)step+=2*Math.PI;CRequire(Math.Abs(step)>.01,"Invalid radial parameter interval");
  double target=(angle-a)/step,period=2*Math.PI/Math.Abs(step);while(target<0)target+=period;while(target>=period)target-=period;d.TrackDistance=target;
  double error=RadialTerminatorAngle(d)-angle;while(error>Math.PI)error-=2*Math.PI;while(error< -Math.PI)error+=2*Math.PI;CRequire(Math.Abs(error)<1e-5,"Radial leader direction did not settle");
 }
 static string EngineeringFingerprint(S.Dimension d){return ((int)d.DimensionType)+"|"+Math.Round(d.Value,10)+"|"+Math.Round(d.SecondValue,10)+"|"+d.DisplayType+"|"+d.PrefixString+"|"+d.SuffixString+"|"+d.SuperfixString+"|"+d.SubfixString+"|"+d.SubfixString2+"|"+string.Join("/",CTexts(d));}
 static void Rearrange19679(){LoadCap();var dims=(S.Dimensions)csh.Dimensions;CRequire(dims.Count==19,"Unexpected annotation count");
  for(int i=1;i<=dims.Count;i++){string role=dims.Item(i).GetComment();CRequire(string.IsNullOrEmpty(role)||role=="CAP19679-"+i,"Drawing is already rearranged; use placement-refine instead of rebuilding annotations");}
  string backup=Path.Combine(CapWork,"placement-backup.dft");
  if(File.Exists(backup)){
   // Resume a failed candidate build only while all original engineering content
   // is intact. Discard our unsaved placement attempt, reload the saved original,
   // and retain the original recovery copy until final verification succeeds.
   var baseline=File.ReadAllLines(Path.Combine(CapWork,"placement-before.txt"));
   CRequire(baseline.SequenceEqual(dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s)),"Current content differs from recovery baseline");
   cdoc.Close(false);Release(cdoc);LoadCap();dims=(S.Dimensions)csh.Dimensions;
   CRequire(baseline.SequenceEqual(dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s)),"Saved original differs from recovery baseline");
  }else{cdoc.Save();cdoc.SaveCopyAs(backup);}
  var before=dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s).ToArray();File.WriteAllLines(Path.Combine(CapWork,"placement-before.txt"),before);
  var original=dims.Cast<S.Dimension>().ToArray();for(int i=0;i<original.Length;i++)original[i].SetComment("CAP19679-"+(i+1));
  // Use the same clear view spacing as the supplied actual drawing. Preserve
  // the current orientations and section direction; the user requested placement only.
  cface.SetOrigin(.327798787577005,.165361170037598);csection.SetOrigin(.132024326085195,.165361170037598);ciso.SetOrigin(.214578711184290,.0542211684716281);ciso.ScaleFactor=.5;ciso.Update();
  var replacements=new Dictionary<int,S.Dimension>();
  try{
   // Upper dimensions attach to the upper silhouette, not edges across the view.
   replacements[6]=NewAxial(original[5],-.055,-.028448,-.006096);
   replacements[7]=NewAxial(original[6],.077,-.028448,-.015748);
   replacements[8]=NewAxial(original[7],.072,-.006096,-.013716);
   replacements[9]=NewAxial(original[8],-.0718,-.013716,-.0100965);
   replacements[10]=NewAxial(original[9],-.087,-.028448,-.0200345925196437);
   replacements[11]=NewAxial(original[10],-.087,-.0236540925196437,-.0200345925196437);
   replacements[19]=NewAxial(original[18],-.08763,-.015748,-.0171262925196437);
   replacements[13]=NewChamfer(original[12],.1236423,.1320046);
   replacements[17]=NewChamfer(original[16],.1236423,.2315504);
   replacements[15]=NewUpperRadius(original[14]);
   // Check every replacement before removing any original. The backup remains
   // available until the complete saved and reopened drawing passes verification.
   foreach(var kv in replacements)CRequire(EngineeringFingerprint(kv.Value)==EngineeringFingerprint(original[kv.Key-1]),"Engineering content changed for role "+kv.Key+"\nBefore: "+EngineeringFingerprint(original[kv.Key-1])+"\nAfter: "+EngineeringFingerprint(kv.Value));
   foreach(int role in replacements.Keys.OrderByDescending(i=>i))original[role-1].Delete();
  }catch{foreach(var d in replacements.Values){try{d.Delete();}catch{}}throw;}
  PlaceGrouped19679();cdoc.Save();CPreview();
 }
 static void PlaceGrouped19679(){
  var dims=(S.Dimensions)csh.Dimensions;
  ByRole(dims,8).BreakPosition=S.DimBreakPositionConstants.igDimBreakLeft;
  // Nested diameter banks, staggered numerals, short axial dimensions nearest
  // the relevant silhouette. The additional retained dimensions need more room
  // than the example, so their labels use separate upper and lower rows.
  double[,] pos={{1,.077,.165},{2,.035,.165},{3,.054,.188},{4,.196,.175},{5,.173,.151},{18,.216,.139},{6,.108,.263},{7,.100,.082},{8,.166,.082},{9,.164,.237},{10,.083,.252},{11,.065,.238},{19,.181,.251}};
  for(int i=0;i<pos.GetLength(0);i++)PutLinear(ByRole(dims,(int)pos[i,0]),pos[i,1],pos[i,2]);
  ByRole(dims,10).Style.SecondaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_4;
  var bc=ByRole(dims,12);bc.TerminatorPosition=true;bc.DisplayHalf=false;bc.BreakPosition=S.DimBreakPositionConstants.igDimBreakRight;SetRadialDirection(bc,5*Math.PI/6);bc.LeaderDistance=-.00476250005885959;bc.BreakDistance=.007;
  var chamfer=ByRole(dims,13);chamfer.TerminatorPosition=true;chamfer.BreakPosition=S.DimBreakPositionConstants.igDimBreakRight;chamfer.TrackDistance=2.35619449615479;chamfer.BreakDistance=.0177116505801678;chamfer.LeaderDistance=-.00476250005885959;
  chamfer=ByRole(dims,17);chamfer.TerminatorPosition=true;chamfer.BreakPosition=S.DimBreakPositionConstants.igDimBreakRight;chamfer.TrackDistance=2.09439516067505;chamfer.BreakDistance=.00874701421707869;chamfer.LeaderDistance=-.00476250005885959;
  // Use the canonical distance saved by Solid Edge for this leftward text block;
  // a shorter transient value is normalized on reopening and shifts the label.
  chamfer=ByRole(dims,14);chamfer.TerminatorPosition=true;chamfer.BreakPosition=S.DimBreakPositionConstants.igDimBreakRight;chamfer.BreakDistance=.012;chamfer.LeaderDistance=-.0390841104090214;
  var rad=ByRole(dims,15);rad.BreakPosition=S.DimBreakPositionConstants.igDimBreakRight;rad.TrackDistance=.667314410209656;rad.BreakDistance=.00695227039977908;rad.LeaderDistance=.0047625;
  rad=ByRole(dims,16);rad.BreakPosition=S.DimBreakPositionConstants.igDimBreakLeft;rad.TrackDistance=.56305342912674;rad.BreakDistance=.00715418625622988;rad.LeaderDistance=.0047625;
  var hole=((S.Balloons)csh.Balloons).Item(1);hole.BreakLineDistance=.005;hole.SetKeyPoint(2,.257,.239,0);
  double[,] finish={{.091,.19871772},{.040,.22991211},{.184,.21845352},{.043,.09917193},{.184,.11060193}};foreach(S.SurfaceFinishSymbol f in (S.SurfaceFinishSymbols)csh.SurfaceFinishSymbols)f.SetKeyPoint(1,finish[f.Index-1,0],finish[f.Index-1,1],0);
  csection.SetCaptionPosition(.132,.065);
 }
 static void InspectPlacement19679(){LoadCap();PrintPlacement(cdoc,"CURRENT");string path=Path.Combine(folder,"CAP_19679-actual.dft");bool wasOpen=IsOpen(path);var reference=(D.DraftDocument)app.Documents.Open(path);try{PrintPlacement(reference,"ACTUAL");}finally{if(!wasOpen)reference.Close(false);Marshal.ReleaseComObject(reference);}cdoc.Activate();}
 static void PrintPlacement(D.DraftDocument doc,string label){Console.WriteLine(label);var sheet=doc.Sheets.Item("Sheet1");foreach(D.DrawingView v in sheet.DrawingViews){double x,y;v.GetOrigin(out x,out y);var pt=CMap(v,0,0,-.017272);Console.WriteLine("VIEW "+v.Index+" origin "+x+","+y+" center "+pt[0]+","+pt[1]);}foreach(S.Dimension d in (S.Dimensions)sheet.Dimensions){var pt=TextPoint(d);Console.WriteLine("DIM "+d.Index+" "+d.Value+" text "+pt[0]+","+pt[1]);if(d.DimensionType==S.DimTypeConstants.igDimTypeLinear||d.DimensionType==S.DimTypeConstants.igDimTypeSDiameter){try{double x,y,z;F.KeyPointType t;int h;d.GetKeyPoint(8,out x,out y,out z,out t,out h);Console.WriteLine("KEY8 "+x+","+y);}catch{}}}}
}
