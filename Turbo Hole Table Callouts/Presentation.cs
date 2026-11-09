using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

namespace HoleTableFormatter
{
    internal static class Presentation
    {
        internal static readonly Color SetColor=Color.FromArgb(202,239,206);
        internal static readonly Color ReadyColor=Color.FromArgb(255,239,156);
        internal static readonly Color ReviewColor=Color.FromArgb(255,199,206);
        internal const string MixedTypeWarning="Multiple hole types found with the same selected hole size";
        internal static string Display(string text)
        {
            // SE returns rendered stacked limits inside formatting markup.
            // Show the two evaluated values without exposing that markup in UI.
            text=Regex.Replace(text??"",@"%\{(?<upper>[+\-]?(?:\d*\.)?\d+)\^(?<lower>[+\-]?(?:\d*\.)?\d+)/LT[^{}]*\}","${upper}/${lower}");
            return text.Replace("%RT",Environment.NewLine).Replace("%DI","\u00D8").Replace("%PM","\u00B1").Replace("%DG","\u00B0");
        }
        internal static string Title(TableInfo table){return String.IsNullOrWhiteSpace(table.TitleText)?"Untitled hole table":Display(table.TitleText);}
        internal static string Caption(TableInfo table){return Regex.Replace(Title(table),@"\s+"," ").Trim()+" (Table "+table.Index+")";}
        internal static Color RowColor(string status)
        {
            if(String.Equals(status,"Already set",StringComparison.OrdinalIgnoreCase)||status=="Applied")return SetColor;
            if(status=="Ready")return ReadyColor;
            return status!=null&&status.StartsWith("Review",StringComparison.Ordinal)?ReviewColor:Color.White;
        }
        internal static Color RowColor(RowPlan plan)
        {return plan.Decision!=null&&plan.Decision.Slot==0?ReviewColor:RowColor(plan.Status);}
        internal static string FormatLabel(RowPlan plan)
        {
            if(plan.SeparateLinked)
            {
                if(plan.Decision!=null&&plan.Decision.IsDowel)return "Linked F2 fit";
                if(plan.Decision!=null&&plan.Decision.Family=="Spring pin")return "Linked spring pin";
                return plan.Decision!=null&&plan.Decision.Port>0?"Linked #"+plan.Decision.Port:"Linked callout";
            }
            return plan.Decision==null||plan.Decision.Slot==0?"":"%F"+plan.Decision.Slot;
        }
        internal static string HoleLetter(string label)
        {return Regex.Match((label??"").Trim(),@"^[A-Za-z]+").Value.ToUpperInvariant();}
        internal static HashSet<string> MixedHoleGroups(IEnumerable<RowPlan> rows)
        {
            // Group within one table only. Status and selection are deliberately
            // excluded: applying or unchecking a row does not change hole type.
            return new HashSet<string>(rows.GroupBy(r=>HoleLetter(r.Label)).Where(g=>g.Key.Length>0&&
                g.Select(HoleTypeKey).Distinct(StringComparer.Ordinal).Skip(1).Any()).Select(g=>g.Key),StringComparer.Ordinal);
        }
        static string HoleTypeKey(RowPlan row)
        {
            string native=row.Facts==null?"unknown":row.Facts.Type.ToString();
            Decision choice=row.Decision;
            if(choice!=null&&choice.Slot>0)
                return (choice.IsDowel?"Dowel":choice.Family??row.Category??"Unknown")+":"+choice.Port+":"+choice.Fit+":"+native;
            return "Unmatched:"+native+":"+(row.Facts==null?"":Regex.Replace(row.Facts.Thread??"",@"\s+","").ToUpperInvariant());
        }
        internal static IEnumerable<RowPlan> OrderRows(IEnumerable<RowPlan> rows){return rows.OrderBy(r=>r.Label,HoleLabelComparer.Instance).ThenBy(r=>r.RowIndex);}
    }

    // Compare digit runs by magnitude without integer overflow. Hole labels are
    // identifiers: A.2 precedes A.10. This only sorts the preview, never SE rows.
    internal sealed class HoleLabelComparer:IComparer<string>
    {
        internal static readonly HoleLabelComparer Instance=new HoleLabelComparer();
        public int Compare(string left,string right)
        {
            left=left??"";right=right??"";int a=0,b=0;
            while(a<left.Length&&b<right.Length)
            {
                if(Digit(left[a])&&Digit(right[b]))
                {
                    int ae=a,be=b;while(ae<left.Length&&Digit(left[ae]))ae++;while(be<right.Length&&Digit(right[be]))be++;
                    int az=a,bz=b;while(az<ae&&left[az]=='0')az++;while(bz<be&&right[bz]=='0')bz++;
                    int length=(ae-az).CompareTo(be-bz);if(length!=0)return length;
                    int number=String.Compare(left,az,right,bz,ae-az,StringComparison.Ordinal);if(number!=0)return number;
                    a=ae;b=be;
                }
                else{int order=Char.ToUpperInvariant(left[a]).CompareTo(Char.ToUpperInvariant(right[b]));if(order!=0)return order;a++;b++;}
            }
            int remaining=(left.Length-a).CompareTo(right.Length-b);
            return remaining!=0?remaining:StringComparer.OrdinalIgnoreCase.Compare(left,right);
        }
        static bool Digit(char value){return value>='0'&&value<='9';}
    }
}
