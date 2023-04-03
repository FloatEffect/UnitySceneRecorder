using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneRecorder;

// The Recorder duplicates GameObjects by instantiation. To prevent unknown components from interfering with the scene, they need to be cleaned.
// Some components cannot be destroyed without causing errors.
// To disable these components without destroying them, add a component with this extension to the GameObject that holds the SceneObserver component.
// It will be automatically called during the duplication process.

namespace UnitySceneRecorder{
	public interface IComponentTypeExtension
	{
		// Disable all components of a/multiple specific type/types in '_gameObject' and its children.
		// This function is called on GameObjects duplicated by the Recorder to clean them of unwanted components without destroying them.
		void DisableComponentsInDuplicatedChildren(GameObject _gameObject);
		/* Example code:
		{
			SpecificComponentType[] componentsToBeDisabled = _gameObject.GetComponentsInChildren<SpecificComponentType>(true);
			foreach(SpecificComponentType component in componentsToBeDisabled)
				component.enabled = false;
		}*/

		// Returns 'true' if a component is of a specific type that should NOT be destroyed.
		// This function is called for every component of a GameObject duplicated by the Recorder.
		// If it returns 'false', the component will be destroyed.
		bool DoNotDestroyComponentInDuplicatedChildren(Component component);
		/* Example code:
		{
			if(component is SpecificComponentType || component is AnotherSpecificComponentType)
				return true;
			return false;
		}*/
	}
}

