using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneRecorder;

// The NoMovementExtension component can be attached to GameObjects that should appear in the recording but not have their movements recorded.
// This is especially useful for still models in a scene such as ground and buildings, as it can help prevent excessive memory consumption and high CPU load.

// It's important to note that this component will affect all children of the attached GameObject, unless those children have the DoRecordIndividually component attached to them, or they are listed in the individuallyRecordedGameObjects of the SceneObserver.
// Additionally, adding a GameObject to the nonMovingGameObjects list of the SceneObserver has the same effect as attaching this component.
// Any changes made to this component after the recording has started will be ignored.

namespace UnitySceneRecorder{
	public class DoNotAnimateRecording : MonoBehaviour { }
}