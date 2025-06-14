﻿using System.IO;
using UnityEngine;
using MelonLoader;

namespace InteractiveTutorials;

public class MicrophoneRecorder
{
    private static AudioSource micSource;
    private static AudioClip recordedClip;
    private static string device;
    private static bool isRecording = false;

    public static void InitMicRecording()
    {
        MelonLogger.Msg("Microphone Recorder Mod Loaded.");

        GameObject go = new GameObject("MicRecorder");
        //UnityEngine.Object.DontDestroyOnLoad(go);
        micSource = go.AddComponent<AudioSource>();
    }

    public static void StartRecording()
    {
        if (isRecording) return;

        if (Microphone.devices.Length == 0)
        {
            MelonLogger.Warning("No microphone devices found.");
            return;
        }

        device = Microphone.devices[(int)Main.microphoneIndex.Value];
        recordedClip = Microphone.Start(device, true, 1800, 44100);

        micSource.clip = recordedClip;
        micSource.loop = true;

        while (!(Microphone.GetPosition(device) > 0)) { } // wait for mic to start

        if ((bool)Main.hearYourself.Value)
        {
            micSource.Play();
        }
        
        isRecording = true;
        MelonLogger.Msg("Microphone recording started.");
    }

    public static void StopAndSave(string path)
    {
	    if (!isRecording) return;

	    // figure out how many samples were actually recorded
	    int pos = Microphone.GetPosition(device);
	    Microphone.End(device);
	    micSource.Stop();
	    isRecording = false;

	    /// Copy only the “used” samples into a new AudioClip
	    float[] data = new float[pos * recordedClip.channels];
	    recordedClip.GetData(data, 0);

	    AudioClip trimmed = AudioClip.Create("TrimmedRecording", pos, recordedClip.channels, 
		    recordedClip.frequency, false);
	    trimmed.SetData(data, 0);

	    // now save 'trimmed' to WAV (or whatever) at `path`
	    SavWav.Save(path,trimmed);

	    MelonLogger.Msg($"Saved {pos / (float)recordedClip.frequency:F1}s of audio to {path}");
	    
	    PlayRecording();
    }


    
    public static void PlayRecording()
    {
        if (recordedClip == null || isRecording)
        {
            MelonLogger.Warning("No recording available to play or recording is still ongoing.");
            return;
        }

        micSource.clip = recordedClip;
        micSource.loop = false;
        micSource.Play();

        MelonLogger.Msg("Playing back recorded audio.");
    }
}

//####################################
//           SAVE TO WAV             #
//####################################
public static class SavWav {

	const int HEADER_SIZE = 44;

	public static bool Save(string filepath, AudioClip clip) {

		Debug.Log(filepath);

		// Make sure directory exists if user is saving to sub dir.
		Directory.CreateDirectory(Path.GetDirectoryName(filepath));

		using (var fileStream = CreateEmpty(filepath)) {

			ConvertAndWrite(fileStream, clip);

			WriteHeader(fileStream, clip);
		}

		return true; // TODO: return false if there's a failure saving the file
	}

	public static AudioClip TrimSilence(AudioClip clip, float min) {
		var samples = new float[clip.samples];

		clip.GetData(samples, 0);

		return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
	}

	public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz) {
		return TrimSilence(samples, min, channels, hz, false, false);
	}

	public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz, bool _3D, bool stream) {
		int i;

		for (i=0; i<samples.Count; i++) {
			if (Mathf.Abs(samples[i]) > min) {
				break;
			}
		}

		samples.RemoveRange(0, i);

		for (i=samples.Count - 1; i>0; i--) {
			if (Mathf.Abs(samples[i]) > min) {
				break;
			}
		}

		samples.RemoveRange(i, samples.Count - i);

		var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, _3D, stream);

		clip.SetData(samples.ToArray(), 0);

		return clip;
	}

	static FileStream CreateEmpty(string filepath) {
		var fileStream = new FileStream(filepath, FileMode.Create);
	    byte emptyByte = new byte();

	    for(int i = 0; i < HEADER_SIZE; i++) //preparing the header
	    {
	        fileStream.WriteByte(emptyByte);
	    }

		return fileStream;
	}

	static void ConvertAndWrite(FileStream fileStream, AudioClip clip) {

		var samples = new float[clip.samples];

		clip.GetData(samples, 0);

		Int16[] intData = new Int16[samples.Length];
		//converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

		Byte[] bytesData = new Byte[samples.Length * 2];
		//bytesData array is twice the size of
		//dataSource array because a float converted in Int16 is 2 bytes.

		int rescaleFactor = 32767; //to convert float to Int16

		for (int i = 0; i<samples.Length; i++) {
			intData[i] = (short) (samples[i] * rescaleFactor);
			Byte[] byteArr = new Byte[2];
			byteArr = BitConverter.GetBytes(intData[i]);
			byteArr.CopyTo(bytesData, i * 2);
		}

		fileStream.Write(bytesData, 0, bytesData.Length);
	}

	static void WriteHeader(FileStream fileStream, AudioClip clip) {

		var hz = clip.frequency;
		var channels = clip.channels;
		var samples = clip.samples;

		fileStream.Seek(0, SeekOrigin.Begin);

		Byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
		fileStream.Write(riff, 0, 4);

		Byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
		fileStream.Write(chunkSize, 0, 4);

		Byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
		fileStream.Write(wave, 0, 4);

		Byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
		fileStream.Write(fmt, 0, 4);

		Byte[] subChunk1 = BitConverter.GetBytes(16);
		fileStream.Write(subChunk1, 0, 4);

		UInt16 two = 2;
		UInt16 one = 1;

		Byte[] audioFormat = BitConverter.GetBytes(one);
		fileStream.Write(audioFormat, 0, 2);

		Byte[] numChannels = BitConverter.GetBytes(channels);
		fileStream.Write(numChannels, 0, 2);

		Byte[] sampleRate = BitConverter.GetBytes(hz);
		fileStream.Write(sampleRate, 0, 4);

		Byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
		fileStream.Write(byteRate, 0, 4);

		UInt16 blockAlign = (ushort) (channels * 2);
		fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

		UInt16 bps = 16;
		Byte[] bitsPerSample = BitConverter.GetBytes(bps);
		fileStream.Write(bitsPerSample, 0, 2);

		Byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
		fileStream.Write(datastring, 0, 4);

		Byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
		fileStream.Write(subChunk2, 0, 4);

//		fileStream.Close();
	}
}
