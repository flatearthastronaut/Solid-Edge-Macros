using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
namespace BoltCircleHoles
{
    public static class PortableChart
    {
        public static List<HoleSize> TryLoad(string path)
        {
            // The SHA256 binds this embedded transcription to the exact distributed XLS.
            // Refresh both rows and fingerprint after a chart revision; changing only
            // the hash would make stale dimensions appear authoritative. A mismatch
            // returns null to request the workbook reader, while corrupt resources throw.
            string hash;
            using(var input=File.OpenRead(path))
            using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(input)).Replace("-","");
            var doc=new XmlDocument();doc.XmlResolver=null;
            // Chart XML is data only. Disable external resolution and DTD processing;
            // loading dimensions must never fetch another document or expand entities.
            using(var stream=typeof(PortableChart).Assembly.GetManifestResourceStream("BoltCircleHoles.CounterboreChart.xml"))
            {
                if(stream==null)throw new InvalidDataException("The portable chart resource is missing. Rebuild the macro with CounterboreChart.xml.");
                var settings=new XmlReaderSettings { DtdProcessing=DtdProcessing.Prohibit, XmlResolver=null };
                using(var reader=XmlReader.Create(stream,settings))doc.Load(reader);
            }
            var root=doc.DocumentElement;
            if(root.Name!="CounterboreChart" || root.GetAttribute("units")!="inches")throw new InvalidDataException("Invalid portable chart resource.");
            if(!String.Equals(hash,root.GetAttribute("sha256"),StringComparison.OrdinalIgnoreCase))
            {
                RunLog.Write("CHART.portable-mismatch","Workbook differs from the embedded chart. Reading workbook directly; stale embedded sizes will not be used.");
                return null;
            }
            var rows=new List<HoleSize>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach(XmlElement node in root.SelectNodes("Hole"))
            {
                string screw=node.GetAttribute("screw");
                if(String.IsNullOrWhiteSpace(screw) || !names.Add(screw))throw new InvalidDataException("Invalid or duplicate portable screw size.");
                double drill=Positive(node,"drill"),bore=Positive(node,"bore");
                double? depth=node.HasAttribute("depth") ? (double?)Positive(node,"depth") : null;
                // Preserve missing depths as null so the UI can show the size but block
                // creation. No guessed depth or zero-depth counterbore is substituted.
                if(bore<=drill)throw new InvalidDataException("Portable counterbore must exceed drill diameter.");
                rows.Add(new HoleSize {Screw=screw,Drill=drill,Bore=bore,Depth=depth});
            }
            if(rows.Count==0)throw new InvalidDataException("Portable chart contains no sizes.");
            RunLog.Write("CHART.portable","Workbook SHA256 matched; loaded "+rows.Count+" embedded rows in inches without ACE or Excel.");
            return rows;
        }
        static double Positive(XmlElement node,string field)
        {
            double value;
            if(!Double.TryParse(node.GetAttribute(field),NumberStyles.Float,CultureInfo.InvariantCulture,out value) || Double.IsNaN(value) || Double.IsInfinity(value) || value<=0)
                throw new InvalidDataException("Invalid portable chart "+field+" value.");
            return value;
        }
    }
}
