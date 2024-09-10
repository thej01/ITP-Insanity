using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ITP_Insanity.Patches;
using UnityEngine;
using ITP_Insanity.Events;
using BepInEx.Configuration;

namespace ITP_Insanity
{
    public class Logger
    {
        internal ManualLogSource MLS;

        private string modGUID;
        private string modName;
        private string modVersion;

        public void Init(string _modName, string _modVersion, string _modGUID)
        {
            modGUID = _modGUID;
            modName = _modName;
            modVersion = _modVersion;

            MLS = BepInEx.Logging.Logger.CreateLogSource(modGUID);
        }

        public void Log(string text = "", LogLevel level = LogLevel.Info)
        {
            string resultText = string.Format("[{0} v{1}] - {2}", modName, modVersion, text);

            MLS.Log(level, resultText);
        }
    }

    public class InsanityBehavior
    {
        public bool enabled = true;

        public void Enable()
        {
            enabled = true;
        }

        public void Disable()
        {
            enabled = false;
        }
    }

    public class Timer : InsanityBehavior
    {
        public float speed = 1f;
        public float elapsed = -1f;
        public float endTime = -1f;

        public Action OnComplete;

        public Timer(float endTime, Action onComplete)
        {
            this.endTime = endTime;
            OnComplete = onComplete;

            Init();
        }

        public virtual void ResetTimer()
        {
            elapsed = 0f;
        }

        public virtual void Init()
        {
            ResetTimer();
        }

        public void SetRandomTimer(float min, float max)
        {
            ResetTimer();

            endTime = UnityEngine.Random.Range(min, max);
        }

        public virtual void TimerComplete()
        {
            ResetTimer();
        }

        public virtual bool ResetTimerConditions()
        {
            if (!enabled)
                return false;

            if (elapsed < 0)
            {
                ResetTimer();
                return false;
            }

            return true;
        }

        public virtual void Update()
        {
            if (!ResetTimerConditions())
                return;

            elapsed += Time.deltaTime * speed;

            if (elapsed >= endTime)
            {
                TimerComplete();
                OnComplete();
            }
        }
    }

    public class RandomTimer : Timer
    {
        public float timeMin;
        public float timeMax;

        public void RandomizeTimer()
        {
            SetRandomTimer(timeMin, timeMax);
        }

        public RandomTimer(float timeMin, float timeMax, Action onComplete) :
            base (-1f, onComplete)
        {
            this.timeMin = timeMin;
            this.timeMax = timeMax;

            RandomizeTimer();
        }

        public override void TimerComplete()
        {
            RandomizeTimer();
        }

    }

    // Audio data class (holds all the audioclips in the mod)
    public static class InsanityAudio
    {
        public static AudioClip fakeDoor;
        public static AudioClip maxInsanity;
        public static AudioClip aggressiveRunning;
        public static AudioClip discord;
        public static AudioClip fakeSeen;
        public static AudioClip fakeSameRoom;

        public static AudioClip[] laugh = new AudioClip[3];
        public static AudioClip[] doorKnock = new AudioClip[6];
        public static AudioClip[] weirdSounds = new AudioClip[4];
        public static AudioClip[] doorBang = new AudioClip[5];
        public static AudioClip[] bonnieStep = new AudioClip[5];

        public static void Init()
        {
            InsanityMod.logger.Log("Loading assets...");

            string dataPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            //logger.Log("Data Path: " + dataPath);

            AssetBundle assets;

            assets = AssetBundle.LoadFromFile(Path.Combine(dataPath, "insanity"));

            if (assets == null)
            {
                InsanityMod.logger.Log("Failed to load assets!", LogLevel.Error);
                return;
            }

            fakeDoor = assets.LoadAsset<AudioClip>("Assets/AudioClip/Door Squeak_0.ogg");
            maxInsanity = assets.LoadAsset<AudioClip>("Assets/AudioClip/maxInsanityMusicBox.ogg");
            aggressiveRunning = assets.LoadAsset<AudioClip>("Assets/AudioClip/aggresiveRunning.ogg");
            discord = assets.LoadAsset<AudioClip>("Assets/AudioClip/discord.ogg");
            fakeSeen = assets.LoadAsset<AudioClip>("Assets/AudioClip/fakeSeen.ogg");
            fakeSameRoom = assets.LoadAsset<AudioClip>("Assets/AudioClip/fakeSameRoom.ogg");

            for (int i = 0; i < 3; i++)
            {
                laugh[i] = assets.LoadAsset<AudioClip>("Assets/AudioClip/StrangeLaugh/laugh" + (i + 1).ToString() + ".ogg");
            }

            for (int i = 0; i < 6; i++)
            {
                doorKnock[i] = assets.LoadAsset<AudioClip>("Assets/AudioClip/DoorKnock/knock" + (i + 1).ToString() + ".ogg");
            }

            for (int i = 0; i < 5; i++)
            {
                doorBang[i] = assets.LoadAsset<AudioClip>("Assets/AudioClip/DoorBang/bang" + (i + 1).ToString() + ".ogg");
                bonnieStep[i] = assets.LoadAsset<AudioClip>("Assets/AudioClip/Footsteps/Bonnie/step" + (i + 1).ToString() + ".ogg");
            }

            for (int i = 0; i < 4; i++)
            {
                weirdSounds[i] = assets.LoadAsset<AudioClip>("Assets/AudioClip/StrangeTune/tune" + (i + 1).ToString() + ".ogg");
            }

            InsanityMod.logger.Log("Loaded assets!");
        }
    }

    // Helper class for audio
    public static class InsanityAudioPlayer
    {
        public static void Log(string stuff, string clipName, string sndName = "insanity sound")
        {
            InsanityMod.logger.Log(String.Format("\"{0}\" (Clip name: \"{1}\") ", sndName, clipName) + stuff);
        }

        public static void PlayNearOswald(AudioClip clip, float xOffsetMax, float yOffsetMax, string sndName = "insanity sound")
        {
            float xOffset = UnityEngine.Random.Range(-xOffsetMax, xOffsetMax);
            float yOffset = UnityEngine.Random.Range(-yOffsetMax, yOffsetMax);

            Vector3 pos = OswaldInsanity.oswald.Position;
            pos.x += xOffset;
            pos.y += yOffset;

            AudioSource.PlayClipAtPoint(clip, pos);

            Log("Played at pos: " + pos.ToString(), clip.name, sndName);
        }

        public static void PlayOneShot(AudioClip clip, string sndName = "insanity sound")
        {
            OswaldInsanity.source.PlayOneShot(clip);

            Log("Played", clip.name, sndName);
        }

        public static void PlayOneShot(AudioClip clip, float volume = 1f, string sndName = "insanity sound")
        {
            OswaldInsanity.source.PlayOneShot(clip, volume);

            Log("Played", clip.name, sndName);
        }

        public static void PlayFromProperites(InsanityAudioProperties properties, bool randomizeClips = true, bool randomizeVolume = true)
        {
            AudioClip clip;

            if (randomizeClips)
                clip = properties.GetRandomClip();
            else
                clip = properties.GetClip();

            float volume;

            if (randomizeVolume)
                volume = properties.GetRandomVolume();
            else
                volume = properties.GetVolume();

            if (properties.oneShot)
                PlayOneShot(clip, volume, properties.name);
            else
                PlayNearOswald(clip, properties.xOffset, properties.yOffset, properties.name);
        }
    }


    // data class! shouldn't be used on it's own
    public class InsanityAudioProperties
    {
        public bool oneShot = true;

        public float xOffset;
        public float yOffset;
        public float volumeMin = 1f;
        public float volumeMax = 1f;

        public string name;

        public List<AudioClip> clips = new List<AudioClip>();

        public InsanityAudioProperties(AudioClip clip, string name = "insanity sound")
        {
            this.clips.Add(clip);
            this.name = name;
        }

        public InsanityAudioProperties(AudioClip[] clips, string name = "insanity sound")
        {
            this.clips = clips.ToList();
            this.name = name;
        }

        // do this array stuff or CS0111 lol
        public InsanityAudioProperties(AudioClip clip, float[] volumeMinMax, string name = "insanity sound")
        {
            this.clips.Add(clip);
            this.volumeMin = volumeMinMax[0];
            this.volumeMax = volumeMinMax[1];
            this.name = name;
        }

        public InsanityAudioProperties(AudioClip[] clips, float[] volumeMinMax, string name = "insanity sound")
        {
            this.clips = clips.ToList();
            this.volumeMin = volumeMinMax[0];
            this.volumeMax = volumeMinMax[1];
            this.name = name;
        }

        public InsanityAudioProperties(AudioClip clip, float xOffsetMax, float yOffsetMax, string name = "insanity sound")
        {
            this.oneShot = false;
            this.xOffset = xOffsetMax;
            this.yOffset = yOffsetMax;
            this.name = name;
            this.clips.Add(clip);
        }

        public InsanityAudioProperties(AudioClip[] clips, float xOffsetMax, float yOffsetMax, string name = "insanity sound")
        {
            this.oneShot = false;
            this.xOffset = xOffsetMax;
            this.yOffset = yOffsetMax;
            this.name = name;
            this.clips = clips.ToList();
        }

        public AudioClip GetClip()
        {
            return clips[0];
        }

        public AudioClip GetRandomClip()
        {
            if (clips.Count <= 1)
                return GetClip();
            return clips[UnityEngine.Random.Range(0, clips.Count)];
        }

        public float GetVolume()
        {
            return volumeMax;
        }

        public float GetRandomVolume()
        {
            return UnityEngine.Random.Range(volumeMin, volumeMax);
        }
    }

    [BepInPlugin(modGUID, modName, modVersion)]
    public class InsanityMod : BaseUnityPlugin
    {
        private const string modGUID = "thej01.itp.InsanityMod";
        private const string modName = "Insanity";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);

        private static InsanityMod Instance;

        public static Logger logger = new Logger();

        public static float CalculatePercent(float num, float percent)
        {
            return (num / 100) * percent;
        }

        public const float insanityMin = 0f;
        public const float insanityMax = 1f;

        // -- INSANITY MODIFIERS --

        // Reference constant, this value will increase insanity to max in 1 minute
        public const float minuteGain = insanityMax / 60f;

        // Increase insanity by ** every second
        // Default: minuteGain / 10 (10 minutes for full)
        public static float sanityLoss = minuteGain / 10f;

        // Only affects the above, since multiplying everthing would be a bit crazy.
        // each array index is a difficulty in game, from creepy to nightmare.
        // default:[0.8f, 1f, 1.2f, 1.5f]
        public static float[] sanityLossMultiplier = new float[] { 0.8f, 1f, 1.2f, 1.4f };

        // Everything below stacks with the above!

        // Increase insanity by ** every second while standing still for a long time (disabled while hiding)
        // Default: sanityLoss (Basically doubling the current insanity increase)
        public static float waitingLoss = sanityLoss;

        // Timer until the above takes into effect
        // Default: 15f (15 seconds)
        public static float waitingLossTimer = 15f;

        // Increase insanity by ** every second while flashlight is on (disabled while hiding)
        // Default: -(minuteGain / 2f) (2 minutes for full)
        public static float flashlightLoss = -(minuteGain / 2f);

        // Increase insanity by ** every second while in a dark room (this increase is disabled if oswald's flashlight is on!) (disabled while hiding)
        // Default: sanityLoss (Basically doubling the current insanity increase)
        public static float darknessLoss = sanityLoss;

        // Increase insanity by ** when bonnie is in the same room as oswald
        // Default: minuteGain / 4.5f (4.5 minutes for full)
        public static float withBonnieLoss = minuteGain / 4.5f;

        // Divide the above by ** when you are also hiding (not divided during a chase)
        // Default: 2f
        public static float withBonnieHiding = 2f;

        // Increase insanity by ** while in a noisy room (disabled during a chase) (disabled while hiding)
        // Default: minuteGain / 8f (8 minutes for full)
        public static float noisyRoomLoss = minuteGain / 8f;

        // Increase insanity by ** while walking (disabled during a chase)
        // Default: -minuteGain / 5f (5 minutes for full)
        public static float walkingLoss = -minuteGain / 5f;

        // Increase insanity by ** while running
        // Default: sanityLoss (Basically doubling the current insanity increase)
        public static float runningLoss = sanityLoss;

        // This is applied whenever you're in jeff's pizza. (Sanity mechanics pause except for this)
        // Default: -minuteGain / 2.5f (2.5 minutes for full)
        public static float jeffsCooloff = -minuteGain / 2.5f;

        // Increase insanity by ** after bonnie hears oswald
        // Default: 10% Insanity
        public static float luredBonnieLoss = CalculatePercent(insanityMax, 10f);

        // If bonnie is lured several times in quick succesion the insanity gain will start to decay
        // The above will be divided by this every time bonnie is lured.
        // Default: 2f
        public static float luredBonnieDecay = 2f;

        // Take ~** seconds to recover the loss amount.
        // Default: 20f (~20 Seconds)
        public static float luredBonnieDecayRecover = 20f;

        // Increase insanity by ** after the generator shutsdown (this also happens when it flickers!)
        // Default: 10% Insanity
        public static float generatorShutdownLoss = CalculatePercent(insanityMax, 10f);

        // Increase insanity by ** after the generator enables
        // Remember that you usually already lose insanity from the generator shutting down, and when you pull the lever because it lures bonnie.
        // Default: -35% Insanity (Technically -15% due to the above)
        public static float generatorEnabledLoss = -CalculatePercent(insanityMax, 35f);

        // Increase insanity by ** after tripping
        // Default: 5% Insanity
        public static float tripLoss = CalculatePercent(insanityMax, 5f);

        // If the scene contains these words insanity is disabled.
        public static List<string> bannedNights = new List<string>()
        {
            "Episode1",
            "Episode2",
            "Transition"
            //"Night1"
        };



        // -- INSANITY EVENTS --

        /*// Random jumpscare increase (couldn't find a way to get it to work)

            // Minimum and maximum insanity that the aggression of the jumpscares are mapped to
            // Defaults: 25%, 90% Insanity
            public static float jumpFrequencyMin = CalculatePercent(insanityMax, 25);
            public static float jumpFrequencyMax = CalculatePercent(insanityMax, 90);

            // Minimum and maximum aggression that is mapped to the insanity values
            // (basically, smoothly go from 1 to 2 based off insanity)
            // Defaults: 50f (50 seconds), 30f (30 seconds)
            public static float jumpAggressionMin = 50f;
            public static float jumpAggressionMax = 30f;*/

        public static RandomInsanityAudio doorKnockSound;
        public static RandomInsanityAudio weirdSounds;
        public static RandomInsanityAudio discordLmao;
        public static RandomInsanityAudio fakeDoorSound;
        public static RandomInsanityAudio fakeBonnieFootstep;
        public static RandomInsanityAudio fakeDoorBang;
        public static RandomInsanityAudio aggressiveRunning;
        public static RandomInsanityAudio fakeBonnieMusic;

        public static void InitRandomSounds()
        {
            // This uses a different system since i started to duplicate a lot of code.
            // These sounds play randomly once a threshold is reached, and depending on the sound will play within
            // the world, so it feels real.

            // The audio properties contain the sound(s), the maximum x and y offset for the sound, and a name used for logging

            // The RandomInsanityAudio contains the insanity required for the sounds to start playing,
            // and the minimum/maximum time before the sound plays.

            InsanityAudioProperties properties = new InsanityAudioProperties(InsanityAudio.doorKnock, 5f, 1.5f, "Door Knock");
            doorKnockSound = new RandomInsanityAudio(properties, CalculatePercent(insanityMax, 25f), 35f, 50f);

            properties = new InsanityAudioProperties(InsanityAudio.weirdSounds, 7f, 1f, "Weird Sound");
            weirdSounds = new RandomInsanityAudio(properties, CalculatePercent(insanityMax, 35f), 65f, 75f);

            // funny
            properties = new InsanityAudioProperties(InsanityAudio.discord, new float[] { 0.8f, 0.8f }, "Fake Discord");
            discordLmao = new RandomInsanityAudio(properties, CalculatePercent(insanityMax, 45f), 80f, 100f);

            properties = new InsanityAudioProperties(InsanityAudio.fakeDoor, 7f, 2f, "Fake Door");
            fakeDoorSound = new RandomInsanityAudio(properties, CalculatePercent(insanityMax, 50f), 45f, 60f);

            properties = new InsanityAudioProperties(InsanityAudio.bonnieStep, 7f, 2f, "Fake Bonnie Step");
            fakeBonnieFootstep = new RandomInsanityAudio(properties, CalculatePercent(insanityMax, 60f), 55f, 70f);

            properties = new InsanityAudioProperties(InsanityAudio.doorBang, 7f, 2f, "Fake Door Bang");
            fakeDoorBang = new RandomInsanityAudio(properties, CalculatePercent(insanityMax, 70f), 70f, 80f);

            properties = new InsanityAudioProperties(InsanityAudio.aggressiveRunning, 3f, 1f, "Fake Running");
            aggressiveRunning = new RandomInsanityAudio(properties, CalculatePercent(insanityMax, 75f), 65f, 75f);

            properties = new InsanityAudioProperties(InsanityAudio.fakeSameRoom, "Fake Same Room");
            fakeBonnieMusic = new RandomInsanityAudio(properties, CalculatePercent(insanityMax, 85f), 60f, 70f);
        }

        /*// Oswald pushing doors (i also couldn't figure this out!)

            // Oswald starts to push open doors (as if he was being chased)
            // Default: 85% Insanity
            public static float pushDoorEvent = CalculatePercent(insanityMax, 85f);*/

        // Light flickering (disabled) (this already existed in game anyway lmao)

            public static LightFlickerEvent lightFlicker = new LightFlickerEvent
            (
                CalculatePercent(insanityMax, 35f),
                50f,
                75f,
                3,
                5,
                "Light Flicker",
                0.1f
            );

        // Fake Generator Fail (it's different from the one in the normal game okay??)

            public static FakeGeneratorFail fakeGeneratorFail = new FakeGeneratorFail
            (
                CalculatePercent(insanityMax, 65f),
                150f,
                200f,
                15f,
                30f,
                new string[] { "Night1", "Night2" },
                "Fake Generator Fail"
            );

        // Terrified Oswald
            public static TerrifiedOswaldEvent terrifedOswald;

        // The music box when you are 100% insanity
            public static MaxInsanityMusicEvent maxInsanityMusic;

        public static void InitEvents()
        {
            // disable cuz not work
            lightFlicker.Disable();

            InsanityAudioProperties seen = new InsanityAudioProperties(InsanityAudio.fakeSeen, 7f, 2f, "Fake Seen");
            InsanityAudioProperties laugh = new InsanityAudioProperties(InsanityAudio.laugh, 5f, 2f, "Insanity Laugh");

            terrifedOswald = new TerrifiedOswaldEvent
            (
                CalculatePercent(insanityMax, 85f), 
                CalculatePercent(insanityMax, 80f), 
                laugh, 
                seen, 
                "Terrified Oswald"
            );

            maxInsanityMusic = new MaxInsanityMusicEvent
            (
                CalculatePercent(insanityMax, 100f),
                CalculatePercent(insanityMax, 95f),
                InsanityAudio.maxInsanity,
                "Max Insanity Music"
            );

            if (!discordPingEnabled.Value)
                discordLmao.Disable();
        }

        public static ConfigEntry<bool> discordPingEnabled;

        private void Awake()
        {
            if (Instance == null)
                Instance = this;

            logger.Init(modName, modVersion, modGUID);

            logger.Log("logger Initialised!");

            logger.Log("Patching InsanityMod...");
            harmony.PatchAll(typeof(InsanityMod));
            logger.Log("Patched InsanityMod.");

            logger.Log("Patching OswaldInsanity...");
            harmony.PatchAll(typeof(OswaldInsanity));
            logger.Log("Patched OswaldInsanity.");

            logger.Log("Patching OswaldInsanityController...");
            harmony.PatchAll(typeof(OswaldInsanityController));
            logger.Log("Patched OswaldInsanityController.");

            logger.Log("Patching GeneratorInsanity...");
            harmony.PatchAll(typeof(GeneratorInsanity));
            logger.Log("Patched GeneratorInsanity.");

            logger.Log("Patching SpringBonnieInsanity...");
            harmony.PatchAll(typeof(SpringBonnieInsanity));
            logger.Log("Patched SpringBonnieInsanity.");

            logger.Log("Patching GameOverScreenInsanity..");
            harmony.PatchAll(typeof(GameOverScreenInsanity));
            logger.Log("Patched GameOverScreenInsanity.");

            discordPingEnabled = Config.Bind("General", "FakePing", true, "Whether or not the fake discord ping hallucination is enabled.");

            InsanityAudio.Init();
            InitRandomSounds();
            InitEvents();
        }
    }
}
