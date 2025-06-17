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
    public GameObject tutorialCreatorText;
    public GameObject beltText;
    public GameObject descriptionText;
    
    public bool isRecording;
    public bool isPlaying;
    public bool isOnCooldown;

    private ButtonWithLabel prevButton;
    private ButtonWithLabel nextButton;
    private BackButton backButton;
    private ButtonWithLabel selectPlayButton;
    private ButtonWithLabel recordButton;
    
    public AudioManager.ClipData currentAudio;
    
    private TutorialPack SelectedTutorialPack;

    private List<TutorialPack> CurrentList;
    private bool isBrowsingPack = false;
    private int _mainListSelectedIndex = 0; // Track main list selection

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

        // Initialize main list selection
        _mainListSelectedIndex = CurrentList.FindIndex(
            pack => pack.tutorialPackInfo.Name == "Introduction");
        
        if (_mainListSelectedIndex == -1) 
            _mainListSelectedIndex = 0;
        
        SelectedTutorialPack = CurrentList[_mainListSelectedIndex];
    
        string tutorialName = "            " + SelectedTutorialPack.tutorialPackInfo.Name + "            ";
        selectorText = Calls.Create.NewText("Placeholder text", 
            3f, Color.green, vector, Quaternion.identity);
        selectorText.transform.rotation = rotation;
        selectorText.transform.position = vector;
        selectorText.name = "InteractiveTutorials";
        
        TextMeshPro tmpSelectorText = selectorText.GetComponent<TextMeshPro>();
        tmpSelectorText.text = tutorialName;
        tmpSelectorText.enableWordWrapping = false;       // Prevents line breaks
        tmpSelectorText.overflowMode = TextOverflowModes.Overflow; // Allows text to extend infinitely
        
        tutorialCreatorText = Calls.Create.NewText("Placeholder text", 
            1f, Color.white, vector, Quaternion.identity);
        tutorialCreatorText.transform.position = new Vector3(0f,-0.25f, 0f);
        tutorialCreatorText.name = "CreatorText";
        tutorialCreatorText.transform.SetParent(selectorText.transform, false);
        
        TextMeshPro tmpCreatorText = tutorialCreatorText.GetComponent<TextMeshPro>();
        tmpCreatorText.text = "PlaceHolder";
        tmpCreatorText.enableWordWrapping = false;       // Prevents line breaks
        tmpCreatorText.overflowMode = TextOverflowModes.Overflow; // Allows text to extend infinitely
        
        
        // Create beltText as before
        beltText = Calls.Create.NewText("Placeholder text", 1.3f, Color.green, vector, Quaternion.identity);
        beltText.name = "MinBeltText";
        beltText.transform.SetParent(selectorText.transform, true);
        beltText.transform.localPosition = new Vector3(1.4952f, 0.0f, -0.7148f);
        beltText.transform.localRotation = Quaternion.Euler(0f, 57.4816f, 0f);
        
        TextMeshPro tmpBeltText = beltText.GetComponent<TextMeshPro>();
        tmpBeltText.text = "      PlaceHolder      ";
        tmpBeltText.enableWordWrapping = false;       // Prevents line breaks
        tmpBeltText.overflowMode = TextOverflowModes.Overflow; // Allows text to extend infinitely
        
        
        descriptionText = Calls.Create.NewText("Placeholder text", 1f, Color.white, vector, Quaternion.identity);
        descriptionText.name = "DescriptionText";
        descriptionText.transform.SetParent(selectorText.transform, true);
        descriptionText.transform.localPosition = new Vector3(1.4952f, -0.15f, -0.7148f);
        descriptionText.transform.localRotation = Quaternion.Euler(0f, 57.4816f, 0f);
        descriptionText.GetComponent<TextMeshPro>().text = "         PlaceHolder         ";
        
        RectTransform rt = descriptionText.GetComponent<RectTransform>();
        rt.pivot = new Vector2(0.5f, 1f);
        

        
        // Create buttons using ButtonWithLabel class
        prevButton = new ButtonWithLabel(
            new Vector3(-0.3f, -0.5f, 0f),  // Local position offset
            "Previous",
            "PrevButton",
            selectorText.transform
        );

        nextButton = new ButtonWithLabel(
            new Vector3(0.3f, -0.5f, 0f),   // Local position offset
            "Next",
            "NextButton",
            selectorText.transform
        );

        backButton = new BackButton(
            new Vector3(0.0f, -0.5f, 0f),    // Local position offset
            "Back",
            "BackButton",
            selectorText.transform
        );
        backButton.button.SetActive(false);

        selectPlayButton = new ButtonWithLabel(
            new Vector3(0.0f, -0.8f, 0f),   // Local position offset
            "Select",
            "SelectButton",
            selectorText.transform
        );

        recordButton = new ButtonWithLabel(
            new Vector3(0.7f, -0.8f, 0f),   // Local position offset
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
            
            // Return to main list without clearing
            CurrentList = Main.TutorialsAndPacks;
            
            // Restore previous selection
            if (_mainListSelectedIndex < CurrentList.Count)
            {
                SelectedTutorialPack = CurrentList[_mainListSelectedIndex];
            }
            else
            {
                // Handle case where list changed
                SelectedTutorialPack = CurrentList[0];
                _mainListSelectedIndex = 0;
            }
            
            HandleSelectedTutorialUpdate();
        }));
        
        selectPlayButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(() => 
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

        HandleSelectedTutorialUpdate();
    }

    private void HandleSelectedTutorialUpdate()
    {
        Color beltTextColour;

        if (Main.playerBP >= BeltInfo.GetBpFromEnum(SelectedTutorialPack.tutorialPackInfo.MinimumBelt))
        {
            beltTextColour = Color.green;
        }
        else
        {
            beltTextColour = Color.red;
        }

        Color seletctorTextColour = Color.white;

        if (SelectedTutorialPack is Pack)
        {
            seletctorTextColour = Color.yellow;

            selectPlayButton.label.GetComponent<TextMeshPro>().text = "Select";
        }
        else
        {
            selectPlayButton.label.GetComponent<TextMeshPro>().text = "Play !";
        }
        
        ChangeSelectorTextAndColour(SelectedTutorialPack.tutorialPackInfo.Name,seletctorTextColour,SelectedTutorialPack.tutorialPackInfo.Creator);
        
        string beltString = SelectedTutorialPack.tutorialPackInfo.MinimumBelt.ToString().ToLower() + "/" +
                            BeltInfo.GetNameFromBelt(SelectedTutorialPack.tutorialPackInfo.MinimumBelt);
        ChangeDescriptionTextAndColour("Belt required: " + beltString,beltTextColour, SelectedTutorialPack.tutorialPackInfo.Description);
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
            MelonLogger.Error("Cannot skip songs because selctor is null.");
            return;
        }

        int currentIndex = CurrentList.IndexOf(SelectedTutorialPack);
        if (currentIndex == -1) return;

        int count = CurrentList.Count;
        int newIndex = (currentIndex + number) % count;
        if (newIndex < 0) newIndex += count; // Ensure valid index

        // Update main list index if in root view
        if (!isBrowsingPack)
        {
            _mainListSelectedIndex = newIndex;
        }

        TutorialPack nextTutorial = CurrentList[newIndex];
        SelectedTutorialPack = nextTutorial;
        
        HandleSelectedTutorialUpdate();
    }
        
    private void ChangeSelectorTextAndColour(String? text, Color color, String creator)
    {
        if (text != null)
        {
            selectorText.GetComponent<TextMeshPro>().text = text;
        }

        if (color != null)
        {
            selectorText.GetComponent<TextMeshPro>().color = color;
        }

        if (creator != null)
        {
            tutorialCreatorText.GetComponent<TextMeshPro>().text = creator;
        }
    }

    private void ChangeDescriptionTextAndColour(String? beltString, Color beltColour, String descriptionString)
    {
        if (beltString != null)
        {
            beltText.GetComponent<TextMeshPro>().text = beltString;
        }

        if (beltColour != null)
        {
            beltText.GetComponent<TextMeshPro>().color = beltColour;
        }

        if (descriptionString != null)
        {
            descriptionText.GetComponent<TextMeshPro>().text = descriptionString;
        }
    }
    
    private void ChangeSelectorCreatorTextAndColour(String? text, Color color)
    {
        if (text != null)
        {
            tutorialCreatorText.GetComponent<TextMeshPro>().text = text;
        }

        if (color != null)
        {
            tutorialCreatorText.GetComponent<TextMeshPro>().color = color;
        }
    }

    private void SelectEntryButton()
    {
        if (SelectedTutorialPack is Pack)
        {
            // Save current position before switching lists
            if (!isBrowsingPack)
            {
                _mainListSelectedIndex = CurrentList.IndexOf(SelectedTutorialPack);
            }

            isBrowsingPack = true;
            backButton.button.SetActive(true);
            
            // Clear current list and load pack contents
            CurrentList = new List<TutorialPack>();
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

            // Select first item in pack
            SelectedTutorialPack = CurrentList.Count > 0 ? CurrentList[0] : null;
            
            HandleSelectedTutorialUpdate();
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

        public ButtonWithLabel(Vector3 localPosition, string labelText, string objectName, Transform parent)
        {
            // Create button at origin with identity rotation
            button = Calls.Create.NewButton(
                Vector3.zero, 
                Quaternion.identity
            );
    
            button.name = objectName;
            button.transform.SetParent(parent, false); // Set parent while maintaining local space
    
            // Set local transforms
            button.transform.localPosition = localPosition;
            button.transform.localRotation = Quaternion.Euler(90, 180, 0);
    
            // Create label as child of button
            label = Calls.Create.NewText(labelText, 0.5f, Color.white, Vector3.zero, Quaternion.identity);
            label.name = objectName + " label";
            label.transform.SetParent(button.transform, false);
            label.transform.localPosition = new Vector3(0f, 0.12f, 0f); // Position below button   0f, 0f, 0.12f
            label.transform.localRotation = Quaternion.Euler(90, 180, 0);
            
            TextMeshPro tmp = label.GetComponent<TextMeshPro>();
            tmp.text = labelText;
            tmp.enableWordWrapping = false;       // Prevents line breaks
            tmp.overflowMode = TextOverflowModes.Overflow; // Allows text to extend infinitely
        }
    }
    
    public class BackButton
    {
        public GameObject button;
        public GameObject label;

        public BackButton(Vector3 localPosition, string labelText, string objectName, Transform parent)
        {
            // Create button at origin with identity rotation
            button = Calls.Create.NewButton(
                Vector3.zero, 
                Quaternion.identity
            );
    
            button.name = objectName;
            button.transform.SetParent(parent, false); // Set parent while maintaining local space
    
            // Set local transforms
            button.transform.localPosition = localPosition;
            button.transform.localRotation = Quaternion.Euler(90, 180, 0);
    
            // Create label as child of button
            label = Calls.Create.NewText(labelText, 0.5f, Color.white, Vector3.zero, Quaternion.identity);
            label.name = objectName + " label";
            label.transform.SetParent(button.transform, false);
            label.transform.localPosition = new Vector3(-0.32f, 0.12f, 0f); // Position below button   0f, 0f, 0.12f
            label.transform.localRotation = Quaternion.Euler(90, 180, 0);
            
            TextMeshPro tmp = label.GetComponent<TextMeshPro>();
            tmp.text = labelText;
            tmp.enableWordWrapping = false;       // Prevents line breaks
            tmp.overflowMode = TextOverflowModes.Overflow; // Allows text to extend infinitely
        }
    }
}