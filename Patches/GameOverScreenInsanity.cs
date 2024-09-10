using HarmonyLib;

namespace ITP_Insanity.Patches
{
    [HarmonyPatch(typeof(GameOverScreen))]
    internal class GameOverScreenInsanity
    {
        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Init()
        {
            OswaldInsanityController.InitInsanity();
        }
    }
}
