using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SolidEdgeConvert;

// Integration coverage for the real sample's symbol and two red FCFs. All CAD
// edits are confined to unique fixture copies, including the borrowed-doc test.
internal static class LiveGrindPdfTest
{
    [STAThread]
    private static int Main(string[] args)
    {
        object app = null, documents = null, draft = null;
        try
        {
            using (OleMessageFilter filter = new OleMessageFilter())
            {
                string originalHash = Hash(args[0]);
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work", "grind-pdf-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);
                string source = Path.Combine(folder, "GS sample.dft");
                File.Copy(args[0], source);
                app = Marshal.GetActiveObject("SolidEdge.Application"); documents = ((dynamic)app).Documents;
                int count = ((dynamic)documents).Count;
                object pdfBefore = null, stepBefore = null;
                ((dynamic)app).GetGlobalParameter(172, ref pdfBefore); ((dynamic)app).GetGlobalParameter(458, ref stepBefore);
                draft = ((dynamic)documents).Open(source);
                CheckAnnotations(draft, 1, 2, 1);
                GrindStockFilter.Remove(draft);
                CheckAnnotations(draft, 0, 0, 1);
                ((dynamic)draft).Close(false); ComLifetime.Release(ref draft);
                Check(Hash(source) == originalHash, "Filter changed the fixture on disk");
                Console.WriteLine("PASS sample filter: one gsnote symbol and two red frames removed; nonred frame retained.");

                // Keep an ordinary PDF next to the cleaned PDF for visual review.
                string baseline = Conversion.Run(source, false, delegate { return new SolidEdgeSession(); }, delegate { }, ConversionFormat.Pdf);
                File.Move(baseline, Path.Combine(folder, "GS sample original.pdf"));
                foreach (bool borrowed in new[] { false, true })
                {
                    string activeName = null;
                    if (borrowed) {
                        draft = ((dynamic)documents).Open(source);
                        object active = ((dynamic)draft).ActiveSheet;
                        try { activeName = ((dynamic)active).Name; } finally { ComLifetime.Release(ref active); }
                        Check(!(bool)((dynamic)draft).Dirty, "Fixture is unexpectedly dirty");
                    }
                    string result = Conversion.Run(source, true, delegate { return new SolidEdgeSession(); }, delegate { }, ConversionFormat.PdfWithoutGrindStock);
                    LivePdfSheetTest.CheckPages(result, 1);
                    Check((int)((dynamic)documents).Count == count + (borrowed ? 1 : 0), "Document ownership changed");
                    object pdfAfter = null, stepAfter = null;
                    ((dynamic)app).GetGlobalParameter(172, ref pdfAfter); ((dynamic)app).GetGlobalParameter(458, ref stepAfter);
                    Check(Object.Equals(pdfBefore, pdfAfter) && Object.Equals(stepBefore, stepAfter), "Global setting changed");
                    Check(Directory.GetFiles(folder, ".grind-*.dft").Length == 0, "Copy was not cleaned up");
                    if (borrowed) {
                        // Solid Edge can set the modified flag during native PDF
                        // export even for another open draft. Never clear it: that
                        // could hide edits. Check source annotations and bytes instead.
                        CheckAnnotations(draft, 1, 2, 1);
                        object active = ((dynamic)draft).ActiveSheet;
                        try { Check((string)((dynamic)active).Name == activeName, "Active sheet changed"); } finally { ComLifetime.Release(ref active); }
                        ((dynamic)draft).Close(false); ComLifetime.Release(ref draft);
                    }
                    Check(Hash(source) == originalHash && Hash(args[0]) == originalHash, "Source draft bytes changed");
                    Console.WriteLine("PASS cleaned active-sheet PDF from " + (borrowed ? "open" : "closed") + " draft; originals, settings and ownership preserved.");
                }
                Console.WriteLine("Output folder: " + folder);
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally {
            try { if (draft != null) ((dynamic)draft).Close(false); }
            finally { ComLifetime.Release(ref draft); ComLifetime.Release(ref documents); ComLifetime.Release(ref app); }
        }
    }
    private static void CheckAnnotations(object draft, int symbolsExpected, int redExpected, int otherExpected)
    {
        object sheet = null, symbols = null, frames = null;
        try {
            sheet = ((dynamic)draft).ActiveSheet; symbols = ((dynamic)sheet).Symbols; frames = ((dynamic)sheet).FeatureControlFrames;
            Check((int)((dynamic)symbols).Count == symbolsExpected, "Unexpected active-sheet symbol count");
            int red = 0, other = 0;
            for (int i = 1; i <= (int)((dynamic)frames).Count; i++) {
                object frame = null, style = null;
                try { frame = ((dynamic)frames).Item(i); style = ((dynamic)frame).Style; if ((int)((dynamic)style).DrivenColor == 255) red++; else other++; }
                finally { ComLifetime.Release(ref style); ComLifetime.Release(ref frame); }
            }
            Check(red == redExpected && other == otherExpected, "Unexpected frame counts: " + red + " red, " + other + " other");
        }
        finally { ComLifetime.Release(ref frames); ComLifetime.Release(ref symbols); ComLifetime.Release(ref sheet); }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static string Hash(string path) {
        using (SHA256 hash = SHA256.Create()) using (FileStream stream = File.OpenRead(path)) return Convert.ToBase64String(hash.ComputeHash(stream));
    }
}
