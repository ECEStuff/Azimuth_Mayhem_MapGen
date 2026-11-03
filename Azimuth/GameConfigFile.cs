using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Azimuth
{
    internal sealed class GameConfigFile
    {
        private readonly byte[] original;
        private readonly string source;
        private readonly int[] lineStarts;
        private readonly bool hasBom;
        private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);
        public readonly string PathName;
        public readonly string Page;
        public readonly List<XElement> Entries;
        public readonly Dictionary<string, string> Sections = new Dictionary<string, string>();

        public GameConfigFile(string path, string page)
        {
            PathName = path;
            Page = page;
            original = File.ReadAllBytes(path);
            hasBom = original.Take(3).SequenceEqual(Encoding.UTF8.GetPreamble());
            source = Utf8.GetString(original, hasBom ? 3 : 0, original.Length - (hasBom ? 3 : 0));
            var starts = new List<int> { 0 };
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == '\r' && i + 1 < source.Length && source[i + 1] == '\n') i++;
                if (source[i] == '\r' || source[i] == '\n') starts.Add(i + 1);
            }
            lineStarts = starts.ToArray();
            XDocument doc = XDocument.Parse(source, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var settings = doc.Root.Elements("page").SingleOrDefault(p => (string)p.Attribute("id") == page);
            if (doc.Root.Name != "language" || settings == null) throw new InvalidDataException("Missing configuration page " + page);
            Entries = settings.Elements("t").Where(IsSetting).ToList();
            if (Entries.Count == 0 || Entries.GroupBy(Id).Any(group => group.Count() > 1))
                throw new InvalidDataException("Missing settings or duplicate setting IDs on page " + page);
            string section = null;
            foreach (var node in settings.Nodes())
            {
                var heading = node as XComment;
                var before = node.PreviousNode as XText;
                if (heading != null && (node.PreviousNode == null || (before != null && before.Value.Contains("\n"))))
                {
                    section = heading.Value.Trim();
                }
                var entry = node as XElement;
                if (entry != null && entry.Name == "t" && IsSetting(entry))
                    Sections.Add(Id(entry), section);
            }
        }

        public static string Id(XElement entry) => (string)entry.Attribute("id");

        private bool IsSetting(XElement entry)
        {
            int id;
            if (!int.TryParse(Id(entry), out id) || id < 0 || id >= 1000 || entry.HasElements) return false;
            return (Page == "9973" && id == 71) || IsInteger(entry.Value);
        }

        private static bool IsInteger(string value)
        {
            int number;
            return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out number);
        }

        public void Validate(XElement entry, string value)
        {
            XmlConvert.VerifyXmlChars(value);
            if (!(Page == "9973" && Id(entry) == "71") && !IsInteger(value))
                throw new InvalidDataException("Setting " + Id(entry) + " requires a whole number.");
        }

        public static string Description(XElement entry)
        {
            var next = entry.NextNode;
            var whitespace = next as XText;
            if (whitespace != null && !whitespace.Value.Contains("\n") && string.IsNullOrWhiteSpace(whitespace.Value))
                next = next.NextNode;
            var comment = next as XComment;
            return comment == null ? "" : comment.Value.Trim();
        }

        public void Save(Dictionary<string, string> values)
        {
            // Work backwards in the original UTF-8 text. Re-serializing the document would
            // also rewrite unrelated declarations, entity spellings and line endings.
            var output = new StringBuilder(source);
            foreach (var entry in Enumerable.Reverse(Entries))
            {
                string value = values[Id(entry)];
                if (value == entry.Value) continue;
                Validate(entry, value);
                var info = (IXmlLineInfo)entry;
                int tag = lineStarts[info.LineNumber - 1] + info.LinePosition - 2;
                int start = source.IndexOf('>', tag) + 1;
                int end = source.IndexOf('<', start);
                if (start <= tag || end < start || source[tag] != '<' ||
                    string.CompareOrdinal(source, end, "</t>", 0, 4) != 0 ||
                    source[start - 2] == '/')
                    throw new InvalidDataException("Unsupported XML layout for setting " + Id(entry));
                string escaped = value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
                output.Remove(start, end - start).Insert(start, escaped);
            }

            string temp = Path.Combine(Path.GetDirectoryName(PathName), Guid.NewGuid().ToString("N") + ".tmp");
            try
            {
                var rendered = output.ToString();
                var parsed = XDocument.Parse(rendered, LoadOptions.PreserveWhitespace);
                var page = parsed.Root.Elements("page").Single(p => (string)p.Attribute("id") == Page);
                foreach (var t in page.Elements("t"))
                {
                    string expected;
                    if (values.TryGetValue(Id(t), out expected) && t.Value != expected)
                        throw new InvalidDataException("Could not verify setting " + Id(t));
                }
                File.WriteAllText(temp, rendered, new UTF8Encoding(hasBom, true));
                // Serialize comparison and replacement across Azimuth processes.
                bool ownsLock;
                using (var gate = new System.Threading.Mutex(true, @"Local\AzimuthConfigWrite", out ownsLock))
                {
                    if (!ownsLock) throw new IOException("Another editor is saving a configuration. Try again.");
                    try
                    {
                        using (var guard = new FileStream(PathName, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete))
                        using (var bytes = new MemoryStream())
                        {
                            guard.CopyTo(bytes);
                            if (!original.SequenceEqual(bytes.ToArray()))
                                throw new IOException("The configuration changed on disk. Reopen it before saving.");
                            File.Replace(temp, PathName, null);
                        }
                    }
                    finally { gate.ReleaseMutex(); }
                }
            }
            finally
            {
                if (File.Exists(temp)) File.Delete(temp);
            }
        }
    }
}
