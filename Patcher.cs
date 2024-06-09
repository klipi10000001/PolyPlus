using HarmonyLib;

namespace PolyPlus {
    public class PolyPlusPatcher
    {
        public static void Load()
        {
            Console.WriteLine("Loading PolyPlus...");
            Harmony.CreateAndPatchAll(typeof(PolyPlusPatcher));
            Console.WriteLine("PolyPlus Loaded!");
        }
    }
}