using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This component implements an extension for the Recorder, 
// which enables it to make recreated GameObjects follow the movements of the original GameObject.
// Additionally, it will make the component holding GameObject invisible by shrinking it to size 0
// while the original or another registered GameObject is also not visible (disabled in hierarchy or destroyed).
// For this to work, the GameObject owning ImitateMovement must not have parents which transform this GameObject.
// Only after recording the whole recorded scene can be transformed.
// See IRecorderExtension for further documentation

public class ImitateMovement : MonoBehaviour, IRecorderExtension
{
    [Tooltip("While true, the holding GameObject will be scaled to size 0")]
    public bool forceCollapse = false;

    // GameObject to be imitated. This is usually the original GameObject.
    private GameObject toBeImitated;
    // If any of the GameObjects in this List is disabled or null, the component holding GameObject is scaled to size 0.
    private List<GameObject> objectsToBeCheckedIfNullOrDisabled = new List<GameObject>();


    // These functions are unused.
    void IRecorderExtension.GetsCalledAfterInstantiation() { }
    void IRecorderExtension.GetsCalledBeforeRecording() { }
    void IRecorderExtension.GetsCalledDuringPlayback(float playTime) { }
    List<GameObject> IRecorderExtension.GetAllGameObjectsInstantiatedByThis() { return new List<GameObject>(); }


    // This function copies this component to another GameObject.
    // It adds a new component to the GameObject and registers the original to be imitated.
    void IRecorderExtension.CopyComponentToGameObject(GameObject _gameObject)
    {
        // As Unity can't copy instances of single components, a new component has to be added and every variable has to be copied individually.

        ImitateMovement imitateMovement = _gameObject.AddComponent<ImitateMovement>();

        // If this extension is attached as component, the duplicate will register the original GameObjects transform to be imitated.
        imitateMovement.ClearRegisteredObjects();
        imitateMovement.RegisterObjectToBeImitated(gameObject);
        imitateMovement.RegisterObjectToBeCheckedIfNullOrDisabled(gameObject);
    }


    // The Recorder will call this function, if it duplicates this GameObject by instantiation, before the instantiation.
    // The original GameObject is registered as to be imitated, thus after instantiation the duplicate will follow the original.
    void IRecorderExtension.GetsCalledBeforeInstantiation()
    {
        ClearRegisteredObjects();
        RegisterObjectToBeImitated(gameObject);
        RegisterObjectToBeCheckedIfNullOrDisabled(gameObject);
    }


    // This function is called each frame before the Recorder records the transforms.
    // It adjusts the GameObject's transform to match that of the original GameObject.
    void IRecorderExtension.GetsCalledDuringRecording(float deltaTime)
    {
        PrepareFrame();
    }


    // This function is called before the playback of the recorded transform.
    // It disables the component to prevent it from overwriting the replayed transform.
    void IRecorderExtension.GetsCalledBeforePlayback()
    {
        PrepareFrame();
        enabled = false;
    }


    // This function sets the GameObject to be imitated.
    public void RegisterObjectToBeImitated(GameObject _toBeImitated)
    {
        toBeImitated = _toBeImitated;
    }


    // This function adds a GameObject to be checked to see if it is disabled or null.
    // If it is, the component holder is shrunk to size 0.
    public void RegisterObjectToBeCheckedIfNullOrDisabled(GameObject gameObject)
    {
        objectsToBeCheckedIfNullOrDisabled.Add(gameObject);
    }


    // This function clears the variable and List of GameObjects to be imitated and checked.
    public void ClearRegisteredObjects()
    {
        objectsToBeCheckedIfNullOrDisabled = new List<GameObject>();
        toBeImitated = null;
    }


    // This method moves the component holding GameObject to the same position as the GameObject to be imitated.
    // The method 'RecordFrame/GetsCalledDuringRecording' is called directly before recording the transform, allowing modifications to take effect without delay during replay.
    private void PrepareFrame()
    {
        if (!enabled)
            return;

        // If a registered GameObject is null or disabled, collapse this GameObject to size 0.
        // This loop checks all objects in the 'objectsToBeCheckedIfNullOrDisabled' List.
        foreach (GameObject obj in objectsToBeCheckedIfNullOrDisabled)
        {
            if (obj == null || !obj.activeInHierarchy || forceCollapse)
            {
                gameObject.transform.localScale = Vector3.zero;
                if (toBeImitated != null)
                {
                    // Still follow rotation and position to prevent unnecessary jumps during replay.
                    // These jumps could become visible if the recording framerate is low and positions get interpolated.
                    gameObject.transform.localRotation = toBeImitated.transform.rotation;
                    gameObject.transform.localPosition = toBeImitated.transform.position;
                }
                return;
            }
        }

        // Otherwise move this GameObject to the same position as the GameObject to be imitated.
        if (toBeImitated != null)
        {
            gameObject.transform.localScale = toBeImitated.transform.lossyScale; // Note that lossy scale can't handle shearing.
            gameObject.transform.localRotation = toBeImitated.transform.rotation;
            gameObject.transform.localPosition = toBeImitated.transform.position;
            // Note that the global transform is written as local transform. 
            // This GameObject must be non-transformed relative to the scene to match the followed transform.
        }
        else
        {
            // If toBeImitated is not set, this GameObject might be a container for other GameObjects.
            // Rescale to 1 to open the container after collapsing again.
            gameObject.transform.localScale = Vector3.one;
        }
    }
}
