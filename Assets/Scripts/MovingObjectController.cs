using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class MovingObjectController : MonoBehaviour
{
    public GameObject[] allObjects; // All target objects in the scene
    private (float minFreq, float maxFreq)[] frequencyRanges; // Frequency ranges for each object

    public GameObject cuePrefab; // Cue prefab
    public TMP_Text statusText; // Status display text
    public TMP_Text frequencyText; // Frequency display text

    private Vector3 originalPosition;
    private AudioClip micClip;
    private float[] samples;
    private const int sampleRate = 44100;

    // Sequence of target names in the desired order
    private List<string> targetSequence = new List<string>
    {
        "f13s3", "f13s3", "f15s3", "f13s2", "f14s2", "f13s2", "f13s3", "f15s3",
        "f13s3", "f13s3", "f15s3", "f13s2", "f14s2", "f13s2", "f13s3", "f13s4",
        "m", "m", "m", "m",
        "f13s3", "f13s3", "f15s3", "f13s2", "f14s2", "f13s2", "mute s3", "f13s3", "mute s3", "mute s3", "f15s3",
        "f15s3", "f15s3", "f17s3", "ms4", "ms4", "f13s4", "f15s4", "mute s3", "f13s3", "ms4", "f15s4", "f13s3", "f15s4", "f13s4",
        "f13s3", "f13s3", "f15s3", "f13s2", "f14s2", "f13s2", "ms3", "f13s3", "ms3", "ms3", "f13s4", "f15s4", "f15s4", "f13s4", "f16s2", "f16s2", "f14s2", "f14s2",
        "f13s2", "f11s2", "f9s2", "f9s2", "f8s2", "f4s2"
    };
    private int currentTargetIndex = 0;

    void Start()
    {
        if (cuePrefab != null)
        {
            originalPosition = cuePrefab.transform.position;
        }
        else
        {
            Debug.LogError("Cue Prefab is not assigned!");
        }

        // Initialize frequency ranges for each object
        InitializeFrequencyRanges();

        // Start microphone input
        StartMicrophone();
        StartCoroutine(SpawnObjects());
    }

    void InitializeFrequencyRanges()
    {
        frequencyRanges = new (float, float)[allObjects.Length];

        // Randomly assign frequency ranges for each object
        for (int i = 0; i < allObjects.Length; i++)
        {
            float minFreq = Random.Range(100f, 1000f); // Example range: 100 Hz to 1000 Hz
            float maxFreq = minFreq + Random.Range(50f, 200f); // Ensure range is at least 50 Hz wide
            frequencyRanges[i] = (minFreq, maxFreq);

            Debug.Log($"{allObjects[i].name} -> Frequency Range: {minFreq} Hz to {maxFreq} Hz");
        }

        // Ensure frequencyRanges length matches targetSequence length
        if (frequencyRanges.Length < targetSequence.Count)
        {
            Debug.LogWarning("Frequency ranges are fewer than target sequence; reusing last range.");
            (float minFreq, float maxFreq) lastRange = frequencyRanges[frequencyRanges.Length - 1];

            for (int i = frequencyRanges.Length; i < targetSequence.Count; i++)
            {
                frequencyRanges = frequencyRanges.Concat(new[] { lastRange }).ToArray();
            }
        }
    }

    void StartMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            string micDevice = Microphone.devices[0];
            micClip = Microphone.Start(micDevice, true, 1, sampleRate);
            samples = new float[1024];
        }
        else
        {
            Debug.LogError("No microphone detected.");
        }
    }

    IEnumerator SpawnObjects()
    {
        while (true)
        {
            if (currentTargetIndex >= targetSequence.Count)
            {
                currentTargetIndex = 0; // Loop back to the beginning
            }

            string targetName = targetSequence[currentTargetIndex];
            GameObject target = GetTargetByName(targetName);

            if (target != null)
            {
                GameObject newCue = Instantiate(cuePrefab, originalPosition, Quaternion.identity);
                StartCoroutine(MoveToTarget(newCue, target, currentTargetIndex));

                // Make all objects transparent
                SetObjectsTransparency(0.3f); // 0.3f means partially transparent

                currentTargetIndex++; // Move to the next target
            }

            float waitTime = Random.Range(1f, 3f);
            yield return new WaitForSeconds(waitTime);
        }
    }

    GameObject GetTargetByName(string targetName)
    {
        foreach (GameObject target in allObjects)
        {
            if (target.name == targetName)
            {
                return target;
            }
        }
        Debug.LogError($"Target with name {targetName} not found!");
        return null;
    }

    IEnumerator MoveToTarget(GameObject obj, GameObject target, int targetIndex)
    {
        if (targetIndex >= frequencyRanges.Length)
        {
            Debug.LogError("Target index out of bounds of frequencyRanges array.");
            yield break;
        }

        float speed = 5f;
        var (minFreq, maxFreq) = frequencyRanges[targetIndex];

        statusText.text = $"Cue moving to {target.name} (Frequency Range: {minFreq} Hz - {maxFreq} Hz)";

        while (Vector3.Distance(obj.transform.position, target.transform.position) > 0.1f)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, target.transform.position, speed * Time.deltaTime);
            yield return null;
        }

        float detectedFrequency = DetectFrequencyFromMic();
        frequencyText.text = $"Detected Frequency: {detectedFrequency} Hz";

        CompareFrequencyWithRange(detectedFrequency, targetIndex);
        Destroy(obj);
    }

    void CompareFrequencyWithRange(float detectedFrequency, int targetIndex)
    {
        var (minFreq, maxFreq) = frequencyRanges[targetIndex];

        if (detectedFrequency >= minFreq && detectedFrequency <= maxFreq)
        {
            statusText.text = $"Success! {detectedFrequency} Hz is within the range ({minFreq} - {maxFreq} Hz).";
            allObjects[targetIndex].GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            statusText.text = $"Failed! {detectedFrequency} Hz is outside the range ({minFreq} - {maxFreq} Hz).";
            allObjects[targetIndex].GetComponent<Renderer>().material.color = Color.red;
        }

        StartCoroutine(RevertColorAfterDelay(allObjects[targetIndex]));
    }

    IEnumerator RevertColorAfterDelay(GameObject target)
    {
        yield return new WaitForSeconds(1f);
        target.GetComponent<Renderer>().material.color = Color.white;
    }

    void SetObjectsTransparency(float transparency)
    {
        foreach (GameObject obj in allObjects)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = transparency; // Set alpha to make it transparent
                renderer.material.color = color;
            }
        }
    }

    float DetectFrequencyFromMic()
    {
        int position = Microphone.GetPosition(null);
        if (position < samples.Length) return 0f;

        micClip.GetData(samples, position - samples.Length);

        float highestFreq = 0f;
        float maxAmplitude = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float amplitude = Mathf.Abs(samples[i]);
            if (amplitude > maxAmplitude)
            {
                maxAmplitude = amplitude;
                highestFreq = i * (sampleRate / 2) / (samples.Length / 2);
            }
        }

        return highestFreq;
    }
}
