using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace HoleTableFormatter
{
    internal sealed class RowPlan
    {
        internal int TableIndex, RowIndex, ColumnIndex, LabelColumn, DefaultSlot, RowCount;
        internal string TableName, Label, Before, After, ColumnExpression, Category, Reason, Status;
        internal bool Overridden, AllowOverrides, Eligible, Selected;
        internal HoleFacts Facts;
        internal Decision Decision;
        internal string Template;
        internal string Assignment;
        internal bool SeparateLinked;
    }
    internal sealed class ScanResult
    {
        internal string Document, SettingsPath;
        internal int TableCount;
        internal readonly List<TableInfo> Tables = new List<TableInfo>();
        internal readonly List<RowPlan> Rows = new List<RowPlan>();
        internal readonly List<string> Notes = new List<string>();
        internal void Save(string path)
        {
            var xml = new XElement("HoleTableScan", new XAttribute("document",Document), new XAttribute("settings",SettingsPath),
                new XAttribute("tables",TableCount), new XAttribute("eligible",Rows.Count(r=>r.Eligible)),
                Tables.Select(t=>new XElement("Table",new XAttribute("index",t.Index),new XElement("Title",t.TitleText))),
                Notes.Select(n=>new XElement("Note",n)), Rows.Select(r=>new XElement("Row",
                    new XAttribute("table",r.TableIndex),new XAttribute("index",r.RowIndex),new XAttribute("label",r.Label??""),
                    new XAttribute("status",r.Status??""),new XAttribute("category",r.Category??""),new XAttribute("slot",r.Decision==null?0:r.Decision.Slot),
                    new XAttribute("family",r.Decision==null?"":r.Decision.Family??""),new XAttribute("fit",r.Decision==null?"":r.Decision.Fit??""),
                    new XAttribute("port",r.Decision==null?0:r.Decision.Port),
                    new XAttribute("separateLinked",r.SeparateLinked),new XElement("Assignment",r.Assignment??""),
                    new XElement("Reason",r.Reason??""),new XElement("Before",r.Before??""),new XElement("After",r.After??""),
                    r.Facts==null?null:new XElement("Facts",new XAttribute("type",r.Facts.Type),new XAttribute("thread",r.Facts.Thread),
                        Field("Diameter",r.Facts.Diameter),Field("Depth",r.Facts.Depth),Field("Bore",r.Facts.Bore),Field("BoreDepth",r.Facts.BoreDepth)))));
            new XDocument(xml).Save(path);
        }
        static object Field(string name,double? value) {return value.HasValue?new XAttribute(name,value.Value.ToString("R",CultureInfo.InvariantCulture)):null;}
    }

    internal sealed class TableInfo
    {
        internal int Index;
        internal string TitleText;
    }

    internal static class Automation
    {
        // The newer HoleTable2.GetHoles call fails on the supplied legacy draft.
        // Its native callout evaluator does expose the same associated values.
        // Evaluate them in a SaveCopyAs document, never by adding diagnostic
        // columns or overwriting callout definitions in the user's drawing.
        const string Probe = "%{%HS/NU/@8}|%{%HD/NU/@8}|%{%BS/NU/@8}|%{%BD/NU/@8}|%{%SS/NU/@8}|%{%SA/NU/@8}|%TS";
        internal static ScanResult Scan(string documentPath, string scratchFolder)
        {
            object app=null,docs=null,source=null,copy=null,sourceTables=null,copyTables=null;
            string copyPath=null;
            var result=new ScanResult();
            try
            {
                app=System.Runtime.InteropServices.Marshal.GetActiveObject("SolidEdge.Application"); docs=((dynamic)app).Documents;
                source=String.IsNullOrEmpty(documentPath)?((dynamic)app).ActiveDocument:((dynamic)docs).Item(documentPath);
                result.Document=(string)((dynamic)source).FullName;
                if(!String.Equals(Path.GetExtension(result.Document),".dft",StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("Open a saved Solid Edge draft before running this macro.");
                object settings;
                ((dynamic)app).GetGlobalParameter(63,out settings); result.SettingsPath=Convert.ToString(settings);
                if(String.IsNullOrWhiteSpace(result.SettingsPath))throw new InvalidOperationException("Solid Edge has no Custom Settings File configured.");
                RuleBook rules=RuleBook.Load(result.SettingsPath);
                sourceTables=((dynamic)source).HoleTables2; result.TableCount=(int)((dynamic)sourceTables).Count;
                if(result.TableCount==0)throw new InvalidOperationException("This draft has no supported native HoleTables2 tables.");
                Directory.CreateDirectory(scratchFolder);
                copyPath=Path.Combine(Path.GetFullPath(scratchFolder),"hole-table-scan-"+Guid.NewGuid().ToString("N")+".dft");
                ((dynamic)source).SaveCopyAs(copyPath); copy=((dynamic)docs).Open(copyPath); copyTables=((dynamic)copy).HoleTables2;
                if((int)((dynamic)copyTables).Count!=result.TableCount)throw new InvalidOperationException("The temporary copy has different hole tables.");
                for(int ti=1;ti<=result.TableCount;ti++)
                {
                    object original=null,probeTable=null;
                    try
                    {
                        original=((dynamic)sourceTables).Item(ti); probeTable=((dynamic)copyTables).Item(ti);
                        result.Tables.Add(new TableInfo{Index=ti,TitleText=ReadTitle(original)});
                        InspectTable(original,probeTable,ti,rules,result);
                    }
                    finally {Com.Release(probeTable);Com.Release(original);}
                }
                return result;
            }
            finally
            {
                Com.Release(copyTables);Com.Release(sourceTables);
                try {if(copy!=null)((dynamic)copy).Close(false);}
                finally
                {
                    Com.Release(copy);
                    try {if(source!=null)((dynamic)source).Activate();}
                    finally {Com.Release(source);Com.Release(docs);Com.Release(app);}
                }
                // Only the unique single file generated above is removed. No
                // recursive cleanup or path based on document/model names.
                if(copyPath!=null && File.Exists(copyPath))File.Delete(copyPath);
            }
        }

        // Name is Solid Edge's internal table identifier, not the Title text
        // shown in Properties. Read every native title line, retaining order.
        // Acquire/release each COM collection and item once, like the SDK samples.
        internal static string ReadTitle(object table)
        {
            object titles=null;
            var lines=new List<string>();
            try
            {
                titles=((dynamic)table).Titles;
                for(int i=1;i<=(int)((dynamic)titles).Count;i++)
                {
                    object title=null;
                    try
                    {
                        title=((dynamic)titles).Item(i);
                        string value=Convert.ToString(((dynamic)title).Value);
                        if(!String.IsNullOrWhiteSpace(value))lines.Add(value.Trim());
                    }
                    finally{Com.Release(title);}
                }
                return String.Join(Environment.NewLine,lines);
            }
            finally{Com.Release(titles);}
        }

        static void InspectTable(object original,object probeTable,int ti,RuleBook rules,ScanResult result)
        {
            object columns=null,rows=null;
            try
            {
                columns=((dynamic)original).Columns; rows=((dynamic)original).Rows;
                if((int)((dynamic)original).PrimaryLinearUnits!=1)
                {result.Notes.Add("Table "+ti+" uses metric primary units; this version leaves it unchanged for review.");return;}
                int count=(int)((dynamic)rows).Count, callout=0, label=0, slot=0;
                string expression="";
                for(int ci=1;ci<=(int)((dynamic)columns).Count;ci++)
                {
                    object column=null;
                    try
                    {
                        column=((dynamic)columns).Item(ci); string p=((string)((dynamic)column).PropertyText??"").Trim();
                        if(String.Equals(p,"%HN",StringComparison.OrdinalIgnoreCase))label=ci;
                        if(Regex.IsMatch(p,@"^%F[1-4]$",RegexOptions.IgnoreCase))
                        {
                            if(callout!=0){result.Notes.Add("Table "+ti+" has multiple callout columns; left unchanged.");return;}
                            callout=ci;expression=p;slot=Int32.Parse(p.Substring(2));
                        }
                    }
                    finally {Com.Release(column);}
                }
                if(callout==0 || label==0){result.Notes.Add("Table "+ti+" has no single %F callout column and hole-label column; left unchanged.");return;}
                var plans=new List<RowPlan>();
                string name=(string)((dynamic)original).Name;
                // Capture production text and override flags BEFORE the scratch
                // copy's definitions are repurposed. Custom rows are never reset.
                for(int ri=1;ri<=count;ri++)
                {
                    object cell=null;
                    try
                    {
                        cell=((dynamic)original).Cell(ri,callout);
                        plans.Add(new RowPlan{TableIndex=ti,TableName=name,RowIndex=ri,ColumnIndex=callout,LabelColumn=label,ColumnExpression=expression,
                            RowCount=count,DefaultSlot=slot,Label=CellText(original,ri,label),Before=(string)((dynamic)cell).Value,
                            Overridden=(bool)((dynamic)cell).IsOverridden,AllowOverrides=(bool)((dynamic)cell).AllowCellOverrides});
                    }
                    finally {Com.Release(cell);}
                }
                if(plans.Any(p=>String.IsNullOrWhiteSpace(p.Label)) || plans.Select(p=>p.Label).Distinct().Count()!=plans.Count)
                {result.Notes.Add("Table "+ti+" has blank or duplicate hole labels; left unchanged.");return;}
                ((dynamic)probeTable).RenumberOnUpdate=false;
                int[] slotColumns=new int[5];
                for(int s=1;s<=4;s++)slotColumns[s]=AddColumn(probeTable,"Format "+s,"%F"+s);
                int typeColumn=AddColumn(probeTable,"Native type","%HT");
                ((dynamic)probeTable).Update();
                Dictionary<string,int> probeIndices=LabelIndices(probeTable,label,count);
                if(plans.Any(p=>!probeIndices.ContainsKey(p.Label)))throw new InvalidOperationException("The scratch table changed hole identities.");
                var text=new Dictionary<int,string[]>();
                foreach(RowPlan plan in plans)
                {
                    int probeIndex=probeIndices[plan.Label];
                    text[plan.RowIndex]=new string[]{"",CellText(probeTable,probeIndex,slotColumns[1]),CellText(probeTable,probeIndex,slotColumns[2]),
                        CellText(probeTable,probeIndex,slotColumns[3]),CellText(probeTable,probeIndex,slotColumns[4])};
                }
                // Inch output is explicit in the throwaway copy; the original
                // drawing's units, rounding, tolerances and definitions stay intact.
                ((dynamic)probeTable).PrimaryLinearUnits=1;
                for(int type=1;type<=8;type++)((dynamic)probeTable).SetHoleTableCalloutText(4,type,Probe);
                ((dynamic)probeTable).Update();
                probeIndices=LabelIndices(probeTable,label,count);
                // Capture facts before previewing any per-row expression: an
                // update can reorder scratch rows, but hole labels remain stable.
                var facts=plans.ToDictionary(p=>p.Label,p=>new[]{CellText(probeTable,probeIndices[p.Label],typeColumn),CellText(probeTable,probeIndices[p.Label],slotColumns[4])});
                foreach(RowPlan plan in plans)
                {
                    try
                    {
                        plan.Facts=HoleFacts.Parse(facts[plan.Label][0],facts[plan.Label][1]);
                        plan.Decision=rules.Match(plan.Facts);plan.Category=plan.Decision.Category;plan.Reason=plan.Decision.Reason;
                        if(plan.Decision.Slot>0)
                        {
                            plan.Template=GetTemplate(original,plan.Decision.Slot,plan.Facts.Type);
                            plan.Assignment="%F"+plan.Decision.Slot;
                            plan.After=text[plan.RowIndex][plan.Decision.Slot];
                        }
                        if(plan.Overridden){plan.Status="Custom row preserved";plan.Reason+=" Existing override/notes retained.";}
                        else if(plan.Decision.Slot==0)plan.Status="Review";
                        else
                        {
                            string issue=RuleBook.TemplateProblem(plan.Decision,plan.Facts,plan.Template);
                            if(issue!=null)
                            {
                                string linked=LinkedCallouts.For(plan.Decision,plan.Facts,plan.Template);
                                if(linked!=null)
                                {
                                    plan.Assignment=linked;plan.SeparateLinked=true;
                                    plan.After=EvaluateLinked(probeTable,plan);
                                    issue=null;plan.Reason+=" Separate model-linked callout; shared format stays unchanged.";
                                }
                            }
                            if(issue!=null || String.IsNullOrWhiteSpace(plan.After)){plan.Status="Review format";plan.Reason=issue??"Target evaluates to empty text.";plan.After="";}
                            else if(!plan.SeparateLinked&&plan.Decision.Slot==plan.DefaultSlot)plan.Status="Already set";
                            else{plan.Status="Hole type found";plan.Eligible=true;plan.Selected=true;}
                        }
                    }
                    catch(Exception ex){plan.Status="Review";plan.Category="Unreadable";plan.Reason=ex.Message;}
                    result.Rows.Add(plan);
                }
            }
            finally {Com.Release(rows);Com.Release(columns);}
        }

        static string EvaluateLinked(object table,RowPlan plan)
        {
            object cell=null;
            try
            {
                cell=((dynamic)table).Cell(FindLabel(table,plan),plan.ColumnIndex);
                ((dynamic)cell).AllowCellOverrides=true;((dynamic)cell).Value=plan.Assignment;
                ((dynamic)table).Update();
                string text=CellText(table,FindLabel(table,plan),plan.ColumnIndex);
                if(Regex.IsMatch(text,@"%(HS|HD|BS|BD|ZH)\b"))throw new InvalidOperationException("Linked callout fields could not be evaluated.");
                return text;
            }
            finally{Com.Release(cell);}
        }

        static int AddColumn(object table,string header,string expression)
        {
            object columns=null,column=null;
            try
            {
                columns=((dynamic)table).Columns;column=((dynamic)columns).Add((int)((dynamic)columns).Count,false);
                ((dynamic)column).Header=header;((dynamic)column).PropertyText=expression;
                return (int)((dynamic)column).Index;
            }
            finally {Com.Release(column);Com.Release(columns);}
        }
        internal static string CellText(object table,int row,int column)
        {
            object cell=null;
            try{cell=((dynamic)table).Cell(row,column);return (string)((dynamic)cell).Value??"";}
            finally{Com.Release(cell);}
        }
        internal static string GetTemplate(object table,int slot,int type)
        {string value;((dynamic)table).GetHoleTableCalloutText(slot,type,out value);return value??"";}

        internal static int Apply(ScanResult scan,int? tableIndex=null)
        {
            // Each window applies only its own table. Other windows can retain
            // checked rows without accidentally including them in this operation.
            List<RowPlan> selected=SelectedRows(scan,tableIndex);
            if(selected.Count==0)return 0;
            object app=null,docs=null,draft=null,tables=null;
            var attempted=new List<RowPlan>();
            try
            {
                app=System.Runtime.InteropServices.Marshal.GetActiveObject("SolidEdge.Application");docs=((dynamic)app).Documents;
                draft=((dynamic)docs).Item(scan.Document);tables=((dynamic)draft).HoleTables2;
                if((int)((dynamic)tables).Count!=scan.TableCount)throw new InvalidOperationException("Tables changed since scanning. Scan again.");
                // Complete every preflight check before the first setter. Existing
                // user edits and changed templates invalidate the whole selection.
                foreach(RowPlan plan in selected)Validate(tables,plan);
                foreach(var group in selected.GroupBy(r=>r.TableIndex))
                {
                    object table=null;
                    try
                    {
                        table=((dynamic)tables).Item(group.Key);
                        foreach(RowPlan plan in group)
                        {
                            object cell=null;
                            try
                            {
                                cell=((dynamic)table).Cell(plan.RowIndex,plan.ColumnIndex);attempted.Add(plan);
                                ((dynamic)cell).AllowCellOverrides=true;((dynamic)cell).Value=plan.Assignment;
                            }
                            finally{Com.Release(cell);}
                        }
                        ((dynamic)table).Update();
                        foreach(RowPlan plan in group)
                            if(CellText(table,FindLabel(table,plan),plan.ColumnIndex)!=plan.After)
                                throw new InvalidOperationException("Solid Edge did not retain the previewed callout for "+plan.Label+".");
                    }
                    finally{Com.Release(table);}
                }
                foreach(RowPlan plan in selected){plan.Status="Applied";plan.Eligible=false;plan.Selected=false;}
                // Leave the document open and unsaved for the normal drawing review.
                return selected.Count;
            }
            catch(Exception error)
            {
                var failures=new List<string>();
                foreach(var group in attempted.GroupBy(r=>r.TableIndex))
                {
                    object table=null;
                    try
                    {
                        table=((dynamic)tables).Item(group.Key);
                        foreach(RowPlan plan in group)
                        {
                            object cell=null;
                            try
                            {
                                int restoredIndex=FindLabel(table,plan);
                                cell=((dynamic)table).Cell(restoredIndex,plan.ColumnIndex);
                                // All eligible cells were unoverridden. Reset to
                                // their column expression; never restore evaluated
                                // literal text and thereby lose the model link.
                                ((dynamic)cell).AllowCellOverrides=false;
                                if(plan.AllowOverrides)((dynamic)cell).AllowCellOverrides=true;
                            }
                            finally{Com.Release(cell);}
                        }
                        ((dynamic)table).Update();
                        foreach(RowPlan plan in group)
                            if(CellText(table,FindLabel(table,plan),plan.ColumnIndex)!=plan.Before)failures.Add(plan.Label);
                    }
                    catch(Exception recovery){failures.Add(recovery.Message);}
                    finally{Com.Release(table);}
                }
                throw new InvalidOperationException(error.Message+(failures.Count==0?" No formatter changes retained.":" Check partial changes: "+String.Join(", ",failures)),error);
            }
            finally{Com.Release(tables);Com.Release(draft);Com.Release(docs);Com.Release(app);}
        }
        internal static List<RowPlan> SelectedRows(ScanResult scan,int? tableIndex)
        {return scan.Rows.Where(r=>r.Eligible&&r.Selected&&(!tableIndex.HasValue||r.TableIndex==tableIndex.Value)).ToList();}
        static void Validate(object tables,RowPlan plan)
        {
            object table=null,rows=null,columns=null,column=null,cell=null;
            try
            {
                table=((dynamic)tables).Item(plan.TableIndex);rows=((dynamic)table).Rows;columns=((dynamic)table).Columns;
                column=((dynamic)columns).Item(plan.ColumnIndex);cell=((dynamic)table).Cell(plan.RowIndex,plan.ColumnIndex);
                if((string)((dynamic)table).Name!=plan.TableName || (int)((dynamic)rows).Count!=plan.RowCount ||
                    (string)((dynamic)column).PropertyText!=plan.ColumnExpression || (bool)((dynamic)cell).IsOverridden ||
                    (string)((dynamic)cell).Value!=plan.Before || CellText(table,plan.RowIndex,plan.LabelColumn)!=plan.Label ||
                    GetTemplate(table,plan.Decision.Slot,plan.Facts.Type)!=plan.Template)
                    throw new InvalidOperationException("Table "+plan.TableIndex+", "+plan.Label+" changed since scanning. Scan again.");
            }
            finally{Com.Release(cell);Com.Release(column);Com.Release(columns);Com.Release(rows);Com.Release(table);}
        }
        static int FindLabel(object table,RowPlan plan)
        {
            object rows=null;
            try
            {
                rows=((dynamic)table).Rows;int found=0;
                for(int i=1;i<=(int)((dynamic)rows).Count;i++)if(CellText(table,i,plan.LabelColumn)==plan.Label)
                {if(found!=0)throw new InvalidOperationException("Duplicate hole label during recovery: "+plan.Label);found=i;}
                if(found==0)throw new InvalidOperationException("Missing hole label during recovery: "+plan.Label);
                return found;
            }
            finally{Com.Release(rows);}
        }
        static Dictionary<string,int> LabelIndices(object table,int column,int expectedCount)
        {
            object rows=null;
            try
            {
                rows=((dynamic)table).Rows;
                if((int)((dynamic)rows).Count!=expectedCount)throw new InvalidOperationException("The native table changed its hole count during inspection.");
                var indices=new Dictionary<string,int>(StringComparer.Ordinal);
                for(int i=1;i<=expectedCount;i++)
                {
                    string label=CellText(table,i,column);
                    if(String.IsNullOrWhiteSpace(label)||indices.ContainsKey(label))throw new InvalidOperationException("Ambiguous hole labels after native table refresh.");
                    indices.Add(label,i);
                }
                return indices;
            }
            finally{Com.Release(rows);}
        }
    }
}
