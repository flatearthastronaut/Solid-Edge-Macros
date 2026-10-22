using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace CoopManager
{
    // One cache per displayed catalog. Every bitmap is owned here, never by a package or file stream.
    // Loading closes all streams immediately so displaying an icon cannot lock a program being updated.
    internal sealed class PackageIcons : IDisposable
    {
        readonly Dictionary<string, Bitmap> cache = new Dictionary<string, Bitmap>(StringComparer.OrdinalIgnoreCase);
        static bool Artwork(string path) { string ext = Path.GetExtension(path); return ext.Equals(".ico", StringComparison.OrdinalIgnoreCase) || ext.Equals(".png", StringComparison.OrdinalIgnoreCase); }
        static Bitmap Resize(Image image)
        {
            var result = new Bitmap(32,32);
            using (var g = Graphics.FromImage(result))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                double scale = Math.Min(32.0/image.Width,32.0/image.Height);
                int w = Math.Max(1,(int)(image.Width*scale)), h = Math.Max(1,(int)(image.Height*scale));
                g.DrawImage(image,(32-w)/2,(32-h)/2,w,h);
            }
            return result;
        }
        static Bitmap Read(Stream stream, string name)
        {
            if (Path.GetExtension(name).Equals(".ico",StringComparison.OrdinalIgnoreCase))
                return ReadIcon(stream);
            using (var image = Image.FromStream(stream))
            {
                if (image.Width>4096 || image.Height>4096) throw new IOException("Icon dimensions exceed 4096 pixels.");
                return Resize(image);
            }
        }
        static Bitmap ReadIcon(Stream input)
        {
            // GDI+ Icon.ToBitmap can misinterpret PNG-compressed ICO frames as DIB pixels.
            // Decode the PNG payload directly, retaining alpha and selecting the closest 32px frame.
            using(var buffer=new MemoryStream())
            {
                input.CopyTo(buffer); byte[] data=buffer.ToArray();
                if(data.Length<6 || BitConverter.ToUInt16(data,0)!=0 || BitConverter.ToUInt16(data,2)!=1)throw new IOException("Invalid ICO header.");
                int count=BitConverter.ToUInt16(data,4);
                if(count==0 || 6L+16L*count>data.Length)throw new IOException("Invalid ICO directory.");
                var frames=new List<Tuple<int,int,int>>();
                for(int n=0;n<count;n++)
                {
                    int p=6+16*n; long length=BitConverter.ToUInt32(data,p+8), offset=BitConverter.ToUInt32(data,p+12);
                    if(offset<6L+16L*count || length<8 || offset+length>data.Length)continue;
                    int size=data[p]==0?256:data[p];
                    if(data[offset]==137 && data[offset+1]==80 && data[offset+2]==78 && data[offset+3]==71)
                        frames.Add(Tuple.Create(size,(int)offset,(int)length));
                }
                foreach(var frame in frames.OrderBy(f=>f.Item1>=32?f.Item1-32:1000-f.Item1))
                    using(var png=new MemoryStream(data,frame.Item2,frame.Item3))
                    using(var image=Image.FromStream(png))return Resize(image);
                buffer.Position=0;
                using(var icon=new Icon(buffer,32,32))using(var bitmap=icon.ToBitmap())return Resize(bitmap);
            }
        }
        static Bitmap TryFile(string path)
        {
            try
            {
                Files.NoLinks(path);
                if (Artwork(path))
                {
                    if(new FileInfo(path).Length>4*1024*1024) return null;
                    using(var stream=File.OpenRead(path)) return Read(stream,path);
                }
                // Shell extraction reads resources only; the executable is never launched.
                using(var icon=Icon.ExtractAssociatedIcon(path))
                    if(icon!=null) using(var bitmap=icon.ToBitmap()) return Resize(bitmap);
            }
            catch(Exception ex) { Diagnostics.Error("ICON SKIPPED path="+path,ex); }
            return null;
        }
        public Bitmap Get(Entry entry)
        {
            string path=entry.Available==null ? entry.Details : entry.Available.Path;
            string key=path??"";
            Bitmap found;
            if(cache.TryGetValue(key,out found)) return found;
            try { found=Load(path,entry.Available!=null && entry.Available.Bundle); }
            catch(Exception ex) { Diagnostics.Error("ICON LOOKUP FAILED path="+path,ex); }
            if(found==null) using(var fallback=SystemIcons.Application.ToBitmap()) found=Resize(fallback);
            cache.Add(key,found); return found;
        }
        static Bitmap Load(string path,bool bundle)
        {
            if(string.IsNullOrEmpty(path)) return null;
            bool folder=Directory.Exists(path);
            if(!folder && !File.Exists(path)) return null;
            Files.NoLinks(path);
            string family=bundle ? ReleaseName.Parse("SEAddIn",false).Key : ReleaseName.Parse(Path.GetFileName(path),!folder).Key;
            var candidates=folder ? (bundle ? Directory.GetFiles(path).AsEnumerable() : Files.SourceTree(path)) : Directory.GetFiles(Path.GetDirectoryName(path)).Where(f=>ReleaseName.Parse(Path.GetFileName(f),true).Key==family);
            var files=candidates.ToList();
            // The ZIP is the release being offered: its own ICO wins over adjacent images
            // and over PNG previews, even when the ICO is in a nested assets directory.
            if(!folder && Path.GetExtension(path).Equals(".zip",StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    using(var zip=ZipFile.OpenRead(path))
                        foreach(var e in zip.Entries.Where(e=>Artwork(e.FullName) && e.Length<=4*1024*1024)
                            .OrderBy(e=>Path.GetExtension(e.FullName).Equals(".ico",StringComparison.OrdinalIgnoreCase)?0:1)
                            .ThenBy(e=>ReleaseName.Parse(Path.GetFileName(e.FullName),true).Key==family?0:1)
                            .ThenBy(e=>e.FullName,StringComparer.OrdinalIgnoreCase))
                            try
                            {
                                using(var stream=e.Open()) using(var buffer=new MemoryStream())
                                {
                                    stream.CopyTo(buffer); buffer.Position=0;
                                    var image=Read(buffer,e.FullName);
                                    Diagnostics.Info("ICON SELECTED archive="+path+" entry="+e.FullName);
                                    return image;
                                }
                            }
                            catch(Exception ex) { Diagnostics.Error("ZIP ICON SKIPPED archive="+path+" entry="+e.FullName,ex); }
                }
                catch(Exception ex) { Diagnostics.Error("ZIP ICON LOOKUP FAILED archive="+path,ex); }
            }
            foreach(string f in files.Where(Artwork).Where(f=>!bundle || ReleaseName.Parse(Path.GetFileName(f),true).Key==family)
                .OrderBy(f=>Path.GetExtension(f).Equals(".ico",StringComparison.OrdinalIgnoreCase)?0:1)
                .ThenBy(f=>ReleaseName.Parse(Path.GetFileName(f),true).Key==family?0:1).ThenBy(f=>f,StringComparer.OrdinalIgnoreCase))
            { var icon=TryFile(f); if(icon!=null) { Diagnostics.Info("ICON SELECTED file="+f); return icon; } }
            var binaries=folder ? files.Where(f=>Path.GetExtension(f).Equals(".exe",StringComparison.OrdinalIgnoreCase) || Path.GetExtension(f).Equals(".dll",StringComparison.OrdinalIgnoreCase)) : new[]{path}.Where(f=>Path.GetExtension(f).Equals(".exe",StringComparison.OrdinalIgnoreCase));
            foreach(var f in binaries.Where(f=>!bundle || ReleaseName.Parse(Path.GetFileName(f),true).Key==family).OrderBy(f=>ReleaseName.Parse(Path.GetFileName(f),true).Key==family?0:1))
            { var icon=TryFile(f); if(icon!=null)return icon; }
            return null;
        }
        public void Dispose() { foreach(var image in cache.Values) image.Dispose(); cache.Clear(); }
    }
}
