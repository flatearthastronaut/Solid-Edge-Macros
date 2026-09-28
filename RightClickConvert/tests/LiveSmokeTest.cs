using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using SolidEdgeConvert;

// Optional integration test. Creates only its own cylinder in tests/work, uses
// the running Solid Edge instance, and never saves/closes a user's documents.
internal static class LiveSmokeTest
{
    [STAThread]
    private static int Main()
    {
        object app = null, documents = null, document = null;
        List<object> geometry = new List<object>();
        try
        {
            using (OleMessageFilter filter = new OleMessageFilter())
            {
                app = Marshal.GetActiveObject("SolidEdge.Application");
                documents = ((dynamic)app).Documents;
                int initialCount = ((dynamic)documents).Count;
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "work", "live-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(folder);
                string source = Path.Combine(folder, "Cylinder & STEP é.par");
                Console.WriteLine("Creating test cylinder: " + source);
                document = ((dynamic)documents).Add("SolidEdge.PartDocument");
                ((dynamic)app).DoIdle();

                // SDK FiniteExtrudedProtrusion sequence, with a circular profile
                // instead of a polyline. API geometry dimensions are metres.
                dynamic sets = Keep(geometry, ((dynamic)document).ProfileSets);
                dynamic set = Keep(geometry, sets.Add());
                dynamic profiles = Keep(geometry, set.Profiles);
                dynamic planes = Keep(geometry, ((dynamic)document).RefPlanes);
                dynamic plane = Keep(geometry, planes.Item(3));
                dynamic profile = Keep(geometry, profiles.Add(plane));
                dynamic circles = Keep(geometry, profile.Circles2d);
                Keep(geometry, circles.AddByCenterRadius(0.0, 0.0, 0.01));
                profile.End(1); // igProfileClosed, verified in Batch's constants
                dynamic models = Keep(geometry, ((dynamic)document).Models);
                // As in the SDK and SketchToModels, this geometry API needs a
                // typed interface SAFEARRAY; dynamic dispatch cannot marshal it.
                Array profileArray = Array.CreateInstance(typeof(SolidEdge.Part.Interop.Profile), 1);
                profileArray.SetValue((SolidEdge.Part.Interop.Profile)profile, 0);
                Keep(geometry, ((SolidEdge.Part.Interop.Models)models).AddFiniteExtrudedProtrusion(
                    1, ref profileArray, SolidEdge.Part.Interop.FeaturePropertyConstants.igRight, 0.02));
                profile.Visible = false;
                ((dynamic)document).SaveAs(source);
                ReleaseGeometry(geometry);
                ((dynamic)document).Close(false);
                ComLifetime.Release(ref document);
                ((dynamic)app).DoIdle();
                string before = Hash(source);

                object originalAdapter = null;
                ((dynamic)app).GetGlobalParameter(458, ref originalAdapter);
                string result = Conversion.Run(source, false, delegate { return new SolidEdgeSession(); }, Console.WriteLine);
                CheckStep(result);
                if (Hash(source) != before) throw new Exception("Source was modified.");
                object restoredAdapter = null;
                ((dynamic)app).GetGlobalParameter(458, ref restoredAdapter);
                if (!Object.Equals(originalAdapter, restoredAdapter)) throw new Exception("STEP adapter was not restored.");
                if ((int)((dynamic)documents).Count != initialCount) throw new Exception("Conversion left a document open.");

                // Exercise actual reuse and replacement, rather than just mocks.
                document = ((dynamic)documents).Open(source);
                ((dynamic)app).DoIdle();
                Conversion.Run(source, true, delegate { return new SolidEdgeSession(); }, Console.WriteLine);
                CheckStep(result);
                if ((int)((dynamic)documents).Count != initialCount + 1) throw new Exception("An already-open document was closed.");
                ((dynamic)document).Close(false);
                ComLifetime.Release(ref document);
                ((dynamic)app).DoIdle();
                if (Hash(source) != before) throw new Exception("Source was modified during reuse.");
                Console.WriteLine("PASS live cylinder export, replacement, source preservation, adapter restoration, and open-document reuse.");
                Console.WriteLine("Fixture retained for visual inspection: " + result);
            }
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
        finally
        {
            ReleaseGeometry(geometry);
            if (document != null)
            {
                try { ((dynamic)document).Close(false); }
                catch (Exception error) { Console.Error.WriteLine("Test fixture cleanup: " + error.Message); }
            }
            ComLifetime.Release(ref document);
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
    private static void CheckStep(string path)
    {
        string text = File.ReadAllText(path);
        if (!text.StartsWith("ISO-10303-21;") || !text.Contains("END-ISO-10303-21;") || !text.Contains("MANIFOLD_SOLID_BREP"))
            throw new Exception("Output is not a complete STEP solid.");
    }
}
