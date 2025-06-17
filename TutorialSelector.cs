using System.Collections;
using CustomBattleMusic;
using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppTMPro;
using MelonLoader;
using RumbleModdingAPI;
using UnityEngine;

namespace InteractiveTutorials;

public class TutorialSelector
{
    public GameObject selectorText;
    
    public bool isRecording;
    public bool isPlaying;
    public bool isOnCooldown;

    private ButtonWithLabel prevButton;
    private ButtonWithLabel nextButton;
    private ButtonWithLabel backButton;
    private ButtonWithLabel selectButton;
    private ButtonWithLabel recordButton;
    
    public AudioManager.ClipData currentAudio;
    
    private TutorialPack SelectedTutorialPack;

    private List<TutorialPack> CurrentList;
    private bool isBrowsingPack = false;

    public TutorialSelector(Vector3 vector, Quaternion rotation)
    {
        CurrentList = Main.TutorialsAndPacks;

        // Handle empty list case first
        if (CurrentList.Count == 0)
        {
            string errorMessage = "   NO TUTORIALS FOUND   ";
            selectorText = Calls.Create.NewText(errorMessage, 
                3f, Color.red, vector, Quaternion.Euler(0f, 0f, 0f));
            selectorText.name = "InteractiveTutorials";
            selectorText.GetComponent<TextMeshPro>().text = errorMessage;
            selectorText.transform.rotation = rotation;
            selectorText.transform.position = vector;
            return; // Exit early
        }

        // Proceed if list has items
        SelectedTutorialPack = CurrentList.FirstOrDefault(pack => pack.tutorialPackInfo.Name == "Introduction") ?? CurrentList[0];
    
        string tutorialName = "            " + SelectedTutorialPack.tutorialPackInfo.Name + "            ";
        selectorText = Calls.Create.NewText("Placeholder text", 
            3f, Color.green, vector, Quaternion.identity);
        selectorText.transform.rotation = rotation;
        selectorText.transform.position = vector;
        selectorText.name = "InteractiveTutorials";
        selectorText.GetComponent<TextMeshPro>().text = tutorialName;

        // Define common button rotation
        Quaternion buttonRotation = Quaternion.Euler(90, rotation.y - 180, 0);
        
        // Create buttons using ButtonWithLabel class
        prevButton = new ButtonWithLabel(
            selectorText.transform.position - new Vector3(0.3f, 0.1f, 0f),
            buttonRotation,
            "Previous",
            "PrevButton",
            selectorText.transform
        );
        
        nextButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.3f, -0.1f, 0f),
            buttonRotation,
            "Next",
            "NextButton",
            selectorText.transform
        );
        
        backButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.3f, 0.3f, 0f),
            buttonRotation,
            "Forward/Back",
            "ForwardBackButton",
            selectorText.transform
        );
        backButton.button.SetActive(false);
        
        selectButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.0f, -0.3f, 0f),
            buttonRotation,
            "Select",
            "SelectButton",
            selectorText.transform
        );
        
        recordButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.7f, -0.3f, 0f),
            buttonRotation,
            "Record",
            "RecordButton",
            selectorText.transform
        );


        
        prevButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => CycleTutorialBy(-1)));
        nextButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => CycleTutorialBy(1)));
        
        backButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() =>
        {
            isBrowsingPack = false;
            backButton.button.SetActive(false);
            
            CurrentList.Clear();
            CurrentList = Main.TutorialsAndPacks;
            
            SelectedTutorialPack = CurrentList[0];
            
            Color color;

            if (SelectedTutorialPack is Pack)
            {
                if ( Main.playerBP >= BeltInfo.GetBpFromEnum(SelectedTutorialPack.tutorialPackInfo.MinimumBelt) )
                {
                    color = Color.yellow;
                }
                else
                {
                    color = Color.red;
                }
            }
            else
            {
                if (Main.playerBP >= BeltInfo.GetBpFromEnum(SelectedTutorialPack.tutorialPackInfo.MinimumBelt))
                {
                    color = Color.green;
                }
                else
                {
                    color = Color.red;
                }
            }
            
            ChangeSelectorTextAndColour(SelectedTutorialPack.tutorialPackInfo.Name,color);
        }));
        
        selectButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => 
        {
            if (isRecording) return;
            if (isPlaying) StopPlayback();
            else SelectEntryButton();
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

    public void StopPlayback()
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

            int currentIndex = CurrentList.IndexOf(SelectedTutorialPack);
            if (currentIndex == -1) return;

            int count = CurrentList.Count;
            int newIndex = (currentIndex + number) % count;
            if (newIndex < 0) newIndex += count; // Ensure valid index

            TutorialPack nextTutorial = CurrentList[newIndex];
            SelectedTutorialPack = nextTutorial;

            string tutorialName = nextTutorial.tutorialPackInfo.Name;

            Color color;

            if (SelectedTutorialPack is Pack)
            {
                if (Main.playerBP >= BeltInfo.GetBpFromEnum(SelectedTutorialPack.tutorialPackInfo.MinimumBelt))
                {
                    color = Color.yellow;
                }
                else
                {
                    color = Color.red;
                }
            }
            else
            {
                if (Main.playerBP >= BeltInfo.GetBpFromEnum(SelectedTutorialPack.tutorialPackInfo.MinimumBelt))
                {
                    color = Color.green;
                }
                else
                {
                    color = Color.red;
                }
            }
            
            ChangeSelectorTextAndColour(tutorialName, color);
        }
        
        private void ChangeSelectorTextAndColour(String? text, Color color)
        {
            if (text != null)
            {
                selectorText.GetComponent<TextMeshPro>().text = text;
            }

            if (color != null)
            {
                selectorText.GetComponent<TextMeshPro>().color = color;
            }
        }

        private void SelectEntryButton()
        {

            if (SelectedTutorialPack is Pack)
            {
                isBrowsingPack = true;
                backButton.button.SetActive(true);
                CurrentList.Clear();

                string[] tutorialPaths = Directory.GetDirectories(SelectedTutorialPack.path);

                foreach (string path in tutorialPaths)
                {
                    if (!File.Exists(Path.Combine(path, "tutorialInfo.json")))
                    {
                        continue;
                    }
                    
                    string json = File.ReadAllText(Path.Combine(path, "tutorialInfo.json"));
                    TutorialInfo info = TutorialInfo.FromJson(json);
                    Tutorial tutorial = new Tutorial(path,info);
                    CurrentList.Add(tutorial);
                }

                SelectedTutorialPack = CurrentList[0];
                
                Color color;

                if (SelectedTutorialPack is Pack)
                {
                    if (Main.playerBP >= BeltInfo.GetBpFromEnum(SelectedTutorialPack.tutorialPackInfo.MinimumBelt))
                    {
                        color = Color.yellow;
                    }
                    else
                    {
                        color = Color.red;
                    }
                }
                else
                {
                    if (Main.playerBP >= BeltInfo.GetBpFromEnum(SelectedTutorialPack.tutorialPackInfo.MinimumBelt))
                    {
                        color = Color.green;
                    }
                    else
                    {
                        color = Color.red;
                    }
                }
                
                ChangeSelectorTextAndColour(SelectedTutorialPack.tutorialPackInfo.Name,color);
            }
            else
            {
                AudioManager.StopPlayback(currentAudio);
                CloneBendingAPI.StopClone();
                
                string pathToClone = Path.Combine(SelectedTutorialPack.path, "clone.json");
                string pathToAudio = Path.Combine(SelectedTutorialPack.path, "audio.wav");
                CloneBendingAPI.LoadClone(pathToClone);
            
                MelonLogger.Msg("Playing Tutorial");
                CloneBendingAPI.PlayClone();
                isPlaying = true;
                currentAudio = AudioManager.PlaySoundIfFileExists(pathToAudio);
            }
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

        public void Delete()
        {
            try
            {
                AudioManager.StopPlayback(currentAudio);
                CloneBendingAPI.StopClone();
                
                string pathToClone = Path.Combine(SelectedTutorialPack.path, "clone.json");
                string pathToAudio = Path.Combine(SelectedTutorialPack.path, "audio.wav");
                CloneBendingAPI.LoadClone(pathToClone);
            
                GameObject.Destroy(selectorText);
            }
            catch (Exception e) { }
        }
        
        private IEnumerator CooldownCoroutine()
        {
            yield return new WaitForSeconds(1f);
            isOnCooldown = false;
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

    