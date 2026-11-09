using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using HoleTableFormatter;

internal static class LiveTest
{
    static void Require(bool condition,string message){if(!condition)throw new Exception(message);}
    static void SetExpression(object draft,RowPlan plan,string expression)
    {
        object tables=null,table=null,rows=null,cell=null;
        try
        {
            tables=((dynamic)draft).HoleTables2;table=((dynamic)tables).Item(plan.TableIndex);rows=((dynamic)table).Rows;
            int index=0;for(int i=1;i<=(int)((dynamic)rows).Count;i++)if(Automation.CellText(table,i,plan.LabelColumn)==plan.Label)index=i;
            Require(index>0,"Missing row in override regression.");cell=((dynamic)table).Cell(index,plan.ColumnIndex);
            ((dynamic)cell).AllowCellOverrides=true;((dynamic)cell).Value=expression;((dynamic)table).Update();
        }
        finally{Com.Release(cell);Com.Release(rows);Com.Release(table);Com.Release(tables);}
    }
    static Dictionary<string,string> Capture(object draft)
    {
        var values=new Dictionary<string,string>();object tables=null;
        try
        {
            tables=((dynamic)draft).HoleTables2;
            for(int ti=1;ti<=(int)((dynamic)tables).Count;ti++)
            {
                object table=null,rows=null,cols=null;
                try
                {
                    table=((dynamic)tables).Item(ti);rows=((dynamic)table).Rows;cols=((dynamic)table).Columns;
                    values[ti+"/TITLE"]=Automation.ReadTitle(table);
                    for(int c=1;c<=(int)((dynamic)cols).Count;c++)
                    {
                        object column=null;try{column=((dynamic)cols).Item(c);values[ti+"/COLUMN/"+c]=(string)((dynamic)column).PropertyText;}
                        finally{Com.Release(column);}
                    }
                    for(int r=1;r<=(int)((dynamic)rows).Count;r++)
                    {
                        string label=Automation.CellText(table,r,1);
                        for(int c=1;c<=(int)((dynamic)cols).Count;c++)
                        {
                            values[ti+"/"+label+"/"+c]=Automation.CellText(table,r,c);
                            object cell=null;try{cell=((dynamic)table).Cell(r,c);values[ti+"/"+label+"/"+c+"/override"]=((bool)((dynamic)cell).IsOverridden).ToString();}
                            finally{Com.Release(cell);}
                        }
                    }
                    for(int f=1;f<=4;f++)for(int type=1;type<=8;type++)values[ti+"/FORMAT/"+f+"/"+type]=Automation.GetTemplate(table,f,type);
                }
                finally{Com.Release(cols);Com.Release(rows);Com.Release(table);}
            }
            return values;
        }
        finally{Com.Release(tables);}
    }
    static void Same(Dictionary<string,string> before,Dictionary<string,string> after,string context,HashSet<string> allowed,bool refreshed=false)
    {
        Require(before.Count==after.Count,context+": data count changed.");
        foreach(var item in before)
        {
            if(allowed!=null && allowed.Contains(item.Key))continue;
            // Solid Edge 2026 collapses some doubled spaces in generated text
            // cached by the 2022 drawing, including padding before coordinates.
            // Native evaluation also normalizes padding around property fields
            // inside overridden notes. Permit whitespace-only changes in cell
            // display text; numeric characters, note words, labels, override flags
            // and raw format definitions remain exact. This does not authorize
            // writing to any preserved row or replacing a dynamic note with text.
            bool normalized=refreshed&&before.ContainsKey(item.Key+"/override")&&
                after.ContainsKey(item.Key)&&Regex.Replace(item.Value.Trim(),@"\s+"," ")==Regex.Replace(after[item.Key].Trim(),@"\s+"," ");
            Require(normalized||(after.ContainsKey(item.Key)&&after[item.Key]==item.Value),context+": changed "+item.Key+" from ["+item.Value+"] to ["+(after.ContainsKey(item.Key)?after[item.Key]:"MISSING")+"].");
        }
    }
    [STAThread] static int Main(string[] args)
    {
        string root=Path.GetFullPath(args[0]);Directory.CreateDirectory(root);
        var report=new StringBuilder();object app=null,docs=null,source=null,copy=null;
        try
        {
            using(var filter=new BusyFilter())
            {
                app=System.Runtime.InteropServices.Marshal.GetActiveObject("SolidEdge.Application");docs=((dynamic)app).Documents;
                source=args.Length>1?((dynamic)docs).Item(args[1]):((dynamic)app).ActiveDocument;
                var original=Capture(source);
                string copyPath=Path.Combine(root,"turbo-hole-table-test-"+Guid.NewGuid().ToString("N")+".dft");
                ((dynamic)source).SaveCopyAs(copyPath);copy=((dynamic)docs).Open(copyPath);
                var before=Capture(copy);
                ScanResult scan=Automation.Scan(copyPath,root);
                Require(scan.TableCount>0 && scan.Tables.Count==scan.TableCount,"Native table inventory is incomplete.");
                Require(scan.Rows.Any(r=>r.Eligible),"This test needs at least one proposed format change.");
                foreach(TableInfo info in scan.Tables)Require(before[info.Index+"/TITLE"]==info.TitleText,"Scan lost a native title.");
                Require(scan.Rows.All(r=>!r.Overridden||!r.Eligible||!String.IsNullOrEmpty(r.OriginalAssignment)),"An unverified custom override became eligible.");
                Same(before,Capture(copy),"Read-only scan",null);report.AppendLine("PASS: scan inspected "+scan.TableCount+" tables / "+scan.Rows.Count+" rows with native title text and no source changes.");Console.WriteLine("Scan preservation passed.");
                // Seed v0.7 dowel notes and v0.6 port expressions in this disposable copy. This guarantees
                // that migration and rollback are exercised even when the user's
                // current draft has no previously applied standard overrides.
                int seeded=0,seededDowelNotes=0;
                foreach(RowPlan row in scan.Rows.Where(r=>r.Eligible&&LinkedCallouts.RevisedFamily(r.Decision)))
                {
                    string previous=row.Decision.IsDowel?LinkedCallouts.PreviousDowelNote(row.Decision,row.Facts,row.Template):
                        LinkedCallouts.Previous(row.Decision,row.Facts,row.Template);
                    if(previous==null&&row.Decision.Family=="Koenig plug")previous="%F3";
                    if(previous==null)continue;SetExpression(copy,row,previous);seeded++;
                    if(row.Decision.IsDowel&&previous!=row.Assignment)seededDowelNotes++;
                }
                before=Capture(copy);scan=Automation.Scan(copyPath,root);Same(before,Capture(copy),"Prior-version scan preservation",null);
                Require(seeded==0||scan.Rows.Any(r=>r.Overridden&&r.Eligible),"Prior linked callouts were not offered for the requested update.");
                Require(scan.Rows.Count(r=>r.Overridden&&r.Eligible&&r.Decision.IsDowel)>=seededDowelNotes,"A prior dual-unit dowel note was not offered for its inch-only update.");
                report.AppendLine("Verified prior standard callouts eligible for update: "+scan.Rows.Count(r=>r.Overridden&&r.Eligible));
                report.AppendLine("Prior dual-unit dowel notes verified for inch-only updates: "+seededDowelNotes);
                scan.Save(Path.Combine(root,"validated-preview.xml"));

                // Force verification to fail after native assignments. Recovery
                // must restore dynamic defaults, not merely matching literal text.
                RowPlan first=scan.Rows.First(r=>r.Eligible);string expected=first.After;first.After="INTENTIONAL VERIFICATION FAILURE";
                bool failed=false;try{Automation.Apply(scan);}catch(InvalidOperationException ex){failed=ex.Message.Contains("No formatter changes retained.");}
                Require(failed,"The forced failure did not report a successful rollback.");
                first.After=expected;Same(before,Capture(copy),"Rollback",null);
                report.AppendLine("PASS: forced verification failure restored every original cell and definition.");
                Console.WriteLine("Rollback passed.");
                scan=Automation.Scan(copyPath,root);
                var changed=scan.Rows.Where(r=>r.Eligible).ToList();
                var allowed=new HashSet<string>(changed.Select(r=>r.TableIndex+"/"+r.Label+"/"+r.ColumnIndex));
                foreach(RowPlan row in changed)allowed.Add(row.TableIndex+"/"+row.Label+"/"+row.ColumnIndex+"/override");
                int applied=0;
                foreach(var group in changed.GroupBy(r=>r.TableIndex))
                {
                    var priorTableApply=Capture(copy);
                    var allowedInTable=new HashSet<string>(group.Select(r=>r.TableIndex+"/"+r.Label+"/"+r.ColumnIndex));
                    foreach(RowPlan row in group)allowedInTable.Add(row.TableIndex+"/"+row.Label+"/"+row.ColumnIndex+"/override");
                    applied+=Automation.Apply(scan,group.Key);
                    Same(priorTableApply,Capture(copy),"Apply only table "+group.Key,allowedInTable,true);
                }
                Require(applied==changed.Count,"Wrong applied count.");
                var after=Capture(copy);Same(before,after,"Apply",allowed,true);
                foreach(RowPlan row in changed)Require(after[row.TableIndex+"/"+row.Label+"/"+row.ColumnIndex]==row.After,"Applied text did not match preview.");
                ((dynamic)copy).Save();((dynamic)copy).Close(false);Com.Release(copy);copy=null;
                copy=((dynamic)docs).Open(copyPath);Same(after,Capture(copy),"Save/reopen",null);
                report.AppendLine("PASS: "+applied+" native format assignments applied one table at a time and survived save/reopen; other tables, titles, labels, numeric coordinates, custom rows and templates retained (native refresh may normalize spacing).");
                Console.WriteLine("Apply and save/reopen passed.");

                object tables=null,table=null,rows=null;
                try
                {
                    RowPlan row=changed.FirstOrDefault(r=>!r.SeparateLinked);
                    if(row!=null)
                    {
                    tables=((dynamic)copy).HoleTables2;table=((dynamic)tables).Item(row.TableIndex);rows=((dynamic)table).Rows;
                    int index=0;for(int i=1;i<=(int)((dynamic)rows).Count;i++)if(Automation.CellText(table,i,row.LabelColumn)==row.Label)index=i;
                    Require(index>0,"Cannot find saved test row.");
                    string template=Automation.GetTemplate(table,row.Decision.Slot,row.Facts.Type);
                    try
                    {
                        ((dynamic)table).SetHoleTableCalloutText(row.Decision.Slot,row.Facts.Type,"ASSOCIATION_CHECK%RT"+template);((dynamic)table).Update();
                        Require(Automation.CellText(table,index,row.ColumnIndex).StartsWith("ASSOCIATION_CHECK"),"The saved row contains literal text instead of a native %F reference.");
                    }
                    finally{((dynamic)table).SetHoleTableCalloutText(row.Decision.Slot,row.Facts.Type,template);((dynamic)table).Update();}
                    }
                }
                finally{Com.Release(rows);Com.Release(table);Com.Release(tables);}
                Same(after,Capture(copy),"Native linkage restoration",null);((dynamic)copy).Save();
                // A saved separate expression must still evaluate model fields,
                // rather than retaining the scan's rendered numbers. Changing
                // only this test copy's display units proves reevaluation without
                // touching the linked part/model or the user's active document.
                foreach(var group in changed.Where(r=>r.SeparateLinked).GroupBy(r=>r.TableIndex))
                {
                    object linkedTables=null,linkedTable=null,linkedRows=null;
                    try
                    {
                        linkedTables=((dynamic)copy).HoleTables2;linkedTable=((dynamic)linkedTables).Item(group.Key);linkedRows=((dynamic)linkedTable).Rows;
                        int units=(int)((dynamic)linkedTable).PrimaryLinearUnits;
                        try
                        {
                            ((dynamic)linkedTable).PrimaryLinearUnits=0;((dynamic)linkedTable).Update();
                            foreach(RowPlan linked in group)
                            {
                                int index=0;for(int i=1;i<=(int)((dynamic)linkedRows).Count;i++)if(Automation.CellText(linkedTable,i,linked.LabelColumn)==linked.Label)index=i;
                                Require(index>0,"Saved linked row disappeared.");
                                Require(Automation.CellText(linkedTable,index,linked.ColumnIndex)!=linked.After,"Separate row lost its native hole-field links: "+linked.Label);
                            }
                        }
                        finally{((dynamic)linkedTable).PrimaryLinearUnits=units;((dynamic)linkedTable).Update();}
                    }
                    finally{Com.Release(linkedRows);Com.Release(linkedTable);Com.Release(linkedTables);}
                }
                Same(after,Capture(copy),"Linked field unit restoration",null);((dynamic)copy).Save();
                var refreshed=Automation.Scan(copyPath,root);
                foreach(RowPlan row in changed.Where(r=>LinkedCallouts.RevisedFamily(r.Decision)))
                    Require(refreshed.Rows.Single(r=>r.TableIndex==row.TableIndex&&r.Label==row.Label).Status=="Already set","Updated standard callout was not retained as Already Set after refresh: "+row.Label);
                var protectedRows=changed.Where(r=>r.SeparateLinked&&LinkedCallouts.RevisedFamily(r.Decision)).Take(2).ToList();
                if(protectedRows.Count==2)
                {
                    try
                    {
                        SetExpression(copy,protectedRows[0],protectedRows[0].Assignment+"%RTCUSTOM NOTE TO PRESERVE");
                        SetExpression(copy,protectedRows[1],protectedRows[1].After);
                        var protectedScan=Automation.Scan(copyPath,root);
                        foreach(RowPlan row in protectedRows)
                        {
                            var plan=protectedScan.Rows.Single(r=>r.TableIndex==row.TableIndex&&r.Label==row.Label);
                            Require(!plan.Eligible&&plan.Status=="Custom row preserved","Custom note or literal lookalike was offered for replacement: "+row.Label);
                        }
                    }
                    finally{foreach(RowPlan row in protectedRows)SetExpression(copy,row,row.Assignment);}
                    Same(after,Capture(copy),"Custom-row test restoration",null);
                    report.AppendLine("PASS: refresh recognizes updated linked standards; custom notes and literal lookalikes stay preserved.");
                }
                Same(original,Capture(source),"Original drawing",null);
                report.AppendLine("PASS: saved F selections react to native definitions; "+changed.Count(r=>r.SeparateLinked)+" separate callouts reevaluate model fields after reopening; original drawing remained unchanged.");
                report.AppendLine("Test copy: "+copyPath);
                File.WriteAllText(Path.Combine(root,"live-validation.txt"),report.ToString());Console.WriteLine(report);return 0;
            }
        }
        catch(Exception error){report.AppendLine(error.ToString());File.WriteAllText(Path.Combine(root,"live-validation.txt"),report.ToString());Console.WriteLine(report);return 1;}
        finally
        {
            if(copy!=null){try{((dynamic)copy).Close(false);}finally{Com.Release(copy);}}
            if(source!=null){try{((dynamic)source).Activate();}finally{Com.Release(source);}}
            Com.Release(docs);Com.Release(app);
        }
    }
}
