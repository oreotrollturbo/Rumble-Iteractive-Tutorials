using System;
using System.IO;
using NAudio.Wave;
using MelonLoader;
#nullable disable
namespace InteractiveTutorials;

    public class MicrophoneRecorder
    {
        private static WaveInEvent waveIn;
        private static WaveFileWriter writer;
        private static string filePath = "UserData/InteractiveTutorials/MyRecording/audio.wav";

        public static void StartRecording()
        {
            try
            {
                waveIn = new WaveInEvent();
                waveIn.WaveFormat = new WaveFormat(44100, 1); // 44.1 kHz mono

                writer = new WaveFileWriter(filePath, waveIn.WaveFormat);
                waveIn.DataAvailable += (object s, WaveInEventArgs a) =>
                {
                    writer.Write(a.Buffer, 0, a.BytesRecorded);
                };

                waveIn.RecordingStopped += (object s, StoppedEventArgs a) =>
                {
                    if (writer != null) writer.Dispose();
                    waveIn.Dispose();
                    MelonLogger.Msg("Recording stopped and saved.");
                };

                waveIn.StartRecording();
                MelonLogger.Msg("Recording started...");
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error starting recording: " + ex.Message);
            }
        }

        public static void StopRecording()
        {
            try
            {
                if (waveIn != null) waveIn.StopRecording();
            }
            catch (Exception ex)
            {
                MelonLogger.Error("Error stopping recording: " + ex.Message);
            }
        }
    }
