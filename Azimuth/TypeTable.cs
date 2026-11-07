using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Azimuth
{
    // Keeps comments, blank lines and record order intact. References in TLaser are
    // zero-based record indices, not physical text-file line numbers.
    internal sealed class TypeTable
    {
        internal readonly string[] Lines;
        internal readonly List<int> RecordLines = new List<int>();
        internal readonly string Name;

        internal TypeTable(string name, string[] lines, int minimumFields)
        {
            Name = name;
            Lines = (string[])lines.Clone();
            int declared = -1;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < Lines.Length; i++)
            {
                string line = Lines[i].Trim().TrimStart('\uFEFF');
                if (line.Length == 0 || line.StartsWith("//", StringComparison.Ordinal)) continue;
                string[] fields = line.Split(';');
                if (declared < 0)
                {
                    int version;
                    if (fields.Length != 3 || fields[2] != "" ||
                        !int.TryParse(fields[0], out version) ||
                        !int.TryParse(fields[1], out declared) || declared < 0)
                        throw Invalid(name, i + 1);
                    continue;
                }
                if (fields.Length < minimumFields || fields[fields.Length - 1] != "" ||
                    !fields[fields.Length - 2].StartsWith("SS_", StringComparison.Ordinal) ||
                    !ids.Add(fields[fields.Length - 2])) throw Invalid(name, i + 1);
                RecordLines.Add(i);
            }
            if (declared != RecordLines.Count) throw Invalid(name, 0);
        }

        internal string[] Fields(int record)
        {
            return Lines[RecordLines[record]].Trim().Split(';');
        }

        internal static decimal Number(string value)
        {
            return decimal.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        internal static int Integer(string value)
        {
            return int.Parse(value, NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        internal static string Rounded(decimal value)
        {
            return checked((int)Math.Round(value, 0, MidpointRounding.AwayFromZero))
                .ToString(CultureInfo.InvariantCulture);
        }

        internal static InvalidDataException Invalid(string name, int line)
        {
            return new InvalidDataException(X3Utils.Ui("Randomizer.InvalidTable", name, line));
        }
    }
}
