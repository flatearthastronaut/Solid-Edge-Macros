using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Hammill.MacroLauncher
{
    // Tooltips are optional data. A missing or malformed folder must never remove
    // its macro button. Generated pages live in the user's cache, not the install.
    internal static class VideoTooltip
    {
        static string Escape(string text) { return text.Replace("&","&amp;").Replace("<","&lt;").Replace(">","&gt;").Replace("\"","&quot;"); }
        internal static string Discover(string executable, string name)
        {
            string root=Path.GetDirectoryName(executable);
            string folder=MacroCatalog.Child(root,"Tooltips");
            if(!Directory.Exists(folder))return null;
            foreach(string ext in new[]{".mp4",".wmv"})
            {
                var files=Directory.GetFiles(folder).Where(p=>Path.GetExtension(p).Equals(ext,StringComparison.OrdinalIgnoreCase)).OrderBy(p=>p,StringComparer.OrdinalIgnoreCase).ToArray();
                string selected=files.FirstOrDefault(p=>MacroCatalog.FamilyKey(Path.GetFileNameWithoutExtension(p))==MacroCatalog.FamilyKey(Path.GetFileNameWithoutExtension(executable)));
                if(selected==null && files.Length==1)selected=files[0];
                if(selected==null && files.Length>1)throw new InvalidDataException("Multiple tooltip videos: name the intended clip after the macro.");
                if(selected==null)continue;
                return MacroCatalog.Child(folder,Path.GetFileName(selected));
            }
            return null;
        }
        internal static string Page(string video,string title,string cache)
        {
            Directory.CreateDirectory(cache);
            string url=Escape(new Uri(video).AbsoluteUri);
            string body=Path.GetExtension(video).Equals(".wmv",StringComparison.OrdinalIgnoreCase)
                ? "<p>This recording is WMV. Use an MP4 copy for playback here.</p>"
                : "<video width=\"320\" height=\"180\" autoplay muted loop controls><source src=\""+url+"\" type=\"video/mp4\"></video>";
            string html="<!doctype html><html><head><meta charset=\"utf-8\"><style>body{font:14px Segoe UI;margin:12px;width:320px}h3{color:#729403}</style></head><body><h3>"+Escape(title)+"</h3>"+body+"<p><a href=\""+url+"\">Open recording</a></p></body></html>";
            // Content-addressed pages avoid races between multiple SE sessions.
            string path=Path.Combine(cache,MacroCatalog.Hash(Encoding.UTF8.GetBytes(html))+".htm");
            if(!File.Exists(path))using(var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.Read))using(var writer=new StreamWriter(stream,Encoding.UTF8))writer.Write(html);
            return new Uri(path).AbsoluteUri;
        }
    }
}
