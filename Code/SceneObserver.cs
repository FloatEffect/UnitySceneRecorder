using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SceneObserver : MonoBehaviour
{
    // SceneObserver initializes the recording functionality and must be attached as a component to a root level GameObject in the scene exactly once.
    // It keeps track of recordable GameObjects by attaching ObjectObservers to them and their children.
    // Call CreateNewSceneRecording() to start a new recording.
    // Call EndRecording() on the returned Recorder to end the recording and use GetReplayIfFinished() to retrieve the Replay as soon as the recording is processed.
    // To take a snapshot, call CreateNewSceneSnapshot(), which directly returns a Replay.



    // Currently recording Recorders
    private List<Recorder> activeRecords = new List<Recorder>();
    // Lists ObjectObservers at the root level and ObjectObservers of individually recorded GameObjects
    private List<ObjectObserver> registeredObjectObservers = new List<ObjectObserver>();
    // Lists IComponentTypeExtension attached to the holding GameObject
    private List<IComponentTypeExtension> componentTypeExtensions = new List<IComponentTypeExtension>();

    [Tooltip("Recording in frames per second")]
    public float fps = 60f;

    [Tooltip("These GameObjects and their children won't be animated but will be visible in recordings. Add still GameObjects here for better performance.")]
    public List<GameObject> nonMovingGameObjects = new List<GameObject>(); // Check DoNotAnimateRecording.cs for further documentation.
    [Tooltip("These GameObjects and their children won't be visible in recordings/snapshots.")]
    public List<GameObject> nonRecordedGameObjects = new List<GameObject>(); // Check DoNotRecord.cs for further documentation.
    [Tooltip("These GameObjects are recorded and animated even when their parent GameObjects are listed as non-recorded or non-animated.")]
    public List<GameObject> individuallyRecordedGameObjects = new List<GameObject>(); // Check DoRecordIndividually.cs for further documentation.

    [Header("Recording controls for debugging purposes")]
    [Tooltip("Press to start/stop recording")]
    public bool toggleRecording = false;
    [Tooltip("Press to create snapshot (Checkmark will disappear instantly)")]
    public bool shootSnapshot = false;
    [Tooltip("Press to run/pause replay")]
    public bool runReplay = false;
    [Tooltip("Current replay time")]
    public float replayTime = 0f;

    // Recorder and Replay created by debug controls
    private Recorder manuallyStartedRecording;
    private Replay manuallyCreatedReplay;


    // Start is called before the first frame update
    void Start()
    {
        // Exclude this GameObject from being recorded to avoid recursion
        if (!nonRecordedGameObjects.Contains(gameObject))
            nonRecordedGameObjects.Add(gameObject);

        // Search for new GameObjects in the scene and add them to the list of observed objects
        SearchNewGameObjectsInScene();
    }


    // Update is called once per frame
    void Update()
    {
        // Update active recordings by registering new GameObjects and recording the next frame
        UpdateActiveRecords();

        // Update control checkboxes in the Unity Editor inspector
        UpdateManualControl();
    }


    // Create and start a new recording of the scene
    public Recorder CreateNewSceneRecording(string customName = "")
    {
        // Check for updates in the scene to ensure new extensions and GameObjects are included in the recording
        UpdateSceneObserver();

        // Create and return a new Recorder instance
        Recorder ret = Recorder.Constructor(this, true, customName);
        activeRecords.Add(ret);
        return ret;
    }


    // Create a snapshot of the scene at the current time
    public Replay CreateNewSceneSnapshot(string customName = "")
    {
        // Check for updates in the scene to ensure new extensions and GameObjects are included in the recording
        UpdateSceneObserver();

        // Create a non-animated recording of the scene and return the Replay instance
        return Recorder.CreateNonAnimatedRecording(this, customName);
    }


    // Update the list of observed GameObjects in the scene and check for new Extensions
    public void UpdateSceneObserver()
    {
        // Update observers for all listed GameObjects in the scene
        UpdateListedObjectObservers();

        // Search for new GameObjects in the scene and add them to the list of observed objects
        SearchNewGameObjectsInScene();

        // Update the extensions for non-standard component types
        UpdateComponentTypeExtensions();
    }


    // Unregisters a Recorder when the recording was ended
    // This method is called by the Recorder to notify the SceneObserver that newly instantiated GameObjects don't have to be registered for this recording.
    public void UnregisterRecord(Recorder recorder)
    {
        activeRecords.Remove(recorder);

        // Remove recording checkmark in the debug controls
        if (activeRecords.Count == 0)
            toggleRecording = false;
    }


    // Registers new GameObjects found in the scene and adds them to all active recordings
    public void RegisterNewObjectObserver(ObjectObserver newObjectObserver, bool treatAsRoot = false)
    {
        // Ignore if this GameObject should not be recorded
        if (!newObjectObserver.IfRecord())
            return;

        // Post-register the ObjectObserver in all active Recorders
        foreach (Recorder recorder in activeRecords)
            recorder.PostRegisterObject(newObjectObserver);

        // Register it as a root GameObject
        // Listed objects and their children will be registered for the next recording
        if (treatAsRoot)
        {
            UnregisterObjectObserver(newObjectObserver);
            registeredObjectObservers.Add(newObjectObserver);
        }
    }


    // Unregisters an ObjectObserver when it gets destroyed
    public void UnregisterObjectObserver(ObjectObserver objectObserver)
    {
        registeredObjectObservers.Remove(objectObserver);
    }


    // Returns a List with all registered ObjectObservers
    public List<ObjectObserver> GetRegisteredObjectObservers()
    {
        return registeredObjectObservers;
    }


    // Returns a List of all registered IComponentTypeExtensions
    // IComponentTypeExtensions define component types which should be deactivated instead of destroyed during cleaning of instantiated GameObjects to prevent errors.
    public List<IComponentTypeExtension> GetComponentTypeExtensions()
    {
        return componentTypeExtensions;
    }


    // Updates active recordings by post-registering new GameObjects and recording the next frame
    private void UpdateActiveRecords()
    {
        // Search for new GameObjects at root level
        if (activeRecords.Count > 0)
            SearchNewGameObjectsInScene();

        // Save frame
        foreach (Recorder activeRecord in activeRecords)
            activeRecord.SaveSnapshot(Time.deltaTime);
    }


    // Search for new GameObjects at root level and create an ObjectObserver for them if they don't already have one
    private void SearchNewGameObjectsInScene()
    {
        foreach (GameObject rootGameObject in gameObject.scene.GetRootGameObjects())
        {
            // If the GameObject already has an ObjectObserver, skip it
            if (rootGameObject.GetComponent<ObjectObserver>() != null)
                continue;

            // Create a new ObjectObserver for the GameObject and register it
            ObjectObserver objectObserver = ObjectObserver.Constructor(rootGameObject, this, true);
            RegisterNewObjectObserver(objectObserver, true);
        }
    }


    // Update the list of IComponentTypeExtension components attached to this GameObject
    private void UpdateComponentTypeExtensions()
    {
        componentTypeExtensions = gameObject.GetComponents<IComponentTypeExtension>().ToList();
    }


    // Update the ObjectObservers for new GameObjects that were listed to not be recorded, recorded individually, or not animated in the recording
    private void UpdateListedObjectObservers()
    {
        // Update the ObjectObservers for GameObjects that are not moving in the replay
        foreach (GameObject nonMovingGameObject in nonMovingGameObjects)
        {
            // Create a new ObjectObserver for the GameObject if it doesn't already have one
            ObjectObserver objectObserver = nonMovingGameObject.GetComponent<ObjectObserver>();
            if (objectObserver == null)
                objectObserver = ObjectObserver.Constructor(nonMovingGameObject, this);

            // Configure the ObjectObserver as non-animated
            objectObserver.MakeNonAnimated();
        }

        // Update the ObjectObservers for GameObjects that are not visible in the replay
        foreach (GameObject nonRecordedGameObject in nonRecordedGameObjects)
        {
            // Create a new ObjectObserver for the GameObject if it doesn't already have one
            ObjectObserver objectObserver = nonRecordedGameObject.GetComponent<ObjectObserver>();
            if (objectObserver == null)
                objectObserver = ObjectObserver.Constructor(nonRecordedGameObject, this);

            // Configure the ObjectObserver as non-recorded
            objectObserver.MakeNonRecorded();
        }

        // Update the ObjectObservers for GameObjects that are recorded individually even when being a child of another recorded GameObject
        foreach (GameObject individuallyRecordedGameObject in individuallyRecordedGameObjects)
        {
            // Create a new ObjectObserver for the GameObject if it doesn't already have one
            ObjectObserver objectObserver = individuallyRecordedGameObject.GetComponent<ObjectObserver>();
            if (objectObserver == null)
                objectObserver = ObjectObserver.Constructor(individuallyRecordedGameObject, this, true);

            // Configure the ObjectObserver as recorded individually
            objectObserver.MakeRoot();
        }
    }


    // Update debug controls in the inspector
    private void UpdateManualControl()
    {
        // Check if recording button has been toggled
        bool atLeastOneActiveRecord = activeRecords.Count > 0;
        if (!atLeastOneActiveRecord && toggleRecording)
        {
            // Start a new recording
            manuallyCreatedReplay = null;
            manuallyStartedRecording = CreateNewSceneRecording();
        }
        else if (atLeastOneActiveRecord && toggleRecording == false && manuallyStartedRecording != null && !manuallyStartedRecording.ifRecordingEnded)
        {
            // End the current recording
            manuallyStartedRecording.EndRecording();
        }

        // Check if the replay is not ready yet
        if (manuallyStartedRecording != null && manuallyStartedRecording.ifRecordingEnded && manuallyCreatedReplay == null)
        {
            // Get the replay when it is ready
            manuallyCreatedReplay = manuallyStartedRecording.GetReplayIfFinished();
        }

        // Check if the snapshot button has been pressed
        if (shootSnapshot && toggleRecording == false)
        {
            // Take a new snapshot
            manuallyCreatedReplay = CreateNewSceneSnapshot();
            shootSnapshot = false;
        }

        // Check if the playback button has been pressed and a replay is available
        if (runReplay && manuallyCreatedReplay != null)
        {
            // Move forward in time
            replayTime += Time.deltaTime;
            if (replayTime > manuallyCreatedReplay.GetLength())
                replayTime = 0f;

            // Update the replay to the current time
            manuallyCreatedReplay.SetTime(replayTime);
        }
    }
}