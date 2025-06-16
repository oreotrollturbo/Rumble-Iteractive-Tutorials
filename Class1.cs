using System.Collections;
using System.Text.Json;
using CloneBending;
using HarmonyLib;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Managers;
using Il2CppTMPro;
using RumbleModdingAPI;
using MelonLoader;
using MelonLoader.Utils;
using RumbleModUI;
using UnityEngine;
using AudioManager = CustomBattleMusic.AudioManager;
using BuildInfo = InteractiveTutorials.BuildInfo;

[assembly: MelonInfo(typeof(InteractiveTutorials.Main), BuildInfo.ModName, BuildInfo.ModVersion, BuildInfo.Author)]
[assembly: MelonGame("Buckethead Entertainment", "RUMBLE")]

namespace InteractiveTutorials
{
    public static class BuildInfo
    {
        public const string ModName = "Interactive Tutorials";
        public const string ModVersion = "1.0.0";
        public const string Description = "Interactive tutorials for various moves and poses using clonebending!";
        public const string Author = "oreotrollturbo";
    }
    
    public class Main : MelonMod
    {
        private Mod mod = new Mod();
        public static string FolderPath => Path.Combine(MelonEnvironment.UserDataDirectory, "InteractiveTutorials");
        public static string LocalRecordedPath => Path.Combine(FolderPath, "MyRecording");
        
        public static string[] tutorials = Directory.GetDirectories(FolderPath);

        // Try to find the directory named "Introduction"
        public static string selectedTutorial = tutorials
                                                    .FirstOrDefault(t => Path.GetFileName(t).Contains("Introduction", StringComparison.OrdinalIgnoreCase))
                                                ?? tutorials[0]; // fallback to first if not found

        public static TutorialSelector? tutorialSelector;

        public static PlayerManager playerManager;

        public static int playerBP;

        public static ModSetting<bool> hearYourself;
        public static ModSetting<int> microphoneIndex;
        
        public override void OnLateInitializeMelon()
        {
            CloneBendingAPI.LoggerInstance = LoggerInstance;
            Calls.onMapInitialized += SceneLoaded;
            UI.instance.UI_Initialized += OnUIInit;
            
            if (!Directory.Exists(FolderPath))
            {
                MelonLogger.Warning("Creating tutorials directory");
                Directory.CreateDirectory(FolderPath);
                Directory.CreateDirectory(LocalRecordedPath);
            }
            else if (!Directory.Exists(LocalRecordedPath))
            {
                Directory.CreateDirectory(LocalRecordedPath);
            }
        }
        
        public void OnUIInit()
        {
            mod.ModName = BuildInfo.ModName;
            mod.ModVersion = BuildInfo.ModVersion;
            mod.SetFolder("InteractiveTutorials");
            mod.AddDescription("Description", "A platform to create and share in-game tutorials with audio :)", BuildInfo.Description,
                new Tags { IsSummary = true });

            //hearYourself = mod.AddToList("Hear yourself", false, 1, "Plays your voice back as you are recording", new Tags());

            // Corrected assignments: swapped variable names and labels
            //microphoneIndex = mod.AddToList("Default microphone index", 0, "The index of your input device (change if audio doesnt record)", new Tags());

            mod.GetFromFile();

            //mod.ModSaved += Save;
            UI.instance.AddMod(mod);
            MelonLogger.Msg("Added Mod: " + BuildInfo.ModName);
        }

        private void SceneLoaded()
        {
            if (tutorialSelector != null)
            {
                tutorialSelector.StopPlayback();
            }
            
            if (!Calls.Scene.GetSceneName().Equals("Gym"))
            {
                tutorialSelector = null;
                return;
            }
            
            Type mainType = typeof(MainClass);
            CloneBendingAPI.cloneBendingType = mainType;
            CloneBendingAPI.cloneBendingInstance = RegisteredMelons.FirstOrDefault
                (mod => mod.GetType() == mainType) as CloneBending.MainClass;

            Vector3 selectorLoc = new Vector3(-14.9161f, 1.6887f, 2.6538f);
            Quaternion rotation = Quaternion.Euler(4.8401f, 355.3026f, 1.9727f);
            tutorialSelector = new TutorialSelector(selectorLoc,rotation);
            
            playerManager = Calls.Managers.GetPlayerManager();
            playerBP = GetPlayerBP();
        }
        
        // Borrowed this function from NichRumbleDev
        // https://thunderstore.io/c/rumble/p/NichRumbleDev/ExtraBBRanks/source/
        public int GetPlayerBP()  
        {
            if (playerManager.localPlayer != null)
            {
                return playerManager.localPlayer.Data.GeneralData.BattlePoints;
            }
            MelonLogger.Msg("localPlayer is null. Player data might not be initialized yet.");
            return 0;
        }
        
        
        [HarmonyPatch(typeof(MainClass), "finishPlaying")]
        public static class UploadClonePatch
        {
            static void Postfix(MainClass __instance, ref IEnumerator __result)
            {
                __result = FinishPlayingWrapper(__instance, __result);
            }

            private static IEnumerator FinishPlayingWrapper(MainClass instance, IEnumerator original)
            {
                yield return original;

                 tutorialSelector.isPlaying = false;
        
                // Only stop if we have a valid clip
                if (tutorialSelector.currentAudio != null)
                {
                    AudioManager.StopPlayback(tutorialSelector.currentAudio);
                    tutorialSelector.currentAudio = null; // Clear the reference after stopping
                }
            }
        }
    }

    public class TutorialInfo
    {
        public string name;
        public BeltInfo.BeltEnum minimumBelt;
        public string creator;
        public string description;
        public bool isPack;

        public TutorialInfo(string name, BeltInfo.BeltEnum belt, string creator, string description, bool isPack = false)
        {
            this.name = name;
            this.minimumBelt = belt;
            this.creator = creator;
            this.description = description;
            this.isPack = isPack;
        }
        
        public static string ToJson(TutorialInfo tutorialInfo)
        {
            return JsonSerializer.Serialize(tutorialInfo);
        }

        public static TutorialInfo FromJson(string json)
        {
            return JsonSerializer.Deserialize<TutorialInfo>(json);
        }
    }

    public static class BeltInfo
    {
        public enum BeltEnum
        {
            WHITE,
            YELLOW,
            GREEN,
            BLUE,
            RED,
            BLACK
        }

        public static string GetNameFromBelt(BeltEnum belt)
        {
            switch (belt)
            {
                case BeltEnum.WHITE:
                    return "pebble";
                case BeltEnum.YELLOW:
                    return "cobble";
                case BeltEnum.GREEN:
                    return "boulder";
                case BeltEnum.BLUE:
                    return "tor";
                case BeltEnum.RED:
                    return "monolith";
                case BeltEnum.BLACK:
                    return "mountain";
                default:
                    throw new ArgumentOutOfRangeException(nameof(belt), belt, null);
            }
        }

        public static BeltEnum GetEnumFromBp(int bp) //Found this shenanigan on the internet, looks cool
        {
            return bp switch
            {
                >= 156 => BeltEnum.BLACK,
                >= 96  => BeltEnum.RED,
                >= 54  => BeltEnum.BLUE,
                >= 30  => BeltEnum.GREEN,
                >= 12  => BeltEnum.YELLOW,
                _      => BeltEnum.WHITE
            };
        }
    }
}