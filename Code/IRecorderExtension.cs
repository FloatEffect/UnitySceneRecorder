using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneRecorder;

// This interface defines a set of functions that can be used to extend the recording and replay process.
// When added to a MonoBehaviour component, this interface allows the component to be called at various points during duplication, recording, and playback.
// The component can be attached to the original GameObject in the scene, which then gets recreated manually or instantiated when starting a recording.
// The functions GetsCalledBefore/DuringRecording/Playback() will only be called on the duplicated GameObject.

namespace UnitySceneRecorder{
	public interface IRecorderExtension
	{
		// Option 1: The GameObject will be recreated manually.
		// As Unity can't copy instances of single components, a new component has to be added and every variable has to be copied individually.
		// CopyComponentToGameObject() will be called while it is attached to the original GameObject.
		// That means, gameObject will reference the original, while _duplicatedGameObject will reference the duplicate.
		void CopyComponentToGameObject(GameObject _duplicatedGameObject);
		/* Example code:
		{
			ComponentImplementingIRecorderExtension duplicatedComponent = _duplicatedGameObject.AddComponent<ComponentImplementingIRecorderExtension>();
			duplicatedComponent.originalGameObject = gameObject;
			duplicatedComponent.duplicatedGameObject = _duplicatedGameObject;
			duplicatedComponent.anotherPublicParameter = anotherPublicParameter;
		}*/

		// Option 2: The GameObject will be duplicated by instantiation.
		// In this case, the duplicated GameObject will have identical copies of the original component.
		// GetsCalledBeforeInstantiation() will be called before instantiating.
		// You can use it to store the original GameObject in a variable.
		void GetsCalledBeforeInstantiation();
		/* Example code:
		{
			originalGameObject = gameObject;
		}*/

		// Then for option 2, directly after instantiation, GetsCalledAfterInstantiation() will be called on the duplicated GameObject.
		void GetsCalledAfterInstantiation();
		/* Example code:
		{
			duplicatedGameObject = gameObject;
		}*/

		// Both options can occur, so all three functions must be implemented.
		// The following functions will be called independently of recreation or instantiation.


		// This function will be called once before the recording starts.
		// Look for example for the implementation in CustomMaterialExtension.
		void GetsCalledBeforeRecording();

		// This function will be called once every frame during recording before the animation itself is recorded.
		// So in case of a recreated GameObject, adjustments of the position of the duplicated GameObject will be recorded immediately.
		// Look for example for the implementation in ImitateMovement or MeshRecordingExtension.
		// deltaTime: elapsed seconds since the last frame
		void GetsCalledDuringRecording(float deltaTime);

		// This function will be called once between recording and first playback.
		// Look for example for the implementation in VrWindowExtension or MeshRecordingExtension.
		void GetsCalledBeforePlayback();

		// This function will be called every time the playback time changes.
		// Look for example for the implementation in MeshRecordingExtension.
		// playTime: time of playback in seconds
		void GetsCalledDuringPlayback(float playTime);

		// If the extension instantiates new GameObjects, they have to be registered to be able to modify their materials.
		// This function can be called multiple times to request a List of all GameObjects instantiated by this extension.
		// Look for example for the implementation in MeshRecordingExtension.
		List<GameObject> GetAllGameObjectsInstantiatedByThis();
	}
}