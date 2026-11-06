using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Hammill.ORingFinder
{
    internal sealed class RingSize
    {
        public string Number;
        public decimal Width, InsideDiameter;
    }

    internal sealed class RingMatch
    {
        public RingSize Suggested, Smaller;
        public decimal PartDiameter;
        public string WidthLabel;
        // Preserve the original finder's displayed calculation. This is not the
        // alternative engineering stretch formula that divides by the ring ID.
        public decimal Difference(RingSize size)
        { return Math.Round((1m - size.InsideDiameter / PartDiameter) * 100m, 0, MidpointRounding.ToEven); }
    }

    internal sealed class RingTable
    {
        public readonly List<RingSize> Sizes;
        private RingTable(List<RingSize> sizes) { Sizes = sizes; }
        internal static decimal PositiveNumber(string value)
        {
            decimal result;
            if (!Decimal.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign,
                CultureInfo.InvariantCulture, out result) || result <= 0)
                throw new FormatException("Expected a positive inch value using a decimal point.");
            return result;
        }
        private static decimal Width(string value)
        {
            string[] pieces = value.Split('/');
            if (pieces.Length == 1) return PositiveNumber(value);
            if (pieces.Length == 2) return PositiveNumber(pieces[0]) / PositiveNumber(pieces[1]);
            throw new FormatException("Width must be a decimal or a fraction such as 1/16.");
        }
        public static RingTable Load(string path)
        {
            // Read the external file on every scan. Never fall back to an embedded
            // resource or an old cached table after an invalid/missing-file error.
            if (!File.Exists(path)) throw new FileNotFoundException("O-ring table not found. Keep Oring_table.txt beside the EXE, or select a table.\r\n" + path, path);
            if (new FileInfo(path).Length > 4000000) throw new InvalidDataException("O-ring table exceeds the 4 MB limit.");
            var sizes = new List<RingSize>();
            var numbers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dimensions = new HashSet<string>();
            int lineNumber = 0;
            foreach (string raw in File.ReadLines(path))
            {
                lineNumber++;
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                try
                {
                    var fields = line.Split(',').Select(x => x.Trim()).ToArray();
                    if (fields.Length != 3) throw new FormatException("Expected three comma-separated fields: part number, width, inside diameter.");
                    // The accepted part number also supplies the TORING suffix,
                    // so reject names that cannot preserve the legacy BOM format.
                    if (!Regex.IsMatch(fields[0], @"^2-\d{3,}$")) throw new FormatException("Part number must use the 2-001 format.");
                    var size = new RingSize { Number = fields[0], Width = Width(fields[1]), InsideDiameter = PositiveNumber(fields[2]) };
                    if (!numbers.Add(size.Number)) throw new FormatException("Duplicate part number: " + size.Number);
                    string key = size.Width.ToString("G29", CultureInfo.InvariantCulture) + "/" + size.InsideDiameter.ToString("G29", CultureInfo.InvariantCulture);
                    if (!dimensions.Add(key)) throw new FormatException("Duplicate width and inside diameter.");
                    sizes.Add(size);
                }
                catch (Exception error)
                {
                    if (!(error is FormatException) && !(error is OverflowException)) throw;
                    throw new InvalidDataException("Invalid O-ring table, line " + lineNumber + ": " + error.Message, error);
                }
            }
            if (sizes.Count == 0) throw new InvalidDataException("The O-ring table contains no size rows.");
            return new RingTable(sizes);
        }

        internal static string NominalWidth(decimal grooveWidth)
        {
            decimal three = Math.Round(grooveWidth, 3, MidpointRounding.ToEven), four = Math.Round(grooveWidth, 4, MidpointRounding.ToEven);
            // These are the five existing WD-to-nominal-width rules recovered
            // from SEAddIn.ORingFinder. They are not newly inferred groove sizes.
            if (three == .090m || three == .098m || four == .0955m) return "1/16";
            if (three == .120m || three == .145m || four == .1425m) return "3/32";
            if (three == .160m || three == .192m || four == .1895m) return "1/8";
            if (three == .240m || three == .286m || four == .2835m) return "3/16";
            if (three == .320m || three == .380m || four == .3775m) return "1/4";
            throw new InvalidDataException("WD does not match an existing O-ring width rule: " + grooveWidth.ToString("0.####", CultureInfo.InvariantCulture) + " in.");
        }
        public RingMatch Match(decimal partDiameter, decimal grooveWidth)
        {
            decimal diameter = Math.Round(partDiameter, 3, MidpointRounding.ToEven);
            if (diameter <= 0) throw new InvalidDataException("The ID dimension must be positive.");
            string widthLabel = NominalWidth(grooveWidth);
            decimal width = Width(widthLabel);
            // Sort/filter before taking predecessors: the old streaming search
            // could cross a width-group boundary and carry a stale suggestion.
            var group = Sizes.Where(x => x.Width == width).OrderBy(x => x.InsideDiameter).ToList();
            if (group.Count == 0) throw new InvalidDataException("No " + widthLabel + " rows exist in the table.");
            var eligible = group.Where(x => x.InsideDiameter <= diameter).ToList();
            if (eligible.Count == 0) throw new InvalidDataException("Part ID is smaller than the smallest " + widthLabel + " ring in the table.");
            return new RingMatch { PartDiameter = diameter, WidthLabel = widthLabel, Suggested = eligible[eligible.Count - 1],
                Smaller = eligible.Count > 1 ? eligible[eligible.Count - 2] : null };
        }
        internal static string DrawingNumber(RingSize size, bool highPressure)
        { return "PARKER# " + size.Number + (highPressure ? " P4300" : ""); }
        internal static string StockNumber(RingSize size, bool highPressure)
        { return "TORING" + size.Number.Substring(2) + (highPressure ? "-P4300" : ""); }
    }
}
