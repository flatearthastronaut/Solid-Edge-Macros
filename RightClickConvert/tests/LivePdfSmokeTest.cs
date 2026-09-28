using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using SolidEdgeConvert;

// Creates only its own drawing under tests/work. No existing user document is
// saved, modified, or closed. Reference: SDK Draft.Sheets and Batch SaveAs(.pdf).
internal static class LivePdfSmokeTest
{
    [STAThread]
    private static int Main()
    {
        object app = null, documents = null, draft = null;
        List<object> geometry = new List<object>();
        try
        {
            using (OleMessageFilter filter = new OleMessageFilter())
            {
                app = Marshal.GetActiveObject("SolidEdge.Application");
                documents = ((dynamic)app).Documents;
                int initialCount = ((dynamic)documents).Count;
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work", "pdf-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);
                string source = Path.Combine(folder, "Drawing & PDF é.dft");
                Console.WriteLine("Creating test draft: " + source);
                draft = ((dynamic)documents).Add("SolidEdge.DraftDocument");
                ((dynamic)app).DoIdle();
                dynamic sheet = Keep(geometry, ((dynamic)draft).ActiveSheet);
                dynamic lines = Keep(geometry, sheet.Lines2d);
                Keep(geometry, lines.AddBy2Points(0.03, 0.03, 0.13, 0.03));
                Keep(geometry, lines.AddBy2Points(0.13, 0.03, 0.13, 0.10));
                Keep(geometry, lines.AddBy2Points(0.13, 0.10, 0.03, 0.10));
                Keep(geometry, lines.AddBy2Points(0.03, 0.10, 0.03, 0.03));
                dynamic circles = Keep(geometry, sheet.Circles2d);
                Keep(geometry, circles.AddByCenterRadius(0.08, 0.065, 0.02));
                ((dynamic)draft).SaveAs(source);
                ReleaseGeometry(geometry);
                ((dynamic)draft).Close(false);
                ComLifetime.Release(ref draft);
                ((dynamic)app).DoIdle();
                string before = Hash(source);

                object originalAdapter = null;
                ((dynamic)app).GetGlobalParameter(458, ref originalAdapter);
                string result = Conversion.Run(source, false, delegate { return new SolidEdgeSession(); }, Console.WriteLine, ConversionFormat.Pdf);
                CheckPdf(result);
                if (Hash(source) != before) throw new Exception("Draft source was modified.");
                if ((int)((dynamic)documents).Count != initialCount) throw new Exception("PDF conversion left a document open.");

                // Repeat with an open draft and existing PDF to exercise the real
                // borrowed-document and atomic replacement paths.
                draft = ((dynamic)documents).Open(source);
                ((dynamic)app).DoIdle();
                Conversion.Run(source, true, delegate { return new SolidEdgeSession(); }, Console.WriteLine, ConversionFormat.Pdf);
                CheckPdf(result);
                if ((int)((dynamic)documents).Count != initialCount + 1) throw new Exception("An already-open draft was closed.");
                ((dynamic)draft).Close(false);
                ComLifetime.Release(ref draft);
                ((dynamic)app).DoIdle();
                if (Hash(source) != before) throw new Exception("Draft changed during reuse.");
                object finalAdapter = null;
                ((dynamic)app).GetGlobalParameter(458, ref finalAdapter);
                if (!Object.Equals(originalAdapter, finalAdapter)) throw new Exception("PDF conversion changed the STEP setting.");
                Console.WriteLine("PASS live draft PDF export, replacement, source preservation, unchanged STEP setting, and open-draft reuse.");
                Console.WriteLine("Fixture retained for inspection: " + result);
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            ReleaseGeometry(geometry);
            if (draft != null)
            {
                try { ((dynamic)draft).Close(false); }
                catch (Exception error) { Console.Error.WriteLine("Test draft cleanup: " + error.Message); }
            }
            ComLifetime.Release(ref draft);
            ComLifetime.Release(ref documents);
            ComLifetime.Release(ref app);
        }
    }
    private static object Keep(List<object> owned, object value) { owned.Add(value); return value; }
    private static void ReleaseGeometry(List<object> owned)
    {
        for (int i = owned.Count - 1; i >= 0; i--) { object value = owned[i]; ComLifetime.Release(ref value); }
        owned.Clear();
    }
    private static string Hash(string path)
    {
        using (SHA256 hash = SHA256.Create())
        using (FileStream stream = File.OpenRead(path)) return Convert.ToBase64String(hash.ComputeHash(stream));
    }
    private static void CheckPdf(string path)
    {
        string text = Encoding.ASCII.GetString(File.ReadAllBytes(path));
        if (!text.StartsWith("%PDF-") || !text.Contains("%%EOF"))
            throw new Exception("Output is not a complete PDF file.");
    }
}
