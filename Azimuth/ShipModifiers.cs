using System;

namespace Azimuth
{
    // Six race-wide rolls. Power controls both shield generator and target capacity.
    // Yaw, pitch, and roll share Turning.
    internal sealed class ShipModifiers
    {
        internal decimal Speed = 1m;
        internal decimal Acceleration = 1m;
        internal decimal Turning = 1m;
        internal decimal Power = 1m;
        internal decimal Hull = 1m;
        internal decimal Cargo = 1m;

        internal static decimal Roll(Random random, int range)
        {
            if (range != 20 && range != 30) throw new ArgumentOutOfRangeException("range");
            // Include zero and small changes; the old +/-10% clamp biased zero upward.
            return (100m + random.Next(-range, range + 1)) / 100m;
        }

        internal static ShipModifiers Create(Random random, int range)
        {
            return new ShipModifiers {
                Speed = Roll(random, range), Acceleration = Roll(random, range),
                Turning = Roll(random, range), Power = Roll(random, range),
                Hull = Roll(random, range), Cargo = Roll(random, range)
            };
        }
    }
}
