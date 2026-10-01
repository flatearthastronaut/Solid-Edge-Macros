using System;
using System.IO;
using DualDimensionToggle;

internal static class LiveCalloutHistory
{
    internal static int Run(object app)
    {
        string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CalloutRoundtrip-" + Guid.NewGuid().ToString("N") + ".dft");
        object docs = null, doc = null, sheet = null, notes = null, note = null;
        try
        {
            docs = ((dynamic)app).Documents;
            doc = ((dynamic)docs).Add("SolidEdge.DraftDocument");
            sheet = ((dynamic)doc).ActiveSheet;
            notes = ((dynamic)sheet).Balloons;
            note = ((dynamic)notes).Add(0.1, 0.1, 0.0);
            ((dynamic)note).Callout = 1;
            object definitions = null, baseStyle = null, customStyle = null, noteStyle = null;
            try
            {
                definitions = ((dynamic)doc).DimensionStyles;
                baseStyle = ((dynamic)definitions).Item(1);
                customStyle = ((dynamic)definitions).Add("DTT history test", (string)((dynamic)baseStyle).Name);
                noteStyle = ((dynamic)note).Style;
                ((dynamic)noteStyle).Name = "DTT history test";
            }
            finally { Com.Release(ref noteStyle); Com.Release(ref customStyle); Com.Release(ref baseStyle); Com.Release(ref definitions); }
            const string dual = "%DI 6.50/6.35[.256/.250] DIA\rBARE .125 MUST STAY";
            const string single = "%DI .256/.250 DIA\rBARE .125 MUST STAY";
            ((dynamic)note).BalloonText = dual;
            // Empty style catalog isolates text behavior regardless of which
            // styles the user's default template contains.
            ConversionReport report = new ConversionReport();
            CalloutConverter.ConvertSheet(sheet, new StyleMap(new string[0]), false, report);
            if (report.CalloutsChanged != 1)
                throw new Exception("Save/reopen seed callout was not converted: " + report);
            if ((string)((dynamic)note).BalloonText != single || CalloutHistoryStore.Read(note) == null)
                throw new Exception("Save/reopen seed conversion failed.");
            ((dynamic)doc).SaveAs(path);
            Com.Release(ref note); Com.Release(ref notes); Com.Release(ref sheet);
            ((dynamic)doc).Close(false); Com.Release(ref doc);
            doc = ((dynamic)docs).Open(path);
            sheet = ((dynamic)doc).ActiveSheet; notes = ((dynamic)sheet).Balloons;
            note = ((dynamic)notes).Item(1);
            if ((string)((dynamic)note).BalloonText != single || CalloutHistoryStore.Read(note) == null)
                throw new Exception("Pair history did not persist after save/reopen.");
            report = new ConversionReport();
            CalloutConverter.ConvertSheet(sheet, new StyleMap(new string[0]), true, report);
            if (report.CalloutsChanged != 1 || report.CalloutsFailed != 0 || (string)((dynamic)note).BalloonText != dual)
                throw new Exception("Saved callout pairs could not be restored: " + report);
            if (CalloutHistoryStore.Read(note) != null) throw new Exception("Restored callout history was not cleared.");
            return 5;
        }
        finally
        {
            Com.Release(ref note); Com.Release(ref notes); Com.Release(ref sheet);
            try { if (doc != null) ((dynamic)doc).Close(false); }
            finally { Com.Release(ref doc); Com.Release(ref docs); }
            // This test owns only this unique file in the test executable folder.
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
