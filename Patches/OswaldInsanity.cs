using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ITP_Insanity.InsanityMod;
using static ITP_Insanity.Patches.OswaldInsanityController;

namespace ITP_Insanity.Patches
{
    [HarmonyPatch(typeof(Oswald))]
    internal class OswaldInsanity
    {
        public static float noMoveTimer = 0f;

        public static Oswald oswald = null;

        public static AudioSource source = null;

        public static void InitInsanityEvents()
        {
            noMoveTimer = 0f;
        }

        public static void DisableInsanityEvents()
        {
            InitInsanityEvents();
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Init(ref Oswald __instance)
        {
            InitInsanityEvents();
            if (oswald == null)
            {
                oswald = __instance;
                source = oswald.GetOrAddComponent<AudioSource>();
            }
        }

        public static bool IsOswaldValid()
        {
            if (oswald == null)
                return false;

            if (!oswald.isActiveAndEnabled)
                return false;

            if (!oswald.CanMove)
                return false;

            if (!SingletonMB<BonnieController>.Instance)
                return false;

            if (!IsSceneValid())
                return false;

            /*BonnieController bonnie = SingletonMB<BonnieController>.Instance;

            if (oswaldController.interacting)
            {
                if (!bonnie.IsSameRoom)
                    return false;
            }*/

            return true;
        }

        /*[HarmonyPatch("Update")]
        [HarmonyPrefix]*/
        public static void OzUpdate()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            bool pizzera = sceneName.Contains("PIZ");
            bool night = sceneName.Contains("Night");
            bool past = sceneName.Contains("Past");
            bool house = sceneName.Contains("OSH");
            bool outside = sceneName.Contains("Outside");

            if (IsBannedScene())
            {
                DisableInsanityEvents();
                return;
            }

            if (pizzera)
            {
                if (!past)
                {
                    DisableInsanityEvents();
                    return;
                }
            }
            else
            {
                bool oshNight = house && night;
                if (!oshNight || outside)
                {
                    DisableInsanityEvents();
                    return;
                }
            }

            if (oswald == null)
                return;

            if (!oswald.isActiveAndEnabled)
                return;

            bool bonnieExists = SingletonMB<BonnieController>.Instance;

            // i don't think this actually works
            if (!bonnieExists)
                return;

            /*BonnieController bonnie = SingletonMB<BonnieController>.Instance;

            if (oswaldController.interacting)
            {
                if (!bonnie.IsSameRoom)
                    return;
            }*/

            if (!oswald.CanMove)
            {
                noMoveTimer = 0f;
                return;
            }

            if (!oswald.IsMoving)
                noMoveTimer += Time.deltaTime;
            else
                noMoveTimer = 0f;

            if (noMoveTimer >= waitingLossTimer)
            {
                IncreaseInsanity(waitingLoss * Time.deltaTime);
            }

            if (!SingletonMB<LocationManager>.Instance.CurrentRoom.IsRoomLit())
            {
                if (!oswald.FlashtlightEquipped)
                {
                    IncreaseInsanity(darknessLoss * Time.deltaTime);
                }
            }

            if (oswald.FlashtlightEquipped)
                IncreaseInsanity(flashlightLoss * Time.deltaTime);
        }

        public static void UpdateSanityEvents()
        {
            doorKnockSound.Update();

            weirdSounds.Update();

            lightFlicker.Update();

            UpdateDiscordLmao();

            fakeDoorSound.Update();

            fakeBonnieFootstep.Update();

            fakeGeneratorFail.Update();

            fakeDoorBang.Update();

            aggressiveRunning.Update();

            fakeBonnieMusic.Update();

            maxInsanityMusic.Update();

            if (!IsOswaldValid())
                return;

            terrifedOswald.Update();
        }

        [HarmonyPatch("OnActionOver")]
        [HarmonyPostfix]
        public static void OnActionOver()
        {
            if (!IsOswaldValid())
                return;

            if (oswald.TripState == Oswald.TripStates.Hold)
            {
                logger.Log("Tripped! Increasing insanity...");
                IncreaseInsanity(tripLoss);
            }
        }
    }
}
