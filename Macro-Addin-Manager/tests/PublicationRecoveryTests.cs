using System;
using System.IO;
using System.Xml.Linq;
using CoopManager;

internal static partial class Tests
{
    // Model the on-disk states left by a terminated publisher, without accessing the real share.
    static string CompletedPublication(string shared)
    {
        string destination=Path.Combine(shared,"Utility-v2.0.exe");Write(destination,"verified current build");
        string txn=Path.Combine(shared,".se-publisher","transaction-completed");
        Write(Path.Combine(txn,"obsolete-0"),"old release");
        new XDocument(new XElement("publication",new XAttribute("destination",destination),
            new XAttribute("committed",true),new XAttribute("publishedHash",Files.Hash(destination)),
            new XElement("obsolete",new XAttribute("path",Path.Combine(shared,"Utility-v1.0.exe")),
                new XAttribute("backup",Path.Combine(txn,"obsolete-0"))))).Save(Path.Combine(txn,"recovery.xml"));
        return txn;
    }
    static void PublicationRecoveryCases()
    {
        Case("publish-clean-all-current-ui",(s,l)=>{
            Write(Path.Combine(s,"Utility-v2.0.exe"),"verified current build");string txn=CompletedPublication(l);
            using(var manager=new ManagerForm(Path.Combine(s,"settings.xml"),false,true))
            {
                Configure(manager,s,l);
                Assert(!((Entry)Row(manager,"Utility").Tag).Actionable,"Completed build has no publish action in the grid");
                WaitUi(manager.Publish(true));
                Assert(!Directory.Exists(txn) && File.ReadAllText(Path.Combine(l,"Utility-v2.0.exe"))=="verified current build","Publish / clean all retries cleanup even with no actionable builds");
            }
        });
        Case("publish-resume-completed",(s,l)=>{
            string txn=CompletedPublication(l);string current=Path.Combine(l,"Utility-v2.0.exe"),hash=Files.Hash(current);
            Write(Path.Combine(l,"Unrelated.exe"),"keep");
            var entry=new Entry{Name="Utility"};CategoryPreferences.AssignShared(l,entry,Categories.Windows);
            Assert(!Directory.Exists(txn),"Verified completed publication cleanup resumes before category writes");
            Assert(Files.Hash(current)==hash && File.ReadAllText(Path.Combine(l,"Unrelated.exe"))=="keep","Cleanup preserves published and unrelated files");
            Assert(CategoryPreferences.ReadShared(l).Get(entry)==Categories.Windows,"Admin writes unblock after cleanup");
        });
        Case("publish-locked-cleanup-journal",(s,l)=>{
            string txn=CompletedPublication(l),journal=Path.Combine(txn,"recovery.xml");
            string before=File.ReadAllText(journal),backup=Path.Combine(txn,"obsolete-0");
            using(var held=new FileStream(backup,FileMode.Open,FileAccess.Read,FileShare.Read))
            {
                Throws(()=>Publisher.CleanupTransaction(Path.Combine(l,".se-publisher"),txn),"Locked obsolete file blocks cleanup");
                Assert(File.ReadAllText(journal)==before && File.Exists(backup),"Locked backup never loses its recovery record");
                string error=null;
                try{using(Publisher.Lock(l)){} }catch(IOException ex){error=ex.Message;}
                Assert(error!=null && error.Contains("Publication completed") && error.Contains("cleanup"),"Completed cleanup failure is distinguished from an interrupted publication");
                Assert(File.ReadAllText(journal)==before,"Retry while locked preserves recovery evidence");
            }
            using(Publisher.Lock(l)){}
            Assert(!Directory.Exists(txn),"Cleanup succeeds after the file is unlocked");
        });
        Case("publish-partial-cleanup",(s,l)=>{
            string txn=CompletedPublication(l);
            // Simulate termination after deleting one backup, before deleting another.
            Write(Path.Combine(txn,"obsolete-1","support.dll"),"older dependency");
            File.Delete(Path.Combine(txn,"obsolete-0"));
            using(Publisher.Lock(l)){}
            Assert(!Directory.Exists(txn) && File.Exists(Path.Combine(l,"Utility-v2.0.exe")),"Partial committed cleanup safely resumes");
            Directory.CreateDirectory(txn);
            using(Publisher.Lock(l)){}
            Assert(!Directory.Exists(txn),"Empty post-cleanup transaction does not block publishing");
        });
        Case("publish-recovery-verification",(s,l)=>{
            string txn=CompletedPublication(l),current=Path.Combine(l,"Utility-v2.0.exe");
            Write(current,"changed after publication");
            Throws(()=>{using(Publisher.Lock(l)){}},"Checksum mismatch blocks automatic backup deletion");
            Assert(File.Exists(Path.Combine(txn,"obsolete-0")),"Backup retained for changed current build");
            File.Delete(current);
            Throws(()=>{using(Publisher.Lock(l)){}},"Missing published build blocks cleanup");
            Write(current,"verified current build");Write(Path.Combine(l,"Utility-v1.0.exe"),"restored old release");
            Throws(()=>{using(Publisher.Lock(l)){}},"Reappeared old release requires review");
            Assert(File.Exists(Path.Combine(txn,"recovery.xml")),"Verification failures preserve the journal");
        });
        Case("publish-unknown-recovery",(s,l)=>{
            string txn=CompletedPublication(l),journal=Path.Combine(txn,"recovery.xml");
            var document=XDocument.Load(journal);document.Root.Attribute("publishedHash").Remove();document.Save(journal);
            Throws(()=>{using(Publisher.Lock(l)){}},"Legacy completed record without checksum requires review");
            File.Delete(journal);
            string error=null;try{using(Publisher.Lock(l)){} }catch(IOException ex){error=ex.Message;}
            Assert(error!=null && error.Contains("recovery.xml is missing") && File.Exists(Path.Combine(txn,"obsolete-0")),"Orphaned backup is identified and never automatically deleted");
            Write(journal,"<publication");
            Throws(()=>{using(Publisher.Lock(l)){}},"Truncated journal is retained for review");
            Write(journal,"<publication committed=\"false\" />");
            Throws(()=>{using(Publisher.Lock(l)){}},"Uncommitted publication is never treated as disposable cleanup");
        });
        Case("publish-recovery-boundaries",(s,l)=>{
            string txn=CompletedPublication(l),journal=Path.Combine(txn,"recovery.xml");
            var original=XDocument.Load(journal);
            Write(Path.Combine(s,"Unrelated.exe"),"outside the shared root");
            var changed=new XDocument(original);changed.Root.SetAttributeValue("destination",Path.Combine(s,"Unrelated.exe"));changed.Save(journal);
            Throws(()=>{using(Publisher.Lock(l)){}},"Journal destination outside the share is refused");
            changed=new XDocument(original);changed.Root.Element("obsolete").SetAttributeValue("path",Path.Combine(s,"Unrelated.exe"));changed.Save(journal);
            Throws(()=>{using(Publisher.Lock(l)){}},"Journal obsolete path outside the share is refused");
            original.Save(journal);Write(Path.Combine(txn,"obsolete-folder","Exclude","archive.txt"),"preserve");
            Throws(()=>{using(Publisher.Lock(l)){}},"Recovery never removes a nested Exclude folder");
            Assert(File.Exists(journal) && File.Exists(Path.Combine(txn,"obsolete-0")) && File.ReadAllText(Path.Combine(s,"Unrelated.exe"))=="outside the shared root","Boundary failures preserve all evidence and unrelated files");
            Throws(()=>Publisher.CleanupTransaction(Path.Combine(l,".se-publisher"),s),"Cleanup rejects a directory outside its control root");
        });
    }
}
