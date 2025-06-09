using System;
using System.Collections;
using System.Reflection;
using CloneBending;
using RumbleModdingAPI;
using MelonLoader;
using HarmonyLib;
using UnityEngine;

namespace InteractiveTutorials;

public static class CloneBendingAPI
{
    public static object cloneBendingInstance;
    public static Type cloneBendingType;
    public static MelonLogger.Instance LoggerInstance;
    

    public static IEnumerator DoTestRecording()
    {
        yield return new WaitForSeconds(1f);
            
        StartRecording();
                
        yield return new WaitForSeconds(30f);
            
        MelonLogger.Msg("Stopped recording");
        StopRecording();
        SaveClone();
    }
    
    public static void SaveClone()
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
            
            if (!cloneMethod.IsStatic)
            {
                if (cloneBendingInstance == null)
                {
                    LoggerInstance.Error("Failed to create MainClass instance");
                    return;
                }
            }
            
            cloneMethod.Invoke(cloneBendingInstance, new object[] { null, EventArgs.Empty });
            //cloneMethod.Invoke(instance, null);

            LoggerInstance.Msg("SaveClone method invoked");
        }
        catch (Exception ex)
        {
            LoggerInstance.Error($"Error saving clone: {ex}");
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
}