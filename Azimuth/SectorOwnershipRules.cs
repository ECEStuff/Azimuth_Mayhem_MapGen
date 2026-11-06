using System;

namespace Azimuth
{
    // Raw owner/company IDs are part of the exported t-file format.
    internal static class SectorOwnershipRules
    {
        internal static bool IsFaction(int owner) { return (owner >= 1 && owner <= 5) || owner == 18; }
        internal static bool ValidDejure(int owner, int dejure)
        { return dejure == 0 || (IsFaction(owner) && dejure == owner); }
        internal static int CompanyForOwner(int owner)
        {
            switch (owner)
            {
                case 1: return 0; // Argon: Markus
                case 2: return 1; // Boron: Oceana
                case 3: return 3; // Split: Thurok (not company 2)
                case 4: return 2; // Paranid: Pontifex
                case 5: return 4;
                case 18: return 5;
                default: return -1;
            }
        }
        internal static bool ValidCompany(int owner, int company)
        {
            if (company == -1) return true;
            return owner == 14 ? company >= 0 && company <= 5
                : IsFaction(owner) && company == CompanyForOwner(owner);
        }
        internal static bool ValidXenonCount(int total, int xenon, bool peaceful, bool chaotic, int percent)
        {
            if (total < 1 || xenon < 0 || xenon > total) return false;
            if (peaceful) return xenon == 1;
            int requiredPercent = chaotic ? 50 : percent;
            if (requiredPercent < 0 || requiredPercent > 100) return false;
            return xenon >= ((long)total * requiredPercent + 99) / 100;
        }
    }
}
