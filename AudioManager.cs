﻿using System.Collections;
using MelonLoader;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using UnityEngine;

namespace CustomBattleMusic;
public static class AudioManager
{
    public class ClipData
    {
        public WaveOutEvent WaveOut { get; set; }
        public ISampleProvider VolumeProvider { get; set; }
        public AudioFileReader Reader { get; set; }
        public long PausedPosition { get; set; } = 0;
        public bool IsPaused { get; set; } = false;
    }

    private static IEnumerator PlaySound(ClipData clipData, bool loop)
    {
        // Take local references that won't change
        var waveOut = clipData.WaveOut;
        var reader = clipData.Reader;
    
        if (waveOut == null || reader == null) yield break;

        do {
            reader.Position = 0;
            waveOut.Play();
        
            while (waveOut.PlaybackState == PlaybackState.Playing)
            {
                yield return null;
            
                // Check if we should abort
                if (clipData.WaveOut != waveOut) // Was replaced
                    yield break;
            }
        
        } while (loop && clipData.WaveOut == waveOut);
    
        // Only dispose if this is still the active clip
        if (clipData.WaveOut == waveOut)
        {
            waveOut.Dispose();
            reader.Dispose();
        }
    }
    

    public static ClipData PlaySoundIfFileExists(string soundFilePath, float volume = 1.0f, bool loop = false)
    {
        if (!File.Exists(soundFilePath))
        {
            MelonLogger.Error($"Audio file not found: {soundFilePath}");
            return null;
        }

        var reader = new AudioFileReader(soundFilePath);
        var volumeProvider = new VolumeSampleProvider(reader)
        {
            Volume = Mathf.Clamp01(volume)
        };

        var waveOut = new WaveOutEvent();
        waveOut.Init(volumeProvider);

        var clipData = new ClipData
        {
            WaveOut = waveOut,
            VolumeProvider = volumeProvider,
            Reader = reader
        };

        MelonCoroutines.Start(PlaySound(clipData, loop));
        return clipData;
    }


    public static void PausePlayback(ClipData clipData)
    {
        if (clipData == null || clipData.WaveOut == null)
        {
            MelonLogger.Warning("Clipdata or waveout is null cant pause");
            return;
        }
        
        clipData.PausedPosition = clipData.Reader.Position;
        clipData.IsPaused = true;
        clipData.WaveOut.Pause();
    }

    public static void ResumePlayback(ClipData clipData)
    {
        if (clipData == null || clipData.WaveOut == null)
        {
            MelonLogger.Warning("Clipdata or waveout is null cant resume");
            return;
        }

        clipData.IsPaused = false;
        clipData.WaveOut.Play();
    }

    public static void ChangeVolume(ClipData clipData, float volume)
    {
        if (clipData == null || clipData.VolumeProvider == null)
        {
            return;
        }

        if (clipData.VolumeProvider is VolumeSampleProvider volumeProvider)
        {
            volumeProvider.Volume = Mathf.Clamp01(volume);
        }
    }

    public static void StopPlayback(ClipData clipData)
    {
        if (clipData == null)
        {
            MelonLogger.Warning("Attempted to stop playback on a null clipData.");
            return;
        }

        if (clipData.WaveOut != null)
        {
            clipData.WaveOut.Stop();
            clipData.WaveOut.Dispose();
        }

        if (clipData.Reader != null)
        {
            clipData.Reader.Dispose();
        }

        clipData.WaveOut = null;
    }
}