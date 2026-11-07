using System;
using System.Collections.Generic;

namespace Azimuth
{
    internal static class WeaponRandomizer
    {
        // Manufacturing groups, not trading availability. FBC remains Argon and
        // PAA remains Split even though Terran trading stations also sell them.
        private static readonly string[][] Groups = {
            new[] { "PRG", "CFA", "FBC" },
            new[] { "ID", "IPG", "IC" },
            new[] { "FBL", "PSG", "TBC" },
            new[] { "PBG", "ISR", "PALC", "PAA" },
            new[] { "EBC", "FAA", "GC" },
            new[] { "APC", "EMPC", "PG", "SSC", "PSP" },
            new[] { "AKE", "BKE", "GKE" },
            new[] { "PBC" },
            new[] { "HEPT", "IRE", "MD", "PAC", "PBE", "CIG", "GRC", "MAML", "IBL", "PPC" }
        };

        internal static int Group(string laserId)
        {
            for (int group = 0; group < Groups.Length; group++)
                foreach (string suffix in Groups[group])
                    if (laserId == "SS_LASER_" + suffix) return group;
            return -1; // Tools, mounted railgun and unrecognized records stay unchanged.
        }

        internal static string[] Randomize(TypeTable bullets, TypeTable lasers,
            Random random, int range, out int count, out string[] laserOutput)
        {
            var bulletGroups = new Dictionary<int, int>();
            var protectedBullets = new HashSet<int>();
            for (int i = 0; i < lasers.RecordLines.Count; i++)
            {
                string[] fields = lasers.Fields(i);
                int bullet = TypeTable.Integer(fields[9]);
                if (bullet < 0 || bullet >= bullets.RecordLines.Count)
                    throw TypeTable.Invalid(lasers.Name, lasers.RecordLines[i] + 1);
                int group = Group(fields[fields.Length - 2]);
                if (group < 0) { protectedBullets.Add(bullet); continue; }
                int prior;
                if (bulletGroups.TryGetValue(bullet, out prior) && prior != group)
                    throw TypeTable.Invalid(lasers.Name, lasers.RecordLines[i] + 1);
                bulletGroups[bullet] = group;
            }

            var modifiers = new Dictionary<int, WeaponModifiers>();
            var modifiedBullets = new HashSet<int>();
            laserOutput = (string[])lasers.Lines.Clone();
            string[] output = (string[])bullets.Lines.Clone();
            count = 0;
            for (int i = 0; i < bullets.RecordLines.Count; i++)
            {
                string[] fields = bullets.Fields(i);
                string id = fields[fields.Length - 2];
                // Also protect by bullet ID, independently of TLaser mappings.
                if (id == "SS_BULLET_RL" || id == "SS_BULLET_MDS" || id == "SS_BULLET_RG" ||
                    protectedBullets.Contains(i)) continue;
                int group;
                if (!bulletGroups.TryGetValue(i, out group)) continue;
                WeaponModifiers modifier;
                if (!modifiers.TryGetValue(group, out modifier))
                {
                    modifier = WeaponModifiers.Create(random, range);
                    modifiers.Add(group, modifier);
                }
                output[bullets.RecordLines[i]] = AdjustBullet(fields, modifier);
                modifiedBullets.Add(i);
                count++;
            }
            // TLaser production values are separate from bullet damage. Adjust
            // only lasers whose bullets were actually modified, including aliases.
            for (int i = 0; i < lasers.RecordLines.Count; i++)
            {
                string[] fields = lasers.Fields(i);
                if (!modifiedBullets.Contains(TypeTable.Integer(fields[9]))) continue;
                WeaponModifiers modifier = modifiers[Group(fields[fields.Length - 2])];
                // TLaser stores milliseconds between shots, not shots per second.
                fields[7] = FiringDelay(fields[7], modifier.FireRate);
                Scale(fields, 14, modifier.Production); // NPC production relative value
                Scale(fields, 18, modifier.Production); // Player production relative value
                laserOutput[lasers.RecordLines[i]] = string.Join(";", fields);
            }
            return output;
        }

        internal static string AdjustBullet(string[] original, WeaponModifiers modifier)
        {
            string[] fields = (string[])original.Clone();
            Scale(fields, 7, modifier.Shield);
            Scale(fields, 37, modifier.Shield);
            Scale(fields, 22, modifier.Hull);
            Scale(fields, 38, modifier.Hull);

            decimal oldSpeed = TypeTable.Number(fields[11]);
            decimal oldLifetime = TypeTable.Number(fields[10]);
            if (oldSpeed < 0 || oldLifetime < 0) throw TypeTable.Invalid("TBullets.txt", 0);
            Scale(fields, 11, modifier.Speed);
            decimal newSpeed = TypeTable.Number(fields[11]);
            // Range is proportional to speed * lifetime. Use the rounded speed
            // to compensate lifetime so the two requested rolls remain independent.
            fields[10] = oldSpeed > 0
                ? TypeTable.Rounded(oldLifetime > 0
                    ? Math.Max(1m, oldLifetime * oldSpeed * modifier.Range / newSpeed) : 0m)
                : original[10]; // Zero-speed effects have no travel range to rescale.
            return string.Join(";", fields);
        }

        internal static string FiringDelay(string original, decimal rate)
        {
            decimal delay = TypeTable.Number(original);
            if (delay <= 0 || rate <= 0) throw TypeTable.Invalid("TLaser.txt", 0);
            return TypeTable.Rounded(Math.Max(1m, delay / rate));
        }

        private static void Scale(string[] fields, int index, decimal modifier)
        {
            decimal value = TypeTable.Number(fields[index]);
            fields[index] = TypeTable.Rounded(value > 0 ? Math.Max(1m, value * modifier) : value * modifier);
        }
    }
}
