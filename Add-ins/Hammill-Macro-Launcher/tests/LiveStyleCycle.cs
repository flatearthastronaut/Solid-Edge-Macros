using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml.Linq;
using Hammill.Automation;
using Hammill.DimensionStyleCycle;
using Hammill.MacroLauncher;

internal static class LiveStyleCycle
{
    private static int checks;
    private static void Check(bool ok, string label) { if (!ok) throw new Exception(label); checks++; Console.WriteLine("PASS " + label); }
    [STAThread]
    private static int Main()
    {
        string root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
        string run = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".work", "live-cycle-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(run);
        object scratch = null, original = null, appObject = null; bool created = false;
        using (new OleMessageFilter())
        using (var connection = new ComScope())
        {
            try
            {
                try { appObject = Marshal.GetActiveObject("SolidEdge.Application"); }
                catch (COMException error) { if (error.ErrorCode != unchecked((int)0x800401E3)) throw; appObject = Activator.CreateInstance(Type.GetTypeFromProgID("SolidEdge.Application", true)); created = true; }
                dynamic app = connection.Own(appObject);
                dynamic documents = connection.Own(app.Documents);
                if ((int)documents.Count > 0) original = connection.Own(app.ActiveDocument);
                // A new disposable document from the project reference template.
                // No production drawing, template or installed add-in is modified.
                scratch = documents.Add("SolidEdge.DraftDocument", Path.Combine(root, "DraftingStandards", "detailing example 1.dft"));
                using (var scope = new ComScope())
                {
                    dynamic doc = scratch;
                    dynamic sheet = scope.Own(doc.ActiveSheet);
                    dynamic styles = scope.Own(doc.DimensionStyles);
                    dynamic first = scope.Own(styles.Item(1));
                    string parent = (string)first.Name;
                    var originalNames = new List<string>();
                    for (int i = 1; i <= (int)styles.Count; i++)
                    { using (var item = new ComScope()) { dynamic s = item.Own(styles.Item(i)); originalNames.Add((string)s.Name); } }
                    File.WriteAllLines(Path.Combine(run, "Reference style names.txt"), originalNames);
                    string[] families = { "Inch", "Inch-Metric", "Vertical Inch", "Vertical Inch-Metric" };
                    var config = new StringBuilder();
                    var dimensions = new List<object>();
                    for (int family = 0; family < families.Length; family++)
                    {
                        config.AppendLine("[" + families[family] + "]");
                        bool dual = family % 2 == 1;
                        for (int places = 2; places <= 4; places++)
                        {
                            string name = "Cycle test " + family + " " + places;
                            config.AppendLine(name);
                            dynamic definition = scope.Own(styles.Add(name, parent));
                            definition.DualDisplay = dual;
                            definition.PrimaryUnits = dual ? 3 : 5; definition.SecondaryUnits = 5;
                            definition.PrimaryDecimalRoundOff = dual ? places + 1 : places + 2;
                            definition.SecondaryDecimalRoundOff = places + 2;
                            definition.TextOrientation = family >= 2 ? 2 : 1;
                        }
                        dynamic circles = scope.Own(sheet.Circles2d);
                        dynamic circle = scope.Own(circles.AddByCenterRadius(.03 + family * .04, .03, .01));
                        dynamic collection = scope.Own(sheet.Dimensions);
                        dynamic dim = scope.Own(collection.AddCircularDiameter(circle)); dimensions.Add(dim);
                        dynamic style = scope.Own(dim.Style); style.Name = "Cycle test " + family + " 2";
                        dim.PrefixString = "TEST "; dim.SuffixString = " B.C";
                        dim.PrimaryUpperTolerance = "-.002"; dim.PrimaryLowerTolerance = "-.006";
                    }
                    string shared = Path.Combine(run, "Available"), local = Path.Combine(run, "Installed");
                    Directory.CreateDirectory(shared); Directory.CreateDirectory(local);
                    string configPath = Path.Combine(shared, "Hammill.DimensionStyles.txt"), settings = Path.Combine(run, "settings.xml");
                    new XDocument(new XElement("settings", new XElement("source", shared), new XElement("local", local))).Save(settings);
                    File.WriteAllText(Path.Combine(local, "Hammill.DimensionStyles.txt"), "[Inch]\nOld local\nMust not run\n");
                    File.WriteAllText(configPath, config.ToString());
                    var file = Addin.SharedStyleSequence(settings); file.Get(); var index = new StyleIndex();
                    var secondReader = Addin.SharedStyleSequence(settings); secondReader.Get();
                    dynamic selected = scope.Own(doc.SelectSet);
                    var times = new List<double>();
                    var report = new StringBuilder();
                    for (int family = 0; family < families.Length; family++)
                    {
                        dynamic dim = dimensions[family];
                        dynamic style = scope.Own(dim.Style);
                        double measured = (double)dim.Value, track = (double)dim.TrackDistance;
                        string upper = (string)dim.PrimaryUpperTolerance, lower = (string)dim.PrimaryLowerTolerance;
                        selected.RemoveAll(); selected.Add(dim);
                        for (int click = 0; click < 12; click++)
                        {
                            var timer = Stopwatch.StartNew();
                            string target = Addin.ExecuteStyleCycle((object)app, file, index);
                            timer.Stop(); times.Add(timer.Elapsed.TotalMilliseconds);
                            int places = 2 + (click + 1) % 3;
                            Check(target == "Cycle test " + family + " " + places && (string)style.Name == target, families[family] + " click " + (click + 1) + " advances/wraps");
                            Check((int)selected.Count == 1 && (double)dim.Value == measured && (double)dim.TrackDistance == track &&
                                (string)dim.PrefixString == "TEST " && (string)dim.SuffixString == " B.C" &&
                                (string)dim.PrimaryUpperTolerance == upper && (string)dim.PrimaryLowerTolerance == lower,
                                "native selection, measurement, text, tolerance and placement persist");
                            int roundOff = family % 2 == 1 ? (int)style.SecondaryDecimalRoundOff : (int)style.PrimaryDecimalRoundOff;
                            Check(roundOff == places + 2 && (int)style.TextOrientation == (family >= 2 ? 2 : 1), "inch precision and orientation follow the requested style");
                        }
                    }
                    selected.RemoveAll(); selected.Add(dimensions[0]);
                    DateTime stamp = File.GetLastWriteTimeUtc(configPath);
                    File.WriteAllText(configPath, config.ToString().Replace("Cycle test 0 2\r\nCycle test 0 3\r\nCycle test 0 4", "Cycle test 0 2\r\nCycle test 0 4\r\nCycle test 0 3"));
                    File.SetLastWriteTimeUtc(configPath, stamp);
                    Check(Addin.ExecuteStyleCycle((object)app, file, index) == "Cycle test 0 4", "editing the real configuration changes the next click without restarting");
                    Check(Addin.ExecuteStyleCycle((object)app, secondReader, new StyleIndex()) == "Cycle test 0 3", "a second reader also applies the shared edit despite unchanged size and timestamp");
                    File.Delete(configPath); bool refused = false;
                    try { Addin.ExecuteStyleCycle((object)app, file, index); } catch (IOException) { refused = true; }
                    using (var verify = new ComScope())
                    {
                        dynamic style = verify.Own(((dynamic)dimensions[0]).Style);
                        Check(refused && (string)style.Name == "Cycle test 0 3" && (int)selected.Count == 1, "unavailable shared file leaves the native dimension and selection unchanged");
                    }
                    File.WriteAllText(configPath, config.ToString());
                    Check(Addin.ExecuteStyleCycle((object)app, file, index) == "Cycle test 0 4", "restored shared file recovers on the next native command");
                    double firstMs = times[0]; var steady = times.Skip(1).OrderBy(t => t).ToArray();
                    report.AppendLine("Live checks: " + checks);
                    report.AppendLine("First call (external test process): " + firstMs.ToString("F2") + " ms");
                    report.AppendLine("Steady median (external test process): " + steady[steady.Length / 2].ToString("F2") + " ms");
                    report.AppendLine("Steady maximum (external test process): " + steady.Last().ToString("F2") + " ms");
                    report.AppendLine("The deployed command runs inside SE; this harness includes cross-process COM overhead and is not an installed ribbon timing measurement.");
                    File.WriteAllText(Path.Combine(run, "Results.txt"), report.ToString());
                    Console.WriteLine(report);
                    Console.WriteLine("Fixtures: " + run);
                }
                return 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
            finally
            {
                if (scratch != null) { try { ((dynamic)scratch).Close(false); } finally { ComScope.Release(scratch); } }
                if (original != null) { try { ((dynamic)original).Activate(); } catch (Exception e) { Console.Error.WriteLine("Restore active document: " + e.Message); } }
                if (created && appObject != null) using (var scope = new ComScope()) { dynamic docs = scope.Own(((dynamic)appObject).Documents); if ((int)docs.Count == 0) ((dynamic)appObject).Quit(); }
            }
        }
    }
}
