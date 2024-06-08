using HarmonyLib;

namespace PolyPlus {
    public class Patcher
    {
        public static void Load()
        {
            Harmony.CreateAndPatchAll(typeof(Patcher));
            Console.WriteLine("Load!");
        }
    }
}