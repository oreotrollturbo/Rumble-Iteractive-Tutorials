using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
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
        
        public static List<TutorialPack> TutorialsAndPacks = new List<TutorialPack>();

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
            
            HandleTutorialList();
        }

        private static void HandleTutorialList()
        {
           string[] tutorialPaths = Directory.GetDirectories(FolderPath);

           foreach (string path in tutorialPaths)
           {
               if (File.Exists(Path.Combine(path, "packInfo.json")))  // Pack logic
               {
                   string json = File.ReadAllText(Path.Combine(path, "packInfo.json"));
                   TutorialInfo info = TutorialInfo.FromJson(json);
                   Pack pack = new Pack(path,info); // Pass other required arguments if needed
                   TutorialsAndPacks.Add(pack);
               }
               else if (File.Exists(Path.Combine(path, "tutorialInfo.json")))
               {
                   string json = File.ReadAllText(Path.Combine(path, "tutorialInfo.json"));
                   TutorialInfo info = TutorialInfo.FromJson(json);
                   Tutorial tutorial = new Tutorial(path,info);
                   TutorialsAndPacks.Add(tutorial);
               }
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
                if (tutorialSelector != null)
                {
                    tutorialSelector.Delete();
                    tutorialSelector = null;
                }
                return;
            }
            
            Type mainType = typeof(MainClass);
            CloneBendingAPI.cloneBendingType = mainType;
            CloneBendingAPI.cloneBendingInstance = RegisteredMelons.FirstOrDefault
                (mod => mod.GetType() == mainType) as CloneBending.MainClass;

            Vector3 selectorLoc = new Vector3(-14.9161f, 1.6073f, 2.6538f);
            Quaternion rotation = Quaternion.Euler(4.8401f, 355.3026f, 0f);
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

    public class TutorialPack
    {
        public TutorialInfo tutorialPackInfo;
        public string path;
    }

    public class Pack : TutorialPack
    {
        public Pack(string path, TutorialInfo tutorialPackInfo)
        {
            this.path = path;
            this.tutorialPackInfo = tutorialPackInfo;
        }
    }

    public class Tutorial : TutorialPack
    {
        public Tutorial(string path, TutorialInfo tutorialPackInfo)
        {
            this.path = path;
            this.tutorialPackInfo = tutorialPackInfo;
        }
    }

    public class TutorialInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("minimumBelt")]
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public BeltInfo.BeltEnum MinimumBelt { get; set; }

        [JsonPropertyName("creator")]
        public string Creator { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        // Parameterless constructor required for deserialization
        public TutorialInfo() { }

        public TutorialInfo(string name, BeltInfo.BeltEnum minimumBelt, string creator, string description)
        {
            Name = name;
            MinimumBelt = minimumBelt;
            Creator = creator;
            Description = description;
        }
    
        public static string ToJson(TutorialInfo tutorialInfo)
        {
            var options = new JsonSerializerOptions {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Serialize(tutorialInfo, options);
        }

        public static TutorialInfo FromJson(string json)
        {
            var options = new JsonSerializerOptions {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<TutorialInfo>(json, options);
        }

        public static void CreateBlankInfoJson(string path)
        {
            var blankInfo = new TutorialInfo(
                name: "Enter tutorial name here",
                minimumBelt: BeltInfo.BeltEnum.WHITE,
                creator: "Enter creator name here",
                description: "Enter description here"
            );

            string json = ToJson(blankInfo);
            File.WriteAllText(path, json);
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
        
        public static int GetBpFromEnum(BeltEnum belt)
        {
            return belt switch
            {
                BeltEnum.BLACK  => 156,
                BeltEnum.RED    => 96,
                BeltEnum.BLUE   => 54,
                BeltEnum.GREEN  => 30,
                BeltEnum.YELLOW => 12,
                BeltEnum.WHITE  => 0,
                _ => throw new ArgumentOutOfRangeException(nameof(belt), $"Unhandled belt: {belt}")
            };
        }

    }
}