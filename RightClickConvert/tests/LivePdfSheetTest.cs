using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using SolidEdgeConvert;

// Creates only a generated normal draft. Check actual PDF page dictionaries,
// unchanged source bytes, borrowed-document ownership and restored global state.
internal static class LivePdfSheetTest
{
    [STAThread]
    private static int Main()
    {
        object app = null, documents = null, draft = null;
        List<object> owned = new List<object>();
        try
        {
            using (OleMessageFilter filter = new OleMessageFilter())
            {
                app = Marshal.GetActiveObject("SolidEdge.Application");
                documents = ((dynamic)app).Documents;
                int beforeCount = ((dynamic)documents).Count;
                object pdfBefore = null, stepBefore = null;
                ((dynamic)app).GetGlobalParameter(172, ref pdfBefore);
                ((dynamic)app).GetGlobalParameter(458, ref stepBefore);
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work", "pdf-sheets-" + Guid.NewGuid().ToString("N").Substring(0, 12));
                Directory.CreateDirectory(folder);
                string source = Path.Combine(folder, "Drawing scopes.dft");
                draft = ((dynamic)documents).Add("SolidEdge.DraftDocument", "normal.dft");
                dynamic sheets = Keep(owned, ((dynamic)draft).Sheets);
                dynamic active = Keep(owned, ((dynamic)draft).ActiveSheet);
                dynamic lines = Keep(owned, active.Lines2d);
                Keep(owned, lines.AddBy2Points(0.02, 0.02, 0.12, 0.09));
                // Explicit section constants: 0 working, 1 background. The extra
                // background must not become an extra PDF page in same-type mode.
                Keep(owned, sheets.AddSheet("Second working test", 0));
                Keep(owned, sheets.AddSheet("Third working test", 0));
                Keep(owned, sheets.AddSheet("Excluded background test", 1));
                active.Activate();
                string activeName = active.Name;
                dynamic sections = Keep(owned, ((dynamic)draft).Sections);
                dynamic working = Keep(owned, sections.WorkingSection);
                dynamic workingSheets = Keep(owned, working.Sheets);
                int expectedAll = workingSheets.Count;
                if (expectedAll < 3) throw new Exception("Multi-sheet fixture was not created.");
                ((dynamic)draft).SaveAs(source);
                Release(owned);
                ((dynamic)draft).Close(false); ComLifetime.Release(ref draft);
                ((dynamic)app).DoIdle();
                string hash = Hash(source);
                File.WriteAllText(Path.Combine(folder, "pages.txt"), expectedAll.ToString());

                foreach (bool borrowed in new[] { false, true })
                {
                    foreach (ConversionFormat format in new[] { ConversionFormat.Pdf, ConversionFormat.PdfSameType, ConversionFormat.PdfWithDate, ConversionFormat.PdfWithDateSameType })
                    {
                        // Reopen the generated fixture for each borrowed test:
                        // native SaveAs(PDF) can mark an open draft dirty even
                        // though the source file is never saved by the converter.
                        if (borrowed) { draft = ((dynamic)documents).Open(source); ((dynamic)app).DoIdle(); }
                        string result = Conversion.Run(source, true, delegate { return new SolidEdgeSession(); }, delegate { }, format, new DateTime(2031, 2, 3));
                        bool all = format == ConversionFormat.PdfSameType || format == ConversionFormat.PdfWithDateSameType;
                        CheckPages(result, all ? expectedAll : 1);
                        // Solid Edge may exclusively lock an open draft. Hash it
                        // after closing our fixture; use ownership/active-sheet checks
                        // below while validating borrowed-document exports.
                        if (!borrowed && Hash(source) != hash) throw new Exception("PDF export modified the source draft.");
                        if ((int)((dynamic)documents).Count != beforeCount + (borrowed ? 1 : 0)) throw new Exception("Document ownership was not preserved.");
                        object pdfAfter = null, stepAfter = null;
                        ((dynamic)app).GetGlobalParameter(172, ref pdfAfter);
                        ((dynamic)app).GetGlobalParameter(458, ref stepAfter);
                        if (!Object.Equals(pdfBefore, pdfAfter) || !Object.Equals(stepBefore, stepAfter)) throw new Exception("Export changed a global preference.");
                        if (borrowed)
                        {
                            object current = ((dynamic)draft).ActiveSheet;
                            try { if ((string)((dynamic)current).Name != activeName) throw new Exception("Export changed active sheet."); }
                            finally { ComLifetime.Release(ref current); }
                            ((dynamic)draft).Close(false); ComLifetime.Release(ref draft); ((dynamic)app).DoIdle();
                            if (Hash(source) != hash) throw new Exception("Borrowed export modified the source draft file.");
                        }
                        Console.WriteLine("PASS " + format + ": " + (all ? expectedAll : 1) + " page(s), " + (borrowed ? "open" : "closed") + " draft; settings restored.");
                    }
                    if (Hash(source) != hash) throw new Exception("PDF export modified the source draft.");
                }
                Console.WriteLine("Fixture: " + source);
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            Release(owned);
            try { if (draft != null) ((dynamic)draft).Close(false); }
            finally { ComLifetime.Release(ref draft); ComLifetime.Release(ref documents); ComLifetime.Release(ref app); }
        }
    }
    internal static void CheckPages(string path, int expected)
    {
        string text = Encoding.ASCII.GetString(File.ReadAllBytes(path));
        if (!text.StartsWith("%PDF-") || !text.Contains("%%EOF")) throw new Exception("Incomplete PDF: " + path);
        int pages = Regex.Matches(text, @"/Type\s*/Page\b").Count;
        if (pages != expected) throw new Exception("Expected " + expected + " pages, found " + pages + ": " + path);
    }
    private static object Keep(List<object> owned, object value) { owned.Add(value); return value; }
    private static void Release(List<object> owned)
    {
        for (int i = owned.Count - 1; i >= 0; i--) { object value = owned[i]; ComLifetime.Release(ref value); }
        owned.Clear();
    }
    private static string Hash(string path)
    {
        using (SHA256 hash = SHA256.Create())
        using (FileStream stream = File.OpenRead(path)) return Convert.ToBase64String(hash.ComputeHash(stream));
    }
}
