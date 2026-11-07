using System;

namespace Azimuth
{
    // One profile per manufacturing group, reused by all of its lasers/bullets.
    internal sealed class WeaponModifiers
    {
        internal decimal Hull = 1m;
        internal decimal Shield = 1m;
        internal decimal Range = 1m;
        internal decimal Speed = 1m;
        internal decimal FireRate = 1m;

        internal decimal Production
        {
            // Equal weight for average damage, firing rate, and range.
            // Speed and OOS damage add no extra production factor.
            get { return (((Hull + Shield) / 2m) + FireRate + Range) / 3m; }
        }

        internal static WeaponModifiers Create(Random random, int range)
        {
            return new WeaponModifiers {
                Hull = 1m + (ShipModifiers.Roll(random, range) - 1m) / 2m,
                Shield = 1m + (ShipModifiers.Roll(random, range) - 1m) / 2m,
                // Halve the signed adjustment, retaining half-percentage steps.
                Range = 1m + (ShipModifiers.Roll(random, range) - 1m) / 2m,
                Speed = 1m + (ShipModifiers.Roll(random, range) - 1m) / 2m,
                FireRate = 1m + (ShipModifiers.Roll(random, range) - 1m) / 2m
            };
        }
    }
}
