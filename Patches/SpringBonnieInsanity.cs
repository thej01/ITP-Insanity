using HarmonyLib;
using static ITP_Insanity.Patches.OswaldInsanityController;
using static ITP_Insanity.InsanityMod;

namespace ITP_Insanity.Patches
{
    [HarmonyPatch(typeof(SpringBonnie))]
    internal class SpringBonnieInsanity
    {
        public static SpringBonnie springBonnie = null;

        public static bool oldMatchingRoom = false;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Init(ref SpringBonnie __instance)
        {
            if (springBonnie == null)
            {
                springBonnie = __instance;
            }
        }
    }
}
