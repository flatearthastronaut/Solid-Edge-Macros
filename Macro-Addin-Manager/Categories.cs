using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace CoopManager
{
    internal static class Categories
    {
        public const string Macros = "Solid Edge Macros", Addins = "Solid Edge Add-ins", Windows = "Windows Programs";
        public static readonly string[] All = { Macros, Addins, Windows };
        public static string Detect(Package package)
        {
            if(package.Bundle) return Addins;
            try
            {
                IEnumerable<string> names;
                if(Directory.Exists(package.Path)) names=Files.SourceTree(package.Path).ToArray();
                else if(Path.GetExtension(package.Path).Equals(".zip",StringComparison.OrdinalIgnoreCase))
                { using(var zip=ZipFile.OpenRead(package.Path)) names=zip.Entries.Select(e=>e.FullName).ToArray(); }
                else return Macros; // Both macro and desktop EXEs are executable files; never guess from the extension.
                if(names.Any(n=>Path.GetFileName(n).Equals("Register.cmd",StringComparison.OrdinalIgnoreCase)) ||
                    (names.Any(n=>Path.GetExtension(n).Equals(".dll",StringComparison.OrdinalIgnoreCase)) && !names.Any(n=>Path.GetExtension(n).Equals(".exe",StringComparison.OrdinalIgnoreCase)))) return Addins;
            }
            catch(Exception ex) { Diagnostics.Error("CATEGORY DETECTION FAILED path="+package.Path,ex); }
            return Macros;
        }
    }
    internal sealed class CategoryPreferences
    {
        readonly Dictionary<string,string> values = new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
        static string Key(Entry entry) { return entry.PackageKey ?? (entry.Available==null ? ReleaseName.Parse(entry.Name??"",false).Key : entry.Available.Key); }
        public string Get(Entry entry)
        { string category; return values.TryGetValue(Key(entry),out category) ? category : entry.Category; }
        public void Set(Entry entry,string category)
        { if(!Categories.All.Contains(category)) throw new ArgumentException("Unknown category."); values[Key(entry)]=category; }
        public void Load(XElement parent)
        {
            values.Clear();
            if(parent==null)return;
            foreach(var item in parent.Elements("package"))
            { string key=(string)item.Attribute("key"), category=(string)item.Attribute("category"); if(!string.IsNullOrEmpty(key) && Categories.All.Contains(category)) values[key]=category; }
        }
        public XElement Save()
        { return new XElement("categories",values.OrderBy(p=>p.Key).Select(p=>new XElement("package",new XAttribute("key",p.Key),new XAttribute("category",p.Value)))); }

        public const string FileName = ".se-manager-categories.xml";
        public static CategoryPreferences ReadShared(string root)
        {
            var result=new CategoryPreferences(); string path=Files.Child(root,FileName);
            if(File.Exists(path))
            {
                var document=XDocument.Load(path);
                if(document.Root==null || document.Root.Name!="categories")throw new IOException("Invalid shared category catalog: "+path);
                result.Load(document.Root);
            }
            return result;
        }
        internal void WriteShared(string root)
        {
            string path=Files.Child(root,FileName), temporary=path+"."+Guid.NewGuid().ToString("N")+".tmp";
            try
            {
                new XDocument(Save()).Save(temporary);
                if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);
            }
            finally{if(File.Exists(temporary))File.Delete(temporary);}
        }
        public static CategoryPreferences AssignShared(string root,Entry entry,string category)
        {
            // Reload while holding the publisher lock: two admins editing different rows must not
            // overwrite each other's assignments with stale copies of the catalog.
            using(Publisher.Lock(root))
            {
                var result=ReadShared(root);result.Set(entry,category);result.WriteShared(root);return result;
            }
        }
    }
}
