using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Attach this component to GameObjects if they should not appear in the recording.

// This will also affect all children unless DoRecordIndividually is attached to them or they are listed in individuallyRecordedGameObjects of the SceneObserver.
// Adding a GameObject to the List nonRecordedGameObjects of the SceneObserver has the same effect as adding this component.

// GameObjects with this component will not add ObjectObservers to its children. 
// The duplicated GameObjects, for example, will be automatically placed in a GameObject which will not be recorded to prevent recursion.
// Changes of this component will be ignored after the recording started.

// If you instantiate GameObjects after a recording started that should be recorded from within the scene,
// add DoNotRecord to a parent of the original GameObject to prevent the instantiated GameObject to already own a ObjectObserver.
// Otherwise the instantiated GameObject will not be recognized as new.
// If the original GameObject should also be visible, Destroy it's ObjectObserver component immediately.

public class DoNotRecord : MonoBehaviour { }
