using UnityEngine;
using TMPro;
using System.Collections;

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

        // Randomly assign frequency ranges
        for (int i = 0; i < allObjects.Length; i++)
        {
            float minFreq = Random.Range(100f, 1000f); // Example range: 100 Hz to 1000 Hz
            float maxFreq = minFreq + Random.Range(50f, 200f); // Ensure range is at least 50 Hz wide
            frequencyRanges[i] = (minFreq, maxFreq);

            Debug.Log($"{allObjects[i].name} -> Frequency Range: {minFreq} Hz to {maxFreq} Hz");
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
            float waitTime = Random.Range(1f, 3f);
            yield return new WaitForSeconds(waitTime);

            int randomTargetIndex = Random.Range(0, allObjects.Length);
            GameObject randomTarget = allObjects[randomTargetIndex];

            GameObject newCue = Instantiate(cuePrefab, originalPosition, Quaternion.identity);
            StartCoroutine(MoveToTarget(newCue, randomTarget, randomTargetIndex));
        }
    }

    IEnumerator MoveToTarget(GameObject obj, GameObject target, int targetIndex)
    {
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
