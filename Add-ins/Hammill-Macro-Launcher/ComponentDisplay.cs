using System;
using Hammill.Automation;

namespace Hammill.MacroLauncher
{
    internal static class ComponentDisplay
    {
        // DetailCommandConstants.DetailDrawingViewsUpdateViews. This is the
        // original BWH BeforeCommandRun trigger, verified against the SDK/API.
        internal const int UpdateViewsCommand = 57083;
        public static int BeforeCommand(object application, int command, Action<string> log)
        {
            if (command != UpdateViewsCommand) return 0;
            using (var scope = new ComScope())
            {
                dynamic document = scope.Own(((dynamic)application).ActiveDocument);
                if ((int)document.Type != 2) return 0; // Draft only.
                return Apply(document, log);
            }
        }
        internal static int Apply(object document, Action<string> log)
        {
            int changed = 0;
            using (var scope = new ComScope())
            {
                dynamic sheets = scope.Own(((dynamic)document).Sheets);
                int sheetCount = (int)sheets.Count;
                for (int s=1; s<=sheetCount; s++)
                using (var sheetScope = new ComScope())
                {
                    dynamic sheet = sheetScope.Own(sheets.Item(s));
                    dynamic views = sheetScope.Own(sheet.DrawingViews);
                    int viewCount = (int)views.Count;
                    for (int v=1; v<=viewCount; v++)
                    using (var viewScope = new ComScope())
                    {
                        try
                        {
                            dynamic view = viewScope.Own(views.Item(v));
                            dynamic members = viewScope.Own(view.ModelMembers);
                            changed += Members(members, 0, log);
                        }
                        catch (Exception error) { log("Component Display, sheet " + s + ", view " + v + ": " + error.Message); }
                    }
                }
            }
            return changed;
        }
        private static int Members(object collection, int depth, Action<string> log)
        {
            if (depth > 128) throw new InvalidOperationException("Component hierarchy exceeds 128 levels.");
            int changed = 0;
            dynamic members = collection;
            int count = (int)members.Count;
            for(int i=1;i<=count;i++)
            using(var scope = new ComScope())
            {
                try
                {
                    dynamic member = scope.Own(members.Item(i));
                    int type = (int)member.ComponentType;
                    string name = (string)member.ComponentName;
                    if(type == 0) // Assembly; a prefix applies to the whole branch.
                    {
                        int mode = Prefix(name);
                        if(mode != 0) changed += SetReference(member,mode == 1);
                        else { dynamic children=scope.Own(member.ModelMembers); changed += Members(children,depth+1,log); }
                    }
                    else if(type == 1 || type == 6) // Part or sheet metal.
                    {
                        dynamic children = scope.Own(member.ModelMembers);
                        int childCount = (int)children.Count;
                        for(int c=1;c<=childCount;c++)
                        using(var childScope = new ComScope())
                        {
                            dynamic child = childScope.Own(children.Item(c));
                            string childName = (string)child.ComponentName;
                            if(String.Equals(childName,"Design Body",StringComparison.Ordinal))
                            { int mode=Prefix(name); if(mode != 0) changed += SetReference(member,mode == 1); }
                            string tag=BodyTag(childName);
                            if(tag == "THRD" || tag == "TB")
                            {
                                string style=tag == "TB" ? "Phantom" : "Hidden";
                                bool differs=(int)child.DisplayType != 0 || (string)child.VisibleEdgeStyleName != style || (string)child.HiddenEdgeStyleName != style || (tag == "TB" && !(bool)child.ShowHiddenEdges);
                                if(!differs) continue;
                                child.DisplayType=0; // seShowPart
                                child.VisibleEdgeStyleName=style; child.HiddenEdgeStyleName=style;
                                if(tag == "TB")child.ShowHiddenEdges=true;
                                changed++;
                            }
                        }
                    }
                }
                catch(Exception error) { log("Component Display, member " + i + ": " + error.Message); }
            }
            return changed;
        }
        internal static int Prefix(string name)
        {
            if(String.IsNullOrEmpty(name))return 0;
            if(name.StartsWith("UC_",StringComparison.OrdinalIgnoreCase) || name.StartsWith("REF_",StringComparison.OrdinalIgnoreCase))return 1;
            if(name.StartsWith("NORM_",StringComparison.OrdinalIgnoreCase) || name.StartsWith("UNDO_",StringComparison.OrdinalIgnoreCase))return 2;
            return 0;
        }
        internal static string BodyTag(string name)
        {
            if(String.IsNullOrEmpty(name))return "";
            string[] parts=name.Split('.');
            return parts.Length < 2 ? "" : parts[1].Split('_')[0].ToUpperInvariant();
        }
        private static int SetReference(dynamic member, bool reference)
        {
            string visible=reference ? "Phantom" : "Visible", hidden=reference ? "Phantom" : "Hidden";
            if((bool)member.DisplayAsReference == reference && (string)member.VisibleEdgeStyleName == visible && (string)member.HiddenEdgeStyleName == hidden)return 0;
            member.DisplayAsReference=reference; member.VisibleEdgeStyleName=visible; member.HiddenEdgeStyleName=hidden;
            return 1;
        }
    }
}
