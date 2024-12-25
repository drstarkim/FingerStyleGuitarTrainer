using UnityEngine;
using System.Collections.Generic;

public class GuitarChordGrader : MonoBehaviour
{
    public int sampleSize = 1024; // Size of the FFT window
    private AudioSource audioSource;
    private float[] spectrumData;
    private float sampleRate;

    private float gradingThreshold = 5.0f; // Tolerance in Hz for detecting notes

    // Chord definitions (basic examples)
    private Dictionary<string, List<string>> chords = new Dictionary<string, List<string>>()
    {
        { "G", new List<string> { "G", "B", "D" } },
        { "D", new List<string> { "D", "F#", "A" } },
        { "C", new List<string> { "C", "E", "G" } },
        { "Em", new List<string> { "E", "G", "B" } }
    };

    private float[] noteFrequencies = new float[] { 16.35f, 17.32f, 18.35f, 19.45f, 20.60f };
    private string[] noteNames = new string[] { "C", "C#", "D", "D#", "E" };

    private string currentChord = "";

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        spectrumData = new float[sampleSize];
        sampleRate = AudioSettings.outputSampleRate;

        if (Microphone.devices.Length > 0)
        {
            string micDevice = Microphone.devices[0];
            audioSource.clip = Microphone.Start(micDevice, true, 10, AudioSettings.outputSampleRate);
            audioSource.loop = true;

            while (!(Microphone.GetPosition(micDevice) > 0)) { }
            audioSource.Play();
        }
        else
        {
            Debug.LogError("No microphone detected.");
        }
    }

    void Update()
    {
        if (audioSource.isPlaying)
        {
            audioSource.GetSpectrumData(spectrumData, 0, FFTWindow.BlackmanHarris);
            List<float> peakFrequencies = GetPeakFrequencies();

            if (peakFrequencies.Count > 0)
            {
                List<string> detectedNotes = GetNotesFromFrequencies(peakFrequencies);
                string detectedChord = GetChordFromNotes(detectedNotes);
                GradePerformance(detectedChord);
            }
        }
    }

    List<float> GetPeakFrequencies()
    {
        List<float> peaks = new List<float>();

        for (int i = 1; i < spectrumData.Length - 1; i++)
        {
            if (spectrumData[i] > spectrumData[i - 1] && spectrumData[i] > spectrumData[i + 1] && spectrumData[i] > 0.01f)
            {
                float frequency = i * sampleRate / (2 * spectrumData.Length);
                peaks.Add(frequency);
            }
        }

        return peaks;
    }

    List<string> GetNotesFromFrequencies(List<float> peakFrequencies)
    {
        List<string> detectedNotes = new List<string>();

        foreach (float peak in peakFrequencies)
        {
            float minDifference = float.MaxValue;
            int closestNoteIndex = -1;

            for (int i = 0; i < noteFrequencies.Length; i++)
            {
                float difference = Mathf.Abs(peak - noteFrequencies[i]);
                if (difference < minDifference && difference <= gradingThreshold)
                {
                    minDifference = difference;
                    closestNoteIndex = i;
                }
            }

            if (closestNoteIndex >= 0)
            {
                detectedNotes.Add(noteNames[closestNoteIndex]);
            }
        }

        return detectedNotes;
    }

    string GetChordFromNotes(List<string> notes)
    {
        foreach (KeyValuePair<string, List<string>> chord in chords)
        {
            if (IsSubset(chord.Value, notes))
            {
                return chord.Key;
            }
        }
        return "Unknown";
    }

    bool IsSubset(List<string> chordNotes, List<string> detectedNotes)
    {
        foreach (string note in chordNotes)
        {
            if (!detectedNotes.Contains(note))
            {
                return false;
            }
        }
        return true;
    }

    public void OnChordPlayed(string chord)
    {
        currentChord = chord;
    }

    private void GradePerformance(string detectedChord)
    {
        if (detectedChord == currentChord)
        {
            Debug.Log($"Correct chord: {detectedChord}");
        }
        else
        {
            Debug.Log($"Incorrect chord. Detected: {detectedChord}, Expected: {currentChord}");
        }
    }
}
