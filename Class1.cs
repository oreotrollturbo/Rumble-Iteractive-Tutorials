using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using CloneBending2;
using HarmonyLib;
using Il2CppRUMBLE.Managers;
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
        public const string ModVersion = "1.0.2";
        public const string Description = "Interactive tutorials for various moves and poses using clonebending!";
        public const string Author = "oreotrollturbo";
    }
    
    public class Main : MelonMod
    {
        private Mod mod = new Mod();
        public static string FolderPath => Path.Combine(MelonEnvironment.UserDataDirectory, "InteractiveTutorials");
        public static string LocalRecordedPath => Path.Combine(FolderPath, "MyRecording");
        
        public static List<TutorialPack> TutorialsAndPacks = new List<TutorialPack>();

        public static TutorialSelector tutorialSelector;

        public static PlayerManager playerManager;

        public static int playerBP;

        public static ModSetting<int> countDown;

        public static bool YButtonCooldown;
        public static bool XButtonCooldown;

        private static List<TutorialSelectorJr> logPlayers = new List<TutorialSelectorJr>();
        private static bool hasFreedClone = false;
        
        private static GameObject groundColider1;
        private static GameObject groundColider2;

        public static string ARG_DIR_NAME = "let me out";
        
        public override void OnLateInitializeMelon()
        {
            CloneBendingAPI.LoggerInstance = LoggerInstance;
            Calls.onMapInitialized += SceneLoaded;
            UI.instance.UI_Initialized += OnUIInit;
            
            SetupExampleJson();
            CreateMyRecording(false);
            
            HandleTutorialList();
        }

        /**
         * Add every new event type here so there is an example for all of them
         */
        private static void SetupExampleJson()
        {
            List<TutorialEvents.TutorialEvent> exampleList = new List<TutorialEvents.TutorialEvent>();

            TutorialEvents.TogglePlayerModelEvent playerModelEvent = new TutorialEvents.TogglePlayerModelEvent(
                triggerTime: 0f
                );
            
            exampleList.Add(playerModelEvent);
            
            TutorialEvents.CreateTextBoxEvent textBoxEvent = new TutorialEvents.CreateTextBoxEvent(
                triggerTime: 0f,
                text: "Example text",
                timeExisting: 5f,
                location: new Vector3(0f,0f,0f),
                colour: Color.blue,
                size: 5f
            );
            
            exampleList.Add(textBoxEvent);

            string path = Path.Combine(FolderPath,"exampleEvents.json");
            
            TutorialEvents.SaveEventsJson(exampleList,path);
        }

        public static void CreateMyRecording(bool createJSON)
        {
            MelonLogger.Warning("Creating tutorials directory");
            Directory.CreateDirectory(LocalRecordedPath);

            if (createJSON)
            {
                TutorialInfo.CreateBlankInfoJson(LocalRecordedPath);
            }
        }

        public static void CreateLogPlayers()
        {
            string path = Path.Combine(FolderPath, ARG_DIR_NAME);
            
            string audioPath = Path.Combine(path, "audio.wav");
            if (Directory.Exists(path) && !File.Exists(audioPath))
            {
                hasFreedClone = true;
                
                string log1Path = Path.Combine(path, "log1");
                string log2Path = Path.Combine(path, "log2");
                string log3Path = Path.Combine(path, "log3");

                Vector3 log1Loc = new Vector3(-43.1422f, 9.7706f, -14.8722f);
                Quaternion log1Rot = Quaternion.Euler(0f, 206.6285f, 0f);
                TutorialSelectorJr log1Player = new TutorialSelectorJr(
                    location: log1Loc,
                    rotation: log1Rot,
                    tutorialPath: log1Path
                    );
                logPlayers.Add(log1Player);
                
                Vector3 log2Loc = new Vector3(-26.4121f, -1.4936f, 5.115f);
                Quaternion log2Rot = Quaternion.Euler(0f, 66.3497f, 0f);
                TutorialSelectorJr log2Player = new TutorialSelectorJr(
                    location: log2Loc,
                    rotation: log2Rot,
                    tutorialPath: log2Path
                );
                logPlayers.Add(log2Player);
                
                Vector3 log3Loc = new Vector3(14.3332f, -0.9762f, 3.0303f);
                Quaternion log3Rot = Quaternion.Euler(0f, 159.9691f, 0f);
                TutorialSelectorJr log3Player = new TutorialSelectorJr(
                    location: log3Loc,
                    rotation: log3Rot,
                    tutorialPath: log3Path
                );
                logPlayers.Add(log3Player);
                
                Vector3 loc1 = new Vector3(-29.849f, -3.3591f, 5.2964f); //-2.1
                groundColider1 = GroundCreator.CreateGroundCollider(loc1,10f);
            
                Vector3 loc2 = new Vector3(13.426f, -3.0762f, 2.8757f);
                groundColider2 = GroundCreator.CreateGroundCollider(loc2,4f);
            }
        }

        public static void HandleTutorialList()
        { 
            
           TutorialsAndPacks.Clear();
           string[] tutorialPaths = Directory.GetDirectories(FolderPath);

           foreach (string path in tutorialPaths)
           {
               if (File.Exists(Path.Combine(path, "packInfo.json")))  // Pack logic
               {
                   string json = File.ReadAllText(Path.Combine(path, "packInfo.json"));
                   TutorialInfo info = FromJson<TutorialInfo>(json);
                   Pack pack = new Pack(path,info); // Pass other required arguments if needed
                   TutorialsAndPacks.Add(pack);
               }
               else if (File.Exists(Path.Combine(path, "tutorialInfo.json")))
               {
                   string json = File.ReadAllText(Path.Combine(path, "tutorialInfo.json"));
                   TutorialInfo info = FromJson<TutorialInfo>(json);
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

            countDown = mod.AddToList("Count down", 0, "How long the mo counts down before starting the recording, if its 0 it goes to the mods default: 'lights, camera, action' ", new Tags());

            mod.GetFromFile();

            //mod.ModSaved += Save;
            UI.instance.AddMod(mod);
            MelonLogger.Msg("Added Mod: " + BuildInfo.ModName);
        }

        public override void OnUpdate()
        {
            if (tutorialSelector == null) return;
            if ((double)Calls.ControllerMap.LeftController.GetSecondary() == 1.0 && !YButtonCooldown)
            {
                //tutorialSelector.PlayTutorial();
                
                YButtonCooldown = true;
                MelonCoroutines.Start(StartYButtonCooldown());
            }
            if (tutorialSelector.isRecording && Calls.ControllerMap.LeftController.GetPrimary() == 1.0 && !XButtonCooldown)
            {
                tutorialSelector.SaveEvent();
                XButtonCooldown = true;
                MelonCoroutines.Start(StartXButtonCooldown());
            }
        }
        
        private IEnumerator StartYButtonCooldown()
        {
            yield return new WaitForSeconds(0.5f);
            YButtonCooldown = false;
        }
        
        private IEnumerator StartXButtonCooldown()
        {
            yield return new WaitForSeconds(0.5f);
            XButtonCooldown = false;
        }

        private void SceneLoaded()
        {
            if (tutorialSelector != null)
            {
                tutorialSelector.StopPlayback();
            }
    
            if (!Calls.Scene.GetSceneName().Equals("Gym"))
            {
                if (groundColider1 != null)
                {
                    GameObject.Destroy(groundColider1);
                }
                if (groundColider2 != null)
                {
                    GameObject.Destroy(groundColider2);
                }

                if (tutorialSelector != null)
                {
                    tutorialSelector.Delete();
                    tutorialSelector = null;
                }
                return;
            }

            MelonCoroutines.Start(CreateTutorialSelector());
        }

        private IEnumerator CreateTutorialSelector()
        {
            yield return new WaitForSeconds(3f);
            
            Type mainType = typeof(Core);
            CloneBendingAPI.cloneBendingType = mainType;
            CloneBendingAPI.cloneBendingInstance = RegisteredMelons.FirstOrDefault
                (mod => mod.GetType() == mainType) as CloneBending2.Core;

            Vector3 selectorLoc = new Vector3(-15.0161f, 1.5073f, 3.4538f);
            Quaternion rotation = Quaternion.Euler(1.0f, 66.1951f, 0f);
            tutorialSelector = new TutorialSelector(selectorLoc,rotation);
            
            playerManager = Calls.Managers.GetPlayerManager();

            if (playerManager == null || playerManager.localPlayer == null)
            {
                MelonLogger.Warning("PlayerManager or localPlayer is null during SceneLoaded.");
            }
            else
            {
                playerBP = GetPlayerBP();
            }
            
            CreateLogPlayers();
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
        
        public static string ToJson<T>(T obj)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Serialize(obj, options);
        }

        public static T FromJson<T>(string json)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            };
            return JsonSerializer.Deserialize<T>(json, options);
        }

        
        
        [HarmonyPatch(typeof(Core), "finishPlaying")]
        public static class UploadClonePatch
        {
            static void Postfix(Core __instance, ref IEnumerator __result)
            {
                __result = FinishPlayingWrapper(__instance, __result);
            }

            private static IEnumerator FinishPlayingWrapper(Core instance, IEnumerator original)
            {
                yield return original;

                tutorialSelector.isPlaying = false;
        
                // Add null check here too
                if (tutorialSelector.currentAudio != null)
                {
                    AudioManager.StopPlayback(tutorialSelector.currentAudio);
                    tutorialSelector.currentAudio = null;
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

        [JsonPropertyName("belt")] // Changed from "minimumBelt" to match JSON key
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

        public static void CreateBlankInfoJson(string path)
        {
            MelonLogger.Msg("Creating blank json");
            path = Path.Combine(path, "tutorialInfo.json");
            string name = Calls.Managers.GetPlayerManager().localPlayer.Data.GeneralData.PublicUsername;

            if (name == null)
            {
                name = "Your name";
            }
            
            TutorialInfo blankInfo = new TutorialInfo(
                name: "Recorded tutorial",
                minimumBelt: BeltInfo.BeltEnum.WHITE,
                creator: name,
                description: "Enter description here"
            );

            string json = Main.ToJson(blankInfo);
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