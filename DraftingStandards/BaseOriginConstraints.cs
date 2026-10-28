using System;
using System.Runtime.InteropServices;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;

// Connect the model-linked projected Base CSYS origin to the actual profile
// segment. A point-on relation permits extension along the cutting line while
// the existing Horizontal/Vertical relation maintains its direction.
internal static class BaseOriginConstraints {
 internal static bool Passes(double ax,double ay,double bx,double by,double x,double y){
  double dx=bx-ax,dy=by-ay,length2=dx*dx+dy*dy;if(length2<1e-20)return false;
  double t=((x-ax)*dx+(y-ay)*dy)/length2;
  return t>=-1e-8&&t<=1+1e-8&&Math.Abs((x-ax)*dy-(y-ay)*dx)/Math.Sqrt(length2)<1e-9;
 }
 static D.ModelMember BaseMember(D.ModelMembers members){
  foreach(D.ModelMember member in members){
   if(member.ComponentType==D.ModelMemberComponentTypeConstants.seCoordinateSystemMemberType&&member.ComponentName=="Base")return member;
   var children=member.ModelMembers;D.ModelMember found;
   try{found=BaseMember(children);}finally{Marshal.ReleaseComObject(children);Marshal.ReleaseComObject(member);}
   if(found!=null)return found;
  }return null;
 }
 internal static bool Has(S.Line2d line){
  var relationships=line.Relationships;
  try{for(int i=1;i<=relationships.Count;i++){
   object item=relationships.Item(i);
   try{var relation=item as S.Relation2d;if(relation==null||relation.Type!=(int)F.ObjectType.igPointOnRelation2d)continue;
    int count;relation.GetRelatedObjectCount(out count);
    for(int j=1;j<=count;j++){object related;int key;relation.GetRelatedObject(j,out related,out key);
     try{var reference=related as F.Reference;if(reference==null||key!=0)continue;
      object target=reference.Object;
      try{var point=target as D.DVPoint2d;if(point==null)continue;var member=point.ModelMember;
       try{if(member.ComponentType==D.ModelMemberComponentTypeConstants.seCoordinateSystemMemberType&&member.ComponentName=="Base")return true;}finally{Marshal.ReleaseComObject(member);}
      }finally{if(target!=null)Marshal.ReleaseComObject(target);}
     }finally{if(related!=null)Marshal.ReleaseComObject(related);}
    }
   }finally{Marshal.ReleaseComObject(item);}
  }return false;}finally{Marshal.ReleaseComObject(relationships);}
 }
 internal static int Ensure(D.CuttingPlane cp,bool verify=false){
  var view=cp.SourceDrawingView;double x=0,y=0;bool found=false;
  var systems=view.CoordinateSystems2d;
  try{foreach(D.CoordinateSystem2d system in systems){try{if(system.Name=="Base"){system.GetOrigin(out x,out y);found=true;break;}}finally{Marshal.ReleaseComObject(system);}}}finally{Marshal.ReleaseComObject(systems);}
  if(!found)throw new Exception("Base coordinate system not found in cutting-plane source view");
  var profile=cp.Profile;int count=0;D.ModelMember member=null;
  try{
   // Determine incidence before exposing model geometry or creating relations.
   bool applicable=false;foreach(S.Line2d line in profile.Lines2d){try{double a,b,c,d;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);if(Passes(a,b,c,d,x,y))applicable=true;}finally{Marshal.ReleaseComObject(line);}}
   if(!applicable)return 0;
   if(!verify){var members=view.ModelMembers;try{member=BaseMember(members);}finally{Marshal.ReleaseComObject(members);}if(member==null)throw new Exception("Projected Base model member is missing");
    // Keep the real origin available to the Draft relationship solver.
    if(member.DisplayType==D.ModelMemberDisplayTypeConstants.seHidePart){member.DisplayType=D.ModelMemberDisplayTypeConstants.seShowPart;view.Update();}
    profile.BeginEdit();
   }
   try{foreach(S.Line2d line in profile.Lines2d){try{
    double a,b,c,d;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);if(!Passes(a,b,c,d,x,y))continue;
    if(!Has(line)){
     if(verify)throw new Exception("Cutting plane "+cp.Caption+" lacks a Base-origin point-on relation");
     var points=member.DVPoints2d;D.DVPoint2d origin=null;
     try{foreach(D.DVPoint2d point in points){double u,v;point.GetPoint(out u,out v);view.ViewToSheet(u,v,out u,out v);if(Math.Abs(u-x)+Math.Abs(v-y)<1e-9){origin=point;break;}Marshal.ReleaseComObject(point);}}finally{Marshal.ReleaseComObject(points);}
     if(origin==null)throw new Exception("Base origin point was not generated");
     var reference=origin.Reference;var relations=profile.Relations2d;S.Relation2d relation=null;
     try{relation=relations.AddPointOn(reference,0,line);if(!Has(line))throw new Exception("Base-origin relation readback failed");}
     finally{if(relation!=null)Marshal.ReleaseComObject(relation);Marshal.ReleaseComObject(relations);Marshal.ReleaseComObject(reference);Marshal.ReleaseComObject(origin);}
     double aa,bb,cc,dd;line.GetStartPoint(out aa,out bb);line.GetEndPoint(out cc,out dd);
     if(Math.Abs(a-aa)+Math.Abs(b-bb)+Math.Abs(c-cc)+Math.Abs(d-dd)>1e-9)throw new Exception("Origin constraint changed the cutting line");
    }count++;
   }finally{Marshal.ReleaseComObject(line);}}}finally{if(!verify)profile.EndEdit();}
   return count;
  }finally{if(member!=null)Marshal.ReleaseComObject(member);Marshal.ReleaseComObject(profile);Marshal.ReleaseComObject(view);}
 }
 internal static void Test(){if(!Passes(-1,0,1,0,0,0)||!Passes(0,-1,0,1,0,0)||Passes(-1,1,1,1,0,0)||Passes(1,0,2,0,0,0)||Passes(0,0,0,0,0,0))throw new Exception("Base-origin incidence test failed");}
}
