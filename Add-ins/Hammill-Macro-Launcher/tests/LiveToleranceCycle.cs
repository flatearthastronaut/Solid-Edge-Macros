using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Hammill.Automation;
using Hammill.DimensionStyleCycle;
using Hammill.MacroLauncher;

internal static class LiveToleranceCycle
{
    private static int checks;
    private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); checks++; Console.WriteLine("PASS " + message); }
    [STAThread] private static int Main()
    {
        object scratch=null, original=null, appObject=null; bool created=false;
        string root=Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","..",".."));
        string run=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,".work","live-limits-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(run);
        using(new OleMessageFilter()) using(var connection=new ComScope())
        {
            try
            {
                // Reuse a running session or own an invisible test session. Only
                // our scratch document is saved, closed, or changed by this test.
                try {appObject=Marshal.GetActiveObject("SolidEdge.Application");}
                catch(COMException e) {if(e.ErrorCode!=unchecked((int)0x800401E3))throw;appObject=Activator.CreateInstance(Type.GetTypeFromProgID("SolidEdge.Application",true));created=true;}
                dynamic app=connection.Own(appObject), docs=connection.Own(app.Documents);
                if((int)docs.Count>0)original=connection.Own(app.ActiveDocument);
                scratch=docs.Add("SolidEdge.DraftDocument",Path.Combine(root,"DraftingStandards","detailing example 1.dft"));
                var times=new List<double>(); int lastIndex=0;
                using(var scope=new ComScope())
                {
                    dynamic doc=scratch, sheet=scope.Own(doc.ActiveSheet), circles=scope.Own(sheet.Circles2d), dims=scope.Own(sheet.Dimensions);
                    dynamic definitions=scope.Own(doc.DimensionStyles), parent=scope.Own(definitions.Item(1)), selection=scope.Own(doc.SelectSet);
                    for(int family=0;family<5;family++)
                    {
                        bool dual=family%2==1 || family==4, metricPrimary=family%2==1;
                        int orientation=family==2||family==3?2:1;
                        string name="Limit test "+family;
                        dynamic shared=scope.Own(definitions.Add(name,(string)parent.Name));
                        shared.PrimaryUnits=metricPrimary?3:5;shared.SecondaryUnits=metricPrimary?5:3;shared.DualDisplay=dual;
                        shared.RoundOffType=1;shared.SecondaryLinearRoundOffType=1;
                        shared.PrimaryDecimalRoundOff=4;shared.SecondaryDecimalRoundOff=4;shared.TextOrientation=orientation;
                        shared.PrimaryLeadingZeros=true;shared.PrimaryTrailingZeros=true;shared.SecondaryLeadingZeros=true;shared.SecondaryTrailingZeros=true;
                        dynamic circle=scope.Own(circles.AddByCenterRadius(.03+family*.04,.03,.0127));
                        dynamic dim=scope.Own(dims.AddCircularDiameter(circle)), style=scope.Own(dim.Style);lastIndex=(int)dims.Count;
                        style.Name=name;dim.DisplayType=1;dim.PrefixString="TEST ";dim.SuffixString=" B.C";
                        double nominal=(double)dim.Value,track=(double)dim.TrackDistance;
                        selection.RemoveAll();selection.Add(dim);
                        for(int click=0;click<11;click++)
                        {
                            decimal expected=new[]{.0005m,.001m,.002m,0m}[click%4];
                            var timer=Stopwatch.StartNew();decimal actual=Addin.ExecuteLimitTolerance((object)app);timer.Stop();times.Add(timer.Elapsed.TotalMilliseconds);
                            Check(actual==expected && (int)dim.DisplayType==(expected==0m?1:4),"family "+family+" click "+(click+1)+" cycles limits/Nominal");
                            AssertDisplay((object)dim,expected,dual);
                            Check((double)dim.Value==nominal && (double)dim.TrackDistance==track && (int)selection.Count==1 &&
                                (string)dim.PrefixString=="TEST " && (string)dim.SuffixString==" B.C" && (string)style.Name==name &&
                                (int)style.PrimaryUnits==(metricPrimary?3:5) && (int)style.SecondaryUnits==(metricPrimary?5:3) && (bool)style.DualDisplay==dual && (int)style.TextOrientation==orientation,
                                "native nominal, placement, text, units, orientation and selection persist");
                            Check((metricPrimary?(int)style.PrimaryDecimalRoundOff:(int)style.SecondaryDecimalRoundOff)==(dual && expected==.0005m?5:4) &&
                                (int)shared.PrimaryDecimalRoundOff==4 && (int)shared.SecondaryDecimalRoundOff==4,"metric precision follows inch minus one and shared definitions are unchanged");
                        }
                        // Applying another named style can reset local settings.
                        // Confirm its precision change never changes physical limits.
                        dynamic alternate=scope.Own(definitions.Add(name+" next",name));
                        alternate.PrimaryDecimalRoundOff=metricPrimary?5:6;alternate.SecondaryDecimalRoundOff=metricPrimary?6:5;
                        var sequence=StyleSequence.Parse("[Inch]\n"+name+"\n"+name+" next\n");
                        CycleEngine.Run(scratch,sequence,new StyleIndex());
                        AssertDisplay((object)dim,.002m,dual,4,3);
                        Check(Addin.ExecuteLimitTolerance((object)app)==0m && (int)style.PrimaryDecimalRoundOff==(metricPrimary?5:6) && (int)style.SecondaryDecimalRoundOff==(metricPrimary?6:5),"Nominal preserves places chosen by Cycle Places");
                        AssertDisplay((object)dim,0m,dual,4,3);
                        Check(Addin.ExecuteLimitTolerance((object)app)==.0005m,"style and tolerance commands can alternate on the same dimension");
                        AssertDisplay((object)dim,.0005m,dual);
                    }
                    // Save the last dimension in the newly added Nominal state.
                    Addin.ExecuteLimitTolerance((object)app);Addin.ExecuteLimitTolerance((object)app);
                    Check(Addin.ExecuteLimitTolerance((object)app)==0m,"Nominal state is ready for save/reopen");
                    ((dynamic)scratch).SaveAs(Path.Combine(run,"Tolerance test.dft"));
                }
                ((dynamic)scratch).Close(false);ComScope.Release(scratch);scratch=null;
                scratch=docs.Open(Path.Combine(run,"Tolerance test.dft"));
                using(var scope=new ComScope())
                {
                    dynamic sheet=scope.Own(((dynamic)scratch).ActiveSheet), dims=scope.Own(sheet.Dimensions), dim=scope.Own(dims.Item(lastIndex));
                    dynamic selection=scope.Own(((dynamic)scratch).SelectSet);selection.RemoveAll();selection.Add(dim);
                    AssertDisplay((object)dim,0m,true);
                    Check(Addin.ExecuteLimitTolerance((object)app)==.0005m,"reopened Nominal dimension resumes at half-thousandths without hidden state");
                    AssertDisplay((object)dim,.0005m,true);
                    Check(Addin.ExecuteLimitTolerance((object)app)==.001m,"next click continues to thousandths");
                    AssertDisplay((object)dim,.001m,true);
                }
                var steady=times.Skip(1).OrderBy(x=>x).ToArray();
                string report="Passed "+checks+" live limit checks. External COM median "+steady[steady.Length/2].ToString("F1")+" ms; first "+times[0].ToString("F1")+" ms. Installed ribbon timing remains unmeasured.";
                File.WriteAllText(Path.Combine(run,"Results.txt"),report);Console.WriteLine(report+"\nFixtures: "+run);
                return 0;
            }
            catch(Exception error){Console.Error.WriteLine(error);return 1;}
            finally
            {
                if(scratch!=null){try{((dynamic)scratch).Close(false);}finally{ComScope.Release(scratch);}}
                if(original!=null)((dynamic)original).Activate();
                if(created&&appObject!=null)using(var scope=new ComScope()){dynamic docs=scope.Own(((dynamic)appObject).Documents);if((int)docs.Count==0)((dynamic)appObject).Quit();}
            }
        }
    }
    private static void AssertDisplay(object dim,decimal tolerance,bool dual,int inchPlaces=0,int metricPlaces=0)
    {
        var text=Texts(dim);
        if(inchPlaces==0)inchPlaces=tolerance==.0005m?4:3;
        string format="F"+inchPlaces;
        if(tolerance==0m)
        {
            // Count rendered numeric tokens, not just stored strings: true Nominal
            // must show one value per unit, never a pair of equal zero-tolerance limits.
            double value;var numbers=text.Where(t=>Double.TryParse(t,NumberStyles.Float,CultureInfo.InvariantCulture,out value)).ToArray();
            Check((int)((dynamic)dim).DisplayType==1 && numbers.Length==(dual?2:1) && text.Contains(1m.ToString(format,CultureInfo.InvariantCulture)),"native Nominal renders one inch measurement without tolerance text ("+String.Join(" | ",text)+")");
            if(dual)Check(text.Contains(25.4m.ToString("F"+(metricPlaces==0?2:metricPlaces),CultureInfo.InvariantCulture)),"dual Nominal renders one metric measurement at the retained precision");
            return;
        }
        string upper=(1+tolerance).ToString(format,CultureInfo.InvariantCulture), lower=(1-tolerance).ToString(format,CultureInfo.InvariantCulture);
        Check(text.Contains(upper)&&text.Contains(lower),"native rendered inch limits "+upper+" / "+lower+" ("+String.Join(" | ",text)+")");
        if(dual)
        {
            string metricFormat=metricPlaces==0?(tolerance==.0005m?"F3":"F2"):"F"+metricPlaces;
            upper=(25.4m+tolerance*25.4m).ToString(metricFormat,CultureInfo.InvariantCulture);lower=(25.4m-tolerance*25.4m).ToString(metricFormat,CultureInfo.InvariantCulture);
            Check(text.Contains(upper)&&text.Contains(lower),"native dual conversion renders metric limits "+upper+" / "+lower);
        }
    }
    private static List<string> Texts(object dimension)
    {
        using(var scope=new ComScope())
        {
            dynamic data=scope.Own(((dynamic)dimension).GetDisplayData());var lines=new List<string>();
            for(int i=0;i<(int)data.GetTextCount();i++)
            {string text="";double a=0,b=0,c=0,d=0,e=0,f=0,g=0,h=0,j=0;data.GetTextAtIndex(i,ref text,ref a,ref b,ref c,ref d,ref e,ref f,ref g,ref h,ref j);lines.Add(text.Trim());}
            return lines;
        }
    }
}
