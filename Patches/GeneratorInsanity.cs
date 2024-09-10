using HarmonyLib;
using static ITP_Insanity.Patches.OswaldInsanityController;
using static ITP_Insanity.InsanityMod;

namespace ITP_Insanity.Patches
{
    [HarmonyPatch(typeof(GeneratorSystem))]
    internal class GeneratorInsanity
    {
        public static GeneratorSystem generator = null;

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Init(ref GeneratorSystem __instance)
        {
            if (generator == null)
            {
                generator = __instance;
                generator.OnGeneratorShutdown.AddListener(InsanityShutdown);
                generator.OnGeneratorPowered.AddListener(InsanityEnabled);
                logger.Log("Added generator listeners!");
            }
        }

        [HarmonyPatch("Update")]
        [HarmonyPostfix]
        public static void Update()
        {
            if (generator == null)
            {
                return;
            }

            //logger.Log(string.Format("Timer: {0}, Max: {1}", GeneratorSystem.TripElapsed, GeneratorSystem.TripMaxElapsed));
        }

        public static void InsanityShutdown()
        {
            logger.Log("Generator shutdown!");

            // Don't want to activate the increase from the event
            if (!IsOswaldControllerValid() || fakeGeneratorFail.failActive)
                return;

            logger.Log("Insanity increase from generator shutdown!");
            IncreaseInsanity(generatorShutdownLoss);
        }

        public static void InsanityEnabled()
        {
            logger.Log("Generator enable!");

            if (!IsOswaldControllerValid() || fakeGeneratorFail.failActive)
                return;

            logger.Log("Insanity decrease from generator powered!");
            IncreaseInsanity(generatorEnabledLoss);
        }
    }
}
