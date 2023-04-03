using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this component to GameObjects if they should be recorded and animated even if a parent GameObject is not recorded or animated.

// If this GameObject should not be animated in the recording, additionally add DoNotAnimateRecording, even when a parent GameObject already owns it.
// Adding a GameObject to the List individuallyRecordedGameObjects of the SceneObserver has the same effect as adding this component.

// The duplicated GameObject will be placed in GameObject imitating the movements of the original GameObject's parent.
// This parent imitating GameObject will be added to the record container like root objects.
// Changes of this component will be ignored after the recording started.

// If the SceneObserver is not attached to a root GameObject, link hasSceneObserver to the SceneObserver holding GameObject.

public class DoRecordIndividually : MonoBehaviour
{
    [Tooltip("GameObject with SceneObserver component. If empty, the SceneObserver will be search in root GameObjects.")]
    public GameObject hasSceneObserver;

    void Update()
    {
        // Initialization has to wait for the SceneObserver to be initialized,
        // because of this it can't happen during Start().
        // To prevent repeated initialization, this component disables itself after successful initialization.
        TryToInitialize();
    }

    // Start is called before the first frame update
    void TryToInitialize()
    {
        // Check if this GameObject has already an ObjectObserver, register it as an individual recorded GameObject
        ObjectObserver objectObserver = gameObject.GetComponent<ObjectObserver>();
        if (objectObserver != null)
        {
            objectObserver.MakeRoot();
            enabled = false;
            return;
        }

        // If there is no ObjectObserver, get the SceneObserver, to be able to register a new ObjectObserver
        SceneObserver sceneObserver = null;

        // Fist check hasSceneObserver.
        if (hasSceneObserver != null)
            sceneObserver = hasSceneObserver.GetComponent<SceneObserver>();

        // If hasSceneObserver was not set, search in all root GameObjects.
        if (sceneObserver == null)
        {
            foreach (GameObject rootGameObject in gameObject.scene.GetRootGameObjects())
            {
                sceneObserver = rootGameObject.GetComponent<SceneObserver>();
                if (sceneObserver != null)
                    break;
            }
        }

        // If the SceneObserver was found, construct a new ObjectObserver with the third parameter as true to mark it as root.
        if (sceneObserver != null)
        {
            ObjectObserver.Constructor(gameObject, sceneObserver, true, false, false);
            enabled = false;
        }
        else
        {
            Debug.LogWarning("SceneObserver was not found by 'DoRecordIndividually'. Either attach SceneObserver to a root level GameObject or define the SceneRecorder owner in hasSceneObserver of " + gameObject);
        }
    }
}
