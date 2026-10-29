using System;
using System.IO;
using System.Runtime.InteropServices;
using D=SolidEdge.Draft.Interop;
using S=SolidEdge.FrameworkSupport.Interop;
using F=SolidEdge.Framework.Interop;

// Final presentation and saved-document validation helpers for CAP_19679.
// The native annotations retain their measured geometry references.
partial class CapDetailTool {
 static D.DraftDocument cdoc;
 static D.Sheet csh;
 static D.DrawingView cface,csection,ciso;
 const double HalfOD=.0882523;
 static string capName="CAP_19679";
 static string CapPath {get{return Path.Combine(folder,capName+".dft");}}
 static string CapWork {get{return Path.Combine(root,capName);}}
 static void CText(string text,double x,double y,double width,double height){var box=((S.TextBoxes)csh.TextBoxes).Add(x,y,0);box.Text=text;box.Width=width;ReferenceAnnotationStyle.Apply("TextNote",box);box.Edit.TextSize=height;}
 static double[] CMap(D.DrawingView v,double x,double y,double z){double a,b;v.ModelToView(x,y,z,out a,out b);v.ViewToSheet(a,b,out a,out b);return new[]{a,b};}
 static double[] Rad(D.DrawingView v,double radius,double z){return CMap(v,Math.Cos(Math.PI/8)*radius,-Math.Sin(Math.PI/8)*radius,z);}
 static void LoadCap(){cdoc=(D.DraftDocument)app.Documents.Open(CapPath);csh=cdoc.Sheets.Item("Sheet1");csh.Activate();cface=csh.DrawingViews.Item(1);csection=csh.DrawingViews.Item(2);ciso=csh.DrawingViews.Item(3);}
 static void CStyle(S.DimStyle style){ReferenceDimensionStyle.Apply(style);style.PrimaryUnits=S.DimLinearUnitConstants.igDimStyleLinearMM;style.PrimaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_2;style.DualDisplay=true;style.SecondaryUnits=S.DimLinearUnitConstants.igDimStyleLinearInches;style.SecondaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_3;}
 static D.DVLine2d CEdge(D.DrawingView v,double[] point){D.DVLine2d best=null;double gap=double.MaxValue;foreach(D.DVLine2d line in v.DVLines2d){double a,b,c,d;line.GetStartPoint(out a,out b);line.GetEndPoint(out c,out d);v.ViewToSheet(a,b,out a,out b);v.ViewToSheet(c,d,out c,out d);double dx=c-a,dy=d-b,length=dx*dx+dy*dy;if(length<1e-18)continue;double t=((point[0]-a)*dx+(point[1]-b)*dy)/length;t=Math.Max(0,Math.Min(1,t));double e=Math.Sqrt(Math.Pow(point[0]-a-t*dx,2)+Math.Pow(point[1]-b-t*dy,2));if(e<gap){gap=e;best=line;}}
  if(gap>1e-7)throw new Exception("Missing edge at "+string.Join(",",point)+" gap="+gap);return best;}
 static void CFinish(double radius,double z){var p=Rad(csection,radius,z);var edge=CEdge(csection,p);var f=((S.SurfaceFinishSymbols)csh.SurfaceFinishSymbols).AddByTerminator(edge.Reference,p[0],p[1],0,false);ReferenceAnnotationStyle.Apply("SurfaceFinishSymbol",f);f.MaximumRoughnessValue="32";f.MinimumRoughnessValue="TURN";f.Leader=false;}
 static double[] TextPoint(S.Dimension d){var data=d.GetDisplayData();string t;double x,y,z,a,b,c,e,f,g;data.GetTextAtIndex(0,out t,out x,out y,out z,out a,out b,out c,out e,out f,out g);Marshal.ReleaseComObject(data);return new[]{x,y};}
 static void PlaceChamferText(S.Dimension d,double x,double y){for(int k=0;k<3;k++){var p=TextPoint(d);double l=d.LeaderDistance,b=d.BreakDistance;d.LeaderDistance=l+.001;var u=TextPoint(d);d.LeaderDistance=l;d.BreakDistance=b+.001;var v=TextPoint(d);d.BreakDistance=b;double a=(u[0]-p[0])/.001,c=(u[1]-p[1])/.001,e=(v[0]-p[0])/.001,f=(v[1]-p[1])/.001,det=a*f-c*e;Console.WriteLine("PLACE "+d.Index+" "+p[0]+","+p[1]+" jac="+a+","+c+","+e+","+f);if(Math.Abs(det)<1e-6)throw new Exception("Native leader placement did not respond");double dl=((x-p[0])*f-(y-p[1])*e)/det,db=(a*(y-p[1])-c*(x-p[0]))/det;if(Math.Abs(l+dl)>.2||Math.Abs(b+db)>.2)throw new Exception("Invalid leader placement");d.LeaderDistance=l+dl;d.BreakDistance=b+db;}}
 static void FinalLayout19679(){LoadCap();var dims=(S.Dimensions)csh.Dimensions;
  foreach(S.Dimension d in dims){if(d.DimensionType!=S.DimTypeConstants.igDimTypeChamfer)continue;if(Math.Abs(d.Value-.000762)<1e-7)PlaceChamferText(d,.172,.112);else if(Math.Abs(d.Value-.0006223)<1e-7)PlaceChamferText(d,.172,.094);else PlaceChamferText(d,.060,.113);}
  dims.Item(19).SetKeyPoint(8,.145,.218,0);
  dims.Item(10).Style.SecondaryDecimalRoundOff=S.DimDecimalRoundOffTypeConstants.igDimStyleDecimal_4;
  if(((S.SurfaceFinishSymbols)csh.SurfaceFinishSymbols).Count==3){CFinish(HalfOD,-.025);CFinish(.0730123,-.008);}
  double[,] finishPositions={{.210,.21035655},{.050,.241550925},{.080,.23009235},{.050,.110810775},{.210,.122240775}};
  foreach(S.SurfaceFinishSymbol f in (S.SurfaceFinishSymbols)csh.SurfaceFinishSymbols){f.SurfaceFinishSymbol=S.DimSurfTextureSymTypeConstants.igDimSurfaceFinishBasic;f.SetKeyPoint(1,finishPositions[f.Index-1,0],finishPositions[f.Index-1,1],0);if(!f.IsTerminatorAttachedToEntity)throw new Exception("Finish symbol detached");}
  if(((S.TextBoxes)csh.TextBoxes).Count==1){CText("NOTES UNLESS OTHERWISE SPECIFIED:\r1) ALL 32 TURN SURFACES MUST BE\r   CONCENTRIC, SQUARE AND/OR PARALLEL\r   AS APPLICABLE WITHIN .03 [.001] T.I.R.",.024,.048,.152,.003175);}
  foreach(S.Balloon field in (S.Balloons)csh.Background.Balloons)if(field.BalloonDisplayedText=="--04")field.BalloonText="-";
  cdoc.Save();CPreview();
 }
 static void CPreview(){Console.WriteLine("Readonly="+cdoc.ReadOnly+" Interactive="+app.Interactive+" DelayCompute="+app.DelayCompute);app.Interactive=true;app.DelayCompute=false;cdoc.Activate();csh.Activate();bool stale=false;foreach(D.DrawingView view in csh.DrawingViews)stale|=!view.IsUpToDate;if(stale)cdoc.UpdateAll(true);app.StartCommand((int)F.SolidEdgeCommandConstants.seRefreshViewCommand);app.DoIdle();var window=(D.SheetWindow)app.ActiveWindow;window.FitEx(D.SheetFitConstants.igFitSheet);window.Update();if(File.Exists(Path.Combine(CapWork,capName+"-drawing.jpg")))File.Delete(Path.Combine(CapWork,capName+"-drawing.jpg"));window.SaveAsImage(Path.Combine(CapWork,capName+"-drawing.jpg"),3000,2000,1,24,F.SeImageQualityType.seImageQualityHigh,false);Marshal.ReleaseComObject(window);}
}
