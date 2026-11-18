using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CoopManager;

internal sealed class CancelRegistrar : IRegistrar
{
    internal readonly List<string> Calls=new List<string>();
    internal readonly HashSet<int> CancelCalls=new HashSet<int>();
    public void CheckClosed(){}
    public void Run(string directory,bool unregister)
    {
        Calls.Add((unregister?"unregister:":"register:")+Path.GetFileName(directory));
        if(CancelCalls.Contains(Calls.Count))
            throw new RegistrationNotStartedException("Administrator permission was canceled.",new Win32Exception(1223));
    }
}
internal static partial class Tests
{
    static Package CancellationPackage(string source,string local,bool previous)
    {
        foreach(string name in new[]{"Register.cmd","UNRegister.cmd","Addin.dll"})
        {
            Write(Path.Combine(source,"Launcher-v2.0",name),"new "+name);
            if(previous)Write(Path.Combine(local,"Launcher-v1.0",name),"old "+name);
        }
        if(previous)Write(Path.Combine(local,"Launcher-v1.0","Settings.txt"),"custom settings");
        return Catalog.Scan(source).Single();
    }
    static void RegistrationCancellationCases()
    {
        Case("registration-launch-boundary",(s,l)=>{
            foreach(int code in new[]{1223,5})
            {
                RegistrationNotStartedException failure=null;
                try{ScriptRegistrar.StartScript(new ProcessStartInfo(),info=>{throw new Win32Exception(code);});}
                catch(RegistrationNotStartedException ex){failure=ex;}
                Assert(failure!=null && ((Win32Exception)failure.InnerException).NativeErrorCode==code,"Launch failure carries native cause and proves no script started");
                if(code==1223)Assert(failure.Message.Contains("permission was canceled"),"Canceled UAC has an understandable error");
            }
            Throws(()=>ScriptRegistrar.StartScript(new ProcessStartInfo(),info=>null),"Missing process handle is classified as not started");
        });
        Case("cancel-before-unregister",(s,l)=>{
            var package=CancellationPackage(s,l,true);var registrar=new CancelRegistrar();registrar.CancelCalls.Add(1);
            Throws(()=>Installer.Install(package,s,l,registrar),"Canceled old unregister stops update");
            Assert(registrar.Calls.SequenceEqual(new[]{"unregister:Launcher-v1.0"}),"Cancellation before launch never asks to re-register the untouched old add-in");
            Assert(File.ReadAllText(Path.Combine(l,"Launcher-v1.0","Addin.dll"))=="old Addin.dll" && File.ReadAllText(Path.Combine(l,"Launcher-v1.0","Settings.txt"))=="custom settings","Untouched add-in and custom settings preserved");
            Assert(!File.Exists(Path.Combine(l,"Launcher-v2.0","Addin.dll")) && Directory.GetDirectories(Path.Combine(l,".se-manager"),"transaction-*").Length==0,"Canceled first prompt cleans staging and does not leave an unfinished transaction");
            Installer.Install(package,s,l,new FakeRegistrar());
            Assert(File.ReadAllText(Path.Combine(l,"Launcher-v2.0","Addin.dll"))=="new Addin.dll","Next update succeeds without manual recovery");
        });
        Case("cancel-new-registration",(s,l)=>{
            var package=CancellationPackage(s,l,true);var registrar=new CancelRegistrar();registrar.CancelCalls.Add(2);
            Throws(()=>Installer.Install(package,s,l,registrar),"Canceled new registration rolls back installed files");
            Assert(registrar.Calls.SequenceEqual(new[]{"unregister:Launcher-v1.0","register:Launcher-v2.0","register:Launcher-v1.0"}),"Rollback skips unregister of a script that never launched but restores the previously removed registration");
            Assert(File.ReadAllText(Path.Combine(l,"Launcher-v1.0","Addin.dll"))=="old Addin.dll" && File.ReadAllText(Path.Combine(l,"Launcher-v1.0","Settings.txt"))=="custom settings","Old files and settings restored after cancellation");
            Assert(!File.Exists(Path.Combine(l,"Launcher-v2.0","Addin.dll")) && Directory.GetDirectories(Path.Combine(l,".se-manager"),"transaction-*").Length==0,"Successful cancellation rollback unblocks the installer");
        });
        Case("cancel-first-registration",(s,l)=>{
            var package=CancellationPackage(s,l,false);var registrar=new CancelRegistrar();registrar.CancelCalls.Add(1);
            Throws(()=>Installer.Install(package,s,l,registrar),"Canceled fresh install rolls back files");
            Assert(registrar.Calls.SequenceEqual(new[]{"register:Launcher-v2.0"}),"Fresh install cancellation never invokes unregister");
            Assert(!File.Exists(Path.Combine(l,"Launcher-v2.0","Addin.dll")) && Directory.GetDirectories(Path.Combine(l,".se-manager"),"transaction-*").Length==0,"Canceled fresh install leaves no blocking transaction");
        });
        Case("cancel-needed-rollback-registration",(s,l)=>{
            var package=CancellationPackage(s,l,true);var registrar=new CancelRegistrar();registrar.CancelCalls.UnionWith(new[]{2,3});
            Throws(()=>Installer.Install(package,s,l,registrar),"Canceling required rollback registration still requires recovery");
            Assert(Directory.GetDirectories(Path.Combine(l,".se-manager"),"transaction-*").Length==1,"Actual unresolved registration retains recovery evidence");
            Assert(File.ReadAllText(Path.Combine(l,"Launcher-v1.0","Addin.dll"))=="old Addin.dll","Old files remain restored even when registration recovery is canceled");
        });
    }
}
