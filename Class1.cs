using System;
using System.Collections;
using System.Reflection;
using CloneBending;
using RumbleModdingAPI;
using MelonLoader;
using HarmonyLib;
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
        
        public override void OnLateInitializeMelon()
        {
            CloneBendingAPI.LoggerInstance = LoggerInstance;
            Calls.onMapInitialized += SceneLoaded;
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