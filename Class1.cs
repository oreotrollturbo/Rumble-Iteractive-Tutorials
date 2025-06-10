using System.Collections;
using System.Reflection.Emit;
using CloneBending;
using HarmonyLib;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppRUMBLE.Managers;
using Il2CppTMPro;
using RumbleModdingAPI;
using MelonLoader;
using MelonLoader.Utils;
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
        public static string FolderPath => Path.Combine(MelonEnvironment.UserDataDirectory, "InteractiveTutorials");
        public static string LocalRecordedPath => Path.Combine(FolderPath, "MyRecording");
        
        public static string[] tutorials = Directory.GetDirectories(FolderPath);
        public static string selectedTutorial = tutorials[0];
        public static GameObject selectorText;

        public static AudioManager.ClipData currentAudio;

        public static bool isRecording;
        public static bool isPlaying;
        public static bool isOnCooldown;
        
        public override void OnLateInitializeMelon()
        {
            CloneBendingAPI.LoggerInstance = LoggerInstance;
            Calls.onMapInitialized += SceneLoaded;
            
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

            Vector3 selectorLoc = new Vector3(-14.9161f, 1.6887f, 2.6538f);
            Quaternion rotation = Quaternion.Euler(4.8401f, 355.3026f, 1.9727f);
            CreateTutorialSelector(selectorLoc,rotation);
        }
        
        private static void CreateTutorialSelector(Vector3 vector, Quaternion rotation)
        {
            string directoryName = Path.GetFileName(selectedTutorial);
    
            selectorText = Calls.Create.NewText("Lorem ipsum dolor sit amet long text so stuff fits better"
                , 3f, Color.white, new Vector3(), Quaternion.Euler(0f, 0f, 0f));
            
            selectorText.transform.position = vector;
            selectorText.name = "InteractiveTutorials";
            selectorText.GetComponent<TextMeshPro>().text = directoryName;
    
            // Now the prevButton uses the previous 'nextButton' position, and vice versa.
            GameObject prevButton = Calls.Create.NewButton(
                selectorText.transform.position - new Vector3(0.3f, 0.4f, 0f),
                Quaternion.Euler(90, selectorText.transform.rotation.y - 180, 0));
            prevButton.transform.SetParent(selectorText.transform, true);
            
            prevButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                CycleTutorialBy(-1);
            }));
            

            GameObject nextButton = Calls.Create.NewButton(
                selectorText.transform.position + new Vector3(0.3f, -0.4f, 0f),
                Quaternion.Euler(90, selectorText.transform.rotation.y - 180, 0));
            nextButton.transform.SetParent(selectorText.transform, true);
            
            nextButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                CycleTutorialBy(1);
            }));

            
            // The preview button is now positioned half the previous vertical offset difference
            GameObject playButton = Calls.Create.NewButton(
                selectorText.transform.position + new Vector3(0.0f, -0.6f, 0f),
                Quaternion.Euler(90, selectorText.transform.rotation.y - 180, 0));
            playButton.transform.SetParent(selectorText.transform, true);
            
            playButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                PlayTutorial();
            }));
            
            
            GameObject recordButton = Calls.Create.NewButton(
                selectorText.transform.position + new Vector3(0.7f, -0.6f, 0f),
                Quaternion.Euler(90, selectorText.transform.rotation.y - 180, 0));
            recordButton.transform.SetParent(selectorText.transform, true);
            
            recordButton.transform.GetChild(0).gameObject.GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
            {
                if (isOnCooldown) return;
                isOnCooldown = true;
                MelonCoroutines.Start(CooldownCoroutine());
                if (isPlaying)
                {
                    isPlaying = false;
                    return;
                }
                MelonCoroutines.Start(HandleRecording());
            }));
            
            
            GameObject previewLabel = Calls.Create.NewText("Start", 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
            previewLabel.transform.position = playButton.transform.position + new Vector3(0f, -0.1f, 0f);
            previewLabel.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0f);
            previewLabel.transform.SetParent(playButton.transform, true);
    
            // Set the selectorText rotation last so that button rotations remain unchanged
            selectorText.transform.rotation = rotation;
            

            // Create text labels below each button:
            GameObject prevLabel = Calls.Create.NewText("Previous", 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
            prevLabel.transform.position = prevButton.transform.position + new Vector3(0f, -0.1f, 0f);
            prevLabel.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0f);
            prevLabel.transform.SetParent(prevButton.transform, true);
    
            GameObject nextLabel = Calls.Create.NewText("Next", 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
            nextLabel.transform.position = nextButton.transform.position + new Vector3(0f, -0.1f, 0f);
            nextLabel.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0f);
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
            
            string pathToClone = Path.Combine(selectedTutorial, "clone.json");
            string pathToAudio = Path.Combine(selectedTutorial, "audio.wav");
            CloneBendingAPI.LoadClone(pathToClone);
            
            CloneBendingAPI.PlayClone();
            isPlaying = true;
            currentAudio = AudioManager.PlaySoundIfFileExists(pathToAudio);
        }
        
        
        private static IEnumerator HandleRecording()
        {
            if ( !isRecording )
            {
                GameObject modeTextObject = Calls.Create.NewText("Lorem ipsum dolor sit amet long text so stuff fits better", 3f, Color.white, Vector3.zero, Quaternion.identity);
                modeTextObject.GetComponent<TextMeshPro>().text = "Lights";
                modeTextObject.transform.parent = Calls.Players.GetLocalPlayer().Controller.transform.GetChild(1).transform.GetChild(0).transform.GetChild(0).transform;
                modeTextObject.transform.localPosition = new Vector3(0f, 0f, 1f);
                modeTextObject.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            
                yield return (object) new WaitForSeconds(1f);
                modeTextObject.GetComponent<TextMeshPro>().text = "Camera";
            
                yield return (object) new WaitForSeconds(1f);
                modeTextObject.GetComponent<TextMeshPro>().text = "Action!";
            
                yield return (object) new WaitForSeconds(1f);
                GameObject.Destroy(modeTextObject);
            
                CloneBendingAPI.StartRecording();
                isRecording = true;
                MicrophoneRecorder.StartRecording();
            }
            else
            {
                CloneBendingAPI.StopRecording();
                MicrophoneRecorder.StopRecordingAndSave(LocalRecordedPath);
                CloneBendingAPI.SaveClone(LocalRecordedPath);
                isRecording = false;
            }
        }
        
        private static IEnumerator CooldownCoroutine()
        {
            yield return new WaitForSeconds(1f);
            isOnCooldown = false;
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

                isPlaying = false;
        
                // Only stop if we have a valid clip
                if (currentAudio != null)
                {
                    AudioManager.StopPlayback(currentAudio);
                    currentAudio = null; // Clear the reference after stopping
                }
            }
        }
    }
}