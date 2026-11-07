using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Azimuth
{
    internal static class RandomizerTests
    {
        private static int assertions;
        private static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception(message);
        }

        private static TypeTable Read(string root, string name, int fields)
        {
            return new TypeTable(name, File.ReadAllLines(Path.Combine(root, name)), fields);
        }

        private static string[] Ship(int tier, int count)
        {
            var fields = Enumerable.Repeat("1000", 66).ToArray();
            fields[5] = "SG_SH_M3";
            fields[22] = tier.ToString(); fields[23] = count.ToString();
            fields[45] = "1"; fields[64] = "SS_SH_TEST"; fields[65] = "";
            return fields;
        }

        private static void ShieldTests(decimal[] capacities)
        {
            string[] mamba = Ship(2, 2);
            decimal ratio = ShipRandomizer.AdjustShields(mamba, .8m, capacities);
            Check(mamba[22] == "1" && mamba[23] == "8" && ratio == .8m, "Mamba: 50 MJ -> 40 MJ");
            string[] falcon = Ship(1, 8);
            ratio = ShipRandomizer.AdjustShields(falcon, 1.1m, capacities);
            Check(falcon[22] == "1" && falcon[23] == "9" && ratio == 1.125m, "M3 minimum tier: 40 MJ -> 45 MJ");
            foreach (decimal factor in new[] { .94m, 1.03m, 1.06m })
            {
                string[] sample = Ship(2, 2);
                ShipRandomizer.AdjustShields(sample, factor, capacities);
                string expectedTier = factor == 1.03m ? "2" : "1";
                string expectedCount = factor == .94m ? "9" : factor == 1.03m ? "2" : "11";
                Check(sample[22] == expectedTier && sample[23] == expectedCount, "50 MJ requested rounding example");
            }
            string[] scout = Ship(0, 5);
            ShipRandomizer.AdjustShields(scout, .9m, capacities);
            Check(scout[22] == "0" && scout[23] == "5", "Half rounds upward at lowest tier");
            scout = Ship(0, 1);
            ShipRandomizer.AdjustShields(scout, .7m, capacities);
            Check(scout[23] == "1", "At least one shield on shielded ships");
            string[] empty = Ship(0, 0);
            Check(ShipRandomizer.AdjustShields(empty, 1.3m, capacities) == 1m && empty[23] == "0", "No invented shields");

            string[] classes = { "SG_SH_M5", "SG_SH_M4", "SG_SH_M3", "SG_SH_TS", "SG_SH_TP",
                "SG_SH_M8", "SG_SH_M8", "SG_SH_M6", "SG_SH_M7", "SG_SH_TL", "SG_SH_M1", "SG_SH_M1", "SG_SH_M2", "SG_SH_TM", "SG_SH_GO", "SG_SH_GO" };
            int[] bases = { 1, 1, 2, 2, 2, 2, 3, 3, 4, 4, 4, 5, 5, 3, 5, 0 };
            int[] floors = { 0, 0, 1, 1, 1, 1, 2, 2, 3, 3, 3, 4, 4, 2, 4, 0 };
            for (int i = 0; i < classes.Length; i++)
            {
                Check(ShipRandomizer.MinimumShieldTier(classes[i], bases[i]) == floors[i], "Class minimum tier");
                string[] ship = Ship(bases[i], 2); ship[5] = classes[i];
                ShipRandomizer.AdjustShields(ship, .8m, capacities);
                Check(TypeTable.Integer(ship[22]) >= floors[i], "Conversion respects floor");
                Check(TypeTable.Integer(ship[22]) >= bases[i] - 1, "At most one tier below base");
                if (classes[i] == "SG_SH_M8" || classes[i] == "SG_SH_M1" || classes[i] == "SG_SH_GO") continue; // Their floor depends on original base.
                ship = Ship(floors[i], 2); ship[5] = classes[i];
                ShipRandomizer.AdjustShields(ship, .8m, capacities);
                Check(TypeTable.Integer(ship[22]) == floors[i], "Base at minimum retains tier");
            }
            for (int tier = 0; tier < capacities.Length; tier++)
            for (int slots = 1; slots <= 16; slots++)
            for (int percent = -30; percent <= 30; percent++)
            {
                string[] fields = Ship(tier, slots);
                decimal actual = ShipRandomizer.AdjustShields(fields, (100m + percent) / 100m, capacities);
                Check(TypeTable.Integer(fields[22]) <= tier && TypeTable.Integer(fields[23]) >= 1, "Valid shield configuration");
                Check(percent >= 0 || actual <= 1m, "Negative roll cannot raise shields");
                Check(percent <= 0 || actual >= 1m, "Positive roll cannot lower shields");
            }
        }

        private static void ProductionTests(decimal[] capacities)
        {
            string[] input = Ship(1, 8);
            string[] result = ShipRandomizer.Adjust(input,
                new ShipModifiers { Speed = .9m, Acceleration = .8m }, capacities).Split(';');
            Check(result[result.Length - 10] == "960" && result[result.Length - 6] == "960", "Movement uses average of .9, .8, and 1");
            Check(result[20] == input[20], "Weapon energy untouched");
            result = ShipRandomizer.Adjust(input, new ShipModifiers { Turning = .8m }, capacities).Split(';');
            Check(result[2] == "800.0" || TypeTable.Number(result[2]) == 800, "Turning changes axes");
            Check(result[result.Length - 10] == "973", "Turning contributes to movement average");
            result = ShipRandomizer.Adjust(input, new ShipModifiers { Power = 1.1m }, capacities).Split(';');
            Check(result[result.Length - 10] == "1020", "Durability averages hull and rolled generator modifier");
            string[] reduced = ShipRandomizer.Adjust(Ship(2, 2),
                new ShipModifiers { Power = .94m }, capacities).Split(';');
            Check(reduced[13] == "940" && reduced[22] == "1" && reduced[23] == "9", "Generator -6%, capacity rounded to 45 MJ");
            Check(reduced[reduced.Length - 10] == "988", "Production uses .94 generator, not .90 capacity");
            result = ShipRandomizer.Adjust(input, new ShipModifiers { Cargo = 1.2m }, capacities).Split(';');
            Check(result[28] == "1200" && result[result.Length - 10] == "1040", "Full cargo stat change, 20% cargo price weight");
            result = ShipRandomizer.Adjust(input, new ShipModifiers {
                Speed = 1.1m, Acceleration = 1.1m, Turning = 1.1m,
                Hull = .9m, Power = .9m, Cargo = .8m }, capacities).Split(';');
            Check(result[result.Length - 10] == "960" && result[result.Length - 6] == "960", "User example: 1.1/.9/.8 -> .96");
            foreach (decimal bound in new[] { .7m, .8m, 1.2m, 1.3m })
            {
                result = ShipRandomizer.Adjust(input, new ShipModifiers {
                    Speed = bound, Acceleration = bound, Turning = bound,
                    Hull = bound, Power = bound, Cargo = bound }, capacities).Split(';');
                Check(result[result.Length - 10] == TypeTable.Rounded(1000m * bound), "Ship weighted mean stays at common endpoint");
                Check(new WeaponModifiers { Hull = bound, Shield = bound,
                    FireRate = bound, Range = bound }.Production == bound, "Weapon mean stays at common endpoint");
            }
            input[5] = "SG_SH_TL";
            result = ShipRandomizer.Adjust(input, new ShipModifiers { Cargo = .7m }, capacities).Split(';');
            Check(result[28] == input[28] && result[29] == input[29] && result[result.Length - 10] == "1000", "TL cargo exemption also excluded from price");
        }

        private static void ShipTableTests(TypeTable ships, decimal[] capacities)
        {
            int count;
            string[] output = ShipRandomizer.Randomize(ships, capacities, new Random(123), 20, 0, null, out count);
            Check(count == ships.RecordLines.Count, "All ship records processed");
            var actual = new TypeTable("TShips.txt", output, 60);
            var expectedRandom = new Random(123);
            var profiles = new Dictionary<int, ShipModifiers>();
            for (int i = 0; i < ships.RecordLines.Count; i++)
            {
                string[] before = ships.Fields(i);
                int race = TypeTable.Integer(before[45]);
                if (!profiles.ContainsKey(race)) profiles.Add(race, ShipModifiers.Create(expectedRandom, 20));
                Check(output[ships.RecordLines[i]] == ShipRandomizer.Adjust(before, profiles[race], capacities), "Same profile shared by every ship of a race");
                Check(actual.Fields(i)[20] == before[20], "All weapon-energy values unchanged");
            }
            string[] selected = ShipRandomizer.Randomize(ships, capacities, new Random(456), 30, 1, actual, out count);
            for (int i = 0; i < ships.RecordLines.Count; i++)
                if (TypeTable.Integer(ships.Fields(i)[45]) != 1)
                    Check(selected[ships.RecordLines[i]] == output[ships.RecordLines[i]], "Selected-race reroll preserves other races");
            string[] again = ShipRandomizer.Randomize(ships, capacities, new Random(456), 30, 1,
                new TypeTable("TShips.txt", selected, 60), out count);
            Check(selected.SequenceEqual(again), "Same seed reroll does not compound");
            // Comments and blanks inside a table must not truncate subsequent records.
            var decorated = ships.Lines.ToList();
            decorated.Insert(ships.RecordLines[2], ""); decorated.Insert(ships.RecordLines[2], "// test comment");
            var table = new TypeTable("TShips.txt", decorated.ToArray(), 60);
            output = ShipRandomizer.Randomize(table, capacities, new Random(1), 20, 0, null, out count);
            Check(count == ships.RecordLines.Count && output.Contains("// test comment") && output.Contains(""), "Comments/blanks preserved");
        }

        private static void WeaponTests(TypeTable bullets, TypeTable lasers)
        {
            int count;
            string[] laserOutput;
            string[] result = WeaponRandomizer.Randomize(bullets, lasers, new Random(321), 20, out count, out laserOutput);
            Check(count == 35, "35 combat bullets, three excluded");
            var actual = new TypeTable("TBullets.txt", result, 50);
            var actualLasers = new TypeTable("TLaser.txt", laserOutput, 24);
            var modifiers = new Dictionary<int, WeaponModifiers>();
            var random = new Random(321);
            for (int i = 0; i < bullets.RecordLines.Count; i++)
            {
                string[] before = bullets.Fields(i), after = actual.Fields(i);
                int laserIndex = Enumerable.Range(0, lasers.RecordLines.Count)
                    .First(index => TypeTable.Integer(lasers.Fields(index)[9]) == i);
                string[] laser = lasers.Fields(laserIndex), newLaser = actualLasers.Fields(laserIndex);
                int group = WeaponRandomizer.Group(laser[laser.Length - 2]);
                if (group < 0)
                {
                    Check(result[bullets.RecordLines[i]] == bullets.Lines[bullets.RecordLines[i]], "Excluded bullet preserved exactly");
                    Check(laserOutput[lasers.RecordLines[laserIndex]] == lasers.Lines[lasers.RecordLines[laserIndex]], "Excluded laser preserved exactly");
                    continue;
                }
                if (!modifiers.ContainsKey(group)) modifiers.Add(group, WeaponModifiers.Create(random, 20));
                WeaponModifiers profile = modifiers[group];
                foreach (int field in new[] { 7, 37, 22, 38, 11 })
                {
                    decimal factor = field == 11 ? profile.Speed : field == 7 || field == 37 ? profile.Shield : profile.Hull;
                    decimal value = TypeTable.Number(before[field]);
                    Check(after[field] == TypeTable.Rounded(value > 0 ? Math.Max(1m, value * factor) : value * factor), "Shared group attribute modifier");
                }
                decimal speed = TypeTable.Number(before[11]), lifetime = TypeTable.Number(before[10]);
                if (speed > 0)
                {
                    decimal target = speed * lifetime * profile.Range;
                    decimal rangeError = Math.Abs(TypeTable.Number(after[11]) * TypeTable.Number(after[10]) - target);
                    Check(rangeError <= TypeTable.Number(after[11]) / 2m, "Range within half a lifetime unit after speed change");
                }
                for (int field = 0; field < before.Length; field++)
                    if (!new[] { 7, 10, 11, 22, 37, 38 }.Contains(field))
                        Check(before[field] == after[field], "Other bullet fields, including energy, unchanged");
                for (int field = 0; field < laser.Length; field++)
                {
                    string expected = laser[field];
                    if (field == 14 || field == 18)
                    {
                        decimal value = TypeTable.Number(expected);
                        expected = TypeTable.Rounded(value > 0 ? Math.Max(1m, value * profile.Production) : value * profile.Production);
                    }
                    if (field == 7) expected = TypeTable.Rounded(Math.Max(1m, TypeTable.Number(laser[7]) / profile.FireRate));
                    Check(newLaser[field] == expected, "Only laser production and firing delay change; energy unchanged");
                }
            }
            // Verify the generated profiles, not only manually supplied factors.
            foreach (int maximum in new[] { 20, 30 })
            {
                var generated = new Random(987);
                var raw = new Random(987);
                for (int sample = 0; sample < 1000; sample++)
                {
                    WeaponModifiers profile = WeaponModifiers.Create(generated, maximum);
                    decimal hull = 1m + (ShipModifiers.Roll(raw, maximum) - 1m) / 2m;
                    decimal shield = 1m + (ShipModifiers.Roll(raw, maximum) - 1m) / 2m;
                    decimal rangeFactor = 1m + (ShipModifiers.Roll(raw, maximum) - 1m) / 2m;
                    decimal speedFactor = 1m + (ShipModifiers.Roll(raw, maximum) - 1m) / 2m;
                    decimal fireRate = 1m + (ShipModifiers.Roll(raw, maximum) - 1m) / 2m;
                    Check(profile.Hull == hull && profile.Shield == shield && profile.FireRate == fireRate, "Damage and firing rate halved exactly");
                    Check(profile.Range == rangeFactor && profile.Speed == speedFactor, "Range/speed adjustments halved exactly");
                    Check(Math.Abs(profile.Range - 1m) <= maximum / 200m &&
                        Math.Abs(profile.Speed - 1m) <= maximum / 200m, "Half-range bounds");
                    Check(profile.Production == (((hull + shield) / 2m) + rangeFactor + fireRate) / 3m, "Production uses halved range factor");
                }
            }
            Check(WeaponRandomizer.FiringDelay("2200", 1.1m) == "2000", "Positive fire-rate roll reduces delay");
            Check(WeaponRandomizer.FiringDelay("1800", .9m) == "2000", "Negative fire-rate roll increases delay");
            Check(new WeaponModifiers { Hull = 1.1m, Shield = .9m, Range = 1.05m, FireRate = 1.1m }.Production == 1.05m, "Production includes firing-rate DPS factor");
            var example = new WeaponModifiers { Hull = 1.1m, Shield = .9m, Range = 1.2m, Speed = 1.3m };
            Check(example.Production == 3.2m / 3m, "Approved formula excludes speed");
            Check(new WeaponModifiers { Hull = 1.2m, Shield = 1.2m }.Production == 3.2m / 3m, "Damage factors averaged, not multiplied");
            string[] probe = bullets.Fields(0);
            probe[11] = "100000"; probe[10] = "1200";
            string[] changed = WeaponRandomizer.AdjustBullet(probe, new WeaponModifiers { Speed = 1.2m }).Split(';');
            Check(changed[11] == "120000" && changed[10] == "1000", "Speed rise compensated by shorter lifetime");
            probe[11] = "0";
            changed = WeaponRandomizer.AdjustBullet(probe, example).Split(';');
            Check(changed[11] == "0" && changed[10] == "1200", "Zero-speed effect retained");
            Check(WeaponRandomizer.Group("SS_LASER_FBC") == 0 && WeaponRandomizer.Group("SS_LASER_PAA") == 3, "Trade aliases ignored");
            Check(WeaponRandomizer.Group("SS_LASER_MAML") == 8 && WeaponRandomizer.Group("SS_LASER_PG") == 5, "Commonwealth MAML, Terran PG");
            string[] conflict = (string[])lasers.Lines.Clone();
            string[] argon = lasers.Fields(3); argon[9] = "0";
            conflict[lasers.RecordLines[3]] = string.Join(";", argon);
            bool rejected = false;
            try { WeaponRandomizer.Randomize(bullets, new TypeTable("TLaser.txt", conflict, 24), new Random(1), 20, out count, out laserOutput); }
            catch (InvalidDataException) { rejected = true; }
            Check(rejected, "Conflicting shared bullet rejected");
        }

        private static void FileTests()
        {
            string folder = Path.Combine(Path.GetTempPath(), "Azimuth-RandomizerTests-" + Guid.NewGuid().ToString("N"));
            string testRoot = folder;
            folder = Path.Combine(testRoot, "addon", "types");
            string backups = Path.Combine(testRoot, "Azimuth_Backups", "Types");
            Directory.CreateDirectory(folder);
            try
            {
                string path = Path.Combine(folder, "TShips.txt");
                File.WriteAllText(path, "original");
                TypeOverrideFile.Write(path, new[] { "replacement" });
                Check(File.ReadAllText(path).Trim() == "replacement", "Override replaced");
                Check(Directory.GetFiles(backups, "*.bak").Any(p => File.ReadAllText(p) == "original"), "Previous override backed up");
                string bulletPath = Path.Combine(folder, "TBullets.txt");
                string laserPath = Path.Combine(folder, "TLaser.txt");
                File.WriteAllText(bulletPath, "old bullets");
                // A directory at the second destination forces a mid-commit failure.
                Directory.CreateDirectory(laserPath);
                bool failed = false;
                try { TypeOverrideFile.WriteBatch(new[] { bulletPath, laserPath }, new[] { new[] { "new bullets" }, new[] { "new lasers" } }); }
                catch (IOException) { failed = true; }
                Check(failed && File.ReadAllText(bulletPath) == "old bullets", "Failed pair write rolls back first override");
                Directory.Delete(laserPath);
                TypeOverrideFile.WriteBatch(new[] { bulletPath, laserPath }, new[] { new[] { "new bullets" }, new[] { "new lasers" } });
                Check(File.ReadAllText(laserPath).Trim() == "new lasers", "Paired weapon overrides written");
                Check(Directory.GetFiles(folder, "*.bak").Length == 0, "No backups in active types folder");
                Check(Directory.GetFiles(folder, "*.tmp").Length == 0, "No staged files remain");
                TypeOverrideFile.RemoveOverrides(folder);
                Check(!File.Exists(path), "Restore removes active override");
                Check(!File.Exists(bulletPath) && !File.Exists(laserPath), "Restore also removes both weapon overrides");
                Check(Directory.GetFiles(backups, "*.bak").Length >= 4, "Recovery copies retained");
            }
            finally { Directory.Delete(testRoot, true); }
        }

        private static int Main(string[] args)
        {
            try
            {
                string root = args.Length > 0 ? args[0] : "..";
                // Catch culture-dependent formatting regressions.
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                decimal[] capacities = ShipRandomizer.ShieldCapacities(Read(root, "TShields.txt", 22));
                Check(capacities.SequenceEqual(new decimal[] { 1000, 5000, 25000, 200000, 1000000, 2000000 }), "Supplied shield capacities");
                ShieldTests(capacities); ProductionTests(capacities);
                ShipTableTests(Read(root, "TShips.txt", 60), capacities);
                WeaponTests(Read(root, "TBullets.txt", 50), Read(root, "TLaser.txt", 24));
                FileTests();
                Console.WriteLine("PASS: " + assertions + " assertions.");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
    }
}
