using CloneBending;
using CustomBattleMusic;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppTMPro;
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
        
        public static string[] tutorials = Directory.GetDirectories(FolderPath);
        public static string selectedTutorial = tutorials[0];
        public static GameObject selectorText;

        public static AudioManager.ClipData currentAudio;
        
        public override void OnLateInitializeMelon()
        {
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
            if (!Calls.Scene.GetSceneName().Equals("Gym"))
            {
                return;
            }
            
            Type mainType = typeof(MainClass);
            CloneBendingAPI.cloneBendingType = mainType;
            CloneBendingAPI.cloneBendingInstance = RegisteredMelons.FirstOrDefault
                (mod => mod.GetType() == mainType) as CloneBending.MainClass;

            Vector3 selectorLoc = new Vector3(-14.1415f, 1.6887f, 3.0538f);
            Quaternion rotation = Quaternion.Euler(4.8401f, 355.3026f, 1.9727f);
            CreateTutorialSelector(selectorLoc,rotation);
        }
        
        private static void CreateTutorialSelector(Vector3 vector, Quaternion rotation)
        {
            string directoryName = Path.GetFileName(selectedTutorial);
    
            selectorText = Calls.Create.NewText(directoryName, 3f, Color.white, new Vector3(), Quaternion.Euler(0f, 0f, 0f));
            selectorText.transform.position = vector;
            selectorText.name = "InteractiveTutorials";
    
            // Now the prevButton uses the previous 'nextButton' position, and vice versa.
            GameObject prevButton = Calls.Create.NewButton(
                selectorText.transform.position - new Vector3(0.3f, 0.4f, 0f),
                Quaternion.Euler(90, selectorText.transform.rotation.y - 180, 0));
            prevButton.transform.SetParent(selectorText.transform, true);

            GameObject nextButton = Calls.Create.NewButton(
                selectorText.transform.position + new Vector3(0.3f, -0.4f, 0f),
                Quaternion.Euler(90, selectorText.transform.rotation.y - 180, 0));
            nextButton.transform.SetParent(selectorText.transform, true);

            
            // The preview button is now positioned half the previous vertical offset difference
            GameObject previewButton = Calls.Create.NewButton(
                selectorText.transform.position + new Vector3(0.0f, -0.6f, 0f),
                Quaternion.Euler(90, selectorText.transform.rotation.y - 180, 0));
            previewButton.transform.SetParent(selectorText.transform, true);
            
            previewButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                PlayTutorial();
            }));
            
            
            GameObject previewLabel = Calls.Create.NewText("Start", 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
            previewLabel.transform.position = previewButton.transform.position + new Vector3(0f, -0.1f, 0f);
            previewLabel.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0f);
            previewLabel.transform.SetParent(previewButton.transform, true);
    
            // Set the selectorText rotation last so that button rotations remain unchanged
            selectorText.transform.rotation = rotation;
    
            prevButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                CycleTutorialBy(-1);
            }));
    
            nextButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                CycleTutorialBy(1);
            }));

            // Create text labels below each button:
            GameObject prevLabel = Calls.Create.NewText("Previous", 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
            prevLabel.transform.position = prevButton.transform.position + new Vector3(0f, -0.1f, 0f);
            prevLabel.transform.rotation = Quaternion.Euler(0.0f, 40.0f, 0f);
            prevLabel.transform.SetParent(prevButton.transform, true);
    
            GameObject nextLabel = Calls.Create.NewText("Next", 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
            nextLabel.transform.position = nextButton.transform.position + new Vector3(0f, -0.1f, 0f);
            nextLabel.transform.rotation = Quaternion.Euler(0.0f, 40.0f, 0f);
            nextLabel.transform.SetParent(nextButton.transform, true);
        }
        
        
        public static void CycleTutorialBy(int number)
        {
            if (selectorText == null)
            {
                MelonLogger.Error("Cannot skip songs because mp3Text is null.");
                return;
            }

            int currentIndex = Array.IndexOf(tutorials, selectedTutorial);
            if (currentIndex == -1) return;

            int newIndex = (currentIndex + number) % tutorials.Length;
            if (newIndex < 0) newIndex += tutorials.Length; // Ensure valid index

            string nextTutorial = tutorials[newIndex];
            selectedTutorial = nextTutorial;

            string actualDirName = Path.GetFileName(nextTutorial);
            
            ChangeSelectorText(actualDirName);
        }
        
        private static void ChangeSelectorText(String text)
        {
            selectorText.GetComponent<TextMeshPro>().text = text;
        }

        private static void PlayTutorial()
        {
            MelonLogger.Msg("Playing Tutorial");
            AudioManager.StopPlayback(currentAudio);
            CloneBendingAPI.StopClone();
            MelonLogger.Msg("Start button pressed");
            string pathToClone = Path.Combine(selectedTutorial, "clone.json");
            string pathToAudio = Path.Combine(selectedTutorial, "audio.mp3");
            CloneBendingAPI.LoadClone(pathToClone);
            CloneBendingAPI.PlayClone();
            AudioManager.PlaySoundIfFileExists(pathToAudio);
        }
    }
}