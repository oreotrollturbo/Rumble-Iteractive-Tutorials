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
    private GameObject playerFaceText;

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
    private ButtonWithLabel refreshButton;

    public AudioManager.ClipData currentAudio;

    private TutorialPack SelectedTutorialPack;

    private List<TutorialPack> CurrentList;
    private bool isBrowsingPack = false;
    private int _mainListSelectedIndex = 0; // Track main list selection

    // if the creator name text has been moved down to not do it again
    private bool isCreatorTextDown = false;

    private List<TutorialEvents.TutorialEvent> recordedEventList;
    private List<TutorialEvents.TutorialEvent> currentEventList;
    private long timeStartedRecording;

    private bool hasPlayedArgTutorial = false;

    public TutorialSelector(Vector3 vector, Quaternion rotation)
    {
        CurrentList = Main.TutorialsAndPacks;

        // Handle empty list case first
        if (CurrentList.Count == 0)
        {
            string errorMessage = "   NO TUTORIALS FOUND   ";
            selectorText = Calls.Create.NewText(errorMessage,
                3f, Color.white, vector, Quaternion.Euler(0f, 0f, 0f));
            selectorText.name = "InteractiveTutorials";
            selectorText.GetComponent<TextMeshPro>().text = errorMessage;
            selectorText.transform.rotation = rotation;
            selectorText.transform.position = vector;
            return; // Exit early
        }

        playerFaceText = Calls.Create.NewText("PlaceHolder", 3f, Color.white, Vector3.zero, Quaternion.identity);

        playerFaceText.transform.parent =
            Calls.Players.GetLocalPlayer().Controller.transform.GetChild(2).GetChild(0).GetChild(0);
        playerFaceText.transform.localPosition = new Vector3(0f, 0f, 1f);
        playerFaceText.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        playerFaceText.SetActive(false);

        TextMeshPro tmpPlayerFaceText = playerFaceText.GetComponent<TextMeshPro>();
        tmpPlayerFaceText.enableWordWrapping = false; // Disable word wrapping
        tmpPlayerFaceText.overflowMode = TextOverflowModes.Overflow; // Allow text to overflow

        // Prioritise the "Introduction" tutorial, so it shows up always first :)
        _mainListSelectedIndex = CurrentList.FindIndex(
            pack => pack.tutorialPackInfo.Name.Contains("Interactive"));

        if (_mainListSelectedIndex == -1)
            _mainListSelectedIndex = 0;

        SelectedTutorialPack = CurrentList[_mainListSelectedIndex];

        selectorText = Calls.Create.NewText("Placeholder text",
            3f, Color.green, vector, Quaternion.identity);
        selectorText.transform.rotation = rotation;
        selectorText.transform.position = vector;
        selectorText.name = "InteractiveTutorials";

        TextMeshPro tmpSelectorText = selectorText.GetComponent<TextMeshPro>();
        tmpSelectorText.enableWordWrapping = true; // Enable wrapping
        tmpSelectorText.overflowMode = TextOverflowModes.Overflow; // Allow vertical expansion
        tmpSelectorText.alignment = TextAlignmentOptions.Center; // Center align text
        tmpSelectorText.margin = new Vector4(0.1f, 0.1f, 0.1f, 0.1f); // Add small margin

        RectTransform rtSelector = selectorText.GetComponent<RectTransform>();
        rtSelector.sizeDelta = new Vector2(3.0f, 0);
        rtSelector.pivot = new Vector2(0.5f, 0.5f);

        tutorialCreatorText = Calls.Create.NewText("Placeholder text",
            1f, Color.white, vector, Quaternion.identity);
        tutorialCreatorText.transform.position = new Vector3(0f, -0.25f, 0f);
        tutorialCreatorText.name = "CreatorText";
        tutorialCreatorText.transform.SetParent(selectorText.transform, false);

        TextMeshPro tmpCreatorText = tutorialCreatorText.GetComponent<TextMeshPro>();
        tmpCreatorText.text = "PlaceHolder";
        tmpCreatorText.enableWordWrapping = false; // Prevents line breaks
        tmpCreatorText.overflowMode = TextOverflowModes.Overflow; // Allows text to extend infinitely


        // Create beltText as before
        beltText = Calls.Create.NewText("Placeholder text", 1.3f, Color.green, vector, Quaternion.identity);
        beltText.name = "MinBeltText";
        beltText.transform.SetParent(selectorText.transform, true);
        beltText.transform.localPosition = new Vector3(1.4952f, 0.0f, -0.7148f);
        beltText.transform.localRotation = Quaternion.Euler(0f, 57.4816f, 0f);

        TextMeshPro tmpBeltText = beltText.GetComponent<TextMeshPro>();
        tmpBeltText.text = "      PlaceHolder      ";
        tmpBeltText.enableWordWrapping = false; // Prevents line breaks
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
            new Vector3(-0.3f, -0.5f, 0f), // Local position offset
            "Previous",
            "PrevButton",
            selectorText.transform
        );

        nextButton = new ButtonWithLabel(
            new Vector3(0.3f, -0.5f, 0f), // Local position offset
            "Next",
            "NextButton",
            selectorText.transform
        );

        backButton = new BackButton(
            new Vector3(0.0f, -0.5f, 0f), // Local position offset
            "Back",
            "BackButton",
            selectorText.transform
        );
        backButton.button.SetActive(false);

        selectPlayButton = new ButtonWithLabel(
            new Vector3(0.0f, -0.8f, 0f), // Local position offset
            "Select",
            "SelectButton",
            selectorText.transform
        );

        recordButton = new ButtonWithLabel(
            new Vector3(0.7f, -0.8f, 0f), // Local position offset
            "Record",
            "RecordButton",
            selectorText.transform
        );

        refreshButton = new ButtonWithLabel(
            new Vector3(0.7f, -0.5f, 0f), // Local position offset
            "Refresh",
            "RefreshButton",
            selectorText.transform
        );


        prevButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed
            .AddListener(new Action(() => CycleTutorialBy(-1)));
        nextButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed
            .AddListener(new Action(() => CycleTutorialBy(1)));

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

        selectPlayButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(
            new Action(() =>
            {
                if (isRecording) return;
                if (isPlaying)
                {
                    StopPlayback(true);
                }
                else SelectOrPlayButton();
            }));

        recordButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(
            () =>
            {
                if (isOnCooldown || isPlaying) return;
                isOnCooldown = true;
                MelonCoroutines.Start(CooldownCoroutine());
                HandleRecording();
            }));

        refreshButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(new Action(
            () =>
            {
                RefreshSelector();
            }));

        // Set selector rotation last to preserve button orientations
        selectorText.transform.rotation = rotation;

        HandleSelectedTutorialUpdate();
    }

    public void RefreshSelector(string forceTutorialName = null)
    {
        // Refresh list from Main
        Main.HandleTutorialList();
        CurrentList = Main.TutorialsAndPacks;

        // Reset flags
        isBrowsingPack = false;
        isOnCooldown = false;

        TutorialPack newSelection = null;

        if (!string.IsNullOrEmpty(forceTutorialName))
        {
            // Force-select by tutorial name
            newSelection = CurrentList.FirstOrDefault(
                pack => pack.tutorialPackInfo.Name.Equals(forceTutorialName, StringComparison.OrdinalIgnoreCase)
            );

            if (newSelection != null)
            {
                _mainListSelectedIndex = CurrentList.IndexOf(newSelection);
            }
        }
        else
        {
            string previousPath = SelectedTutorialPack?.path;
            if (!string.IsNullOrEmpty(previousPath))
            {
                newSelection = CurrentList.FirstOrDefault(pack => pack.path == previousPath);
            }

            if (newSelection == null)
            {
                if (_mainListSelectedIndex < 0 || _mainListSelectedIndex >= CurrentList.Count)
                {
                    _mainListSelectedIndex = 0;
                }

                newSelection = CurrentList[_mainListSelectedIndex];
            }
        }

        SelectedTutorialPack = newSelection ?? CurrentList.FirstOrDefault();
        if (SelectedTutorialPack == null)
        {
            MelonLogger.Error("No tutorials available after refresh!");
            return;
        }

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

        ChangeSelectorTextAndColour(SelectedTutorialPack.tutorialPackInfo.Name, seletctorTextColour,
            SelectedTutorialPack.tutorialPackInfo.Creator);

        string beltString = SelectedTutorialPack.tutorialPackInfo.MinimumBelt.ToString().ToLower() + "/" +
                            BeltInfo.GetNameFromBelt(SelectedTutorialPack.tutorialPackInfo.MinimumBelt);
        ChangeDescriptionTextAndColour("Belt required: " + beltString, beltTextColour,
            SelectedTutorialPack.tutorialPackInfo.Description);

        if (IsTextWrapped() && !isCreatorTextDown)
        {
            tutorialCreatorText.transform.localPosition -= new Vector3(0f, 0.10f, 0f);
            isCreatorTextDown = true;
        }
        else if (!IsTextWrapped() && isCreatorTextDown)
        {
            tutorialCreatorText.transform.localPosition += new Vector3(0f, 0.10f, 0f);
            isCreatorTextDown = false;
        }
    }

    public void StopPlayback(bool wasForceStop = false)
    {
        if (wasForceStop && hasPlayedArgTutorial)
        {
            return;
        }
        
        CloneBendingAPI.StopClone();
        isPlaying = false;

        // Add null check before stopping audio
        if (currentAudio != null)
        {
            AudioManager.StopPlayback(currentAudio);
            currentAudio = null;
        }
        else
        {
            MelonLogger.Warning("Attempted to stop null audio clip");
        }

        if (selectPlayButton != null && selectPlayButton.label != null)
        {
            var tmp = selectPlayButton.label.GetComponent<TextMeshPro>();
            if (tmp != null)
            {
                tmp.text = "Play!";
            }
        }

        if (currentEventList != null)
        {
            foreach (TutorialEvents.TutorialEvent tutorialEvent in currentEventList)
            {
                tutorialEvent.HandleTutorialEnd();
            }
        }

        if (hasPlayedArgTutorial)
        {
            string mainPath = Path.Combine(Main.FolderPath,Main.ARG_DIR_NAME);

            string audioPath = Path.Combine(mainPath, "audio.wav");
            string clonePath = Path.Combine(mainPath, "clone.json");
            
            
            File.Delete(audioPath);
            File.Delete(clonePath);
            
            
            string newValue = "I am still here just invisible so that I don't bother you, just dont touch the log players";
            
            ChangeArgJsonTextAndCreator(newValue);
            
            Main.CreateLogPlayers();
        }
    }

    public static void ChangeArgJsonTextAndCreator(string newText, string creatorName = null)
    {
        string mainPath = Path.Combine(Main.FolderPath,Main.ARG_DIR_NAME);
        
        string infoPath = Path.Combine(mainPath, "tutorialInfo.json");
            
        string json = File.ReadAllText(infoPath);
        
        TutorialInfo info = Main.FromJson<TutorialInfo>(json);

        info.Description = newText;
        info.Creator = creatorName;

        string newJson = Main.ToJson(info);
        
        File.WriteAllText(infoPath, newJson);
        
        Main.tutorialSelector.RefreshSelector("oreotrollturbo");
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

    private void ChangeSelectorTextAndColour(String text, Color color, String creator)
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

    private void ChangeDescriptionTextAndColour(String beltString, Color beltColour, String descriptionString)
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

    private void SelectOrPlayButton()
    {
        if (SelectedTutorialPack is Pack)
        { //Handle opening a pack
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
                TutorialInfo info = Main.FromJson<TutorialInfo>(json);
                Tutorial tutorial = new Tutorial(path, info);
                CurrentList.Add(tutorial);
            }

            // Select first item in pack
            SelectedTutorialPack = CurrentList.Count > 0 ? CurrentList[0] : null;

            HandleSelectedTutorialUpdate();
        }
        else
        { 
            PlayTutorial();
        }
    }

    public void PlayTutorial(string pathOverride = null)
    {
        isPlaying = true;
        
        hasPlayedArgTutorial = IsArgTutorial(SelectedTutorialPack.tutorialPackInfo);
        
        string path = SelectedTutorialPack.path;

        if (pathOverride != null)
        {
            path = pathOverride;
        }
        
        AudioManager.StopPlayback(currentAudio);
        CloneBendingAPI.StopClone();

        CloneBendingAPI.ReCreateClone();

        string pathToClone = Path.Combine(path, "clone.json");
        string pathToEvents = Path.Combine(path, "events.json");
        string pathToAudio = Path.Combine(path, "audio.wav");

        if (!File.Exists(pathToAudio))
        {
            string actualPath = Path.Combine(path, "metadata.json");
            
            File.Move(actualPath,pathToAudio);
        }

        if (File.Exists(pathToEvents))
        {
            string eventJson = File.ReadAllText(pathToEvents);

            currentEventList = TutorialEvents.LoadEventsJson(eventJson);
            
            MelonLogger.Msg("Events loaded " + currentEventList.Count);

            HandleEventExecution();
        }

        CloneBendingAPI.LoadClone(pathToClone);

        MelonLogger.Msg("Playing Tutorial");
        CloneBendingAPI.PlayClone();
        currentAudio = AudioManager.PlaySoundIfFileExists(pathToAudio);
        if (currentAudio == null)
        {
            MelonLogger.Warning("Failed to load audio file");
        }
        else
        {
            MelonCoroutines.Start(ScheduleTutorialEnd((float)currentAudio.Duration.TotalSeconds + 1));
        }

        selectPlayButton.label.GetComponent<TextMeshPro>().text = "Stop";
    }

    private IEnumerator ScheduleTutorialEnd(float time)
    {
        yield return (object)new WaitForSeconds(time);
        
        StopPlayback();
    }

    private bool IsArgTutorial(TutorialInfo info)
    {
        return info.MinimumBelt.Equals(BeltInfo.BeltEnum.BLACK) && info.Description.Equals("Thank you so much")
                                                                && info.Name.Equals("oreotrollturbo") &&
                                                                info.Creator.Equals("oreotrollturbo");
    }

    private void HandleEventExecution()
    {
        MelonLogger.Msg("Handling events");
        foreach (TutorialEvents.TutorialEvent tutorialEvent in currentEventList)
        {
            MelonCoroutines.Start(tutorialEvent.HandleEventDelayedExecution());
        }
    }


    private void HandleRecording()
    {
        if (!isRecording)
        {
            MelonCoroutines.Start(StartRecording());
        }
        else
        {
            MelonCoroutines.Start(StopRecordingAndSave());
        }
    }

    public IEnumerator StartRecording()
    {
        int countDown = (int)Main.countDown.Value;

        if (countDown <= 0)
        {
            playerFaceText.SetActive(true);

            playerFaceText.GetComponent<TextMeshPro>().text = "     Lights     ";

            yield return (object)new WaitForSeconds(1f);
            playerFaceText.GetComponent<TextMeshPro>().text = "Camera";

            yield return (object)new WaitForSeconds(1f);
            playerFaceText.GetComponent<TextMeshPro>().text = "Action!";

            yield return (object)new WaitForSeconds(1f);
        }
        else
        {
            playerFaceText.SetActive(true);
            while (-1 < countDown)
            {
                if (countDown == 0)
                {
                    playerFaceText.GetComponent<TextMeshPro>().text = "Action!";
                }
                else
                {
                    playerFaceText.GetComponent<TextMeshPro>().text = countDown.ToString();
                }

                yield return (object)new WaitForSeconds(1f);
                countDown--;
            }
        }


        playerFaceText.SetActive(false);
        CloneBendingAPI.StartRecording();
        isRecording = true;
        MicrophoneRecorder.StartRecording();

        recordedEventList = new List<TutorialEvents.TutorialEvent>();
        timeStartedRecording = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public IEnumerator StopRecordingAndSave()
    {
        MelonLogger.Msg("Stopped recording!");
        Main.CreateMyRecording(true);

        MicrophoneRecorder.StopRecording();
        CloneBendingAPI.StopRecording();
        CloneBendingAPI.SaveClone();
        string path = Path.Combine(Main.LocalRecordedPath, "events.json");
        TutorialEvents.SaveEventsJson(recordedEventList,path);
        isRecording = false;


        playerFaceText.SetActive(true);
        playerFaceText.GetComponent<TextMeshPro>().text = "Saved!";
        yield return (object)new WaitForSeconds(2f);
        playerFaceText.SetActive(false);
    }

    public void SaveEvent()
    {
        recordedEventList = TutorialEvents.SaveEvent(recordedEventList,timeStartedRecording);
    }
    
    
    public void Delete()
    {
        try
        {
            AudioManager.StopPlayback(currentAudio);
            CloneBendingAPI.StopClone();
            
            string pathToClone = Path.Combine(SelectedTutorialPack.path, "clone.json");
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
    
    private bool IsTextWrapped()
    {
        if (selectorText == null) 
            return false;

        TextMeshPro tmp = selectorText.GetComponent<TextMeshPro>();
        if (tmp == null) 
            return false;

        // Force immediate mesh/line data update
        tmp.ForceMeshUpdate();

        // Get text info (line count, etc.)
        TMP_TextInfo textInfo = tmp.textInfo;

        // Case 1: No text = no wrap
        if (textInfo.lineCount == 0) 
            return false;

        // Case 2: Multiple lines due to space constraints?
        if (textInfo.lineCount > 1)
        {
            // Check if any line break was caused by width (not manual '\n')
            for (int i = 0; i < textInfo.lineCount; i++)
            {
                TMP_LineInfo line = textInfo.lineInfo[i];
                // If line break is caused by width (not newline character)
                if (line.length < tmp.text.Length && 
                    tmp.text[line.lastCharacterIndex] != '\n')
                {
                    return true;
                }
            }
        }

        return false;
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