using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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

    private Dictionary<GameObject, Color> originalColors = new Dictionary<GameObject, Color>(); // To store original colors of targets

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

        // Assign frequency ranges based on target names
        for (int i = 0; i < allObjects.Length; i++)
        {
            string targetName = allObjects[i].name;

            switch (targetName)
            {
                case "f4s2":
                    frequencyRanges[i] = (310f, 312f);
                    break;
                case "f8s2":
                    frequencyRanges[i] = (390f, 393f);
                    break;
                case "f9s2":
                    frequencyRanges[i] = (414f, 417f);
                    break;
                case "f11s2":
                    frequencyRanges[i] = (465f, 468f);
                    break;
                case "f13s2":
                    frequencyRanges[i] = (522f, 525f);
                    break;
                case "f13s3":
                    frequencyRanges[i] = (414f, 417f);
                    break;
                case "f13s4":
                    frequencyRanges[i] = (310f, 312f);
                    break;
                case "f14s2":
                    frequencyRanges[i] = (553f, 556f);
                    break;
                case "f15s3":
                    frequencyRanges[i] = (465f, 468f);
                    break;
                case "f15s4":
                    frequencyRanges[i] = (348f, 351f);
                    break;
                case "f16s2":
                    frequencyRanges[i] = (621f, 624f);
                    break;
                case "f17s3":
                    frequencyRanges[i] = (522f, 525f);
                    break;
                default:
                    frequencyRanges[i] = (0f, float.MaxValue); // Allow any frequency for unspecified names
                    break;
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
                // Store the original color of the target before modifying it
                if (!originalColors.ContainsKey(target))
                {
                    originalColors[target] = target.GetComponent<Renderer>().material.color;
                }

                GameObject newCue = Instantiate(cuePrefab, originalPosition, Quaternion.identity);
                // Change the cue's shape and color to match the target
                ChangeCueAppearance(newCue, target);
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
        // Ensure the index is within bounds of frequencyRanges array
        if (targetIndex >= frequencyRanges.Length)
        {
            Debug.LogError("Target index out of bounds of frequencyRanges array.");
            yield break;
        }

        float speed = 5f;
        var (minFreq, maxFreq) = frequencyRanges[targetIndex]; // Get the frequency range for the current target

        // Update status text with the next cue's frequency range (display the cue, not the result)
        // statusText.text = $"Play {target.name}";

        // Move the cue to the target object
        while (Vector3.Distance(obj.transform.position, target.transform.position) > 0.1f)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, target.transform.position, speed * Time.deltaTime);
            yield return null;
        }

        // Detect frequency and compare with range
        float detectedFrequency = DetectFrequencyFromMic();
        // frequencyText.text = $"Detected Frequency: {detectedFrequency} Hz";

        // Update note text based on the comparison result
        CompareFrequencyWithRange(detectedFrequency, target, minFreq, maxFreq);
        
        // Destroy the cue object after reaching the target
        Destroy(obj); 
    }

    void CompareFrequencyWithRange(float detectedFrequency, GameObject target, float minFreq, float maxFreq)
    {
        // Check if detected frequency is within the target frequency range
        if (detectedFrequency >= minFreq && detectedFrequency <= maxFreq)
        {
            // If successful, display the success message in noteText
            frequencyText.text = $"Correct! We detected {detectedFrequency} Hz which is within the range ({minFreq} - {maxFreq} Hz).";
            target.GetComponent<Renderer>().material.color = Color.green; // Change target color to green
        }
        else
        {
            // If failure, display the failure message in noteText
            frequencyText.text = $"Wrong! We detected {detectedFrequency} Hz which is outside the range ({minFreq} - {maxFreq} Hz).";
            target.GetComponent<Renderer>().material.color = Color.red; // Change target color to red
        }

        // Revert the target color back to its original color after a short delay
        StartCoroutine(RevertTargetColor(target));
    }

    IEnumerator RevertTargetColor(GameObject target)
    {
        // Wait for 1 second before reverting the color
        yield return new WaitForSeconds(1f);
        
        // Check if the target has an original color stored in the dictionary
        if (originalColors.ContainsKey(target))
        {
            // Revert color to its original (stored color)
            target.GetComponent<Renderer>().material.color = originalColors[target];
        }
        else
        {
            // If no original color is found, revert to a default color (e.g., white)
            target.GetComponent<Renderer>().material.color = Color.white;
        }
    }


    void SetObjectsTransparency(float alpha)
    {
        foreach (GameObject obj in allObjects)
        {
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color color = renderer.material.color;
                color.a = alpha;
                renderer.material.color = color;
            }
        }
    }

    void ChangeCueAppearance(GameObject cue, GameObject target)
    {
        Renderer targetRenderer = target.GetComponent<Renderer>();
        if (targetRenderer != null)
        {
            // Change color
            cue.GetComponent<Renderer>().material.color = targetRenderer.material.color;
            
            // You can change shape here if needed (e.g., by adjusting the scale or model of the cue)
        }
    }

    float DetectFrequencyFromMic()
    {
        micClip.GetData(samples, 0);

        float maxFrequency = 0;
        float maxAmplitude = 0;

        for (int i = 0; i < samples.Length / 2; i++)
        {
            float frequency = i * (sampleRate / 2) / (samples.Length / 2);
            float amplitude = Mathf.Abs(samples[i]);

            if (amplitude > maxAmplitude)
            {
                maxAmplitude = amplitude;
                maxFrequency = frequency;
            }
        }

        return maxFrequency;
    }
}
