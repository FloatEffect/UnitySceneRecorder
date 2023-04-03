using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneRecorder;

// Attach this component to GameObjects which should be duplicated by the Recorder by instantiation instead of recreation.
// Instantiation is suggested over recreation if the original GameObject owns a non-standard Renderer.

namespace UnitySceneRecorder{
	public class ForceDuplicationByInstantiation : MonoBehaviour { }
}