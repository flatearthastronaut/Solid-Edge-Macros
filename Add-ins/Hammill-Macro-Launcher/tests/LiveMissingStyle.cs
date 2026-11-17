using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Hammill.Automation;
using Hammill.DimensionStyleCycle;
using Hammill.MacroLauncher;

internal static class LiveMissingStyle
{
    static void Check(bool ok, string message) { if (!ok) throw new Exception(message); Console.WriteLine("PASS " + message); }
    [STAThread] static int Main()
    {
        string project = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".."));
        string root = Path.GetFullPath(Path.Combine(project, "..", ".."));
        string run = Path.Combine(project, "tests", ".work", "missing-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(run);
        string master = Path.Combine(run, MissingStyleImport.MasterName), path = Path.Combine(run, "target.dft");
        File.Copy(Path.Combine(root, MissingStyleImport.MasterName), master); File.Copy(master, path);
        string config = Path.Combine(run, "Hammill.DimensionStyles.txt");
        File.WriteAllText(config, "[Vertical Inch]\n3 3 place (vert)\n3 4 place (vert)\n");
        object target = null, previous = null;
        using (new OleMessageFilter()) using (var scope = new ComScope())
        {
            try
            {
                dynamic app = scope.Own(Marshal.GetActiveObject("SolidEdge.Application"));
                dynamic docs = scope.Own(app.Documents);
                if ((int)docs.Count > 0) previous = scope.Own(app.ActiveDocument);
                int initialCount = (int)docs.Count;
                target = docs.Open(path);
                using (var edit = new ComScope())
                {
                    dynamic doc = target, styles = edit.Own(doc.DimensionStyles);
                    var index = new StyleIndex();
                    dynamic existing = edit.Own(index.Find((object)styles, "1 3 place")); existing.Height = .0042;
                    styles.Remove("3 4 place (vert)");
                    string active = (string)styles.Active;
                    dynamic sheet = edit.Own(doc.ActiveSheet), circles = edit.Own(sheet.Circles2d);
                    dynamic circle = edit.Own(circles.AddByCenterRadius(.1, .1, .01));
                    dynamic dims = edit.Own(sheet.Dimensions), dim = edit.Own(dims.AddCircularDiameter(circle));
                    dynamic style = edit.Own(dim.Style); style.Name = "3 3 place (vert)";
                    dim.SuffixString = " B.C"; dim.PrimaryUpperTolerance = "-.002"; dim.PrimaryLowerTolerance = "-.006";
                    double value = (double)dim.Value;
                    dynamic selection = edit.Own(doc.SelectSet); selection.RemoveAll(); selection.Add(dim);
                    var file = new StyleSequenceFile(config);
                    var timer = Stopwatch.StartNew();
                    Check(Addin.ExecuteStyleCycle((object)app, file, index) == "3 4 place (vert)", "same click imports and applies missing vertical style");
                    timer.Stop(); Console.WriteLine("First missing-style click: " + timer.ElapsedMilliseconds + " ms (external COM harness)");
                    Check((int)selection.Count == 1 && (double)dim.Value == value && (string)dim.SuffixString == " B.C" && (string)dim.PrimaryLowerTolerance == "-.006", "selection, measured value, suffix and tolerance survive master open/import/close");
                    Check((double)existing.Height == .0042 && (string)styles.Active == active, "existing customized style and active style preserved");
                    Check((int)docs.Count == initialCount + 1, "helper-opened master closed without closing user documents");
                    dynamic added = edit.Own(index.Find((object)styles, "3 4 place (vert)"));
                    Check((int)added.TextOrientation == 2 && (int)added.PrimaryDecimalRoundOff == 6, "master vertical orientation and four-place precision retained");
                    // Remove access to the source: normal subsequent clicks must
                    // not need to open it or run an import again.
                    File.Move(master, master + ".unavailable");
                    timer.Restart();
                    Check(Addin.ExecuteStyleCycle((object)app, file, index) == "3 3 place (vert)", "next click works with master unavailable");
                    timer.Stop(); Console.WriteLine("Existing-style click: " + timer.ElapsedMilliseconds + " ms (external COM harness)");
                    doc.Save();
                }
                ((dynamic)target).Close(false); ComScope.Release(target); target = null;
                target = docs.Open(path);
                using (var verify = new ComScope())
                {
                    object styles = verify.Own(((dynamic)target).DimensionStyles);
                    Check(verify.Own(new StyleIndex().Find(styles, "3 4 place (vert)")) != null, "imported style persists after save/reopen");
                }
                return 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error); return 1; }
            finally { if (target != null) { ((dynamic)target).Close(false); ComScope.Release(target); } if (previous != null) ((dynamic)previous).Activate(); }
        }
    }
}
