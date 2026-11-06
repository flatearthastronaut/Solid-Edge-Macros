using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace Hammill.ORingFinder
{
    internal sealed class RingPart : IDisposable
    {
        public string Name, FileName, Error;
        public decimal Diameter, GrooveWidth;
        public object Document;
        public override string ToString() { return Name + (Error == null ? "" : " [check dimensions]"); }
        public void Dispose() { ComScope.Release(Document); Document = null; }
    }

    internal sealed class SolidEdgeSession : IDisposable
    {
        private object application;
        public SolidEdgeSession() { application = Marshal.GetActiveObject("SolidEdge.Application"); }
        internal static bool IsRingName(string name)
        { return name != null && (name.StartsWith("O_", StringComparison.OrdinalIgnoreCase) || name.StartsWith("O-", StringComparison.OrdinalIgnoreCase)); }
        public List<RingPart> Scan()
        {
            using (var scope = new ComScope())
            {
                dynamic app = application;
                dynamic documents = scope.Own(app.Documents);
                if ((int)documents.Count == 0) throw new InvalidOperationException("Open an assembly in Solid Edge first.");
                dynamic document = scope.Own(app.ActiveDocument);
                // SDK DocumentTypeConstants: assembly=3, synchronous assembly=10.
                int type = (int)document.Type;
                if (type != 3 && type != 10) throw new InvalidOperationException("Make an assembly the active Solid Edge document, then reload.");
                return ReadAssembly(document);
            }
        }
        internal static List<RingPart> ReadAssembly(object assembly)
        {
            var result = new List<RingPart>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                using (var scope = new ComScope())
                {
                    dynamic occurrences = scope.Own(((dynamic)assembly).Occurrences);
                    int count = (int)occurrences.Count;
                    for (int i = 1; i <= count; i++)
                    using (var item = new ComScope())
                    {
                        dynamic occurrence = item.Own(occurrences.Item(i));
                        string name = (string)occurrence.Name;
                        if (!IsRingName(name)) continue;
                        var part = new RingPart { Name = name };
                        try
                        {
                            part.Document = (object)occurrence.OccurrenceDocument;
                            dynamic doc = part.Document;
                            int type = (int)doc.Type;
                            if (type != 1 && type != 8) throw new InvalidDataException("The O-ring occurrence must reference a part document.");
                            part.FileName = (string)doc.FullName;
                            if (String.IsNullOrWhiteSpace(part.FileName)) throw new InvalidDataException("Save this part before using O-Ring Finder.");
                            if (!seen.Add(part.FileName)) { part.Dispose(); continue; }
                            ReadDimensions(part);
                        }
                        catch (Exception error) { part.Error = error.Message; }
                        result.Add(part);
                    }
                }
                return result;
            }
            catch { foreach (var part in result) part.Dispose(); throw; }
        }
        internal static void ReadDimensions(RingPart part)
        {
            decimal? diameter = null, width = null;
            using (var setsScope = new ComScope())
            {
                dynamic sets = setsScope.Own(((dynamic)part.Document).ProfileSets);
                int setCount = (int)sets.Count;
                for (int s = 1; s <= setCount; s++)
                using (var setScope = new ComScope())
                {
                    dynamic set = setScope.Own(sets.Item(s));
                    dynamic profiles = setScope.Own(set.Profiles);
                    int profileCount = (int)profiles.Count;
                    for (int p = 1; p <= profileCount; p++)
                    using (var profileScope = new ComScope())
                    {
                        dynamic profile = profileScope.Own(profiles.Item(p));
                        dynamic dimensions = profileScope.Own(profile.Dimensions);
                        int dimensionCount = (int)dimensions.Count;
                        for (int d = 1; d <= dimensionCount; d++)
                        using (var dimScope = new ComScope())
                        {
                            dynamic dimension = dimScope.Own(dimensions.Item(d));
                            string name = (string)dimension.DisplayName;
                            if (!String.Equals(name, "ID", StringComparison.OrdinalIgnoreCase) && !String.Equals(name, "WD", StringComparison.OrdinalIgnoreCase)) continue;
                            // Solid Edge internal length units are metres. Use the
                            // exact inch conversion instead of the legacy 39.3701.
                            decimal inches = Convert.ToDecimal((object)dimension.Value, CultureInfo.InvariantCulture) / .0254m;
                            if (inches <= 0) throw new InvalidDataException(name + " must be a positive dimension.");
                            if (String.Equals(name, "ID", StringComparison.OrdinalIgnoreCase)) SetDimension(ref diameter, inches, "ID");
                            else SetDimension(ref width, inches, "WD");
                        }
                    }
                }
            }
            if (!diameter.HasValue || !width.HasValue) throw new InvalidDataException("Missing ID and/or WD profile dimensions.");
            part.Diameter = diameter.Value; part.GrooveWidth = width.Value;
        }
        private static void SetDimension(ref decimal? slot, decimal value, string name)
        {
            if (slot.HasValue && Math.Abs(slot.Value - value) > .00000001m)
                throw new InvalidDataException("Conflicting dimensions named " + name + ". Give the intended dimension a unique name.");
            slot = value;
        }
        internal static void Apply(RingPart part, RingSize size, bool highPressure)
        {
            if (part.Document == null || part.Error != null) throw new InvalidOperationException("Reload and select a valid O-ring part first.");
            dynamic doc = part.Document;
            if (!String.Equals((string)doc.FullName, part.FileName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("The part was renamed. Reload before applying a size.");
            using (var scope = new ComScope())
            {
                // Resolve both existing properties before writing either. Missing
                // BOM fields are reported instead of silently adding new fields.
                dynamic sets = scope.Own(doc.Properties);
                dynamic custom = scope.Own(sets.Item("Custom"));
                dynamic drawing = scope.Own(custom.Item("drawing number"));
                dynamic stock = scope.Own(custom.Item("dave_b"));
                object oldDrawing = drawing.Value, oldStock = stock.Value;
                try
                {
                    drawing.Value = RingTable.DrawingNumber(size, highPressure);
                    stock.Value = RingTable.StockNumber(size, highPressure);
                    // Save the document once after both values are assigned. This
                    // also saves any other unsaved edits, as the UI explicitly says.
                    doc.Save();
                }
                catch (Exception error)
                {
                    string rollback = "Previous property values were restored in memory; verify the part before saving.";
                    try { drawing.Value = oldDrawing; stock.Value = oldStock; }
                    catch (Exception restore) { rollback = "Restoring previous properties also failed: " + restore.Message; }
                    throw new InvalidOperationException("Could not apply and save the O-ring properties. " + error.Message + "\r\n" + rollback, error);
                }
            }
        }
        public void Dispose() { ComScope.Release(application); application = null; }
    }
}
