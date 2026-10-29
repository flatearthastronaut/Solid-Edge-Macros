using System;
using System.IO;
using System.Linq;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;

partial class CapDetailTool {
 const double CapDrawingScale=.625;
 static void InspectHiddenMembers19679(){LoadCap();var members=cface.ModelMembers;try{PrintHiddenMembers(members);}finally{Release(members);}CPreview();}
 static void PrintHiddenMembers(D.ModelMembers members){foreach(D.ModelMember member in members){var children=member.ModelMembers;try{Console.WriteLine("MEMBER file="+member.FileName+" component="+member.ComponentName+" type="+member.Type+" compType="+member.ComponentType+" hidden="+member.ShowHiddenEdges+" style="+member.HiddenEdgeStyleName);if(children.Count>0)PrintHiddenMembers(children);}finally{Release(children);Release(member);}}}

 static void SetHiddenMembers(D.ModelMembers members,bool show){
  // Display defaults alone do not change existing member overrides. Apply the
  // setting to projected solid bodies throughout the member hierarchy.
  foreach(D.ModelMember member in members){D.ModelMembers children=null;try{
   if(member.ComponentType==D.ModelMemberComponentTypeConstants.seSolidBodyMemberType){member.ShowHiddenEdges=show;member.HiddenEdgeStyleName="Hidden";}
   children=member.ModelMembers;if(children.Count>0)SetHiddenMembers(children,show);
  }finally{Release(children);Release(member);}}
 }
 static int VerifyHiddenMembers(D.ModelMembers members){int bodies=0;
  foreach(D.ModelMember member in members){D.ModelMembers children=null;try{
   // The part container reports false/blank even when its body override is on.
   // Require the setting on the solid body that actually supplies the graphics.
   if(member.ComponentType==D.ModelMemberComponentTypeConstants.seSolidBodyMemberType){bodies++;CRequire(member.ShowHiddenEdges,"Top-view body hides its hidden edges");CRequire(member.HiddenEdgeStyleName=="Hidden","Hidden edge style differs");}
   children=member.ModelMembers;if(children.Count>0)bodies+=VerifyHiddenMembers(children);
  }finally{Release(children);Release(member);}}
  return bodies;
 }
 static void ResizeAndShowHidden19679(){
  LoadCap();var dims=(S.Dimensions)csh.Dimensions;
  var content=dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s).ToArray();
  var notes=AnnotationContent(csh);
  string backup=Path.Combine(CapWork,"view-scale-backup.dft");
  if(!File.Exists(backup)){cdoc.Save();cdoc.SaveCopyAs(backup);}
  double fx,fy,sx,sy,ix,iy;cface.GetOrigin(out fx,out fy);csection.GetOrigin(out sx,out sy);ciso.GetOrigin(out ix,out iy);
  // Shrink geometry only. Keep the reference's full annotation text size and
  // the model-linked section relationship; neither dimensions nor notes are
  // replaced by this operation.
  cface.ScaleDimensionsAndAnnotations=false;csection.ScaleDimensionsAndAnnotations=false;ciso.ScaleDimensionsAndAnnotations=false;
  cface.Defaults_ShowHiddenEdges=true;cface.Defaults_HiddenEdgeStyleName="Hidden";
  var members=cface.ModelMembers;try{SetHiddenMembers(members,true);}finally{Release(members);}
  cface.ScaleFactor=CapDrawingScale;csection.ScaleFactor=CapDrawingScale;ciso.ScaleFactor=.4;
  cface.Update();csection.Update();ciso.Update();
  cface.SetOrigin(fx,fy);csection.SetOrigin(sx,fy);ciso.SetOrigin(ix,iy);
  PlaceGrouped19679();
  // Preserve the title block's %{Sheet scale} property field and make its
  // source the top view, so future view scale changes stay synchronized.
  var setup=csh.SheetSetup;try{setup.DrawingViewForSheetScale=cface;}finally{Release(setup);}
  cdoc.UpdatePropertyTextCacheAndDisplay();
  foreach(S.Balloon field in (S.Balloons)csh.Background.Balloons){
   if(field.BalloonText.Trim()=="3:4")field.BalloonText="5:8";
   if(field.BalloonText.Contains("%{Sheet scale}"))Console.WriteLine("Title-block scale: "+field.BalloonDisplayedText);
  }
  CRequire(content.SequenceEqual(dims.Cast<S.Dimension>().Select(EngineeringFingerprint).OrderBy(s=>s)),"Scaling changed dimension content");
  CRequire(notes.SequenceEqual(AnnotationContent(csh)),"Scaling changed annotation instructions");
  VerifyViewPresentation19679();cdoc.Save();CPreview();
 }
 static void VerifyViewPresentation19679(){
  CRequire(cface.Defaults_ShowHiddenEdges,"Top-view hidden lines are disabled");
  CRequire(Math.Abs(cface.ScaleFactor-CapDrawingScale)<1e-9&&Math.Abs(csection.ScaleFactor-CapDrawingScale)<1e-9,"Top and section scales differ from 5:8");
  CRequire(Math.Abs(ciso.ScaleFactor-.4)<1e-9,"Isometric scale differs from 2:5");
  var members=cface.ModelMembers;try{CRequire(VerifyHiddenMembers(members)>0,"No solid-body display member found");}finally{Release(members);}
 }
}
