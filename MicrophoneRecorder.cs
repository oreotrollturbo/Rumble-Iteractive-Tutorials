namespace InteractiveTutorials;

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using MelonLoader;

public static class MicrophoneRecorder
{
    private static AudioClip _recordingClip;
    private static bool _isRecording = false;
    
    public static void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            MelonLogger.Error("No microphone detected!");
            return;
        }
        
        string micDevice = Microphone.devices[0]; // Use first available mic
        _recordingClip = Microphone.Start(
            deviceName: micDevice,
            loop: false,
            lengthSec: 300, // Max 5 minutes
            frequency: 44100
        );
        
        if (_recordingClip == null)
        {
            MelonLogger.Error("Failed to start recording!");
            return;
        }
        
        _isRecording = true;
        MelonLogger.Msg($"Recording started with {micDevice}");
    }
    
    public static void StopRecordingAndSave(string filePath)
    {
        if (!_isRecording) return;
        
        int recordingPosition = Microphone.GetPosition(null);
        Microphone.End(null);
        
        SaveWav(_recordingClip, filePath, recordingPosition);
        _isRecording = false;
        MelonLogger.Msg($"Recording saved to: {filePath}");
    }
    
    public static void SaveWav(AudioClip clip, string path, int trimPosition)
    {
        // Convert audio data to WAV byte array
        float[] samples = new float[clip.samples * clip.channels];
        clip.GetData(samples, 0);
        
        byte[] wavBytes = EncodeToWav(
            samples: samples,
            sampleCount: trimPosition * clip.channels,
            frequency: clip.frequency,
            channels: clip.channels
        );
        
        File.WriteAllBytes(path, wavBytes);
    }
    
    private static byte[] EncodeToWav(float[] samples, int sampleCount, int frequency, int channels)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            // WAV header
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + sampleCount * 2); // File size
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16); // PCM chunk size
            writer.Write((ushort)1); // PCM format
            writer.Write((ushort)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * 2); // Byte rate
            writer.Write((ushort)(channels * 2)); // Block align
            writer.Write((ushort)16); // Bits per sample
            
            // Data chunk
            writer.Write("data".ToCharArray());
            writer.Write(sampleCount * 2);
            
            // Convert float samples to 16-bit integers
            for (int i = 0; i < sampleCount; i++)
            {
                writer.Write((short)(samples[i] * short.MaxValue));
            }
            
            return stream.ToArray();
        }
    }
}