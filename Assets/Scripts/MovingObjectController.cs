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
        "f13s3", "f13s3", "f15s3", "f13s2", "f14s2", "f13s2", "ms3", "f13s3", "ms3", "ms3", "f15s3",
        "f15s3", "f15s3", "f17s3", "ms4", "ms4", "f13s4", "f15s4", "ms3", "f13s3", "ms4", "f15s4", "f13s3", "f15s4", "f13s4",
        "f13s3", "f13s3", "f15s3", "f13s2", "f14s2", "f13s2", "ms3", "f13s3", "ms3", "ms3", "f13s4", "f15s4", "f15s4", "f13s4", "f16s2", "f16s2", "f14s2", "f14s2",
        "f13s2", "f11s2", "f9s2", "f9s2", "f8s2", "f4s2"
    };


    // Timing array in seconds between each target
    private float[] targetTiming = new float[]
    {
        1, 2, 1, 1, 1, 1, 2, 2, 1, 2, 1, 1, 1, 1, 2, 2, 1, 1, 1, 1, 1, 2, 1, 1, 1,
        1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 1, 
        1, 1, 1, 1, 1, 1, 1, 1, 2, 1, 1, 1, 1, 4, 4, 4, 4, 4
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

        // Specific frequency ranges for each target
        frequencyRanges[0] = (310f, 312f); // f4s2
        frequencyRanges[1] = (390f, 393f); // f8s2
        frequencyRanges[2] = (414f, 417f); // f9s2
        frequencyRanges[3] = (522f, 525f); // f13s2
        frequencyRanges[4] = (414f, 417f); // f13s3
        frequencyRanges[5] = (310f, 312f); // f13s4
        frequencyRanges[6] = (553f, 556f); // f14s2
        frequencyRanges[7] = (465f, 468f); // f15s3
        frequencyRanges[8] = (348f, 351f); // f15s4
        frequencyRanges[9] = (621f, 624f); // f16s2
        frequencyRanges[10] = (522f, 525f); // f17s3

        // For m, ms3, and ms4, allow any frequency range
        for (int i = 11; i < frequencyRanges.Length; i++)
        {
            frequencyRanges[i] = (0f, float.MaxValue); // Allow any frequency range for these targets
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

            // Use the specific timing for this target
            float waitTime = targetTiming[currentTargetIndex];
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

        // Move the cue to the target object
        while (Vector3.Distance(obj.transform.position, target.transform.position) > 0.1f)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, target.transform.position, speed * Time.deltaTime);
            yield return null;
        }

        // Detect frequency and compare with range
        float detectedFrequency = DetectFrequencyFromMic();
        frequencyText.text = $"Detected Frequency: {detectedFrequency} Hz";

        CompareFrequencyWithRange(detectedFrequency, target); // Use the target directly
        Destroy(obj); // Destroy cue object after reaching the target
    }

    void CompareFrequencyWithRange(float detectedFrequency, GameObject target)
    {
        var (minFreq, maxFreq) = frequencyRanges[currentTargetIndex]; // Use currentTargetIndex for range

        if (detectedFrequency >= minFreq && detectedFrequency <= maxFreq)
        {
            statusText.text = $"Success! {detectedFrequency} Hz is within the range ({minFreq} - {maxFreq} Hz).";
            target.GetComponent<Renderer>().material.color = Color.green; // Apply color change to the target
        }
        else
        {
            statusText.text = $"Failed! {detectedFrequency} Hz is outside the range ({minFreq} - {maxFreq} Hz).";
            target.GetComponent<Renderer>().material.color = Color.red; // Apply color change to the target
        }

        StartCoroutine(RevertColorAfterDelay(target)); // Pass target to revert color
    }

    IEnumerator RevertColorAfterDelay(GameObject target)
    {
        yield return new WaitForSeconds(1f); // Wait for 1 second before reverting color
        target.GetComponent<Renderer>().material.color = Color.white; // Revert color back to white
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
