using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Diagnostics;
using static ITP_Insanity.Patches.OswaldInsanity;
using static ITP_Insanity.Patches.SpringBonnieInsanity;
using static ITP_Insanity.InsanityMod;

namespace ITP_Insanity.Patches
{
    [HarmonyPatch(typeof(OswaldController))]
    internal class OswaldInsanityController
    {
        public static float insanityMeter = 0f;
        public static float luredBonnieCurrentLoss = luredBonnieLoss;

        public static bool giveBonnieLureInsanity = true;

        public static float discordCheckTimer = 0f;

        public static OswaldController oswaldController = null;

        public static AudioSource source;

        public static float Remap(float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            float oldRange = oldMax - oldMin;
            float newRange = newMax - newMin;

            return (((value - oldMin) * newRange) / oldRange) + newMin;
        }

        public static void SanityCap()
        {
            insanityMeter = Mathf.Clamp(insanityMeter, insanityMin, insanityMax);
        }

        public static void IncreaseInsanity(float amount)
        {
            insanityMeter += amount;
            SanityCap();
        }

        public static void InitInsanity()
        {
            insanityMeter = 0f;
            InitInsanityEvents();
        }

        public static void InitInsanityEvents()
        {
            doorKnockSound.Init();
            weirdSounds.Init();
            lightFlicker.Init();
            lightFlicker.InitEvent();
            discordLmao.Init();
            fakeDoorSound.Init();
            fakeBonnieFootstep.Init();
            fakeGeneratorFail.Init();
            fakeGeneratorFail.InitEvent();
            fakeDoorBang.Init();
            aggressiveRunning.Init();
            fakeBonnieMusic.Init();
            terrifedOswald.InitEvent();
            maxInsanityMusic.InitEvent();

            discordCheckTimer = 0f;
            luredBonnieCurrentLoss = luredBonnieLoss;

            giveBonnieLureInsanity = true;
        }

        public static void DisableInsanityEvents()
        {
            InitInsanityEvents();

            //Oswald.SetOswaldTerrifiedState(false);

            //source.Stop();
        }

        [HarmonyPatch("Start")]
        [HarmonyPostfix]
        public static void Init(ref OswaldController __instance)
        {
            if (oswaldController == null)
            {
                oswaldController = __instance;
                source = oswaldController.GetOrAddComponent<AudioSource>();
            }
        }

        public static bool IsOswaldControllerValid()
        {
            if (oswaldController == null)
                return false;

            if (!oswaldController.isActiveAndEnabled)
                return false;

            if (!SingletonMB<BonnieController>.Instance)
                return false;

            if (!IsSceneValid())
                return false;

            // attempt to disable insanity stuff while interacting... didn't work

            /*BonnieController bonnie = SingletonMB<BonnieController>.Instance;

            if (oswaldController.interacting)
            {
                if (!bonnie.IsSameRoom)
                    return false;
            }*/

            return true;
        }

        public static bool IsSceneValid()
        {
            if (IsBannedScene())
                return false;

            string sceneName = SceneManager.GetActiveScene().name;

            bool night = sceneName.Contains("Night");
            bool house = sceneName.Contains("OSH");
            bool outside = sceneName.Contains("Outside");

            bool oshNight = house && night;

            if (!oshNight || outside)
            {
                return true;
            }

            return false;
        }

        public static bool IsBannedScene()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            foreach (string curString in bannedNights)
            {
                if (sceneName.Contains(curString))
                {
                    //logger.Log("banned nighjt!");
                    return true;
                }
            }

            return false;
        }

        public static void UpdateDiscordLmao()
        {
            discordCheckTimer += Time.deltaTime;

            if (discordCheckTimer >= 5)
            {
                discordCheckTimer = 0f;

                if (Process.GetProcessesByName("Discord").Length >= 1)
                {
                    discordLmao.Enable();
                }
                else
                {
                    discordLmao.Disable();
                }
            }

            discordLmao.Update();
        }

        public static float GetDifficultyMultiplier(string difficulty)
        {
            int index = 0;

            switch (difficulty) 
            {
                case "Frightening":
                    index = 1;
                    break;

                case "Terrifying":
                    index = 2;
                    break;

                case "Nightmare":
                    index = 3;
                    break;

                default:
                    index = 0;
                    break;
            }

            return sanityLossMultiplier[index];
        }

        [HarmonyPatch("Update")]
        [HarmonyPrefix]
        public static void Update()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            bool pizzera = sceneName.Contains("PIZ");
            bool night = sceneName.Contains("Night");
            bool past = sceneName.Contains("Past");
            bool house = sceneName.Contains("OSH");
            bool outside = sceneName.Contains("Outside");

            string difficulty = GameSave.Current.Difficulty_Level.ToString();

            float multiplier = GetDifficultyMultiplier(difficulty);

            //logger.Log("Scene name: " + sceneName);

            OzUpdate();

            if (IsBannedScene())
            {
                InitInsanity();
                DisableInsanityEvents();
                return;
            }
            
            if (pizzera)
            {
                if (!past)
                {
                    DisableInsanityEvents();
                    IncreaseInsanity(jeffsCooloff * Time.deltaTime);
                    return;
                }
            }
            else
            {
                bool oshNight = house && night;
                if (!oshNight || outside)
                {
                    InitInsanity();
                    DisableInsanityEvents();
                    return;
                }
            }

            if (oswaldController == null)
                return;

            if (!oswaldController.isActiveAndEnabled)
                return;

            bool bonnieExists = SingletonMB<BonnieController>.Instance;
            
            if (!bonnieExists)
            {
                InitInsanity();
                OswaldInsanity.InitInsanityEvents();
                return;
            }

            BonnieController bonnie = SingletonMB<BonnieController>.Instance;

            /*if (oswaldController.interacting)
            {
                if (!bonnie.IsSameRoom)
                    return;
            }*/

            IncreaseInsanity((sanityLoss * multiplier) * Time.deltaTime);

            bool bonnieChasing = SingletonMB<BonnieController>.Instance.IsChasingMode;
            bool noiseReachedBonnie = SingletonMB<NoiseSystem>.Instance.NoiseReachedBonnie;

            if (noiseReachedBonnie && !bonnieChasing)
            {
                if (giveBonnieLureInsanity)
                {
                    logger.Log("Applied lure insanity");

                    giveBonnieLureInsanity = false;
                    IncreaseInsanity(luredBonnieCurrentLoss);
                    luredBonnieCurrentLoss = luredBonnieCurrentLoss / luredBonnieDecay;

                    logger.Log("New current lure insanity gain: " + luredBonnieCurrentLoss.ToString());
                }
            }
            else
            {
                giveBonnieLureInsanity = true;
            }

            if (bonnie.IsSameRoom)
            {
                float divide = 1f;

                if (oswald.IsHiding)
                {
                    if (!bonnieChasing)
                    {
                        divide = withBonnieHiding;
                    }
                }

                IncreaseInsanity((withBonnieLoss / divide) * Time.deltaTime);
            }

            if (oswald.currentRoom == SingletonMB<NoiseSystem>.Instance.NoisyRoom)
            {
                if (!oswald.IsHiding)
                {
                    IncreaseInsanity(noisyRoomLoss * Time.deltaTime);
                }
            }

            if (oswald.IsMoving)
            {
                if (oswald.IsRunning)
                {
                    IncreaseInsanity(runningLoss * Time.deltaTime);
                }
                else
                {
                    if (!bonnieChasing)
                    {
                        IncreaseInsanity(walkingLoss * Time.deltaTime);
                    }
                }

            }

            luredBonnieCurrentLoss += (luredBonnieLoss / luredBonnieDecayRecover) * Time.deltaTime;
            luredBonnieCurrentLoss = Mathf.Clamp(luredBonnieCurrentLoss, 0, luredBonnieLoss);

            //logger.Log("INSANITY: " + insanityMeter.ToString());

            UpdateSanityEvents();
        }
    }
}
