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
        
        backButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.3f, 0.0f, 0f), //TODO weird
            buttonRotation,
            "Forward/Back",
            "ForwardBackButton",
            selectorText.transform
        );
        
        selectButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.0f, -0.6f, 0f),
            buttonRotation,
            "Select",
            "SelectButton",
            selectorText.transform
        );
        
        recordButton = new ButtonWithLabel(
            selectorText.transform.position + new Vector3(0.7f, -0.6f, 0f),
            buttonRotation,
            "Record",
            "RecordButton",
            selectorText.transform
        );

        
        prevButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => CycleTutorialBy(-1)));
        nextButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => CycleTutorialBy(1)));
        
        backButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => 
        {
            // TODO: Implement forward/back functionality
        }));
        
        selectButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => 
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

    