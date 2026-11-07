using System;

namespace Azimuth
{
    // Test-only stand-in: keeps the core tests independent of WinForms and JSON.
    internal static class X3Utils
    {
        internal static string Ui(string key, params object[] args)
        {
            return key + ": " + string.Join(", ", args);
        }
    }
}
