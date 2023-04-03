using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Replay : MonoBehaviour
{
    // A Replay is returned after making a recording. See SceneObserver and Recorder for recording details.
    // With SetVisible(bool visible), you can show/hide the replay. Invisible replays don't consume GPU resources.
    // With SetTime(float second), you can set the current time of the replay.
    // With GetRecordContainer(), you get the GameObject containing the recorded scene, which can be translated to translate the whole scene


    // A List of all individual Recordings contained in the Replay.
    private List<Recording> recordings = new List<Recording>();
    // The current time of the replay (when it was updates last)
    private float currentTime = 0f;


    // Replay Constructor adds a Replay as component to recordContainer.
    // recordContainer should have all in _recordings listed Recordings as children.
    public static Replay Constructor(GameObject recordContainer, List<Recording> _recordings)
    {
        // As Unity doesn't support true object-oriented programming constructors in MonoBehaviour components,
        // this static function emulates constructor functionality.

        // Return empty Replay if recordContainer is null
        if (recordContainer == null)
        {
            Debug.LogWarning("Replay Constructor parameter recordContainer was NULL. The recording was lost.");
            GameObject emptyComponentHolder = new GameObject();
            emptyComponentHolder.name = "If_you_see_this_GameObject,_Replay_Constructor_parameter_was_NULL";
            return emptyComponentHolder.AddComponent<Replay>();
        }

        // If Replay is already existing, return this one.
        Replay ret = recordContainer.GetComponent<Replay>();
        if (ret != null)
        {
            Debug.LogWarning("Replay Constructor parameter recordContainer had already a Replay Component. The new recording was lost.");
            return ret;
        }

        // Create a new Recording component and add recordings
        ret = recordContainer.AddComponent<Replay>();
        ret.AddRecordings(_recordings);
        return ret;
    }


    // Shows/hides the replay.
    // An invisible replay does not consume GPU resources.
    public void SetVisible(bool visible)
    {
        gameObject.SetActive(visible);
    }


    // Returns whether the replay is visible.
    public bool ifVisible()
    {
        return gameObject.activeSelf;
    }


    // Returns length in seconds of the recording
    public float GetLength()
    {
        // All contained recordings should have the same recordLength.
        if (recordings.Count > 0)
            return recordings[0].recordLength;
        return 0f;
    }


    // Sets the current time of the replay in seconds
    public void SetTime(float second)
    {
        currentTime = second;
        foreach (Recording rec in recordings)
            rec.LoadSnapshot(currentTime);
    }


    // Advances the replay time by deltaTime seconds.
    public void AddTime(float deltaTime)
    {
        // Forward time
        currentTime += deltaTime;
        // Clip at end
        currentTime = Mathf.Min(GetLength(), currentTime);
        // Load snapshots
        foreach (Recording rec in recordings)
            rec.LoadSnapshot(currentTime);
    }


    // Returns the current time of the replay in seconds.
    public float GetTime()
    {
        return currentTime;
    }


    // Returns the GameObject containing the recorded scene
    // You can translate this GameObject to translate the replayed scene
    public GameObject GetRecordContainer()
    {
        return gameObject;
    }


    // Add recordings to replay
    // Don't use this manually but by calling Replay.Constructor.
    public void AddRecordings(List<Recording> _recordings)
    {
        recordings.AddRange(_recordings);
    }
}
