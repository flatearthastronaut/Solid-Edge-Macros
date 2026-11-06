using System;
using SolidEdgeFramework;

// Capture the actual native registration call without starting Solid Edge.
// Unsupported members fail immediately so the test cannot silently exercise a
// different command-registration path from the one used by the launcher.
internal sealed class RibbonTarget : ISEAddInEx2
{
    public int Style, SmallImage, LargeImage, CommandId, CommandCount;
    public string Group, Names;
    public void SetAddInInfoEx2(string resource, string environment, string category, int small, int large,
        int monoSmall, int monoLarge, int count, ref Array names, ref Array ids, ref Array styles)
    {
        Group = category; Names = (string)names.GetValue(0); SmallImage = small; LargeImage = large;
        CommandId = (int)ids.GetValue(0); Style = (int)styles.GetValue(0); CommandCount = count;
        // Solid Edge rewrites this output array to session IDs. The launcher must
        // continue to register each requested stable command ID independently.
        ids.SetValue(9999, 0);
    }
    public object Application { get { throw new NotSupportedException(); } }
    public object AddInEvents { get { throw new NotSupportedException(); } }
    public object AddInEdgeBarEvents { get { throw new NotSupportedException(); } }
    public bool Connect { get; set; }
    public string Description { get; set; }
    public string GUID { get { throw new NotSupportedException(); } }
    public int GuiVersion { get; set; }
    public object Object { get; set; }
    public string ProgID { get { throw new NotSupportedException(); } }
    public bool Visible { get; set; }
    public int AddCommand(string environment, string name, int id) { throw new NotSupportedException(); }
    public CommandBarButton AddCommandBarButton(string environment, string name, int id) { throw new NotSupportedException(); }
    public void SetAddInInfo(int instance, string environment, string category, int small, int large,
        int monoSmall, int monoLarge, int count, ref Array names, ref Array ids) { throw new NotSupportedException(); }
    public void SetAddInInfoEx(string resource, string environment, string category, int small, int large,
        int monoSmall, int monoLarge, int count, ref Array names, ref Array ids) { throw new NotSupportedException(); }
}
