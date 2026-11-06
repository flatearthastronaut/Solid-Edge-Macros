using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace HoleTableFormatter
{
    internal sealed class HoleFacts
    {
        internal int Type;
        internal double? Diameter, Depth, Bore, BoreDepth, Sink, Angle;
        internal string Thread = "";
        internal static HoleFacts Parse(string type, string fields)
        {
            string[] types = { "Other", "Simple", "Counterbore", "Countersink", "Simple Threaded", "Counterbore Threaded", "Countersink Threaded", "Counterdrill", "Counterdrill Threaded" };
            int code = Array.FindIndex(types, t => String.Equals(t, type.Trim(), StringComparison.OrdinalIgnoreCase));
            string[] values = fields.Split('|');
            if (code < 0 || values.Length != 7) throw new InvalidDataException("Unsupported hole type or unreadable native hole fields.");
            return new HoleFacts { Type = code, Diameter = Number(values[0]), Depth = Number(values[1]), Bore = Number(values[2]),
                BoreDepth = Number(values[3]), Sink = Number(values[4]), Angle = Number(values[5]), Thread = values[6].Trim() };
        }
        static double? Number(string text)
        {
            if (String.IsNullOrWhiteSpace(text)) return null;
            double number;
            // Probe templates explicitly request inch decimal values, no symbols,
            // and eight places. Do not interpret rounded rendered production text.
            if (!Double.TryParse(text.Trim(), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out number) || Double.IsNaN(number) || Double.IsInfinity(number))
                throw new InvalidDataException("Cannot read native numeric field: " + text);
            return number;
        }
    }

    internal sealed class Preset
    {
        internal string Name, Kind, Family;
        internal int Slot, Port;
        internal double Diameter, Depth, Bore, BoreDepth, Angle, HalfWidth;
        internal string Thread;
    }

    internal sealed class Decision
    {
        internal string Category, Reason;
        internal int Slot;
        internal int Port;
        internal double HalfWidth;
        internal static Decision Review(string reason) { return new Decision { Category = "Review", Reason = reason }; }
    }

    internal sealed class RuleBook
    {
        internal readonly List<Preset> Presets = new List<Preset>();
        internal string Source;
        // Numerical comparison only, far below manufacturing tolerances. Never
        // use a fit/tolerance interval to blur one nominal hole into another.
        internal const double Epsilon = 0.000001;
        internal static bool Near(double? a, double b) { return a.HasValue && Math.Abs(a.Value - b) <= Epsilon; }

        internal static RuleBook Load(string path)
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = XmlReader.Create(file, settings)) return Read(XDocument.Load(reader), path);
        }
        internal static RuleBook Read(XDocument document, string source)
        {
            var book = new RuleBook { Source = source };
            foreach (XElement node in document.Descendants("SavedHole"))
            {
                string name = (string)node.Attribute("name") ?? "", family = "";
                int slot = 0, port = 0;
                if (Regex.IsMatch(name, @"^CV Hole-\s*\d+mm Plug$", RegexOptions.IgnoreCase)) { family = "Koenig plug"; slot = 3; }
                else if (Regex.IsMatch(name, @"^SAE Port- #(?:2|4|6)\b", RegexOptions.IgnoreCase))
                { family = "SAE port"; port = Int32.Parse(Regex.Match(name, @"#(\d+)").Groups[1].Value); slot = port == 2 ? 2 : 4; }
                else if (name.StartsWith("C' Bore-", StringComparison.OrdinalIgnoreCase)) { family = "SHCS counterbore"; slot = 1; }
                else continue;
                string unit = (string)node.Element("Unit");
                double scale = unit == "StandardMeasurementUnit" ? 1 : unit == "MetricMeasurementUnit" ? 1 / 25.4 : Double.NaN;
                if (Double.IsNaN(scale)) throw new InvalidDataException("Unknown units in preset " + name);
                book.Presets.Add(new Preset { Name = name, Kind = (string)node.Element("Type"), Family = family, Slot = slot, Port = port,
                    Diameter = Value(node,"Diameter") * scale, Depth = Value(node,"Depth") * scale,
                    Bore = Value(node,"Counterbore_Diameter") * scale, BoreDepth = Value(node,"Counterbore_Depth") * scale,
                    Angle = Value(node,"Counterbore_Bottom_Angle"), Thread = (string)node.Element("Thread_Designation") ?? "" });
            }
            using(var stream=typeof(RuleBook).Assembly.GetManifestResourceStream("HoleTableFormatter.DowelSeats.xml"))
            {
                if(stream==null)throw new InvalidDataException("The packaged dowel design chart is missing. Rebuild the formatter.");
                var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null};
                using(var reader=XmlReader.Create(stream,settings))foreach(XElement node in XDocument.Load(reader).Root.Elements("Seat"))
                {
                    book.Presets.Add(new Preset{Name="Pin Sizes.doc: "+(string)node.Attribute("pin")+" "+(string)node.Attribute("fit"),
                        Family="Dowel",Kind="Simple",Slot=2,Diameter=(double)node.Attribute("diameter"),Depth=(double)node.Attribute("depth"),HalfWidth=(double)node.Attribute("halfWidth")});
                }
            }
            if (!book.Presets.Any(p => p.Family == "Koenig plug") || !book.Presets.Any(p => p.Family == "SAE port"))
                throw new InvalidDataException("The configured custom settings do not contain the expected CV plug and SAE port families.");
            return book;
        }
        static double Value(XElement node, string name)
        {
            double value;
            if (!Double.TryParse((string)node.Element(name), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                Double.IsNaN(value) || Double.IsInfinity(value) || value < 0) throw new InvalidDataException("Invalid " + name + " in " + (string)node.Attribute("name"));
            return value;
        }

        internal Decision Match(HoleFacts hole)
        {
            if (Near(hole.BoreDepth,.052)) return Decision.Review("Possible 1/16-inch O-ring face seal; not an SAE-port rule.");
            var hits = new List<Preset>();
            foreach (Preset preset in Presets)
            {
                bool threaded = !String.IsNullOrWhiteSpace(hole.Thread);
                bool match = false;
                if (preset.Family == "Dowel" && !threaded)
                {
                    // Older drawings can model only the pin seat as a simple
                    // hole; current presets can add a smaller through-relief.
                    // Compare the seat dimensions in either representation.
                    double seat=preset.Kind=="CounterDrill"?preset.Bore:preset.Diameter;
                    double seatDepth=preset.Kind=="CounterDrill"?preset.BoreDepth:preset.Depth;
                    if (hole.Type == 1) match = Near(hole.Diameter,seat) && Near(hole.Depth,seatDepth);
                    else if (hole.Type == 7) match = Near(hole.Bore,seat) && Near(hole.BoreDepth,seatDepth);
                }
                else if (preset.Family == "Koenig plug" && !threaded && hole.Type == 7)
                    // Passage length varies with the part. The plug seat diameter
                    // and depth identify the installed plug, not the preset's
                    // convenient default drilling length or passage diameter.
                    match = Near(hole.Bore,preset.Bore) && Near(hole.BoreDepth,preset.BoreDepth);
                else if (preset.Family == "SAE port")
                {
                    if (preset.Kind == "CounterBore" && (hole.Type == 2 || hole.Type == 5))
                        match = Near(hole.Bore,preset.Bore) && Near(hole.BoreDepth,preset.BoreDepth);
                    else if (preset.Kind == "CounterDrill" && hole.Type == 8)
                        match = PortThread(hole.Thread) == preset.Port && Near(hole.Bore,preset.Bore) && Near(hole.BoreDepth,preset.BoreDepth);
                }
                else if (preset.Family == "SHCS counterbore" && !threaded && hole.Type == 2)
                    match = Near(hole.Diameter,preset.Diameter) && Near(hole.Bore,preset.Bore) && Near(hole.BoreDepth,preset.BoreDepth);
                if (match) hits.Add(preset);
            }
            // Conflicts are reviewed even if both happen to select the same F slot:
            // a shared format number does not make different hole identities equal.
            var distinct = hits.GroupBy(p => p.Family + ":" + p.Port).ToList();
            if (distinct.Count > 1) return Decision.Review("Conflicting saved preset matches.");
            if (hits.Count > 0)
            {
                Preset preset = hits[0];
                if (preset.Port != 0 && !String.IsNullOrWhiteSpace(hole.Thread) && PortThread(hole.Thread) != preset.Port)
                    return Decision.Review("Thread and spotface identify different port sizes.");
                return new Decision { Category = preset.Family + (preset.Port == 0 ? "" : " #" + preset.Port), Slot = preset.Slot,
                    Port = preset.Port, HalfWidth=preset.HalfWidth, Reason = "Matches " + preset.Name };
            }
            if (PortThread(hole.Thread) != 0) return Decision.Review("Port-size thread: entrance/spotface does not match a verified preset. May be an older port.");
            if (hole.Type == 4 && !String.IsNullOrWhiteSpace(hole.Thread))
                return new Decision { Category = "Threaded hole", Slot = 1, Reason = "Standard threaded-hole format." };
            return Decision.Review("No verified diameter/depth combination. Leave unchanged.");
        }

        internal static int PortThread(string thread)
        {
            string normalized = Regex.Replace(thread ?? "", @"\s+", "");
            if (Regex.IsMatch(normalized,@"^(?:5/16|0?\.3125)-24(?:UNF.*)?$",RegexOptions.IgnoreCase)) return 2;
            if (Regex.IsMatch(normalized,@"^(?:7/16|0?\.4375)-20(?:UNF.*)?$",RegexOptions.IgnoreCase)) return 4;
            if (Regex.IsMatch(normalized,@"^(?:9/16|0?\.5625)-18(?:UNF.*)?$",RegexOptions.IgnoreCase)) return 6;
            return 0;
        }

        internal static string TemplateProblem(Decision choice, HoleFacts hole, string template)
        {
            if (String.IsNullOrWhiteSpace(template)) return "Target format is empty for this native hole type.";
            if (choice.Port != 0)
            {
                // A separate native threaded entrance can use the shared smart
                // thread callout. The spotface row needs the explicit port text.
                if (hole.Type == 8 && template.Contains("%TS") && PortThread(hole.Thread) == choice.Port) return null;
                string expected = "#" + choice.Port;
                bool number = Regex.IsMatch(template, Regex.Escape(expected) + @"\b");
                string thread = choice.Port == 2 ? "5/16-24" : choice.Port == 4 ? "7/16-20" : "9/16-18";
                if (!number || !template.Contains(thread)) return "Target format does not explicitly describe the matching " + expected + " SAE port.";
            }
            if (choice.Category == "Dowel")
            {
                string field=hole.Type==1?"HS":"BS";
                Match expression=Regex.Match(template,@"%\{%"+field+@"(?<options>[^{}]*)\}");
                Match limits=Regex.Match(expression.Groups["options"].Value,@"/LT(?<upper>[+\-]?(?:\d*\.)?\d+)\^(?<lower>[+\-]?(?:\d*\.)?\d+)(?=/|$)");
                double upper,lower;
                if(!limits.Success || !Double.TryParse(limits.Groups["upper"].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out upper) ||
                    !Double.TryParse(limits.Groups["lower"].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out lower) ||
                    !Near(upper,choice.HalfWidth) || !Near(lower,-choice.HalfWidth) || !Regex.IsMatch(expression.Groups["options"].Value,@"/@4(?=/|$)"))
                    return "Dowel F2 precision/fit limits do not match the pin-size chart. Preserve for review.";
            }
            if (choice.Category == "Koenig plug" && (!template.Contains("%BS") || !template.Contains("%BD"))) return "Plug format is missing linked seat dimensions.";
            return null;
        }
    }
}
