using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HoleTableFormatter
{
    internal static class LinkedCallouts
    {
        static readonly Lazy<Dictionary<string,string>> saved=new Lazy<Dictionary<string,string>>(LoadSaved);
        internal static string For(Decision choice,HoleFacts hole,string sharedTemplate)
        {return Build(choice,hole,sharedTemplate,false);}
        internal static string Previous(Decision choice,HoleFacts hole,string sharedTemplate)
        {return Build(choice,hole,sharedTemplate,true);}
        internal static string PreviousDowelNote(Decision choice,HoleFacts hole,string sharedTemplate)
        {
            if(!choice.IsDowel)return null;
            string text=For(choice,hole,sharedTemplate);
            return text==null?null:WithDowelNote(text,choice,true);
        }
        internal static bool RevisedFamily(Decision choice)
        {return choice.IsDowel||choice.Family=="Koenig plug"||choice.Port==6||choice.Port==8;}
        static string Build(Decision choice,HoleFacts hole,string sharedTemplate,bool previous)
        {
            if(choice.Port==6 && (hole.Type==2||hole.Type==5))
            {
                // Greg approved a separate native linked expression for #6 when
                // F4's shared counterbore text describes #4. Never change F4.
                string text=ReadSaved("SAE Port #6");
                if(!text.Contains("#6")||!text.Contains("9/16-18"))return null;
                text=Regex.Replace(text,@"%(HS|BD)\b",m=>"%{%"+m.Groups[1].Value+"/NU/@"+(previous?3:2)+"}");
                return ResolveSmartDepth(text,hole,previous?3:2);
            }
            if(choice.Port==8&&(hole.Type==2||hole.Type==5)&&choice.ThreadDepth>0)
            {
                string text="%DI%{%HS/NU/@3} DRILL %ZH%RT#8 STRAIGHT THR'D O-RING PORT%RT"+
                    "%DI%{%BS/NU/@3} SPOTFACE %{%BD/NU/@3} DP.%RT3/4-16 THR'D "+
                    choice.ThreadDepth.Value.ToString("0.000",CultureInfo.InvariantCulture)+" DP.";
                if(!previous)text=PortPrecision(text.Replace(choice.ThreadDepth.Value.ToString("0.000",CultureInfo.InvariantCulture),choice.ThreadDepth.Value.ToString("0.00",CultureInfo.InvariantCulture)));
                return ResolveSmartDepth(text,hole,previous?3:2);
            }
            if(!previous&&(choice.Port==6||choice.Port==8)&&hole.Type==8)
            {
                string text=PortPrecision(ResolveSmartDepth(sharedTemplate,hole,2));
                return RuleBook.TemplateProblem(choice,hole,text)==null?text:null;
            }
            if(!previous&&choice.Family=="Koenig plug")
            {
                if(!sharedTemplate.Contains("%BS")||!sharedTemplate.Contains("%BD"))return null;
                string text=ResolveSmartDepth(sharedTemplate,hole);
                return HasLine(text,KoenigNote)?text:text+"%RT"+KoenigNote;
            }
            if(choice.Family=="Spring pin"&&hole.Type==1&&hole.Diameter.HasValue)
            {
                string saved=ReadSaved("SP 375 PF");
                string limits="%DI%{%HS/LT"+Number(choice.Maximum-hole.Diameter.Value)+"^"+
                    Number(choice.Minimum-hole.Diameter.Value)+"/NU/@3}";
                string text=Regex.Replace(saved,@"%DI\s*(?:\d*\.)?\d+/(?:\d*\.)?\d+",limits,RegexOptions.None);
                text=Regex.Replace(text,@"%HD\b","%{%HD/NU/@2}");
                return RuleBook.TemplateProblem(choice,hole,text)==null?ResolveSmartDepth(text,hole):null;
            }
            if(choice.IsDowel)
            {
                string field=hole.Type==1?"HS":"BS";
                double? nominal=hole.Type==1?hole.Diameter:hole.Bore;
                if(!nominal.HasValue||choice.Maximum<=choice.Minimum)return null;
                Match size=Regex.Match(sharedTemplate??"",@"%\{%"+field+@"(?<options>[^{}]*)\}");
                if(!size.Success)return null;
                string options=Regex.Replace(size.Groups["options"].Value,@"/LT[^/]*|/@\d+","");
                string replacement="%{%"+field+"/LT"+Number(choice.Maximum-nominal.Value)+"^"+Number(choice.Minimum-nominal.Value)+options+"/@4}";
                string text=sharedTemplate.Substring(0,size.Index)+replacement+sharedTemplate.Substring(size.Index+size.Length);
                if(!previous)text=WithDowelNote(text,choice);
                if(!previous&&RuleBook.TemplateProblem(choice,hole,text)!=null)return null;
                return ResolveSmartDepth(text,hole);
            }
            return null;
        }
        internal const string KoenigNote="(DO NOT CHAMFER HOLE)";
        internal static bool HasLine(string text,string line)
        {return (text??"").Split(new[]{"%RT"},StringSplitOptions.None).Any(s=>s.Trim()==line);}
        internal static string DowelFitNote(Decision choice)
        {
            string note=SavedDowelFitNote(choice);if(note==null)return null;
            // Saved dual-unit notes put the approved inch fit amount in brackets.
            // Keep that exact value rather than converting its rounded metric
            // equivalent. The nominal pin designation (e.g. 10mm) is unchanged.
            return Regex.Replace(note,@"^(\(\s*)(?:[+\-]?(?:\d*\.)?\d+)?\[(?<inch>[+\-]?(?:\d*\.)?\d+)\]",
                m=>m.Groups[1].Value+m.Groups["inch"].Value);
        }
        static string SavedDowelFitNote(Decision choice)
        {
            if(String.IsNullOrEmpty(choice.Pin)||(choice.Fit!="PF"&&choice.Fit!="SF"))return null;
            string pin=choice.Pin.Replace(" inch","").Replace(" mm","mm"),name;
            if(choice.Pin.EndsWith(" mm",StringComparison.Ordinal))
                name="DOWEL- "+Int32.Parse(choice.Pin.Split(' ')[0],CultureInfo.InvariantCulture).ToString("00",CultureInfo.InvariantCulture)+"mm "+choice.Fit;
            else
            {
                string[] fraction=pin.Split('/');
                double nominal=Double.Parse(fraction[0],CultureInfo.InvariantCulture)/(fraction.Length==2?Double.Parse(fraction[1],CultureInfo.InvariantCulture):1);
                name="DOWEL "+Math.Round(nominal*1000,MidpointRounding.AwayFromZero).ToString("000",CultureInfo.InvariantCulture)+" "+choice.Fit;
            }
            string library;
            if(saved.Value.TryGetValue(name,out library))
            {
                string note=library.Split(new[]{"%RT"},StringSplitOptions.None).SingleOrDefault(line=>Regex.IsMatch(line,@"\b[PS]\.F\. FOR\b"));
                if(note!=null)return note;
            }
            // Some chart sizes have no saved callout. Identify their fit and pin
            // without inventing a maximum interference/clearance specification.
            return "("+(choice.Fit=="PF"?"P.F.":"S.F.")+" FOR "+pin+" DOWEL)";
        }
        static string WithDowelNote(string text,Decision choice,bool savedUnits=false)
        {
            string note=savedUnits?SavedDowelFitNote(choice):DowelFitNote(choice);if(note==null)return text;
            var lines=text.Split(new[]{"%RT"},StringSplitOptions.None).Where(line=>!Regex.IsMatch(line,@"^\s*\(.*\b[PS]\.F\..*DOWEL\)\s*$"));
            return String.Join("%RT",lines)+"%RT"+note;
        }
        internal static string PortPrecision(string text)
        {
            // Change only explicit inch field precision. Thread designations and
            // tolerance magnitudes are not dimensions to round or rewrite.
            const string fields="HS|HD|BS|BD|SS|SA|TD";
            text=Regex.Replace(text,@"%\{%(?:"+fields+@")(?<options>[^{}]*)\}",m=>
                m.Value.Substring(0,5)+Regex.Replace(m.Groups["options"].Value,@"/@\d+","")+"/@2}");
            return Regex.Replace(text,@"(?<!\{)%("+fields+@")\b",m=>"%{%"+m.Groups[1].Value+"/NU/@2}");
        }
        internal static bool PortHasTwoPlaces(string text)
        {
            if(Regex.IsMatch(text,@"%Z[HT]\b|(?<!\{)%(HS|HD|BS|BD|SS|SA|TD)\b"))return false;
            return Regex.Matches(text,@"%\{%(?:HS|HD|BS|BD|SS|SA|TD)(?<options>[^{}]*)\}").Cast<Match>()
                .All(m=>Regex.IsMatch(m.Groups["options"].Value,@"/@2(?=/|$)"))&&
                !Regex.IsMatch(text,@"\d*\.\d{3,}\s+DP\.");
        }
        static string Number(double value){return Math.Round(value,8).ToString("0.########",CultureInfo.InvariantCulture);}
        internal static bool SavedDiameterLimits(string name,out double minimum,out double maximum)
        {
            minimum=maximum=0;
            Match limits=Regex.Match(ReadSaved(name),@"%DI\s*(?<upper>(?:\d*\.)?\d+)/(?<lower>(?:\d*\.)?\d+)");
            return limits.Success&&Double.TryParse(limits.Groups["upper"].Value,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out maximum)&&
                Double.TryParse(limits.Groups["lower"].Value,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out minimum)&&maximum>minimum;
        }
        static string ResolveSmartDepth(string text,HoleFacts hole,int precision=3)
        {
            // Hole-table overrides evaluate %HD, %BD, %HS and %BS directly.
            // %ZH is only expanded inside the native shared F definitions.
            // Use the associated depth field for blind holes; a through feature
            // has no %HD value and receives the same THRU wording as the native
            // evaluator. Subsequent blind/through type changes require a rescan.
            return text.Replace("%ZH",hole.Depth.HasValue?"%{%HD/NU/@"+precision+"} DP.":"THRU")
                .Replace("%ZT",hole.ThreadDepth.HasValue?"%{%TD/NU/@"+precision+"} DP.":"THRU");
        }
        internal static string ReadSaved(string name)
        {
            string text;if(saved.Value.TryGetValue(name,out text))return text;
            throw new InvalidDataException("Saved callout not found: "+name);
        }
        static Dictionary<string,string> LoadSaved()
        {
            var result=new Dictionary<string,string>(StringComparer.Ordinal);
            using(var stream=typeof(LinkedCallouts).Assembly.GetManifestResourceStream("HoleTableFormatter.DraftCallout.txt"))
            {
                if(stream==null)throw new InvalidDataException("The packaged DraftCallout.txt reference is missing.");
                using(var reader=new StreamReader(stream))
                {
                    string source=reader.ReadToEnd().Replace("\r","");
                    foreach(Match block in Regex.Matches(source,@"^<<\n(?<body>.*?)^>>",RegexOptions.Multiline|RegexOptions.Singleline))
                    {
                        string[] lines=block.Groups["body"].Value.Split('\n');
                        if(lines.Length==0)continue;
                        result.Add(lines[0].Trim(),String.Join("%RT",lines.Skip(1).TakeWhile(line=>!line.StartsWith("[",StringComparison.Ordinal)).Where(line=>!String.IsNullOrWhiteSpace(line)).Select(line=>line.Trim())));
                    }
                }
            }
            return result;
        }
    }
}
