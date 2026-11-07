using System;
using Azimuth;

internal static class OwnershipRulesTests
{
    private static int assertions;
    private static void Check(bool ok, string message)
    { assertions++; if (!ok) throw new Exception(message); }
    public static int Main()
    {
        int[] factions = { 1, 2, 3, 4, 5, 18 };
        int[] companies = { 0, 1, 3, 2, 4, 5 };
        for (int i = 0; i < factions.Length; i++)
        {
            int owner = factions[i];
            Check(SectorOwnershipRules.ValidDejure(owner, 0), "None de jure");
            Check(SectorOwnershipRules.ValidDejure(owner, owner), "Matching de jure");
            foreach (int other in factions)
                Check(SectorOwnershipRules.ValidDejure(owner, other) == (owner == other), "No foreign de jure");
            for (int company = -1; company <= 6; company++)
                Check(SectorOwnershipRules.ValidCompany(owner, company) == (company == -1 || company == companies[i]), "Matching company only");
        }
        foreach (int owner in new[] { 6, 14 })
        {
            Check(SectorOwnershipRules.ValidDejure(owner, 0), "Unowned de jure");
            foreach (int other in factions) Check(!SectorOwnershipRules.ValidDejure(owner, other), "Xenon/unknown de jure blocked");
        }
        for (int company = -1; company <= 6; company++)
        {
            Check(SectorOwnershipRules.ValidCompany(6, company) == (company == -1), "Xenon cannot host companies");
            Check(SectorOwnershipRules.ValidCompany(14, company) == (company >= -1 && company <= 5), "Unknown company choices");
        }
        Check(SectorOwnershipRules.ValidXenonCount(85, 1, true, false, 10), "Peaceful one");
        Check(!SectorOwnershipRules.ValidXenonCount(85, 0, true, false, 10), "Peaceful cannot remove last Xenon");
        Check(!SectorOwnershipRules.ValidXenonCount(85, 2, true, false, 10), "Peaceful cannot add second Xenon");
        Check(!SectorOwnershipRules.ValidXenonCount(85, 42, false, true, 3), "Chaotic odd-count ceiling");
        Check(SectorOwnershipRules.ValidXenonCount(85, 43, false, true, 3), "Chaotic exact allocation");
        Check(SectorOwnershipRules.ValidXenonCount(85, 44, false, true, 3), "Chaotic allows one extra Xenon");
        Check(SectorOwnershipRules.ValidXenonCount(84, 42, false, true, 3), "Even-count threshold");
        Check(!SectorOwnershipRules.ValidXenonCount(85, 8, false, false, 10), "Standard percentage ceiling");
        Check(SectorOwnershipRules.ValidXenonCount(85, 9, false, false, 10), "Standard exact allocation");
        Check(SectorOwnershipRules.ValidXenonCount(80, 8, false, false, 10), "Before non-Xenon addition");
        Check(!SectorOwnershipRules.ValidXenonCount(81, 8, false, false, 10), "Non-Xenon addition can violate floor");
        Check(!SectorOwnershipRules.ValidXenonCount(84, 8, false, false, 10), "Xenon removal changes both counts");
        Check(SectorOwnershipRules.ValidXenonCount(85, 3, false, false, 3), "Stored three percent");
        Check(!SectorOwnershipRules.ValidXenonCount(85, 3, false, false, 10), "Stored ten percent differs");
        Check(SectorOwnershipRules.ValidXenonCount(85, 10, false, false, 10), "Standard allows one extra Xenon");
        Check(SectorOwnershipRules.ValidXenonCount(84, 43, false, true, 3), "Non-Xenon removal uses Chaotic allowance");
        Check(SectorOwnershipRules.ValidXenonCount(86, 43, false, true, 3), "Non-Xenon addition preserves Chaotic quota");
        Check(SectorOwnershipRules.ValidXenonCount(86, 44, false, true, 3), "Xenon addition uses Chaotic allowance");
        Check(SectorOwnershipRules.ValidXenonCount(81, 9, false, false, 10), "Xenon addition meets new Standard quota");
        Check(SectorOwnershipRules.ValidXenonCount(80, 9, false, false, 10), "Non-Xenon removal uses Standard allowance");
        for (int total = 50; total <= 246; total++)
        {
            for (int percent = 3; percent <= 10; percent++)
            {
                int expected = (int)Math.Ceiling(total * percent / 100m);
                Check(SectorOwnershipRules.ValidXenonCount(total, expected, false, false, percent), "Standard rounded quota");
                Check(!SectorOwnershipRules.ValidXenonCount(total, expected - 1, false, false, percent), "Standard below quota");
                Check(SectorOwnershipRules.ValidXenonCount(total, expected + 1, false, false, percent), "Standard one extra allowed");
                Check(!SectorOwnershipRules.ValidXenonCount(total, expected + 2, false, false, percent), "Standard two extra blocked");
            }
            int chaotic = (int)Math.Ceiling(total / 2m);
            Check(SectorOwnershipRules.ValidXenonCount(total, chaotic, false, true, 3), "Chaotic rounded quota");
            Check(!SectorOwnershipRules.ValidXenonCount(total, chaotic - 1, false, true, 3), "Chaotic below quota");
            Check(SectorOwnershipRules.ValidXenonCount(total, chaotic + 1, false, true, 3), "Chaotic one extra allowed");
            Check(!SectorOwnershipRules.ValidXenonCount(total, chaotic + 2, false, true, 3), "Chaotic two extra blocked");
        }
        Console.WriteLine("Passed " + assertions + " assertions.");
        return 0;
    }
}
