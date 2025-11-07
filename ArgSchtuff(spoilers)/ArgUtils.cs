using MelonLoader;
using Microsoft.VisualBasic;
using RumbleModdingAPI;
using UnityEngine;

namespace InteractiveTutorials;

public static class ArgUtils
{
    public static string ARG_DIR_NAME = "BeginnerTutorial";

    private static string ARG_AGREED_FILE_NAME = "agreed.oreo2";
    private static string ARG_SAW_INTRO = "heard_my_message.oreo2";

    private static GameObject groundColider1;
    private static GameObject groundColider2;

    public static bool hasAgreedToFree = false;
    public static bool hasWatchedIntro = false;

    public static string argDirPath;

    private static List<TutorialSelectorJr> logPlayers = new List<TutorialSelectorJr>();

    public static void InitARG()
    {
        argDirPath = Path.Combine(Main.FolderPath, ARG_DIR_NAME);
        string argAgreedPath = Path.Combine(argDirPath, ARG_AGREED_FILE_NAME);
        string argSawIntroPath = Path.Combine(argDirPath, ARG_SAW_INTRO);

        hasAgreedToFree = File.Exists(argAgreedPath);
        hasWatchedIntro = File.Exists(argSawIntroPath);
    }
    

    public static void InitArgGymSchtuff()
    {
        CreateGroundColliders();
        CreateLogPlayers();
    }
    
    public static void CreateLogPlayers()
    {
        string agreedFilePath = Path.Combine(argDirPath, ARG_AGREED_FILE_NAME);
        if (File.Exists(agreedFilePath))
        {
            string log1Path = Path.Combine(argDirPath, "log1");
            string log2Path = Path.Combine(argDirPath, "log2");
            string log3Path = Path.Combine(argDirPath, "log3");

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
        }
    }

    public static void CreateGroundColliders()
    {
        Vector3 loc1 = new Vector3(-29.849f, -3.3591f, 5.2964f); //-2.1
        groundColider1 = GroundCreator.CreateGroundCollider(loc1,10f);
        
        Vector3 loc2 = new Vector3(13.426f, -3.0762f, 2.8757f);
        groundColider2 = GroundCreator.CreateGroundCollider(loc2,4f);
    }

    public static void DeleteGroundColliders()
    {
        if (groundColider1 != null)
        {
            GameObject.Destroy(groundColider1);
        }
        if (groundColider2 != null)
        {
            GameObject.Destroy(groundColider2);
        }
    }
    
    public static void ChangeArgJsonTextAndCreator(string newText, string creatorName = null)
    {
        string mainPath = Path.Combine(Main.FolderPath, ARG_DIR_NAME);
    
        string infoPath = Path.Combine(mainPath, "tutorialInfo.json");
    
        if (!File.Exists(infoPath))
        {
            MelonLogger.Error($"Info file not found: {infoPath}");
            return;
        }
        
        try
        {
            string json = File.ReadAllText(infoPath);
            TutorialInfo info = Main.FromJson<TutorialInfo>(json);

            info.Description = newText;
            if (creatorName != null)
            {
                info.Creator = creatorName;
            }

            string newJson = Main.ToJson(info);
            File.WriteAllText(infoPath, newJson);
        
            Main.tutorialSelector?.RefreshSelector("oreotrollturbo");
        }
        catch (Exception ex)
        {
            MelonLogger.Error($"Error updating tutorial JSON: {ex.Message}");
        }
    }

    public static bool IsArgIntroTutorialPath(string path)
    {
        return path.Contains(argDirPath);
    }

    public static void JustWatchedIntro()
    {
        if (File.Exists(Path.Combine(argDirPath, ARG_AGREED_FILE_NAME)))
        {
            hasAgreedToFree = true;
            File.Create(Path.Combine(argDirPath, ARG_AGREED_FILE_NAME));
        }
        
        File.Create(Path.Combine(argDirPath, ARG_SAW_INTRO));
        hasWatchedIntro = true;
        
        
    }
}