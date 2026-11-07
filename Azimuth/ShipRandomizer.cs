using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Azimuth
{
    internal static class ShipRandomizer
    {
        internal static decimal[] ShieldCapacities(TypeTable shields)
        {
            var capacities = new decimal[shields.RecordLines.Count];
            for (int i = 0; i < capacities.Length; i++)
            {
                capacities[i] = TypeTable.Number(shields.Fields(i)[8]);
                if (capacities[i] <= 0 || (i > 0 && capacities[i] <= capacities[i - 1]))
                    throw TypeTable.Invalid(shields.Name, shields.RecordLines[i] + 1);
            }
            if (capacities.Length == 0) throw TypeTable.Invalid(shields.Name, 0);
            return capacities;
        }

        internal static string[] Randomize(TypeTable ships, decimal[] capacities,
            Random random, int range, int selectedRace, TypeTable existing, out int count)
        {
            var profiles = new Dictionary<int, ShipModifiers>();
            var replacements = new Dictionary<string, string>(StringComparer.Ordinal);
            count = 0;
            for (int i = 0; i < ships.RecordLines.Count; i++)
            {
                string[] fields = ships.Fields(i);
                int race = TypeTable.Integer(fields[45]);
                if (selectedRace != 0 && race != selectedRace) continue;
                ShipModifiers profile;
                if (!profiles.TryGetValue(race, out profile))
                {
                    profile = ShipModifiers.Create(random, range);
                    profiles.Add(race, profile);
                }
                replacements.Add(fields[fields.Length - 2], Adjust(fields, profile, capacities));
                count++;
            }
            if (count == 0) throw new InvalidDataException(X3Utils.Ui("Randomizer.NoShips"));

            // Always calculate from supplied defaults, never compound previous rolls.
            // A selected-race reroll preserves other races' existing overrides.
            TypeTable destination = selectedRace != 0 && existing != null ? existing : ships;
            string[] output = (string[])destination.Lines.Clone();
            int applied = 0;
            for (int i = 0; i < destination.RecordLines.Count; i++)
            {
                string[] fields = destination.Fields(i);
                string replacement;
                if (replacements.TryGetValue(fields[fields.Length - 2], out replacement))
                {
                    if (selectedRace != 0 && TypeTable.Integer(fields[45]) != selectedRace)
                        throw TypeTable.Invalid(destination.Name, destination.RecordLines[i] + 1);
                    output[destination.RecordLines[i]] = replacement;
                    applied++;
                }
                else if (selectedRace != 0 && TypeTable.Integer(fields[45]) == selectedRace)
                    throw TypeTable.Invalid(destination.Name, destination.RecordLines[i] + 1);
            }
            if (applied != count) throw TypeTable.Invalid(destination.Name, 0);
            return output;
        }

        internal static string Adjust(string[] original, ShipModifiers profile, decimal[] capacities)
        {
            string[] fields = (string[])original.Clone();
            ScaleInteger(fields, 7, profile.Speed);
            ScaleInteger(fields, 8, profile.Acceleration);
            for (int axis = 2; axis <= 4; axis++)
                fields[axis] = (TypeTable.Number(fields[axis]) * profile.Turning).ToString(CultureInfo.InvariantCulture);
            ScaleInteger(fields, 13, profile.Power);
            ScaleInteger(fields, 46, profile.Hull);

            // Preserve the existing TL exemption so station cargo still fits.
            decimal cargo = fields[5].EndsWith("_TL", StringComparison.Ordinal) ? 1m : profile.Cargo;
            ScaleInteger(fields, 28, cargo);
            ScaleInteger(fields, 29, cargo);
            AdjustShields(fields, profile.Power, capacities);

            // Production uses rolled modifiers, not rounded shield capacity.
            // Movement and durability are averages; production weights sum to one.
            decimal movement = (profile.Speed + profile.Acceleration + profile.Turning) / 3m;
            decimal durability = (profile.Hull + profile.Power) / 2m;
            decimal production = movement * 0.4m + durability * 0.4m + cargo * 0.2m;
            ScaleInteger(fields, fields.Length - 10, production);
            ScaleInteger(fields, fields.Length - 6, production);
            return string.Join(";", fields);
        }

        internal static decimal AdjustShields(string[] fields, decimal modifier, decimal[] capacities)
        {
            int tier = TypeTable.Integer(fields[22]);
            int count = TypeTable.Integer(fields[23]);
            if (count < 0 || tier < 0 || tier >= capacities.Length || modifier <= 0)
                throw TypeTable.Invalid("TShips.txt", 0);
            if (count == 0) return 1m; // Do not invent shields for unshielded objects.
            decimal originalCapacity = capacities[tier] * count;
            decimal target = originalCapacity * modifier;
            int newCount = RoundedShieldCount(target, capacities[tier]);
            if (tier > MinimumShieldTier(fields[5], tier))
            {
                int lowerCount = RoundedShieldCount(target, capacities[tier - 1]);
                decimal currentError = Math.Abs(newCount * capacities[tier] - target);
                decimal lowerError = Math.Abs(lowerCount * capacities[tier - 1] - target);
                // Compare the current and immediately lower tier for either sign.
                // Keep the current tier on ties; never cascade through more tiers.
                if (lowerError < currentError)
                {
                    tier--;
                    newCount = lowerCount;
                }
            }
            fields[22] = tier.ToString(CultureInfo.InvariantCulture);
            fields[23] = newCount.ToString(CultureInfo.InvariantCulture);
            return newCount * capacities[tier] / originalCapacity;
        }

        // Called on bundled base data before changing the tier. Never infer the
        // floor from an earlier randomized override, which would permit tier drift.
        internal static int MinimumShieldTier(string shipClass, int baseTier)
        {
            int minimum;
            switch (shipClass)
            {
                case "SG_SH_M5":
                case "SG_SH_M4": minimum = 0; break; // 1 MJ
                case "SG_SH_M3":
                case "SG_SH_TS":
                case "SG_SH_TP": minimum = 1; break; // 5 MJ
                case "SG_SH_M8":
                    minimum = baseTier == 2 ? 1 : baseTier == 3 ? 2 : baseTier;
                    break;
                case "SG_SH_M6":
                case "SG_SH_TM": minimum = 2; break; // 25 MJ
                case "SG_SH_M7":
                case "SG_SH_TL": minimum = 3; break; // 200 MJ
                case "SG_SH_M1": minimum = baseTier == 5 ? 4 : 3; break;
                case "SG_SH_M2": minimum = 4; break; // 1 GJ
                default: minimum = Math.Max(0, baseTier - 1); break; // Special objects: one tier below base.
            }
            // Do not silently upgrade an unusual base ship already below its floor.
            return Math.Min(baseTier, minimum);
        }

        private static int RoundedShieldCount(decimal target, decimal capacity)
        {
            return Math.Max(1, checked((int)Math.Round(target / capacity,
                0, MidpointRounding.AwayFromZero)));
        }

        private static void ScaleInteger(string[] fields, int index, decimal modifier)
        {
            decimal original = TypeTable.Number(fields[index]);
            // Keep existing zero values, and keep positive attributes usable.
            decimal value = original * modifier;
            fields[index] = TypeTable.Rounded(original > 0 ? Math.Max(1m, value) : value);
        }
    }
}
