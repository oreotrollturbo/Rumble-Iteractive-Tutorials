using System.Collections;
using System.Text.Json;
using System.Text.Json.Serialization;
using MelonLoader;
using RumbleModdingAPI;
using UnityEngine;

namespace InteractiveTutorials;

public class TutorialEvents
{
    
    public static List<TutorialEvent> LoadEventsJson(string eventJson)
    {
        var options = new JsonSerializerOptions
        {
            Converters =
            {
                new TutorialEventConverter(),
                new ColorConverter(),
                new Vector3Converter()
            }
        };
    
        return JsonSerializer.Deserialize<List<TutorialEvent>>(eventJson, options);
    }
    
    public static void SaveEventsJson(List<TutorialEvent> recordedEventList, string path)
    {
        if (recordedEventList == null || recordedEventList.Count == 0)
        {
            MelonLogger.Msg("No events to save");
            return;
        }

        MelonLogger.Msg("Saving events json");

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters =
            {
                new TutorialEventConverter(),
                new ColorConverter(),
                new Vector3Converter()
            }
        };

        string json = JsonSerializer.Serialize(recordedEventList, options);
        File.WriteAllText(path, json);

        MelonLogger.Msg($"Saved {recordedEventList.Count} events to {path}");
    }


    public static List<TutorialEvent> SaveEvent(List<TutorialEvent> recordedEventList, long timeStartedRecording)
    {
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        float timeDelay = (now - timeStartedRecording) / 1000f; // keep sub-second precision

        TutorialEvent tutorialEvent = new GenericEvent(timeDelay);

        recordedEventList.Add(tutorialEvent);
        MelonLogger.Msg($"Event saved at {timeDelay} seconds");

        return recordedEventList;
    }

    
    [JsonConverter(typeof(TutorialEventConverter))]
    public abstract class TutorialEvent
    {
        public float TriggerTime { get; set; }
    
        protected TutorialEvent(float triggerTime)
        {
            TriggerTime = triggerTime;
        }

        public IEnumerator HandleEventDelayedExecution()
        {
            MelonLogger.Msg("Executing event of type " + GetType());

            float remainingTime = TriggerTime;

            while (remainingTime > 0f)
            {
                if (!Main.tutorialSelector.isPlaying)
                {
                    yield break;
                }
                    

                float step = Mathf.Min(1f, remainingTime);
                yield return new WaitForSeconds(step);
                remainingTime -= step;
            }

            if (!Main.tutorialSelector.isPlaying)
                yield break;

            ExecuteEvent();
        }
    
        protected abstract void ExecuteEvent();
        public abstract void HandleTutorialEnd();
    }

// Custom converter for TutorialEvent hierarchy
public class TutorialEventConverter : JsonConverter<TutorialEvent>
{
    public override TutorialEvent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("$type", out JsonElement typeElement))
            throw new JsonException("Missing type discriminator");

        string typeDiscriminator = typeElement.GetString();

        return typeDiscriminator switch //TODO add new ones
        {
            "GenericEvent" => JsonSerializer.Deserialize<GenericEvent>(root.GetRawText(), options),
            "TogglePlayerModelEvent" => JsonSerializer.Deserialize<TogglePlayerModelEvent>(root.GetRawText(), options),
            "CreateTextBoxEvent" => JsonSerializer.Deserialize<CreateTextBoxEvent>(root.GetRawText(), options),
            _ => throw new JsonException($"Unknown type discriminator: {typeDiscriminator}")
        };
    }

    public override void Write(Utf8JsonWriter writer, TutorialEvent value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        
        // Write type discriminator
        writer.WriteString("$type", value.GetType().Name);
        
        // Serialize actual properties
        switch (value)
        {
            case GenericEvent generic:
                JsonSerializer.Serialize(writer, generic, options);
                break;
            case TogglePlayerModelEvent toggle:
                JsonSerializer.Serialize(writer, toggle, options);
                break;
            case CreateTextBoxEvent textbox:
                JsonSerializer.Serialize(writer, textbox, options);
                break;
            default:
                throw new JsonException($"Unsupported type: {value.GetType()}");
        }
        
        writer.WriteEndObject();
    }
}

// Converters for Unity types
public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        float r = 0, g = 0, b = 0, a = 1;
        
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                reader.Read();
                
                switch (prop)
                {
                    case "r": r = reader.GetSingle(); break;
                    case "g": g = reader.GetSingle(); break;
                    case "b": b = reader.GetSingle(); break;
                    case "a": a = reader.GetSingle(); break;
                }
            }
        }
        
        return new Color(r, g, b, a);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("r", value.r);
        writer.WriteNumber("g", value.g);
        writer.WriteNumber("b", value.b);
        writer.WriteNumber("a", value.a);
        writer.WriteEndObject();
    }
}

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        float x = 0, y = 0, z = 0;
        
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string prop = reader.GetString();
                reader.Read();
                
                switch (prop)
                {
                    case "x": x = reader.GetSingle(); break;
                    case "y": y = reader.GetSingle(); break;
                    case "z": z = reader.GetSingle(); break;
                }
            }
        }
        
        return new Vector3(x, y, z);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.x);
        writer.WriteNumber("y", value.y);
        writer.WriteNumber("z", value.z);
        writer.WriteEndObject();
    }
}
    
    /**
     * A generic event created by default, does nothing but stores data
     * This is so that if the creator wants to make it into an event, the necessary data is already there
     */
    public class GenericEvent : TutorialEvent
    {
        public float HandX { get; set; }
        public float HandY { get; set; }
        public float HandZ { get; set; }
        public string WarningText { get; set; }
        public string WarningText2 { get; set; }
        public string WarningText3 { get; set; }

        public GenericEvent(float triggerTime) : base(triggerTime){
            Transform playerHand = GameObject.Find("Player Controller(Clone)").transform.GetChild(2).GetChild(1); //Left hand

            HandX = playerHand.position.x;
            HandY = playerHand.position.y;
            HandZ = playerHand.position.z;

            WarningText = "Hey, I am a dummy event type, change me into an actual event (as described in the README.md) to use me";
            WarningText2 = "Don't delete 'triggerTime' as it stores when you triggered the event in the tutorial";
            WarningText3 = "And don't delete 'HandLocation' either, it stores your hand's location when you triggered the event which will be useful";
        }
    
        protected override void ExecuteEvent() { }
        public override void HandleTutorialEnd() { }
    }

    public class TogglePlayerModelEvent : TutorialEvent
    {
        public TogglePlayerModelEvent(float triggerTime) : base(triggerTime) { }
    
        protected override void ExecuteEvent()
        {
            GameObject player = GameObject.Find("Player Controller(Clone)");
            if (player != null && player.transform.childCount > 1)
            {
                GameObject playerVisuals = player.transform.GetChild(1).gameObject;
                playerVisuals.SetActive(!playerVisuals.activeSelf);
            }
            else
            {
                MelonLogger.Warning("Player Controller or child not found in HandleEvent(DISABLE_PLAYER_MODEL)");
            }
        }
    
        public override void HandleTutorialEnd()
        {
            GameObject player = GameObject.Find("Player Controller(Clone)");
            if (player != null && player.transform.childCount > 1)
            {
                GameObject playerVisuals = player.transform.GetChild(1).gameObject;
                playerVisuals.SetActive(true);
            }
            else
            {
                MelonLogger.Warning("Player Controller or child not found in ShowPlayerModel");
            }
        }
    }
    
    public class CreateTextBoxEvent : TutorialEvent
    {
        public string Text { get; set; }
    
        [JsonConverter(typeof(ColorConverter))]
        public Color Colour { get; set; }
    
        public float TimeExisting { get; set; }
        public float Size { get; set; }
    
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 Location { get; set; }

        [JsonIgnore]
        public GameObject TextBox { get; set; }

        public CreateTextBoxEvent(float triggerTime, string text, float timeExisting,
            Vector3 location, Color colour, float size)
            : base(triggerTime)
        {
            Text = text;
            TimeExisting = timeExisting;
            Location = location;
            Colour = colour;
            Size = size;
        }
    
        protected override void ExecuteEvent()
        {
            // MelonLogger.Warning("TextBoxEventRunning");
            // MelonLogger.Warning("text: " + Text);
            // MelonLogger.Warning("timeExisting: " + TimeExisting);
            // MelonLogger.Warning("Location: " + Location);
            // MelonLogger.Warning("Colour: " + Colour);
            // MelonLogger.Warning("Size: " + Size);
            
            Quaternion lookRotation = GetFlatLookRotation(Location, ((Component)Camera.main).transform.position);
    
            TextBox = Calls.Create.NewText(Text, Size, Colour, Location, lookRotation);
            TextBox.transform.position = Location;
            TextBox.transform.rotation = lookRotation;
            
            MelonCoroutines.Start(DeleteTextAfterDelay());
        }
    
        private IEnumerator DeleteTextAfterDelay()
        {
            yield return new WaitForSeconds(TimeExisting);
            if (TextBox != null)
            {
                GameObject.Destroy(TextBox);
            }
        }
    
        public override void HandleTutorialEnd()
        {
            if (TextBox != null)
            {
                GameObject.Destroy(TextBox);
            }
        }
    
        public static Quaternion GetFlatLookRotation(Vector3 objectPosition, Vector3 lookAtPosition)
        {
            Vector3 dir = objectPosition - lookAtPosition; // flipped
            dir.y = 0f; // keep it flat
            if (dir.sqrMagnitude < 0.0001f)
                return Quaternion.identity;

            return Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

    }
}