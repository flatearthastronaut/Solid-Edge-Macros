using System;
using System.Runtime.InteropServices;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;

// Orientation is measured in each sketch's own XY coordinates. Never constrain
// a sloped model edge merely because it looks horizontal in a projected view.
internal static class AxisConstraints {
 internal static int Direction(double x0,double y0,double x1,double y1){
  double dx=Math.Abs(x1-x0),dy=Math.Abs(y1-y0);
  if(Math.Max(dx,dy)<1e-10)return 0;
  if(dy<1e-10)return (int)F.ObjectType.igHorizontalRelation2d;
  if(dx<1e-10)return (int)F.ObjectType.igVerticalRelation2d;
  return 0;
 }
 internal static bool Has(S.Line2d line,int kind){
  var relationships=line.Relationships;
  try{for(int i=1;i<=relationships.Count;i++){
   object item=relationships.Item(i);
   try{var relation=item as S.Relation2d;if(relation!=null&&relation.Type==kind)return true;}
   finally{if(Marshal.IsComObject(item))Marshal.ReleaseComObject(item);}
  }return false;}finally{Marshal.ReleaseComObject(relationships);}
 }
 internal static bool Ensure(S.Relations2d relations,S.Line2d line){
  double x0,y0,x1,y1;line.GetStartPoint(out x0,out y0);line.GetEndPoint(out x1,out y1);
  int kind=Direction(x0,y0,x1,y1);
  if(kind==0||Has(line,kind))return false;
  S.Relation2d added=kind==(int)F.ObjectType.igHorizontalRelation2d?relations.AddHorizontal(line):relations.AddVertical(line);
  try{
   if(added.Type!=kind||!Has(line,kind))throw new Exception("Horizontal/vertical constraint was not attached");
   double a,b,c,d;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);
   if(Math.Abs(a-x0)+Math.Abs(b-y0)+Math.Abs(c-x1)+Math.Abs(d-y1)>1e-9)throw new Exception("Constraint moved the line");
  }finally{Marshal.ReleaseComObject(added);}
  return true;
 }
 internal static void Test(){
  if(Direction(0,0,1,0)!=(int)F.ObjectType.igHorizontalRelation2d||Direction(0,0,0,-1)!=(int)F.ObjectType.igVerticalRelation2d||Direction(0,0,1,1)!=0||Direction(0,0,0,0)!=0)throw new Exception("Axis classification failed");
 }
}
