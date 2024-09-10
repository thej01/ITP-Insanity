using BepInEx.Logging;
using HarmonyLib.Tools;
using ITP_Insanity.Patches;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using static ITP_Insanity.Patches.GeneratorInsanity;

namespace ITP_Insanity.Events
{
    public class InsanityEvent : InsanityBehavior
    {
        public string name;

        public float insanityThreshold;

        public InsanityEvent(float insanityThreshold, string name = "unnamed event")
        {
            this.name = name;
            this.insanityThreshold = insanityThreshold;
        }
    }

    // Class that will start a timer that will end within a random range of time, and call a function once it finishes.
    public class InsanityTimerEvent : InsanityEvent
    {
        public RandomTimer timer;

        public virtual void Init()
        {
            timer.Init();
        }

        public InsanityTimerEvent(float insanityThreshold, float timeQuota, string name = "unnamed event") :
            base(insanityThreshold, name)
        {
            timer = new RandomTimer(timeQuota, timeQuota, OnTimerEnd);

            Init();
        }

        public InsanityTimerEvent(float insanityThreshold, float timeQuotaMin, float timeQuotaMax, string name = "unnamed event") :
            base(insanityThreshold, name)
        {
            timer = new RandomTimer(timeQuotaMin, timeQuotaMax, OnTimerEnd);

            Init();
        }

        public virtual bool ResetTimerConditions()
        {
            if (!enabled)
                return false;

            if (!timer.ResetTimerConditions())
                return false;

            if (OswaldInsanityController.insanityMeter < insanityThreshold)
            {
                timer.ResetTimer();
                return false;
            }

            return true;
        }

        public virtual void Update()
        {
            if (!ResetTimerConditions())
                return;

            timer.Update();
        }

        public virtual void OnTimerEnd()
        {
            InsanityMod.logger.Log("Triggered insanity timer event: " + name);
            // stuff here
        }
    }

    // Plays sound ""randomly"" when insanity is reached
    public class RandomInsanityAudio : InsanityTimerEvent
    {
        public InsanityAudioProperties properties;

        // divide the other audio elapsed times when one goes off
        public float divideOthersMin = 1.15f;
        public float divideOthersMax = 1.25f;

        public static List<RandomInsanityAudio> allInstances = new List<RandomInsanityAudio>();

        public RandomInsanityAudio(AudioClip clip, float insanityThreshold, float timeMin, float timeMax, string name = "insanity sound") :
            base(insanityThreshold, timeMin, timeMax, name)
        {
            this.properties = new InsanityAudioProperties(clip, name);

            this.Init();
            this.AddInstance();
        }

        public RandomInsanityAudio(InsanityAudioProperties audioProperties, float insanityThreshold, float timeMin, float timeMax) :
            base(insanityThreshold, timeMin, timeMax, audioProperties.name)
        {
            this.properties = audioProperties;

            this.Init();
            this.AddInstance();
        }

        public void AddInstance()
        {
            if (!allInstances.Contains(this))
            {
                allInstances.Add(this);
                //InsanityMod.logger.Log("Added instance");
            }
        }

        public override void OnTimerEnd()
        {
            base.OnTimerEnd();

            foreach (RandomInsanityAudio inst in allInstances)
            {
                if (inst != this)
                {
                    inst.timer.elapsed = inst.timer.elapsed / UnityEngine.Random.Range(divideOthersMin, divideOthersMax);
                    //InsanityMod.logger.Log("Get divided nerd");
                }
            }

            InsanityAudioPlayer.PlayFromProperites(properties);
        }
    }

    public class LightFlickerEvent : InsanityTimerEvent
    {
        public int minFlicker;
        public int maxFlicker;

        public int flickerStep = 0;
        public int flickerTotal = 0;

        public Timer flickerTimer;

        public bool flickerActive = false;
        public bool lightsOn = false;

        public LightFlickerEvent(float insanityThreshold, float timeMin, float timeMax, int minFlicker, int maxFlicker, string name = "unnamed light flicker",
                                 float flickerSpeed = 0.100f) :
            base(insanityThreshold, timeMin, timeMax, name)
        {
            this.minFlicker = minFlicker;
            this.maxFlicker = maxFlicker;

            flickerTimer = new Timer(flickerSpeed, OnFlicker);

            InitEvent();
        }

        public void ResetFlickerTimer()
        {
            flickerActive = false;
            lightsOn = false;
            flickerStep = 0;
            flickerTotal = 0;
            flickerTimer.ResetTimer();
        }

        public void InitEvent()
        {
            ResetFlickerTimer();
        }

        public void ToggleLights()
        {
            bool light = SingletonMB<LocationManager>.Instance.CurrentRoom.IsRoomLit();

            SingletonMB<LocationManager>.Instance.CurrentRoom.ToggleFakeLights(!light);

            InsanityMod.logger.Log("toggled light!");
        }

        public override void Update()
        {
            base.Update();

            if (!ResetTimerConditions())
            {
                ResetFlickerTimer();
                return;
            }

            if (flickerActive)
            {
                flickerTimer.Update();
            }
        }

        public void OnFlicker()
        {
            flickerStep += 1;
            ToggleLights();
            if (flickerStep > flickerTotal)
            {
                flickerActive = false;
                SingletonMB<LocationManager>.Instance.CurrentRoom.ToggleFakeLights(lightsOn);
            }
        }

        public void ActivateFlickerEvent()
        {
            flickerTotal = UnityEngine.Random.Range(minFlicker, maxFlicker);
            flickerStep = 0;
            flickerActive = true;
            lightsOn = SingletonMB<LocationManager>.Instance.CurrentRoom.IsRoomLit();
            flickerTimer.ResetTimer();
        }

        public override void OnTimerEnd()
        {
            base.OnTimerEnd();

            ActivateFlickerEvent();
        }
    }

    // not to be confused with the one that's already in the game...
    public class FakeGeneratorFail : InsanityTimerEvent
    {
        public RandomTimer endTimer;

        public bool failActive = false;

        public List<string> bannedScenes = new List<string>();

        public FakeGeneratorFail(float insanityThreshold, float timeMin, float timeMax, float endTimeMin, float endTimeMax, 
                                 string[] bannedScenes, string name = "unnamed event") :
            base(insanityThreshold, timeMin, timeMax, name)
        {
            endTimer = new RandomTimer(endTimeMin, endTimeMax, OnReactivateTimer);
            this.bannedScenes = bannedScenes.ToList();

            InitEvent();
        }

        public void InitEvent()
        {
            // This is just incase it's initalized while it's failing, but before it reactivates (don't want the hallucination to become real lol)
            if (failActive)
            {
                generator.DoGeneratorPowered();
            }

            endTimer.Init();
            failActive = false;
        }

        public bool CheckBannedScenes(string sceneName)
        {
            foreach (string name in bannedScenes)
            {
                if (sceneName.Contains(name))
                    return true;
            }

            return false;
        }

        public override void Update()
        {
            string sceneName = SceneManager.GetActiveScene().name;

            if (CheckBannedScenes(sceneName) || !sceneName.Contains("PIZ"))
            {
                Init();
                InitEvent();
                //InsanityMod.logger.Log("scene is banned!");
                return;
            }

            //InsanityMod.logger.Log("scene is NOT banned!");

            base.Update();

            if (!ResetTimerConditions())
                return;

            if (!failActive)
                return;

            endTimer.Update();
        }

        public override void OnTimerEnd()
        {
            base.OnTimerEnd();

            if (!generator.m_IsActive)
            {
                InsanityMod.logger.Log("Tried to do fake generator shutdown, but generator is already off.");
                return;
            }

            failActive = true;
            generator.DoGeneratorShutdown();
        }

        public void OnReactivateTimer()
        {
            /* NOTE: Reactivating the generator will reset and change the timer for when it ACTUALLY trips.
             * (This is why I added the night check, because i've never actually experienced the generator tripping after night 2
             *  and don't want to make night 2 easier with this.)
             */

            if (generator.m_IsActive)
            {
                failActive = false;
                InsanityMod.logger.Log("Tried to do fake generator enabled, but generator is already on.\n" +
                                       "This is technically possible normally but still weird.", LogLevel.Warning);
                return;
            }

            // We do the fake one as well so the sound plays
            generator.DoGeneratorPowered();
            generator.FakeGeneratorPowered();
            failActive = false;
        }
    }

    public class InsanityTriggeredEvent : InsanityEvent
    {
        public float disableThreshold;

        public bool eventActive = false;

        public InsanityTriggeredEvent(float insanityThreshold, float disableThreshold, string name = "unnamed event") :
            base(insanityThreshold, name)
        {
            this.disableThreshold = disableThreshold;
        }

        public virtual void JustTriggeredEvent()
        {
            InsanityMod.logger.Log("Just triggered insanity event: " + name);
            // stuff here!
        }

        public virtual void JustEndedEvent()
        {
            InsanityMod.logger.Log("Just ended insanity event: " + name);
            // stuff here!
        }

        public virtual void TriggerEvent()
        {
            if (!eventActive)
                JustTriggeredEvent();

            eventActive = true;
            // stuff here!
        }

        public virtual void EndEvent()
        {
            if (eventActive)
                JustEndedEvent();
            
            eventActive = false;
            // stuff here!
        }

        public virtual void Update()
        {
            if (!enabled) return;

            if (OswaldInsanityController.insanityMeter >= insanityThreshold)
                TriggerEvent();

            if (OswaldInsanityController.insanityMeter < disableThreshold)
                EndEvent();
        }
    }

    public class TerrifiedOswaldEvent : InsanityTriggeredEvent
    {

        public InsanityAudioProperties screamSound;
        public InsanityAudioProperties laughSound;

        public float screamChance = 25;
        public float laughChance = 50;

        public TerrifiedOswaldEvent(float insanityThreshold, float disableThreshold, InsanityAudioProperties laughSound, 
                                    InsanityAudioProperties screamSound, string name = "unnamed event") :
            base(insanityThreshold, disableThreshold, name)
        {
            this.screamSound = screamSound;
            this.laughSound = laughSound;

            InitEvent();
        }

        public void InitEvent()
        {
            // Disable if it was active
            if (eventActive)
                Oswald.SetOswaldTerrifiedState(false);

            eventActive = false;
        }

        public void PlayRandomAudio()
        {
            int num = UnityEngine.Random.Range(0, 99) + 1;

            if (num < screamChance)
                InsanityAudioPlayer.PlayFromProperites(screamSound);
            else if (num < laughChance)
                InsanityAudioPlayer.PlayFromProperites(laughSound);
        }

        public override void JustTriggeredEvent()
        {
            base.JustTriggeredEvent();
            PlayRandomAudio();
        }

        public override void TriggerEvent()
        {
            base.TriggerEvent();
            Oswald.SetOswaldTerrifiedState(true);
        }

        public override void JustEndedEvent()
        {
            base.JustEndedEvent();
            Oswald.SetOswaldTerrifiedState(false);
        }
    }

    public class MaxInsanityMusicEvent : InsanityTriggeredEvent
    {
        public AudioClip music;

        public MaxInsanityMusicEvent(float insanityThreshold, float disableThreshold, AudioClip music, string name = "unnamed event") :
            base(insanityThreshold, disableThreshold, name)
        {
            this.music = music;

            InitEvent();
        }

        public void PlayMusic()
        {
            OswaldInsanityController.source.clip = music;
            OswaldInsanityController.source.loop = true;
            OswaldInsanityController.source.volume = 0.5f;
            OswaldInsanityController.source.Play();
        }

        public void StopMusic()
        {
            OswaldInsanityController.source.loop = false;
            OswaldInsanityController.source.volume = 1f;
            OswaldInsanityController.source.Stop();
        }

        public void InitEvent()
        {
            if (eventActive)
                StopMusic();

            eventActive = false;
        }

        public override void JustTriggeredEvent()
        {
            base.JustTriggeredEvent();
            PlayMusic();
        }

        public override void JustEndedEvent()
        {
            base.JustEndedEvent();
            StopMusic();
        }
    }
}
