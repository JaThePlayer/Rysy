namespace Rysy.Helpers;

public static class GCHelper {
    /// <summary>
    /// Causes a *very* aggresive GC run, which optimises memory as much as possible.
    /// </summary>
    public static void VeryAggressiveGC() {
        for (int i = 0; i < 2; i++) {
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Aggressive, true, true);

            GC.WaitForPendingFinalizers();
        }
    }
}
