using System;
using System.IO;
using System.Reflection;
using Hammill.Automation;
namespace Hammill.StyleRepair
{
    // Borrow an already open master; close only a document opened by this helper.
    // Restore the user's original document and never save or edit the source.
    internal sealed class MasterDocument:IDisposable
    {
        readonly ComScope scope=new ComScope();readonly object target;bool opened;
        internal object Document;
        internal MasterDocument(object application,object targetDocument,string path)
        {
            target=targetDocument;
            try
            {
                path=Path.GetFullPath(path);
                if(!File.Exists(path))throw new FileNotFoundException("Select the master draft containing the correct styles.",path);
                if(String.Equals((string)((dynamic)target).FullName,path,StringComparison.OrdinalIgnoreCase))throw new InvalidOperationException("The master and target drawing must be different files.");
                dynamic documents=scope.Own(((dynamic)application).Documents);
                for(int i=1;i<=(int)documents.Count;i++)
                {
                    object item=documents.Item(i);
                    bool match=false;
                    try{match=String.Equals((string)((dynamic)item).FullName,path,StringComparison.OrdinalIgnoreCase);}
                    finally{if(!match)ComScope.Release(item);}
                    if(match){Document=scope.Own(item);break;}
                }
                if(Document==null){Document=scope.Own(documents.Open(path));opened=true;}
                if((int)((dynamic)Document).Type!=2)throw new InvalidOperationException("The master must be a Solid Edge draft.");
                ((dynamic)target).Activate();
            }
            catch {Dispose();throw;}
        }
        public void Dispose()
        {
            try{if(opened && Document!=null)((dynamic)Document).Close(false,Missing.Value,false);}
            finally{try{((dynamic)target).Activate();}finally{Document=null;opened=false;scope.Dispose();}}
        }
    }
}
