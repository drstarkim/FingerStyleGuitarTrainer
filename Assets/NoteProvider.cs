using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class NoteProvider : MonoBehaviour
{
    // Declare game objects for chord visuals
    public GameObject f1s6;
    public GameObject f2s6;
    public GameObject f3s6;
    public GameObject f4s6;
    public GameObject f5s6;
    public GameObject f1s5;
    public GameObject f2s5;
    public GameObject f3s5;
    public GameObject f4s5;
    public GameObject f5s5;
    public GameObject f1s4;
    public GameObject f2s4;
    public GameObject f3s4;
    public GameObject f4s4;
    public GameObject f5s4;
    public GameObject f1s3;
    public GameObject f2s3;
    public GameObject f3s3;
    public GameObject f4s3;
    public GameObject f5s3;
    public GameObject f1s2;
    public GameObject f2s2;
    public GameObject f3s2;
    public GameObject f4s2;
    public GameObject f5s2;
    public GameObject f1s1;
    public GameObject f2s1;
    public GameObject f3s1;
    public GameObject f4s1;
    public GameObject f5s1;
    public GameObject note;
    public Text countdownText;

    // Create a reference to the GuitarChordGrader
    public GuitarChordGrader chordGrader;

    private struct Chord
    {
        public string name;
        public Vector3[] positions;
        public float duration;

        public Chord(string name, Vector3[] positions, float duration)
        {
            this.name = name;
            this.positions = positions;
            this.duration = duration;
        }
    }

    private List<Chord> chords;

    void Start()
    {
        if (!AllGameObjectsAssigned())
        {
            Debug.LogError("Objects missing");
            return;
        }

        InitializeChords();
        StartCoroutine(StartSongWithCountdown());
    }

    private bool AllGameObjectsAssigned()
    {
        return f1s6 && f2s6 && f3s6 && f4s6 && f5s6 &&
               f1s5 && f2s5 && f3s5 && f4s5 && f5s5 &&
               f1s4 && f2s4 && f3s4 && f4s4 && f5s4 &&
               f1s3 && f2s3 && f3s3 && f4s3 && f5s3 &&
               f1s2 && f2s2 && f3s2 && f4s2 && f5s2 &&
               f1s1 && f2s1 && f3s1 && f4s1 && f5s1 &&
               note && countdownText && chordGrader;
    }

    private void InitializeChords()
    {
        // Chord sequence for "I'm Yours" (simplified)
        chords = new List<Chord>
        {
            new Chord("G", new Vector3[] { f3s6.transform.position, f2s5.transform.position, f4s1.transform.position }, 4f),
            new Chord("D", new Vector3[] { f1s3.transform.position, f3s2.transform.position, f2s1.transform.position }, 4f),
            new Chord("Em", new Vector3[] { f3s4.transform.position, f2s5.transform.position }, 4f),
            new Chord("C", new Vector3[] { f3s5.transform.position, f2s4.transform.position, f1s2.transform.position }, 4f)
        };
    }

    IEnumerator StartSongWithCountdown()
    {
        // Countdown timer
        for (int i = 3; i > 0; i--)
        {
            countdownText.text = i.ToString();
            yield return new WaitForSeconds(1);
        }

        countdownText.text = "Go!";
        yield return new WaitForSeconds(1);
        countdownText.text = "";

        yield return StartCoroutine(PlaySong());
    }

    IEnumerator PlaySong()
    {
        while (true) // Repeat the chord sequence indefinitely
        {
            foreach (Chord chord in chords)
            {
                yield return PlayChord(chord);
            }
        }
    }

    IEnumerator PlayChord(Chord chord)
    {
        List<GameObject> notes = new List<GameObject>();
        List<Coroutine> moveCoroutines = new List<Coroutine>();

        foreach (Vector3 targetPosition in chord.positions)
        {
            GameObject noteInstance = Instantiate(note, new Vector3(-59, 70, 44), Quaternion.identity);
            noteInstance.SetActive(true);
            notes.Add(noteInstance);
            moveCoroutines.Add(StartCoroutine(MoveToTarget(noteInstance, targetPosition, chord.duration)));
        }

        // Notify GuitarChordGrader about the chord played
        chordGrader.OnChordPlayed(chord.name);

        foreach (Coroutine coroutine in moveCoroutines)
        {
            yield return coroutine;
        }

        foreach (GameObject noteInstance in notes)
        {
            Destroy(noteInstance);
        }
    }

    IEnumerator MoveToTarget(GameObject play, Vector3 targetPosition, float duration)
    {
        float elapseTime = 0f;
        Vector3 startPosition = play.transform.position;

        while (elapseTime < duration)
        {
            play.transform.position = Vector3.Lerp(startPosition, targetPosition, elapseTime / duration);
            elapseTime += Time.deltaTime;
            yield return null;
        }

        play.transform.position = targetPosition;
    }
}
