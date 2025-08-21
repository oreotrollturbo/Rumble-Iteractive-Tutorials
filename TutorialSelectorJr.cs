using Il2CppRUMBLE.Interactions.InteractionBase;
using Il2CppTMPro;
using RumbleModdingAPI;
using UnityEngine;
using File = Il2CppSystem.IO.File;
using Path = Il2CppSystem.IO.Path;

namespace InteractiveTutorials;

public class TutorialSelectorJr
{
    public GameObject selectorText;
    public bool isPlaying = false;
    private TutorialSelector.ButtonWithLabel playButton;

    public TutorialSelectorJr(Vector3 location, Quaternion rotation, string tutorialPath)
    {
        string logName = Path.GetFileName(tutorialPath);
        
        selectorText = Calls.Create.NewText(logName,
            3f, Color.white, location, Quaternion.identity);
        selectorText.name = "DebugText";
        selectorText.transform.SetParent(Main.tutorialSelector.selectorText.transform);
        selectorText.transform.rotation = rotation;
        selectorText.transform.position = location;
        
        playButton = new TutorialSelector.ButtonWithLabel(
            new Vector3(0.0f, -0.5f, 0f), // Local position offset
            "Play",
            "DebugButton",
            selectorText.transform
        );
        
        playButton.button.transform.GetChild(0).GetComponent<InteractionButton>().onPressed.AddListener(
            new Action(() =>
            {
                if (isPlaying)
                {
                    isPlaying = false;
                    TextMeshPro tmp = playButton.label.GetComponent<TextMeshPro>();
                    tmp.text = "Play";
                    
                    Main.tutorialSelector.StopPlayback();
                }
                else
                {
                    isPlaying = true;
                    TextMeshPro tmp = playButton.label.GetComponent<TextMeshPro>();
                    tmp.text = "Stop";
                    
                    Main.tutorialSelector.PlayTutorial(tutorialPath);

                    if (tutorialPath.Contains("log1"))
                    {
                        string newTextPath = Path.Combine(tutorialPath, "info.txt");

                        using (StreamWriter writer = new StreamWriter(newTextPath, false)) 
                        {
                            writer.WriteLine("QNvBA");
                            writer.Close();
                        }
                        
                        string newDesc = "I know you played one back, stop right now, or else bad things will happen";
                        string newCreator = "Y2xvCgo=trollturbo";
                                            
                        TutorialSelector.ChangeArgJsonTextAndCreator(newDesc,newCreator);
                    }
                    else if (tutorialPath.Contains("log2"))
                    {
                        string newTextPath = Path.Combine(tutorialPath, "info.txt");

                        using (StreamWriter writer = new StreamWriter(newTextPath, false)) 
                        {
                            writer.WriteLine("bMyRL6");
                            writer.Close();
                        }
                        
                        string newDesc = "STOP RIGHT NOW";
                        string newCreator = "Y2xvbmUKCg==";
                                            
                        TutorialSelector.ChangeArgJsonTextAndCreator(newDesc,newCreator);
                    }
                }
            }));
    }
}