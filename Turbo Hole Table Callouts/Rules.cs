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
        internal double? Diameter, Depth, Bore, BoreDepth, Sink, Angle, ThreadDepth;
        internal string Thread = "";
        internal static HoleFacts Parse(string type, string fields)
        {
            string[] types = { "Other", "Simple", "Counterbore", "Countersink", "Simple Threaded", "Counterbore Threaded", "Countersink Threaded", "Counterdrill", "Counterdrill Threaded" };
            int code = Array.FindIndex(types, t => String.Equals(t, type.Trim(), StringComparison.OrdinalIgnoreCase));
            string[] values = fields.Split('|');
            if (code < 0 || (values.Length != 7&&values.Length!=8)) throw new InvalidDataException("Unsupported hole type or unreadable native hole fields.");
            return new HoleFacts { Type = code, Diameter = Number(values[0]), Depth = Number(values[1]), Bore = Number(values[2]),
                BoreDepth = Number(values[3]), Sink = Number(values[4]), Angle = Number(values[5]), Thread = values[6].Trim(),ThreadDepth=values.Length==8?Number(values[7]):null };
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
        internal string Name, Kind, Family, Pin, Fit;
        internal int Slot, Port;
        internal double Diameter, Depth, Bore, BoreDepth, Angle, HalfWidth, Minimum, Maximum;
        internal string Thread;
        internal double? ThreadDepth;
    }

    internal sealed class Decision
    {
        internal string Category, Reason, Family, Fit, Pin;
        internal int Slot;
        internal int Port;
        internal double HalfWidth;
        internal double Minimum, Maximum;
        internal double? ThreadDepth;
        internal bool IsDowel {get{return Family=="Dowel"||Category=="Dowel";}}
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
                else if (Regex.IsMatch(name, @"^SAE Port- #(?:2|4|6|8)\b", RegexOptions.IgnoreCase))
                { family = "SAE port"; port = Int32.Parse(Regex.Match(name, @"#(\d+)").Groups[1].Value); slot = port == 2 ? 2 : 4; }
                else if (name.StartsWith("C' Bore-", StringComparison.OrdinalIgnoreCase)) { family = "SHCS counterbore"; slot = 1; }
                else if (Regex.IsMatch(name,@"^Pin, Dowel-\s+\S+\s+(PF|SF)$",RegexOptions.IgnoreCase)) {family="Dowel";slot=2;}
                else if (name.StartsWith("Face Seal-",StringComparison.OrdinalIgnoreCase)) {family="O-ring face seal";slot=3;}
                else if(Regex.IsMatch(name,@"^Pin, Spring-\s+\.375\s+PF$",RegexOptions.IgnoreCase)){family="Spring pin";slot=1;}
                else continue;
                string unit = (string)node.Element("Unit");
                double scale = unit == "StandardMeasurementUnit" ? 1 : unit == "MetricMeasurementUnit" ? 1 / 25.4 : Double.NaN;
                if (Double.IsNaN(scale)) throw new InvalidDataException("Unknown units in preset " + name);
                book.Presets.Add(new Preset { Name = name, Kind = (string)node.Element("Type"), Family = family, Slot = slot, Port = port,
                    Diameter = Value(node,"Diameter") * scale, Depth = Value(node,"Depth") * scale,
                    Bore = Value(node,"Counterbore_Diameter") * scale, BoreDepth = Value(node,"Counterbore_Depth") * scale,
                    Angle = Value(node,"Counterbore_Bottom_Angle"), Thread = (string)node.Element("Thread_Designation") ?? "",
                    // The saved O-RING FACE SEAL callout specifies .054/.050
                    // for this .052 nominal recess, independently of passage length.
                    Minimum=family=="O-ring face seal"?.050:0,Maximum=family=="O-ring face seal"?.054:0,
                    ThreadDepth=port==8&&(string)node.Element("Thread_Extent_Type")=="FiniteThreadDepth"&&
                        !String.IsNullOrWhiteSpace((string)node.Element("Thread_Designation"))?Value(node,"Thread_Extent_Amount")*scale:(double?)null });
            }
            foreach(Preset spring in book.Presets.Where(p=>p.Family=="Spring pin"))
            {
                double minimum,maximum;
                if(!LinkedCallouts.SavedDiameterLimits("SP 375 PF",out minimum,out maximum))
                    throw new InvalidDataException("The SP 375 PF saved callout has no usable diameter limits.");
                spring.Minimum=minimum;spring.Maximum=maximum;spring.Pin="3/8 inch";spring.Fit="PF";
            }
            using(var stream=typeof(RuleBook).Assembly.GetManifestResourceStream("HoleTableFormatter.DowelSeats.xml"))
            {
                if(stream==null)throw new InvalidDataException("The packaged dowel design chart is missing. Rebuild the formatter.");
                var settings=new XmlReaderSettings{DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null};
                using(var reader=XmlReader.Create(stream,settings))foreach(XElement node in XDocument.Load(reader).Root.Elements("Seat"))
                {
                    double diameter=(double)node.Attribute("diameter"),halfWidth=(double)node.Attribute("halfWidth");
                    book.Presets.Add(new Preset{Name="Pin Sizes.doc: "+(string)node.Attribute("pin")+" "+(string)node.Attribute("fit"),
                        Pin=(string)node.Attribute("pin"),Fit=(string)node.Attribute("fit"),
                        Family="Dowel",Kind="Simple",Slot=2,Diameter=diameter,Depth=(double)node.Attribute("depth"),HalfWidth=halfWidth,
                        Minimum=diameter-halfWidth,Maximum=diameter+halfWidth});
                }
            }
            // Saved modeling sizes can be fit endpoints (or drill stock), not
            // the chart midpoint. Recognize the exact named preset geometry,
            // but carry the chart's finished limits separately from that nominal.
            foreach(Preset preset in book.Presets.Where(p=>p.Family=="Dowel"&&p.Pin==null).ToList())
            {
                Match name=Regex.Match(preset.Name,@"^Pin, Dowel-\s+(?<size>\S+)\s+(?<fit>PF|SF)$",RegexOptions.IgnoreCase);
                string token=name.Groups["size"].Value,fit=name.Groups["fit"].Value.ToUpperInvariant();
                var charts=book.Presets.Where(p=>p.Name.StartsWith("Pin Sizes.doc: ",StringComparison.Ordinal)&&p.Pin!=null&&p.Fit==fit&&PinAlias(p.Pin,token)).ToList();
                if(charts.Count!=1){book.Presets.Remove(preset);continue;}
                Preset chart=charts[0];preset.Pin=chart.Pin;preset.Fit=chart.Fit;
                preset.HalfWidth=chart.HalfWidth;preset.Minimum=chart.Minimum;preset.Maximum=chart.Maximum;
            }
            if (!book.Presets.Any(p => p.Family == "Koenig plug") || !book.Presets.Any(p => p.Family == "SAE port"))
                throw new InvalidDataException("The configured custom settings do not contain the expected CV plug and SAE port families.");
            return book;
        }
        static bool PinAlias(string chart,string token)
        {
            if(chart.EndsWith(" mm",StringComparison.Ordinal))
            {
                int size,actual;return token.EndsWith("mm",StringComparison.OrdinalIgnoreCase)&&
                    Int32.TryParse(chart.Split(' ')[0],out size)&&Int32.TryParse(token.Substring(0,token.Length-2),out actual)&&size==actual;
            }
            double actualSize;if(!Double.TryParse(token,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out actualSize))return false;
            string[] fraction=chart.Split(' ')[0].Split('/');
            double nominal=Double.Parse(fraction[0],CultureInfo.InvariantCulture)/(fraction.Length==2?Double.Parse(fraction[1],CultureInfo.InvariantCulture):1);
            // This rounding resolves preset NAME aliases (.188 = 3/16), never
            // a measured hole diameter or a manufactured fit tolerance.
            return Near(actualSize,nominal)||Near(actualSize,Math.Round(nominal,3,MidpointRounding.AwayFromZero));
        }
        static double Value(XElement node, string name)
        {
            double value;
            if (!Double.TryParse((string)node.Element(name), NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                Double.IsNaN(value) || Double.IsInfinity(value) || value < 0) throw new InvalidDataException("Invalid " + name + " in " + (string)node.Attribute("name"));
            return value;
        }

        internal Decision Match(HoleFacts hole, bool variableShcsDepth = false)
        {
            bool possibleFaceSeal=(hole.Type==2 || hole.Type==5) && Near(hole.BoreDepth,.052) && PortThread(hole.Thread)==0;
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
                    // CV is the former name for Koenig. Greg changes seat depth
                    // for the part, so only its nominal seat diameter and native
                    // unthreaded counterdrill type identify this family. Preserve
                    // the actual model-linked depth in the resulting callout.
                    match = Near(hole.Bore,preset.Bore) && Positive(hole.BoreDepth);
                else if (preset.Family == "O-ring face seal" && !threaded && hole.Type==2 && preset.Kind=="CounterBore")
                    // .052 alone is not sufficient: require one of the explicit
                    // face-seal recess diameters as well. Inlet drilling can be
                    // blind or through and does not identify the seal seat.
                    match=Near(hole.Bore,preset.Bore)&&Near(hole.BoreDepth,preset.BoreDepth)&&Near(preset.BoreDepth,.052);
                else if (preset.Family == "SAE port")
                {
                    if (preset.Kind == "CounterBore" && (hole.Type == 2 || hole.Type == 5))
                        match = Near(hole.Bore,preset.Bore) && Positive(hole.BoreDepth);
                    else if (preset.Kind == "CounterDrill" && hole.Type == 8)
                        match = PortThread(hole.Thread) == preset.Port && Near(hole.Bore,preset.Bore) && Positive(hole.BoreDepth);
                }
                else if (preset.Family == "SHCS counterbore" && !threaded && hole.Type == 2)
                    // Standalone callouts can describe a saved screw-clearance
                    // pair at a part-specific recess depth. Keep exact diameter
                    // matching and include this candidate in the normal conflict
                    // checks; never turn an unidentified .052 face seal into an
                    // SHCS note. The table caller retains its existing policy.
                    match = Near(hole.Diameter,preset.Diameter) && Near(hole.Bore,preset.Bore) &&
                        (Near(hole.BoreDepth,preset.BoreDepth) || (variableShcsDepth && !possibleFaceSeal &&
                            Positive(hole.BoreDepth) && !Double.IsInfinity(hole.BoreDepth.Value)));
                else if(preset.Family=="Spring pin"&&!threaded&&hole.Type==1&&preset.Kind=="Simple")
                    match=Near(hole.Diameter,preset.Diameter)&&Near(hole.Depth,preset.Depth);
                if (match) hits.Add(preset);
            }
            // Conflicts are reviewed even if both happen to select the same F slot:
            // a shared format number does not make different hole identities equal.
            var distinct = hits.GroupBy(p => p.Family + ":" + p.Port+(p.Family=="Dowel"?":"+p.Pin+":"+p.Fit:"")).ToList();
            if (distinct.Count > 1) return Decision.Review("Conflicting saved preset matches.");
            if (hits.Count > 0)
            {
                Preset preset = hits[0];
                if (preset.Port != 0 && !String.IsNullOrWhiteSpace(hole.Thread) && PortThread(hole.Thread) != preset.Port)
                    return Decision.Review("Thread and spotface identify different port sizes.");
                // The spotface feature has no modeled thread. For #8 use the
                // matching saved thread-only definition, never its drilling depth
                // or the spotface preset's unrelated default thread settings.
                var threadDepths=preset.Port==8?Presets.Where(p=>p.Port==8&&p.Kind=="CounterDrill"&&PortThread(p.Thread)==8&&p.ThreadDepth>0)
                    .Select(p=>p.ThreadDepth.Value).Distinct().ToList():new List<double>();
                return new Decision { Family=preset.Family,Fit=preset.Fit,Pin=preset.Pin,
                    Category = preset.Family=="Dowel"?"Dowel pin ("+(preset.Fit=="SF"?"slip fit":"press fit")+")":preset.Family=="Spring pin"?"Spring pin (3/8 inch)":preset.Family + (preset.Port == 0 ? "" : " #" + preset.Port), Slot = preset.Slot,
                    ThreadDepth=threadDepths.Count==1?(double?)threadDepths[0]:null,
                    Port = preset.Port, HalfWidth=preset.HalfWidth,Minimum=preset.Minimum,Maximum=preset.Maximum, Reason = "Matches " + preset.Name +
                        (preset.Family=="Koenig plug"||preset.Family=="SAE port"?" diameter/type; seat depth may vary.":"") };
            }
            if(possibleFaceSeal)return Decision.Review("Shallow recess does not match a saved face-seal diameter; leave unchanged.");
            if (PortThread(hole.Thread) != 0) return Decision.Review("Port-size thread: entrance/spotface does not match a verified preset. May be an older port.");
            if (hole.Type == 4 && !String.IsNullOrWhiteSpace(hole.Thread))
                return new Decision { Category = "Threaded hole", Slot = 1, Reason = "Standard threaded-hole format." };
            return Decision.Review("No verified standard-hole match. Leave unchanged.");
        }
        static bool Positive(double? value){return value.HasValue&&value.Value>0;}

        internal static int PortThread(string thread)
        {
            string normalized = Regex.Replace(thread ?? "", @"\s+", "");
            if (Regex.IsMatch(normalized,@"^(?:5/16|0?\.3125)-24(?:UNF.*)?$",RegexOptions.IgnoreCase)) return 2;
            if (Regex.IsMatch(normalized,@"^(?:7/16|0?\.4375)-20(?:UNF.*)?$",RegexOptions.IgnoreCase)) return 4;
            if (Regex.IsMatch(normalized,@"^(?:9/16|0?\.5625)-18(?:UNF.*)?$",RegexOptions.IgnoreCase)) return 6;
            if (Regex.IsMatch(normalized,@"^(?:3/4|0?\.750*)-16(?:UNF.*)?$",RegexOptions.IgnoreCase)) return 8;
            return 0;
        }
        internal static string PortDesignation(int port)
        {return port==2?"5/16-24":port==4?"7/16-20":port==6?"9/16-18":port==8?"3/4-16":null;}

        internal static string TemplateProblem(Decision choice, HoleFacts hole, string template)
        {
            if (String.IsNullOrWhiteSpace(template)) return "Target format is empty for this native hole type.";
            if (choice.Port != 0)
            {
                if((choice.Port==6||choice.Port==8)&&!LinkedCallouts.PortHasTwoPlaces(template))
                    return "SAE #6 and #8 callout dimensions must use two decimal places.";
                // A separate native threaded entrance can use the shared smart
                // thread callout. The spotface row needs the explicit port text.
                if (hole.Type == 8 && template.Contains("%TS") && PortThread(hole.Thread) == choice.Port) return null;
                string expected = "#" + choice.Port;
                bool number = Regex.IsMatch(template, Regex.Escape(expected) + @"\b");
                string thread = PortDesignation(choice.Port);
                if (!number || thread==null || !template.Contains(thread)) return "Target format does not explicitly describe the matching " + expected + " SAE port.";
            }
            if (choice.IsDowel)
            {
                string field=hole.Type==1?"HS":"BS";
                if(!LinkedLimitsMatch(template,field,hole.Type==1?hole.Diameter:hole.Bore,choice.Minimum,choice.Maximum,4))
                    return "Dowel F2 precision/fit limits do not match the pin-size chart. Preserve for review.";
                string note=LinkedCallouts.DowelFitNote(choice);
                if(note==null||!LinkedCallouts.HasLine(template,note))return "Dowel callout needs its saved P.F./S.F. note on a separate line.";
            }
            if(choice.Family=="O-ring face seal"&&(!template.Contains("%HS")||!template.Contains("%BS")||
                !LinkedLimitsMatch(template,"BD",hole.BoreDepth,choice.Minimum,choice.Maximum,3)))
                return "Face-seal F3 must retain linked diameters and the .054/.050 depth limits at three-place inch precision.";
            if(choice.Family=="Spring pin"&&(!LinkedLimitsMatch(template,"HS",hole.Diameter,choice.Minimum,choice.Maximum,3)||
                !template.Contains("3/8 SPRING PIN")))
                return "Spring-pin callout must retain the saved diameter limits and 3/8 spring-pin note.";
            if (choice.Category == "Koenig plug" && (!template.Contains("%BS") || !template.Contains("%BD"))) return "Plug format is missing linked seat dimensions.";
            if(choice.Family=="Koenig plug"&&!LinkedCallouts.HasLine(template,LinkedCallouts.KoenigNote))return "Koenig callout needs (DO NOT CHAMFER HOLE) on a separate line.";
            return null;
        }
        static bool LinkedLimitsMatch(string template,string field,double? nominal,double minimum,double maximum,int precision)
        {
            Match expression=Regex.Match(template,@"%\{%"+field+@"(?<options>[^{}]*)\}");
            Match limits=Regex.Match(expression.Groups["options"].Value,@"/LT(?<upper>[+\-]?(?:\d*\.)?\d+)\^(?<lower>[+\-]?(?:\d*\.)?\d+)(?=/|$)");
            double upper,lower;
            return limits.Success&&Double.TryParse(limits.Groups["upper"].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out upper)&&
                Double.TryParse(limits.Groups["lower"].Value,NumberStyles.Float,CultureInfo.InvariantCulture,out lower)&&
                Near(nominal+upper,maximum)&&Near(nominal+lower,minimum)&&Regex.IsMatch(expression.Groups["options"].Value,@"/@"+precision+@"(?=/|$)");
        }
    }
}
