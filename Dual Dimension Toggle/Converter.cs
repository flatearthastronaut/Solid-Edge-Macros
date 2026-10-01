using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DualDimensionToggle
{
    internal sealed class ConversionReport
    {
        internal string SheetName;
        internal int Examined, Changed, AlreadyTarget, AngularSkipped, Unrecognized, Missing, Ambiguous, Failed;
        internal int FramesExamined, FramesChanged, FrameRowsChanged, FramesAlreadyTarget, FramesSkipped, FramesFailed;
        internal int CalloutsExamined, CalloutsChanged, CalloutStylesChanged, CalloutValuesChanged, CalloutsSkipped, CalloutsFailed;
        internal readonly List<string> Details = new List<string>();
        public override string ToString()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("Active sheet: " + SheetName);
            text.AppendLine(String.Format("{0} changed; {1} already in the requested style family.", Changed, AlreadyTarget));
            text.AppendLine(AngularSkipped + " angular dimensions skipped (unchanged).");
            text.AppendLine(String.Format("{0} other styles; {1} missing matches; {2} ambiguous matches; {3} errors.", Unrecognized, Missing, Ambiguous, Failed));
            text.AppendLine("Dimensions examined: " + Examined);
            text.AppendLine(String.Format("Feature Control Frames: {0} changed ({1} rows); {2} already in requested format; {3} skipped; {4} errors.",
                FramesChanged, FrameRowsChanged, FramesAlreadyTarget, FramesSkipped, FramesFailed));
            text.AppendLine("Feature Control Frames examined: " + FramesExamined);
            text.AppendLine(String.Format("Callouts: {0} changed ({1} styles, {2} literal values); {3} skipped; {4} errors. Examined: {5}.",
                CalloutsChanged, CalloutStylesChanged, CalloutValuesChanged, CalloutsSkipped, CalloutsFailed, CalloutsExamined));
            text.AppendLine("Changes are not saved automatically. Review the draft, then save it in Solid Edge.");
            foreach (string detail in Details) text.AppendLine(detail);
            return text.ToString();
        }
    }

    internal static class Converter
    {
        internal static ConversionReport Run(bool toDual)
        {
            object application = null, document = null;
            try
            {
                try { application = Marshal.GetActiveObject("SolidEdge.Application"); }
                catch (COMException error)
                {
                    if (error.ErrorCode != unchecked((int)0x800401E3)) throw;
                    throw new InvalidOperationException("Open Solid Edge and activate the draft sheet you want to convert.", error);
                }
                document = ((dynamic)application).ActiveDocument;
                return ConvertDocument(document, toDual);
            }
            finally { Com.Release(ref document); Com.Release(ref application); }
        }

        internal static ConversionReport ConvertDocument(object document, bool toDual)
        {
            // igDraftDocument = 2, verified against the supplied SDK interop.
            if (document == null || (int)((dynamic)document).Type != 2)
                throw new InvalidOperationException("Activate a Solid Edge draft (.dft) before running this macro.");

            object sheet = null, dimensions = null, styles = null;
            ConversionReport report = new ConversionReport();
            try
            {
                // Capture this sheet once. Never traverse document.Sheets,
                // background sheets or drawing-view internal model sheets.
                sheet = ((dynamic)document).ActiveSheet;
                report.SheetName = (string)((dynamic)sheet).Name;
                dimensions = ((dynamic)sheet).Dimensions;
                styles = ((dynamic)document).DimensionStyles;
                List<string> names = new List<string>();
                int styleCount = (int)((dynamic)styles).Count;
                for (int index = 1; index <= styleCount; index++)
                {
                    object style = null;
                    try
                    {
                        style = ((dynamic)styles).Item(index);
                        names.Add((string)((dynamic)style).Name);
                    }
                    finally { Com.Release(ref style); }
                }
                // Complete the catalog before any edits. Only managed names
                // are cached, avoiding retained COM objects and repeated scans.
                StyleMap map = new StyleMap(names);
                int count = (int)((dynamic)dimensions).Count;
                Dictionary<string, int> changes = new Dictionary<string, int>();
                for (int index = 1; index <= count; index++)
                {
                    object dimension = null, style = null;
                    string source = null;
                    bool writeAttempted = false;
                    report.Examined++;
                    try
                    {
                        dimension = ((dynamic)dimensions).Item(index);
                        // Read the measured dimension type before accessing its
                        // style: angular dimensions can share the same named
                        // styles as lengths. SDK DimTypeConstants: Angular = 3,
                        // ArcAngle = 7, AngularCoordinate = 11. ArcLength (6)
                        // measures a length and must still be converted.
                        int dimensionType = (int)((dynamic)dimension).DimensionType;
                        if (dimensionType == 3 || dimensionType == 7 || dimensionType == 11)
                        {
                            report.AngularSkipped++;
                            continue;
                        }
                        style = ((dynamic)dimension).Style;
                        source = (string)((dynamic)style).Name;
                        string target;
                        MatchStatus match = map.Resolve(source, toDual, out target);
                        switch (match)
                        {
                            case MatchStatus.AlreadyTarget: report.AlreadyTarget++; continue;
                            case MatchStatus.Unrecognized: report.Unrecognized++; continue;
                            case MatchStatus.Missing:
                                report.Missing++;
                                report.Details.Add("Dimension " + index + ": no matching style for '" + source + "'.");
                                continue;
                            case MatchStatus.Ambiguous:
                                report.Ambiguous++;
                                report.Details.Add("Dimension " + index + ": multiple matching styles for '" + source + "'; unchanged.");
                                continue;
                        }
                        // DimStyle belongs to the individual dimension. Setting
                        // its Name selects an existing named style; never rename
                        // a DimensionStyle in the document's shared collection.
                        // Do not copy metric round-off values into inch settings:
                        // the matching N-place style defines the correct units.
                        writeAttempted = true;
                        ((dynamic)style).Name = target;
                        if (!String.Equals((string)((dynamic)style).Name, target, StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("Solid Edge did not retain the requested style.");
                        report.Changed++;
                        string change = source + " -> " + target;
                        int previous;
                        changes.TryGetValue(change, out previous);
                        changes[change] = previous + 1;
                    }
                    catch (Exception error)
                    {
                        report.Failed++;
                        string recovery = "";
                        if (writeAttempted)
                        {
                            try
                            {
                                ((dynamic)style).Name = source;
                                if (!String.Equals((string)((dynamic)style).Name, source, StringComparison.OrdinalIgnoreCase))
                                    throw new InvalidOperationException("Original style was not retained.");
                                recovery = " Original style restored; review any local formatting overrides.";
                            }
                            catch (Exception restoreError)
                            { recovery = " Could not restore original style; inspect this dimension: " + restoreError.Message; }
                        }
                        report.Details.Add("Dimension " + index + ": " + error.Message + recovery);
                    }
                    finally { Com.Release(ref style); Com.Release(ref dimension); }
                }
                foreach (KeyValuePair<string, int> change in changes)
                    report.Details.Add(change.Value + " x " + change.Key);
                FeatureFrameConverter.ConvertSheet(sheet, toDual, report);
                CalloutConverter.ConvertSheet(sheet, map, toDual, report);
                return report;
            }
            finally { Com.Release(ref styles); Com.Release(ref dimensions); Com.Release(ref sheet); }
        }
    }

    internal static class Com
    {
        internal static void Release(ref object reference)
        {
            object acquired = reference;
            reference = null;
            // Balance only our acquisition. FinalReleaseComObject could sever
            // another reference to a shared RCW and invalidate the session.
            if (acquired == null || !Marshal.IsComObject(acquired)) return;
            try { Marshal.ReleaseComObject(acquired); }
            catch (InvalidComObjectException) { }
            catch (COMException) { }
        }
    }

    [ComImport, Guid("00000016-0000-0000-C000-000000000046"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IOleMessageFilter
    {
        [PreserveSig] int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info);
        [PreserveSig] int RetryRejectedCall(IntPtr callee, int ticks, int rejectType);
        [PreserveSig] int MessagePending(IntPtr callee, int ticks, int pendingType);
    }

    // Follows the supplied SDK Draft.Sheets/OleMessageFilter example. Bounded
    // retries keep a busy/modal Solid Edge session from hanging indefinitely.
    internal sealed class OleMessageFilter : IOleMessageFilter, IDisposable
    {
        private IOleMessageFilter previous;
        internal OleMessageFilter() { Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(this, out previous)); }
        public int HandleInComingCall(int type, IntPtr caller, int ticks, IntPtr info) { return 0; }
        public int RetryRejectedCall(IntPtr callee, int ticks, int rejectType) { return rejectType == 2 && ticks < 10000 ? 250 : -1; }
        public int MessagePending(IntPtr callee, int ticks, int pendingType) { return 2; }
        public void Dispose()
        {
            IOleMessageFilter removed;
            Marshal.ThrowExceptionForHR(CoRegisterMessageFilter(previous, out removed));
            previous = null;
            GC.KeepAlive(this);
        }
        [DllImport("ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter filter, out IOleMessageFilter previous);
    }
}
