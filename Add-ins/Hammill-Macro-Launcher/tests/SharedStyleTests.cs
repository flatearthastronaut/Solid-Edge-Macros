using System;
using System.IO;
using System.Xml.Linq;
using Hammill.DimensionStyleCycle;
using Hammill.MacroLauncher;

internal static class SharedStyleTests
{
    private const string Original = "[Inch]\nI2\nI3\nI4\n", Changed = "[Inch]\nI2\nI4\nI3\n";
    private static void Settings(string path, string source, string local)
    { new XDocument(new XElement("settings", new XElement("source", source), new XElement("local", local))).Save(path); }
    private static void Fails(Action action, Action<bool,string> check, string message)
    {
        try { action(); } catch (IOException) { check(true, message); return; } catch (InvalidDataException) { check(true, message); return; }
        check(false, message);
    }
    internal static void Run(string run, Action<bool,string> check)
    {
        string root=Path.Combine(run,"Shared style fixtures"), share=Path.Combine(root,"Available"), localA=Path.Combine(root,"User A"), localB=Path.Combine(root,"User B");
        Directory.CreateDirectory(share);Directory.CreateDirectory(localA);Directory.CreateDirectory(localB);
        string settingsA=Path.Combine(root,"A.xml"),settingsB=Path.Combine(root,"B.xml"), shared=Path.Combine(share,"Hammill.DimensionStyles.txt");
        Settings(settingsA,share,localA);Settings(settingsB,share,localB);
        File.WriteAllText(Path.Combine(localA,"Hammill.DimensionStyles.txt"),Changed);
        File.WriteAllText(Path.Combine(localB,"Hammill.DimensionStyles.txt"),"[Inch]\nOther\nNames\n");
        File.WriteAllText(shared,Original);
        var a=Addin.SharedStyleSequence(settingsA);var b=Addin.SharedStyleSequence(settingsB);
        check(a.Path==shared && b.Path==shared && a.Get().Next("I2")=="I3" && b.Get().Next("I2")=="I3", "two independent users follow shared source instead of differing local lists");
        var parsed=a.Get();check(Object.ReferenceEquals(parsed,a.Get()),"identical shared text reuses parsing");
        DateTime stamp=File.GetLastWriteTimeUtc(shared);File.WriteAllText(shared,Changed);File.SetLastWriteTimeUtc(shared,stamp);
        check(a.Get().Next("I2")=="I4" && b.Get().Next("I2")=="I4","both users see an equal-size edit with an unchanged timestamp on the next click");
        File.SetAttributes(shared,FileAttributes.ReadOnly);
        try {check(a.Get().Next("I2")=="I4","ordinary users need read access only");}
        finally {File.SetAttributes(shared,FileAttributes.Normal);}
        File.WriteAllText(shared,"[Inch]\nI2\nI2\n");
        Fails(()=>a.Get(),check,"invalid shared edits do not use cached order");Fails(()=>b.Get(),check,"invalid shared edits are rejected for every reader");
        File.WriteAllText(shared,Original);check(a.Get().Next("I2")=="I3" && b.Get().Next("I2")=="I3","fixing the shared file recovers without restarting either reader");
        string replacement=Path.Combine(share,"edited.tmp");File.WriteAllText(replacement,Changed);File.Replace(replacement,shared,null);
        check(a.Get().Next("I2")=="I4" && b.Get().Next("I2")=="I4","atomic editor saves refresh both readers");
        using(var writer=new FileStream(shared,FileMode.Open,FileAccess.Write,FileShare.Read))
            Fails(()=>a.Get(),check,"in-progress in-place writes cannot supply partially written configuration");
        check(a.Get().Next("I2")=="I4","released write lock allows the next click");
        File.Delete(shared);Fails(()=>a.Get(),check,"missing shared file does not fall back to a local or cached copy");
        check(!File.Exists(shared) && File.ReadAllText(Path.Combine(localA,"Hammill.DimensionStyles.txt"))==Changed,"readers never seed the shared file or overwrite older local lists");
        File.WriteAllText(shared,new string('x',65537));Fails(()=>a.Get(),check,"oversize shared files are rejected before parsing");
        File.WriteAllText(shared,Original,System.Text.Encoding.Unicode);check(a.Get().Next("I2")=="I3","UTF-16 Notepad files are supported");
        string newShare=Path.Combine(root,"New Available");Directory.CreateDirectory(newShare);
        File.WriteAllText(Path.Combine(newShare,"Hammill.DimensionStyles.txt"),Changed);Settings(settingsA,newShare,localA);
        check(a.Path==Path.Combine(newShare,"Hammill.DimensionStyles.txt") && a.Get().Next("I2")=="I4" && b.Get().Next("I2")=="I3","changing a user's saved Available folder takes effect next click");
        Settings(settingsA,"relative\\source",localA);Fails(()=>a.Get(),check,"relative source is rejected");
        Settings(settingsA,@"C:relative",localA);Fails(()=>a.Get(),check,"drive-relative source is rejected");
        Settings(settingsA,@"\relative",localA);Fails(()=>a.Get(),check,"root-relative source is rejected");
        Settings(settingsA,@"\\server\macros",localA);check(a.Path==@"\\server\macros\Hammill.DimensionStyles.txt","UNC paths resolve without probing the network during startup");
        Settings(settingsA,@"H:\solid_edge\Coop\internal\Macros and Add-ins\",localA);
        check(MacroCatalog.ManagerSourceFolder(settingsA)==@"H:\Coop\internal\Macros and Add-ins","old default source migrates exactly like Macro Manager");
        Settings(settingsA,"",localA);check(MacroCatalog.ManagerSourceFolder(settingsA)==@"H:\Coop\internal\Macros and Add-ins","blank source uses Macro Manager's current default");
        File.Delete(settingsA);check(MacroCatalog.ManagerSourceFolder(settingsA)==@"H:\Coop\internal\Macros and Add-ins","missing settings use Macro Manager's default source");
        File.WriteAllText(settingsA,"<settings>");
        bool malformed=false;try{a.Get();}catch(System.Xml.XmlException){malformed=true;}check(malformed,"malformed manager settings never switch to another folder silently");
        File.WriteAllText(settingsA,"<!DOCTYPE settings [<!ENTITY ext SYSTEM 'file:///not-read'>]><settings><source>&ext;</source></settings>");
        bool entity=false;try{a.Get();}catch(System.Xml.XmlException){entity=true;}check(entity,"source settings reject XML external entities");
        Settings(settingsA,share,localA);File.WriteAllText(shared,Original);
        // Exercise the real in-process command entry point with each user's
        // independent resolver and native-shaped dimension selection.
        var styles=new CycleTestStyles();foreach(string name in new[]{"I2","I3","I4"})styles.Values.Add(name,new CycleTestStyle(name));
        var dim=new CycleTestDimension{Style=new CycleTestStyle("I2")};
        var doc=new CycleTestDocument{DimensionStyles=styles,SelectSet=new CycleTestSelection{Values=new object[]{dim}}};
        var app=new CycleTestApplication{ActiveDocument=doc};
        check(Addin.ExecuteStyleCycle(app,a,new StyleIndex())=="I3","addin style entry point uses shared resolver");
        File.WriteAllText(shared,Changed);
        check(Addin.ExecuteStyleCycle(app,b,new StyleIndex())=="I2" && doc.SelectSet.Item(1)==dim,"next command uses changed shared order and retains the selected dimension");
    }
}
