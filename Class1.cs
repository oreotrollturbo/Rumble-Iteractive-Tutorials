using CloneBending;
using RumbleModdingAPI;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
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
        public static string FolderPath => Path.Combine(MelonEnvironment.UserDataDirectory, "InteractiveTutorials");

        public static string CurrentPath => Path.Combine(FolderPath, "tutorial1\\clone.json");
        
        public override void OnLateInitializeMelon()
        {
            MelonLogger.Warning("Path is: " + CurrentPath);
            CloneBendingAPI.LoggerInstance = LoggerInstance;
            Calls.onMapInitialized += SceneLoaded;
            
            if (!Directory.Exists(FolderPath))
            {
                MelonLogger.Warning("Creating tutorials directory");
                Directory.CreateDirectory(FolderPath);
            }
        }

        private void SceneLoaded()
        {
            if (Calls.Scene.GetSceneName().Equals("Gym"))
            {
                Type mainType = typeof(MainClass);
                CloneBendingAPI.cloneBendingType = mainType;
                CloneBendingAPI.cloneBendingInstance = RegisteredMelons.FirstOrDefault
                    (mod => mod.GetType() == mainType) as CloneBending.MainClass;
            }
        }
        
    }
}