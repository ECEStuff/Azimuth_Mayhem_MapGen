using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Azimuth
{
    internal static class TypeOverrideFile
    {
        internal static string BackupName(string path)
        {
            // Active overrides live in <game>/addon/types. Match the Speech
            // archive's game-root backup directory, with a separate Types category.
            var typesFolder = new DirectoryInfo(Path.GetDirectoryName(Path.GetFullPath(path)));
            var addonFolder = typesFolder.Parent;
            if (addonFolder == null || addonFolder.Parent == null ||
                !string.Equals(typesFolder.Name, "types", StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(addonFolder.Name, "addon", StringComparison.OrdinalIgnoreCase))
                throw new IOException(X3Utils.Ui("Randomizer.InvalidOverridePath", path));
            string folder = Path.Combine(addonFolder.Parent.FullName, "Azimuth_Backups", "Types");
            Directory.CreateDirectory(folder);

            // Create timestamp.
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmssfff", System.Globalization.CultureInfo.InvariantCulture);
            string name = Path.GetFileNameWithoutExtension(path);
            string backup = Path.Combine(folder, $"{name}_{timestamp}.txt.bak");

            int suffix = 2;
            while (File.Exists(backup))
            {
                backup = Path.Combine(folder, $"{name}_{timestamp}_{suffix++}.txt.bak");
            }

            return backup;
        }

        internal static void Write(string path, string[] lines)
        {
            WriteBatch(new[] { path }, new[] { lines });
        }

        // Stage every result before replacing any active override. Roll back
        // completed replacements if a later file fails (not a power-loss journal).
        internal static void WriteBatch(string[] paths, string[][] contents)
        {
            if (paths.Length != contents.Length) throw new ArgumentException("contents");
            var temporary = new string[paths.Length];
            var backups = new string[paths.Length];
            var existed = new bool[paths.Length];
            int committed = 0;
            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    temporary[i] = paths[i] + "." + Guid.NewGuid().ToString("N") + ".tmp";
                    File.WriteAllLines(temporary[i], contents[i], new UTF8Encoding(false));
                    backups[i] = BackupName(paths[i]);
                }
                for (int i = 0; i < paths.Length; i++)
                {
                    existed[i] = File.Exists(paths[i]);
                    if (existed[i]) File.Replace(temporary[i], paths[i], backups[i]);
                    else File.Move(temporary[i], paths[i]);
                    committed++;
                }
            }
            catch (Exception failure)
            {
                var errors = new List<Exception> { failure };
                for (int i = committed - 1; i >= 0; i--)
                {
                    try
                    {
                        if (existed[i]) File.Copy(backups[i], paths[i], true);
                        else File.Delete(paths[i]);
                    }
                    catch (Exception rollbackFailure) { errors.Add(rollbackFailure); }
                }
                if (errors.Count > 1) throw new AggregateException(errors);
                throw;
            }
            finally
            {
                foreach (string path in temporary)
                    if (path != null && File.Exists(path)) File.Delete(path);
            }
        }

        internal static void RemoveOverrides(string folder)
        {
            string[] paths = { Path.Combine(folder, "TShips.txt"),
                Path.Combine(folder, "TBullets.txt"), Path.Combine(folder, "TLaser.txt") };
            var backups = new string[paths.Length];
            try
            {
                for (int i = 0; i < paths.Length; i++)
                {
                    if (!File.Exists(paths[i])) continue;
                    string backup = BackupName(paths[i]);
                    File.Move(paths[i], backup);
                    backups[i] = backup;
                }
            }
            catch (Exception failure)
            {
                var errors = new List<Exception> { failure };
                for (int i = paths.Length - 1; i >= 0; i--)
                {
                    if (backups[i] == null) continue;
                    try { File.Move(backups[i], paths[i]); }
                    catch (Exception rollbackFailure) { errors.Add(rollbackFailure); }
                }
                if (errors.Count > 1) throw new AggregateException(errors);
                throw;
            }
        }
    }
}
