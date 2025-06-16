using System.Collections;
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
        public static GameObject selectorText;

        public static TutorialSelector tutorialSelector;

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
    
   public class TutorialSelector
{
    public GameObject selectorText;
    
    public bool isRecording;
    public bool isPlaying;
    public bool isOnCooldown;

    private ButtonWithLabel prevButton;
    private ButtonWithLabel nextButton;
    private ButtonWithLabel forwardBackButton;
    private ButtonWithLabel playButton;
    private ButtonWithLabel recordButton;
    
    public AudioManager.ClipData currentAudio;

    public TutorialSelector(Vector3 vector, Quaternion rotation)
    {
        string directoryName = "    " + Path.GetFileName(Main.selectedTutorial) + "    ";
        
        selectorText = Calls.Create.NewText("Lorem ipsum dolor sit amet long text so stuff fits better", 
            3f, Color.white, new Vector3(), Quaternion.Euler(0f, 0f, 0f));
        
        selectorText.transform.position = vector;
        selectorText.name = "InteractiveTutorials";
        selectorText.GetComponent<TextMeshPro>().text = directoryName;

        // Define common button rotation
        Quaternion buttonRotation = Quaternion.Euler(90, rotation.y - 180, 0);
        
        // Create buttons using ButtonWithLabel class
        prevButton = new ButtonWithLabel(
            selectorText.transform.position - new Vector3(0.3f, 0.4f, 0f),
            buttonRotation,
            "Previous",
            "PrevButton",
            selectorText.transform
        );
        
        nextButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.3f, -0.4f, 0f),
            buttonRotation,
            "Next",
            "NextButton",
            selectorText.transform
        );
        
        forwardBackButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.3f, 0.0f, 0f),
            buttonRotation,
            "Forward/Back",
            "ForwardBackButton",
            selectorText.transform
        );
        
        playButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.0f, -0.6f, 0f),
            buttonRotation,
            "Start",
            "PlayButton",
            selectorText.transform
        );
        
        recordButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.7f, -0.6f, 0f),
            buttonRotation,
            "Record",
            "RecordButton",
            selectorText.transform
        );

        // Set up event listeners - FIXED: Get child(0) for InteractionButton
        prevButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => CycleTutorialBy(-1)));
        nextButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => CycleTutorialBy(1)));
        
        forwardBackButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => 
        {
            // TODO: Implement forward/back functionality
        }));
        
        playButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => 
        {
            if (isRecording) return;
            if (isPlaying) StopPlayback();
            else PlayTutorial();
        }));
        
        recordButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => 
        {
            if (isOnCooldown) return;
            isOnCooldown = true;
            MelonCoroutines.Start(CooldownCoroutine());
            if (isPlaying) isPlaying = false;
            MelonCoroutines.Start(HandleRecording());
        }));

        // Set selector rotation last to preserve button orientations
        selectorText.transform.rotation = rotation;
    }

    private void StopPlayback()
    {
        CloneBendingAPI.StopClone();
        isPlaying = false;
        AudioManager.StopPlayback(currentAudio);
    }
        
        
        private void CycleTutorialBy(int number)
        {
            if (selectorText == null)
            {
                MelonLogger.Error("Cannot skip songs because mp3Text is null.");
                return;
            }

            int currentIndex = Array.IndexOf(Main.tutorials, Main.selectedTutorial);
            if (currentIndex == -1) return;

            int newIndex = (currentIndex + number) % Main.tutorials.Length;
            if (newIndex < 0) newIndex += Main.tutorials.Length; // Ensure valid index

            string nextTutorial = Main.tutorials[newIndex];
            Main.selectedTutorial = nextTutorial;

            string actualDirName = Path.GetFileName(nextTutorial);
            
            ChangeSelectorText(actualDirName);
        }
        
        private void ChangeSelectorText(String text)
        {
            selectorText.GetComponent<TextMeshPro>().text = text;
        }

        private void PlayTutorial()
        {
            
            MelonLogger.Msg("Playing Tutorial");
            AudioManager.StopPlayback(currentAudio);
            CloneBendingAPI.StopClone();
            
            string pathToClone = Path.Combine(Main.selectedTutorial, "clone.json");
            string pathToAudio = Path.Combine(Main.selectedTutorial, "audio.wav");
            CloneBendingAPI.LoadClone(pathToClone);
            
            CloneBendingAPI.PlayClone();
            isPlaying = true;
            currentAudio = AudioManager.PlaySoundIfFileExists(pathToAudio);
        }
        
        
        private IEnumerator HandleRecording()
        {
            if ( !isRecording )
            {
                GameObject modeTextObject = Calls.Create.NewText("Lorem ipsum dolor sit amet long text so stuff fits better", 3f, Color.white, Vector3.zero, Quaternion.identity);
                modeTextObject.GetComponent<TextMeshPro>().text = "   Lights   ";
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
                MicrophoneRecorder.StopRecording();
                CloneBendingAPI.SaveClone(Main.LocalRecordedPath);
                isRecording = false;
            }
        }
        
        private IEnumerator CooldownCoroutine()
        {
            yield return new WaitForSeconds(1f);
            isOnCooldown = false;
        }
    }

    public class ButtonWithLabel
    {
        public GameObject button;
        public GameObject label;

        public ButtonWithLabel(Vector3 buttonLoc, Quaternion rotation , string labelText, string objectName, Transform parent)
        {
            button = Calls.Create.NewButton(
                buttonLoc - new Vector3(0.3f, 0.4f, 0f),
                Quaternion.Euler(90, rotation.y - 180, 0));
            
            button.name = objectName;
            
            if (parent != null)
            {
                button.transform.SetParent(parent, true);
            }
            
            label = Calls.Create.NewText(labelText, 0.5f, Color.white, Vector3.zero, Quaternion.Euler(0.0f, 0.0f, 0f));
            label.transform.position = button.transform.position + new Vector3(0f, -0.1f, 0f);
            label.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0f);
            label.transform.SetParent(button.transform, true);
            label.name = objectName + " label";
        }
    }
}