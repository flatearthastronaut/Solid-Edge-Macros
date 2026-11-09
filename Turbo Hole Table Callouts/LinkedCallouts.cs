using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HoleTableFormatter
{
    internal static class LinkedCallouts
    {
        static readonly Lazy<string> six=new Lazy<string>(()=>ReadSaved("SAE Port #6"));
        internal static string For(Decision choice,HoleFacts hole,string sharedTemplate)
        {
            if(choice.Port==6 && (hole.Type==2||hole.Type==5))
            {
                // Greg approved a separate native linked expression for #6 when
                // F4's shared counterbore text describes #4. Never change F4.
                string text=six.Value;
                if(!text.Contains("#6")||!text.Contains("9/16-18"))return null;
                text=Regex.Replace(text,@"%(HS|BD)\b",m=>"%{%"+m.Groups[1].Value+"/NU/@3}");
                return ResolveSmartDepth(text,hole);
            }
            if(choice.Port==8&&(hole.Type==2||hole.Type==5)&&choice.ThreadDepth>0)
            {
                string text="%DI%{%HS/NU/@3} DRILL %ZH%RT#8 STRAIGHT THR'D O-RING PORT%RT"+
                    "%DI%{%BS/NU/@3} SPOTFACE %{%BD/NU/@3} DP.%RT3/4-16 THR'D "+
                    choice.ThreadDepth.Value.ToString("0.000",CultureInfo.InvariantCulture)+" DP.";
                return ResolveSmartDepth(text,hole);
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
                if(RuleBook.TemplateProblem(choice,hole,text)!=null)return null;
                return ResolveSmartDepth(text,hole);
            }
            return null;
        }
        static string Number(double value){return Math.Round(value,8).ToString("0.########",CultureInfo.InvariantCulture);}
        internal static bool SavedDiameterLimits(string name,out double minimum,out double maximum)
        {
            minimum=maximum=0;
            Match limits=Regex.Match(ReadSaved(name),@"%DI\s*(?<upper>(?:\d*\.)?\d+)/(?<lower>(?:\d*\.)?\d+)");
            return limits.Success&&Double.TryParse(limits.Groups["upper"].Value,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out maximum)&&
                Double.TryParse(limits.Groups["lower"].Value,NumberStyles.AllowDecimalPoint,CultureInfo.InvariantCulture,out minimum)&&maximum>minimum;
        }
        static string ResolveSmartDepth(string text,HoleFacts hole)
        {
            // Hole-table overrides evaluate %HD, %BD, %HS and %BS directly.
            // %ZH is only expanded inside the native shared F definitions.
            // Use the associated depth field for blind holes; a through feature
            // has no %HD value and receives the same THRU wording as the native
            // evaluator. Subsequent blind/through type changes require a rescan.
            return text.Replace("%ZH",hole.Depth.HasValue?"%{%HD/NU/@3} DP.":"THRU");
        }
        internal static string ReadSaved(string name)
        {
            using(var stream=typeof(LinkedCallouts).Assembly.GetManifestResourceStream("HoleTableFormatter.DraftCallout.txt"))
            {
                if(stream==null)throw new InvalidDataException("The packaged DraftCallout.txt reference is missing.");
                using(var reader=new StreamReader(stream))
                {
                    string source=reader.ReadToEnd().Replace("\r","");
                    foreach(Match block in Regex.Matches(source,@"^<<\n(?<body>.*?)^>>",RegexOptions.Multiline|RegexOptions.Singleline))
                    {
                        string[] lines=block.Groups["body"].Value.Split('\n');
                        if(lines.Length==0||lines[0].Trim()!=name)continue;
                        return String.Join("%RT",lines.Skip(1).TakeWhile(line=>!line.StartsWith("[",StringComparison.Ordinal)).Where(line=>!String.IsNullOrWhiteSpace(line)).Select(line=>line.Trim()));
                    }
                }
            }
            throw new InvalidDataException("Saved callout not found: "+name);
        }
    }
}
