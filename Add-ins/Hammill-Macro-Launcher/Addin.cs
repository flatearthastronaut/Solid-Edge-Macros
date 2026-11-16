using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using Hammill.Automation;
using Hammill.DimensionStyleCycle;
using SolidEdgeFramework;
using Environment = System.Environment;

[assembly: AssemblyTitle("Hammill Add-ins")]
[assembly: AssemblyCompany("Hammill")]
[assembly: AssemblyProduct("Hammill Add-ins")]
[assembly: AssemblyVersion("0.6.0.0")]
[assembly: AssemblyFileVersion("0.6.0.0")]
[assembly: ComVisible(false)]

namespace Hammill.MacroLauncher
{
    [ComVisible(true), Guid("AB75B3C0-C461-4AD4-BF81-873EE06DD09C"), ProgId("Hammill.MacroLauncher"), ClassInterface(ClassInterfaceType.None)]
    public sealed class Addin : ISolidEdgeAddIn
    {
        internal const string ApplicationEnvironment = "{26618394-09D6-11D1-BA07-080036230602}";
        internal const int StyleCycleCommand = 2; // Built-ins are below persisted macro IDs (100+).
        internal const int LimitToleranceCommand = 3;
        internal static readonly Dictionary<string, string> Environments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) {
            { ApplicationEnvironment, "application" },
            { "{26618396-09D6-11D1-BA07-080036230602}", "part" },
            { "{D9B0BB85-3A6C-4086-A0BB-88A1AAD57A58}", "part" },
            { "{26618395-09D6-11D1-BA07-080036230602}", "assembly" },
            { "{2C3C2A72-3A4A-471D-98B5-E3A8CFA4A2BF}", "assembly" },
            { "{08244193-B78D-11D2-9216-00C04F79BE98}", "draft" },
            { "{26618398-09D6-11D1-BA07-080036230602}", "sheetmetal" },
            { "{9CBF2809-FF80-4DBC-98F2-B82DABF3530F}", "sheetmetal" }
        };
        private SolidEdgeFramework.AddIn native;
        private ISEAddInEvents_Event events;
        private ISEApplicationEvents_Event applicationEvents;
        private object hostApplication;
        private bool updatingComponentDisplay;
        private bool processingDraftingCommand;
        private readonly Queue<int> pendingDraftingCommands = new Queue<int>();
        private StyleSequenceFile styleSequence;
        private readonly StyleIndex cycleStyleIndex = new StyleIndex();
        private MacroCatalog catalog;
        private IconResources icons;
        private string logPath;
        private readonly HashSet<string> connected = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public void OnConnection(object application, SeConnectMode mode, SolidEdgeFramework.AddIn addIn)
        {
            native = addIn;
            hostApplication = application; // Borrowed host reference; never forcibly release it.
            string userData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string cache = Path.Combine(userData, "Hammill", "MacroLauncher");
            logPath = Path.Combine(cache, "launcher.log");
            try
            {
                Directory.CreateDirectory(cache);
                string managerSettings = Path.Combine(userData, "Coop", "SEMacroManager", "settings.xml");
                catalog = MacroCatalog.Scan(managerSettings, Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                // Avoid network reads during connection and ribbon UI updates.
                styleSequence = SharedStyleSequence(managerSettings);
                RibbonState state = RibbonState.Update(Path.Combine(cache, "ribbon.xml"), catalog);
                // SDK Addins/VC/SEAddin.cpp: GuiVersion changes purge the add-in's cached GUI.
                // Only actual catalog/icon/layout changes advance it; ordinary EXE updates do not.
                native.GuiVersion = state.Version;
                try { icons = IconResources.Create(catalog, Path.Combine(Path.GetDirectoryName(typeof(Addin).Assembly.Location), "Hammill.MacroLauncher.Resources.dll"), cache, state.Signature); }
                catch (Exception error) { catalog.Notices.Add("Icons unavailable; showing text buttons: " + error.Message); }
                native.Description = "Hammill Add-ins";
                native.Visible = true;
                events = (ISEAddInEvents_Event)native.AddInEvents;
                events.OnCommand += OnCommand;
                events.OnCommandHelp += OnHelp;
                events.OnCommandUpdateUI += OnUpdateUI;
                applicationEvents = (ISEApplicationEvents_Event)((SolidEdgeFramework.Application)application).ApplicationEvents;
                applicationEvents.BeforeCommandRun += BeforeHostCommand;
                Log("Startup: " + catalog.Entries.Count + " macro(s); folder=" + catalog.Folder);
                foreach (string message in catalog.Notices) Log(message);
            }
            catch (Exception error) { Log("Connection failed: " + error); OnDisconnection(SeDisconnectMode.seDisconnectByUser); }
        }

        public void OnConnectToEnvironment(string category, object environment, bool firstTime)
        {
            try
            {
                string name;
                if (native == null || catalog == null || !Environments.TryGetValue(category, out name) || connected.Contains(category)) return;
                var extended = (ISEAddInEx2)native;
                RegisterButton(extended, category, 1, "status", "Macro folder", "Launcher", "Show the macro folder, detected tools and startup scan results.", -1);
                if (name == "draft")
                {
                    RegisterButton(extended, category, StyleCycleCommand, "dimension-style-cycle", "Cycle Dimension Style", "Drafting",
                        "Apply the next style in the selected dimension's configured family.", icons == null ? -1 : IconResources.StyleCycleImage);
                    RegisterButton(extended, category, LimitToleranceCommand, "limit-tolerance-cycle", "Cycle Limit Tolerance", "Drafting",
                        "Cycle inch limits: +/-0.0005, +/-0.001, +/-0.002.", icons == null ? -1 : IconResources.LimitToleranceImage);
                }
                foreach (var entry in catalog.Entries.Where(x => x.Environments.Split(',').Contains(name)))
                {
                    int imageId = icons == null ? -1 : icons.ResourceIds[entry.CommandId];
                    RegisterButton(extended, category, entry.CommandId, entry.Key, entry.Name, entry.Group, "Run " + entry.Name, imageId);
                }
                connected.Add(category);
                // environment/application are borrowed callback parameters. Do not forcibly release
                // shared RCWs. The only event subscriptions we create are paired in OnDisconnection.
            }
            catch (Exception error) { Log("Ribbon setup failed for " + category + ": " + error); }
        }

        internal void RegisterButton(ISEAddInEx2 target, string environment, int id, string key, string label, string group, string tooltip, int image)
        {
            Array names = new string[] { "Hammill.MacroLauncher." + key + "\n" + label + "\n" + tooltip + "\n" + label };
            Array ids = new int[] { id };
            // SDK Addins/VC/SEAddin.cpp uses IconAndCaptionBelow to request a
            // large ribbon button. Providing a large image alone does not select
            // that layout; keep caption-only controls usable if artwork is absent.
            // The image/image+1 slots contain 32/40px PNGs for normal/higher DPI.
            Array styles = new int[] { image < 0 ? (int)SeButtonStyle.seButtonCaption : (int)SeButtonStyle.seButtonIconAndCaptionBelow };
            target.SetAddInInfoEx2(icons == null ? typeof(Addin).Assembly.Location : icons.FileName, environment,
                "Hammill Macros\n" + group, image, image < 0 ? -1 : image + 1, -1, -1, 1, ref names, ref ids, ref styles);
            // SE replaces IDs in the output array with session-specific IDs. Its OnCommand events
            // still use our original IDs, so never persist those returned session IDs.
        }

        private void OnCommand(int id)
        {
            try
            {
                if (id == 1)
                {
                    string summary = catalog.Folder + "\r\n\r\n" + catalog.Entries.Count + " installed macro(s).\r\nRestart Solid Edge after installing or removing macros.\r\n\r\n";
                    summary += String.Join("\r\n", catalog.Notices.Count == 0 ? catalog.Entries.Select(x => x.Name) : catalog.Notices);
                    if (styleSequence != null)
                    {
                        try { summary += "\r\n\r\nShared dimension style order: " + styleSequence.Path; }
                        catch (Exception error) { summary += "\r\n\r\nShared dimension style order: " + error.Message; }
                    }
                    MessageBox.Show(summary, "Hammill Macro Launcher", MessageBoxButtons.OK, MessageBoxIcon.Information); return;
                }
                if (id == StyleCycleCommand || id == LimitToleranceCommand)
                {
                    // Runs directly on SE's command callback thread. No EXE launch,
                    // ROT connection, modal success dialog or document-wide scan.
                    // If SE dispatches another button click during a native call,
                    // retain it and apply its step after the current change finishes.
                    pendingDraftingCommands.Enqueue(id);
                    if (processingDraftingCommand) return;
                    processingDraftingCommand = true;
                    try
                    {
                        while (pendingDraftingCommands.Count > 0)
                        {
                            int command = pendingDraftingCommands.Dequeue();
                            if (command == StyleCycleCommand) ExecuteStyleCycle(hostApplication, styleSequence, cycleStyleIndex);
                            else ExecuteLimitTolerance(hostApplication);
                        }
                    }
                    finally { processingDraftingCommand = false; pendingDraftingCommands.Clear(); }
                    return;
                }
                var macro = catalog.Entries.FirstOrDefault(x => x.CommandId == id);
                if (macro == null) return;
                // Recheck the resolved path at click time in case a package was removed or replaced.
                string relative = macro.Path.Substring(catalog.Folder.TrimEnd('\\', '/').Length + 1);
                string executable = MacroCatalog.Child(catalog.Folder, relative);
                if (!File.Exists(executable)) throw new FileNotFoundException("This macro is no longer installed. Refresh Macro Manager and restart Solid Edge.", executable);
                using (var process = Process.Start(new ProcessStartInfo { FileName = executable, WorkingDirectory = Path.GetDirectoryName(executable), UseShellExecute = false, CreateNoWindow = true })) { }
                Log("Launched " + macro.Name);
            }
            catch (Exception error) { Log("Command failed: " + error); MessageBox.Show(error.Message, id == LimitToleranceCommand ? "Hammill Limit Tolerance Cycle" : id == StyleCycleCommand ? "Hammill Dimension Style Cycle" : "Hammill Macro Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }

        internal static string ExecuteStyleCycle(object application, StyleSequenceFile configuration, StyleIndex index)
        {
            if (application == null || configuration == null) throw new InvalidOperationException("Reconnect Hammill Add-ins to initialize the dimension-style command.");
            StyleSequence sequence = configuration.Get();
            using (var scope = new ComScope())
            {
                object document = scope.Own(((dynamic)application).ActiveDocument);
                return CycleEngine.Run(document, sequence, index);
            }
        }

        internal static StyleSequenceFile SharedStyleSequence(string managerSettings)
        {
            // Also follow a changed Available folder on the next click, without
            // reconnecting the add-in. Macros still run from local installations.
            return new StyleSequenceFile(() => Path.Combine(MacroCatalog.ManagerSourceFolder(managerSettings), "Hammill.DimensionStyles.txt"));
        }

        private void OnHelp(int window, int help, int id)
        {
            if (id == LimitToleranceCommand)
                MessageBox.Show("Select one inch or dual-unit dimension. Each click advances the native limits: +/-0.0005, +/-0.001, +/-0.002 inch, then repeats. Other tolerances start at +/-0.0005. The dimension stays selected.", "Hammill Limit Tolerance Cycle", MessageBoxButtons.OK, MessageBoxIcon.Information);
            else if (id == StyleCycleCommand && styleSequence != null)
            {
                string location;
                try { location = styleSequence.Path; }
                catch (Exception error) { location = error.Message; }
                MessageBox.Show("Select one dimension and click to advance within its family.\r\nEveryone uses this shared list; saved edits apply on the next click:\r\n\r\n" + location,
                    "Hammill Dimension Style Cycle", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else OnCommand(1);
        }
        internal static decimal ExecuteLimitTolerance(object application)
        {
            if (application == null) throw new InvalidOperationException("Reconnect Hammill Add-ins before cycling tolerances.");
            using (var scope = new ComScope())
                return LimitToleranceCycle.Run(scope.Own(((dynamic)application).ActiveDocument));
        }
        private void BeforeHostCommand(int command)
        {
            if(updatingComponentDisplay || command != ComponentDisplay.UpdateViewsCommand)return;
            updatingComponentDisplay=true;
            try { int changed=ComponentDisplay.BeforeCommand(hostApplication,command,Log); if(changed != 0)Log("Component Display: updated " + changed + " member(s)."); }
            catch(Exception error) { Log("Component Display failed: " + error.Message); }
            finally { updatingComponentDisplay=false; }
        }
        private void OnUpdateUI(int id, ref int flags, out string text, ref int bitmap)
        {
            // Called frequently by SE: no filesystem scans, hashing or XML reads on this path.
            text = null;
            flags = id == 1 || id == StyleCycleCommand || id == LimitToleranceCommand || (catalog != null && catalog.Entries.Any(x => x.CommandId == id)) ? 1 : 0;
        }

        public void OnDisconnection(SeDisconnectMode mode)
        {
            var hostEvents=applicationEvents; applicationEvents=null;
            if(hostEvents != null)
            {
                try { hostEvents.BeforeCommandRun -= BeforeHostCommand; } catch(Exception error) { Log(error.Message); }
                ComScope.Release(hostEvents);
            }
            hostApplication=null;
            var subscription = events; events = null;
            if (subscription != null)
            {
                try { subscription.OnCommand -= OnCommand; } catch (Exception error) { Log(error.Message); }
                try { subscription.OnCommandHelp -= OnHelp; } catch (Exception error) { Log(error.Message); }
                try { subscription.OnCommandUpdateUI -= OnUpdateUI; } catch (Exception error) { Log(error.Message); }
                // This getter-created event reference is ours; release once, never FinalRelease a
                // shared application/document/add-in object used by other managed extensions.
                try { if (Marshal.IsComObject(subscription)) Marshal.ReleaseComObject(subscription); } catch (Exception error) { Log(error.Message); }
            }
            connected.Clear(); native = null; catalog = null; icons = null; styleSequence = null;
        }

        private void Log(string message)
        {
            try
            {
                if (String.IsNullOrEmpty(logPath)) return;
                if (File.Exists(logPath) && new FileInfo(logPath).Length > 1000000) File.WriteAllText(logPath, "");
                File.AppendAllText(logPath, DateTime.Now.ToString("s") + " " + message + Environment.NewLine);
            }
            catch { /* Logging must never propagate an exception into the host application's event loop. */ }
        }

        [ComRegisterFunction]
        public static void Register(Type type)
        {
            using (var key = Registry.ClassesRoot.CreateSubKey("CLSID\\{" + type.GUID + "}"))
            {
                key.SetValue("AutoConnect", 1, RegistryValueKind.DWord);
                key.SetValue("409", "Hammill Add-ins");
                key.SetValue("Summary", "Macro ribbon launcher and automatic draft Component Display.");
                using (key.CreateSubKey("Implemented Categories\\{26B1D2D1-2B03-11D2-B589-080036E8B802}")) { }
                foreach (string environment in Environments.Keys) using (key.CreateSubKey("Environment Categories\\" + environment)) { }
            }
        }

        [ComUnregisterFunction]
        public static void Unregister(Type type)
        {
            using (var key = Registry.ClassesRoot.OpenSubKey("CLSID\\{" + type.GUID + "}", true))
            {
                if (key == null) return;
                key.DeleteSubKeyTree("Implemented Categories", false); key.DeleteSubKeyTree("Environment Categories", false);
                foreach (string value in new[] { "AutoConnect", "409", "Summary" }) key.DeleteValue(value, false);
            }
        }
    }
}
