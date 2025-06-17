using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using CloneBending;
using RumbleModdingAPI;
using MelonLoader;
using HarmonyLib;
using UnityEngine;
using Directory = Il2CppSystem.IO.Directory;
using Path = Il2CppSystem.IO.Path;

namespace InteractiveTutorials;

public static class CloneUploadInterceptor
{
    public static bool IsCustomUpload = true;
    public static string path;

    public static string GetPath()
    {
        if (IsCustomUpload)
        {
            IsCustomUpload = false;
            return path;
        }
        else
        {
            return "UserData/CloneBending/clone.json";
        } 
    }
}


public static class CloneBendingAPI
{
    public static object cloneBendingInstance;
    public static Type cloneBendingType;
    public static MelonLogger.Instance LoggerInstance;
    
    public static void SaveClone(string? path) //TODO
    {
        Type cloneType = cloneBendingType;
        MethodInfo cloneMethod = AccessTools.Method(cloneType, "downloadClone");
    
        if (cloneMethod == null)
        {
            LoggerInstance.Error("Method not found!");
            return;
        }
        
        try
        {
            cloneMethod.Invoke(cloneBendingInstance, new object[] { null, EventArgs.Empty });

            LoggerInstance.Msg("Saving clone to special directory");


            string destPath = Path.Combine(Main.LocalRecordedPath, "clone.json");
            
            File.Delete(destPath);
            
            File.Move("UserData/CloneBending/clone.json", destPath,true);
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Error saving clone: {ex}");
        }
    }
    
    public static void LoadClone(string? path)
    {
        Type cloneType = cloneBendingType;
        MethodInfo cloneMethod = AccessTools.Method(cloneType, "uploadClone");
    
        if (cloneMethod == null)
        {
            LoggerInstance.Error("Method not found!");
            return;
        }

        if (path == null)
        {
            CloneUploadInterceptor.IsCustomUpload = false;
        }
        else
        {
            CloneUploadInterceptor.IsCustomUpload = true;
            CloneUploadInterceptor.path = path;
        }
        
        try
        {
            cloneMethod.Invoke(cloneBendingInstance, new object[] { null, EventArgs.Empty });
            LoggerInstance.Msg("Uploading clone");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Error uploading clone: {ex}");
        }
    }

    public static void StartRecording()
    {
        Type mainType = cloneBendingType;
        object instance = cloneBendingInstance;

        // Access private fields
        FieldInfo handPositionsField = AccessTools.Field(mainType, "handPositions");
        FieldInfo allRotationsField = AccessTools.Field(mainType, "allRotations");
        FieldInfo recordedStacksField = AccessTools.Field(mainType, "recordedStacks");
        FieldInfo indicatorField = AccessTools.Field(mainType, "indicator");

        if (handPositionsField == null || allRotationsField == null || recordedStacksField == null || indicatorField == null)
        {
            LoggerInstance.Error("One or more fields not found");
            return;
        }

        // Clear the LinkedLists using the correct types
        ((LinkedList<float[]>)handPositionsField.GetValue(instance)).Clear();
        ((LinkedList<Quaternion[]>)allRotationsField.GetValue(instance)).Clear();
        ((LinkedList<Il2CppRUMBLE.MoveSystem.Stack>)recordedStacksField.GetValue(instance)).Clear();

        // Set static field
        FieldInfo isRecordingField = AccessTools.Field(mainType, "isRecording");
        if (isRecordingField != null && isRecordingField.IsStatic)
        {
            isRecordingField.SetValue(null, true);
        }

        // Call SetActive(true) on indicator
        GameObject indicator = (GameObject)indicatorField.GetValue(instance);
        if (indicator != null)
        {
            indicator.SetActive(true);
        }

        LoggerInstance.Msg("Started recording!");
    }


    public static void StopRecording()
    {
        Type mainType = cloneBendingType;

        FieldInfo isRecordedField = AccessTools.Field(mainType, "isRecorded");
        FieldInfo isRecordingField = AccessTools.Field(mainType, "isRecording");
        FieldInfo isPOnCooldownField = AccessTools.Field(mainType, "isPOnCooldown");
        FieldInfo indicatorField = AccessTools.Field(mainType, "indicator");

        if (isRecordedField != null) isRecordedField.SetValue(cloneBendingInstance, true);
        if (isRecordingField != null) isRecordingField.SetValue(cloneBendingInstance, false);
        if (isPOnCooldownField != null) isPOnCooldownField.SetValue(cloneBendingInstance, false);

        
        if (indicatorField == null)
        {
            LoggerInstance.Error("Indicator field not found.");
            return;
        }

        GameObject indicator = (GameObject)indicatorField.GetValue(cloneBendingInstance);
        if (indicator != null)
        {
            indicator.SetActive(false);
        }
        
        LoggerInstance.Msg("Recording stopped.");
    }

    public static void PlayClone()
    {
        FieldInfo currentFrame = AccessTools.Field(cloneBendingType, "curFrame"); //0
        FieldInfo isPlaying = AccessTools.Field(cloneBendingType, "isPlaying"); //true
        FieldInfo isROnCooldownField = AccessTools.Field(cloneBendingType, "isPOnCooldown"); //true
        
        if (currentFrame != null) currentFrame.SetValue(cloneBendingInstance, 0);
        if (isPlaying != null) isPlaying.SetValue(cloneBendingInstance, true);
        if (isROnCooldownField != null) isROnCooldownField.SetValue(cloneBendingInstance, true);
        
        LoggerInstance.Msg("Playing recording.");
    }

    public static void StopClone()
    {
        try 
        {
            // Get MainClass instance
            var mainClassType = Type.GetType("CloneBending.MainClass, CloneBending");
            var instanceField = mainClassType.GetField("instance", BindingFlags.Public | BindingFlags.Static);
            var instance = instanceField.GetValue(null);
        
            // Set isPlaying to false
            var isPlayingField = mainClassType.GetField("isPlaying", BindingFlags.Static | BindingFlags.NonPublic);
            isPlayingField.SetValue(null, false);
        
            // Deactivate clone objects
            var clonePlayerField = mainClassType.GetField("clonePlayer", BindingFlags.Instance | BindingFlags.NonPublic);
            var bodyDoubleField = mainClassType.GetField("bodyDouble", BindingFlags.Instance | BindingFlags.NonPublic);
        
            var clonePlayer = (GameObject)clonePlayerField.GetValue(instance);
            var bodyDouble = (GameObject)bodyDoubleField.GetValue(instance);
        
            clonePlayer?.SetActive(false);
            bodyDouble?.SetActive(false);
        }
        catch (Exception ex)
        {
            MelonLogger.Msg($"Error stopping clone: {ex.Message}");
        }
    }
    
    [HarmonyPatch(typeof(MainClass), "uploadClone")]
    public static class UploadClonePatch
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);
            var target = "UserData/CloneBending/clone.json";
            
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].opcode == OpCodes.Ldstr && codes[i].operand as string == target)
                {
                    codes[i] = new CodeInstruction(OpCodes.Call, typeof(CloneUploadInterceptor).GetMethod("GetPath"));
                }
            }
    
            return codes.AsEnumerable();
        }
    }
}