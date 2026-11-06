using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Hammill.Automation;

namespace Hammill.BatchApprovalExport
{
    internal sealed class ExportJob
    {
        public object Source;
        public string SourcePath,Destination,Label;
        public bool ThreeDimensionalPdf;
        public override string ToString(){return Label+": "+Destination+(File.Exists(Destination)?" [exists: skip]":"");}
    }
    internal interface IExporter
    {
        void SaveCopy(object source,string path);
        object Open(string path);
        void Export(object document,string path,bool pdf3D);
        void Close(object document);
    }
    internal sealed class SolidEdgeExporter:IExporter
    {
        private readonly object application;
        public SolidEdgeExporter(object app){application=app;}
        public void SaveCopy(object source,string path){((dynamic)source).SaveCopyAs(path);}
        public object Open(string path)
        {
            using(var scope=new ComScope())
            {dynamic documents=scope.Own(((dynamic)application).Documents);return documents.Open(path);}
        }
        public void Export(object document,string path,bool pdf3D)
        {
            dynamic doc=document;doc.Activate();
            object format=Path.GetExtension(path).Equals(".pdf",StringComparison.OrdinalIgnoreCase)?(object)pdf3D:Missing.Value;
            doc.SaveAs(path,Missing.Value,format,Missing.Value,Missing.Value,Missing.Value,Missing.Value,Missing.Value,Missing.Value);
        }
        public void Close(object document)
        {try{((dynamic)document).Close(false,Missing.Value,false);}finally{ComScope.Release(document);}}
    }
    internal static class ExportEngine
    {
        public static List<ExportJob> Plan(object draft,string draftPath,object assembly,string assemblyPath,DateTime date,string output,bool dwg,bool pdf,bool step,bool pdf3D)
        {
            var jobs=new List<ExportJob>();
            if(dwg || pdf)ValidateSource(draft,draftPath,".dft");
            if(step || pdf3D)ValidateSource(assembly,assemblyPath,".asm");
            if(output != null && !Directory.Exists(output))throw new DirectoryNotFoundException("Select an existing output folder.");
            Action<object,string,string,string,bool> add=(source,path,extension,label,threeD)=>{
                string stem=Path.GetFileNameWithoutExtension(path)+" "+date.ToString("yyyyMMdd")+(threeD?" 3D":"");
                jobs.Add(new ExportJob{Source=source,SourcePath=path,Destination=Path.Combine(output??Path.GetDirectoryName(path),stem+extension),Label=label,ThreeDimensionalPdf=threeD});
            };
            if(dwg)add(draft,draftPath,".dwg","2D DWG",false);
            if(pdf)add(draft,draftPath,".pdf","2D PDF",false);
            if(step)add(assembly,assemblyPath,".stp","3D STEP",false);
            // A draft and its assembly commonly share their basename. An explicit
            // 3D suffix prevents the assembly PDF overwriting the drawing PDF.
            if(pdf3D)add(assembly,assemblyPath,".pdf","3D PDF",true);
            if(jobs.Count==0)throw new InvalidOperationException("Select at least one export format.");
            return jobs;
        }
        private static void ValidateSource(object document,string path,string extension)
        {
            if(document==null || String.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path) || !Path.GetExtension(path).Equals(extension,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("A saved "+extension+" source document is required.");
        }
        public static List<string> Run(IList<ExportJob> jobs,IExporter exporter,Action<string> progress)
        {
            var results=new List<string>();
            foreach(var job in jobs)
            {
                progress(job.Label+" — "+job.Destination);
                if(File.Exists(job.Destination)){results.Add("Skipped existing file: "+job.Destination);continue;}
                string folder=Path.GetFullPath(Path.GetDirectoryName(job.Destination));
                string work=Path.Combine(folder,".hammill-export-"+Guid.NewGuid().ToString("N"));
                object copy=null;bool closed=true;
                try
                {
                    Directory.CreateDirectory(work);
                    string native=Path.Combine(work,"Source"+Path.GetExtension(job.SourcePath));
                    string staged=Path.Combine(work,Path.GetFileName(job.Destination));
                    // Export disposable native copies, never SaveAs the user's
                    // actual open drawing/assembly. No approval/status property
                    // is changed: the legacy tool is an export workflow only.
                    exporter.SaveCopy(job.Source,native);copy=exporter.Open(native);closed=false;
                    exporter.Export(copy,staged,job.ThreeDimensionalPdf);
                    if(!File.Exists(staged) || new FileInfo(staged).Length==0)throw new IOException("Solid Edge did not produce a nonempty export file.");
                    object toClose=copy;copy=null;exporter.Close(toClose);closed=true;
                    // Move without overwrite also protects a destination created
                    // by another process since the preview/existence check.
                    File.Move(staged,job.Destination);
                    results.Add("Created: "+job.Destination);
                }
                catch(Exception error){results.Add("Failed "+job.Label+": "+error.Message);}
                finally
                {
                    if(copy != null)
                    {try{exporter.Close(copy);closed=true;}catch(Exception error){results.Add("Could not close export copy: "+error.Message);}}
                    if(closed && Directory.Exists(work))
                    {
                        try
                        {
                            // Delete only our fresh, uniquely named workspace;
                            // never recurse into user-created/link directories.
                            string full=Path.GetFullPath(work);
                            if(!full.StartsWith(folder.TrimEnd('\\')+"\\.hammill-export-",StringComparison.OrdinalIgnoreCase))throw new IOException("Unexpected export workspace path.");
                            foreach(string file in Directory.GetFiles(full))
                                if((File.GetAttributes(file)&FileAttributes.ReparsePoint)==0)File.Delete(file);
                            Directory.Delete(full,false);
                        }
                        catch(Exception error){results.Add("Temporary export files retained in "+work+": "+error.Message);}
                    }
                    else if(Directory.Exists(work))results.Add("Export copy retained in "+work+"; close it in Solid Edge before removing it.");
                }
            }
            return results;
        }
    }
}
