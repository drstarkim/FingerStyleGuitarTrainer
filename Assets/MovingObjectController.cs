using System.Collections;
using UnityEngine;
using TMPro; // For TextMeshPro UI display

public class MovingObjectController : MonoBehaviour
{
    public Transform[] targets; // Array of predefined targets (e.g., Target1, Target2, Target3)
    public GameObject cuePrefab; // The 'Cue' prefab to instantiate and move between targets
    public TMP_Text statusText; // TextMeshPro Text for displaying the target information
    public TMP_Text frequencyText; // TextMeshPro Text for displaying the frequency played

    private Vector3 originalPosition; // Define original position in the game scene
    private AudioClip micClip; // The microphone clip
    private float[] samples; // Audio sample data
    private const int sampleRate = 44100; // Standard audio sample rate for mic

    // Target frequencies (in Hz)
    public float[] targetFrequencies = { 440f, 880f, 1760f }; // Example frequencies: A4, A5, A6

    void Start()
    {
        if (cuePrefab != null)
        {
            originalPosition = cuePrefab.transform.position; // Get the original position from the Cue prefab in the scene
        }
        else
        {
            Debug.LogError("Cue Prefab is not assigned in the Inspector!");
        }

        // Start microphone input
        StartMicrophone();
        StartCoroutine(SpawnObjects());
    }

    void StartMicrophone()
    {
        if (Microphone.devices.Length > 0)
        {
            string micDevice = Microphone.devices[0]; // Use the first available microphone
            micClip = Microphone.Start(micDevice, true, 1, sampleRate); // Start capturing from the mic
            samples = new float[1024]; // Array to hold audio samples
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
            float waitTime = Random.Range(1f, 3f); // Randomize the time between appearances (1-3 seconds)
            yield return new WaitForSeconds(waitTime);

            int randomTargetIndex = Random.Range(0, targets.Length);
            Transform randomTarget = targets[randomTargetIndex];

            GameObject newCue = Instantiate(cuePrefab, originalPosition, Quaternion.identity); // Instantiate a new Cue object at the original position
            StartCoroutine(MoveToTarget(newCue, randomTarget, randomTargetIndex));
        }
    }

    IEnumerator MoveToTarget(GameObject obj, Transform target, int targetIndex)
    {
        float speed = 5f; // Speed of the cue object
        float targetFrequency = targetFrequencies[targetIndex]; // Get the target's assigned frequency
        statusText.text = $"Cue moving to {target.name} (Frequency: {targetFrequency} Hz)"; // Update status with target name and frequency

        while (Vector3.Distance(obj.transform.position, target.position) > 0.1f)
        {
            obj.transform.position = Vector3.MoveTowards(obj.transform.position, target.position, speed * Time.deltaTime);
            yield return null;
        }

        // Cue reaches the target
        float detectedFrequency = DetectFrequencyFromMic();
        frequencyText.text = $"Frequency Played: {detectedFrequency} Hz";

        // Compare the detected frequency with the target's frequency
        CompareFrequencies(detectedFrequency, target, targetIndex);

        Destroy(obj); // Destroy the object after it reaches the target
    }

    void CompareFrequencies(float detectedFrequency, Transform target, int targetIndex)
    {
        float targetFrequency = targetFrequencies[targetIndex]; // Get the target's assigned frequency
        float frequencyDifference = Mathf.Abs(detectedFrequency - targetFrequency);

        // Store the original color of the target
        Color originalColor = target.GetComponent<Renderer>().material.color;

        // If the frequency difference is small, consider it a match
        if (frequencyDifference < 20f) // 20 Hz tolerance, adjust as needed
        {
            // Set target color to green if frequencies are similar
            target.GetComponent<Renderer>().material.color = Color.green;
        }
        else
        {
            // Set target color to red if frequencies are not similar
            target.GetComponent<Renderer>().material.color = Color.red;
        }

        // After 1 second, revert the color back to the original
        StartCoroutine(RevertColorAfterDelay(target, originalColor, 1f));
    }

    IEnumerator RevertColorAfterDelay(Transform target, Color originalColor, float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for 1 second
        target.GetComponent<Renderer>().material.color = originalColor; // Revert to the original color
    }

    float DetectFrequencyFromMic()
    {
        // Ensure the microphone is recording before we process the data
        int position = Microphone.GetPosition(null);
        if (position < samples.Length) return 0f; // Wait until we have enough data

        // Get the audio samples from the microphone input
        micClip.GetData(samples, position - samples.Length);

        // Perform a FFT to analyze the frequency
        float highestFreq = 0f;
        float maxAmplitude = 0f;

        // Loop through frequency range to find the peak frequency
        for (int i = 0; i < samples.Length; i++)
        {
            float amplitude = Mathf.Abs(samples[i]);
            if (amplitude > maxAmplitude)
            {
                maxAmplitude = amplitude;
                highestFreq = i * (sampleRate / 2) / (samples.Length / 2); // Calculate frequency from index
            }
        }

        return highestFreq; // Return the detected frequency
    }
}
