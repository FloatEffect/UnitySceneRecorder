using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnitySceneRecorder;

namespace UnitySceneRecorder{
	public class Recorder : MonoBehaviour
	{
		// The Renderer class can record an animation or still image of the scene and returns a Replay component to replay this animation.
		// To do so, it creates a copy of the scene by duplicating every GameObject that has an ObjectObserver configured as to be recorded.
		// A snapshot can be created by calling CreateNonAnimatedRecording() with the SceneObserver as a parameter to receive a Replay directly.
		// An animation can be recorded by calling the Recorder.Constructor() function with SceneObserver to start a new recording.
		// To end the recording, call EndRecording() which starts the processing of the recording.
		// The processed recording can be retrieved by calling GetReplayIfFinished(), but this function will return null until the processing is complete.
		//Processing can take several seconds, for several minutes long recordings of entire large scenes.


		// Indicates whether the recording has ended
		public bool ifRecordingEnded { get; private set; } = false;
		// Length of the recording in seconds
		public float recordLength { get; private set; } = 0f;
		// List of all Recordings created by this Recorder
		private List<Recording> recordings = new List<Recording>();
		// SceneObserver this Recording is registered at
		private SceneObserver sceneObserver;

		// Constructs a new Recorder, adds it to a new GameObject which will contain the duplicated Scene and its Recordings
		// and assigns it as a child to the SceneObserver holding GameObject.
		public static Recorder Constructor(SceneObserver _sceneObserver, bool animate = true, string customName = "")
		{
			// As Unity doesn't support true object-oriented programming constructors in MonoBehaviour components,
			// this static function emulates constructor functionality.

			// Create record containing GameObject and move it into the SceneObserver containing GameObject
			GameObject recordContainer = new GameObject();
			GameObject sceneObserverGameObject = _sceneObserver.gameObject;
			recordContainer.transform.SetParent(sceneObserverGameObject.transform);

			// Rename it
			if (customName.Length == 0)
				recordContainer.name = sceneObserverGameObject.name + "_RecordContainer_GeneratedAt_" + Time.time;
			else
				recordContainer.name = customName;

			// Hide it during the recording
			recordContainer.SetActive(false);

			// Add a Recorder component
			Recorder recorder = recordContainer.AddComponent<Recorder>();
			recorder.SetSceneObserver(_sceneObserver);

			// Create copies of all registered GameObjects and start the recording
			List<ObjectObserver> registeredObjectObservers = _sceneObserver.GetRegisteredObjectObservers();
			foreach (ObjectObserver objectObserver in registeredObjectObservers)
				recorder.CreateIndividualRecording(objectObserver, animate);

			return recorder;
		}


		// Takes a Snapshot and directly returns the Replay
		// Requires the SceneObserver as parameter to register itself and get access to the scene
		public static Replay CreateNonAnimatedRecording(SceneObserver _sceneObserver, string customName = "")
		{
			// Update the SceneObserver so it has an up-to-date list of registered GameObjects.
			_sceneObserver.UpdateSceneObserver();

			// Construct a Recorder
			Recorder recorder = Recorder.Constructor(_sceneObserver, false, customName);
			// Create only one snapshot
			recorder.SaveSnapshot(0);
			// End the recording
			recorder.EndRecording();
			// Immediately return the Replay, which will be already finished, cause no animation needs to be processed.
			return recorder.GetReplayIfFinished();
		}

		// Creates a Recorder object and starts recording the scene.
		public static Recorder CreateAndStartRecording(SceneObserver _sceneObserver, string customName = "")
		{
			// Update the SceneObserver so it has an up-to-date list of registered GameObjects.
			_sceneObserver.UpdateSceneObserver();

			// Construct a Recorder object and begin recording.
			Recorder ret = Recorder.Constructor(_sceneObserver, true, customName);
			ret.SaveSnapshot(0);
			return ret;
		}


		// Saves a snapshot of the current frame.
		// deltaTime: seconds since the last frame
		public void SaveSnapshot(float deltaTime)
		{
			// If the recording has not ended, tell each Recording to save a snapshot of the current frame.
			if (!ifRecordingEnded)
				foreach (Recording rec in recordings)
					rec.SaveSnapshot(deltaTime);

			// Increase the total recorded length by deltaTime.
			recordLength += deltaTime;
		}


		// Tells all Recordings to end and unregisters this Recorder.
		public void EndRecording()
		{
			// Tell each Recording to end start processing the recording.
			foreach (Recording rec in recordings)
				rec.EndRecording();

			// Mark the recording as ended and unregister the Recorder from the SceneObserver.
			ifRecordingEnded = true;
			sceneObserver.UnregisterRecord(this);
		}


		// Returns a Replay object when all recordings are finished processing.
		// Otherwise, returns null.
		public Replay GetReplayIfFinished()
		{
			// Check if each Recording has finished processing.
			foreach (Recording rec in recordings)
				if (!rec.IfReplayReady())
					return null; // If not, return null to try again later.

			// If all Recordings have finished processing, make the recorded scene visible.
			gameObject.SetActive(true);

			// Pass the Records to a Replay object for replay control and returns it.
			return Replay.Constructor(gameObject, recordings);
		}
		

		// Setting the SceneObserver is required to start a Recording
		public void SetSceneObserver(SceneObserver _sceneObserver)
		{
			sceneObserver = _sceneObserver;
		}


		// Creates a recording for a GameObject with it's children by copying it and adding Recording components.
		// Depending on the copying method chosen (Instantiation vs Recreation), one or two Recordings are returned.
		private List<Recording> CreateIndividualRecording(ObjectObserver objectObserver, bool animate)
		{
			// Ensure input is not null
			if (sceneObserver == null)
			{
				Debug.LogWarning("Recorder Constructor parameter SceneObserver got destroyed. Recording failed.");
				return new List<Recording>();
			}

			// Create a GameObject (further called record container) to contain the duplicated copy
			GameObject recordedObjectContainer = new GameObject();
			recordedObjectContainer.transform.SetParent(gameObject.transform);
			recordedObjectContainer.name = objectObserver.gameObject.name + "_Container_" + Random.Range(1, 9999999);
			//Random.Range(1, 9999999) is a workaround. GameObjectRecorder will ignore GameObjects with the same name.

			// Set variables and load parameter
			GameObject observedGameObject = objectObserver.gameObject;
			GameObject duplicatedGameObject = null;
			List<Recording> newRecordings = new List<Recording>();
			List<Component> returnedComponents = new List<Component>();
			bool forceDuplicationByRecreation = observedGameObject.GetComponent<ForceDuplicationByRecreation>() != null;
			bool forceDuplicationByInstantiation = observedGameObject.GetComponent<ForceDuplicationByInstantiation>() != null;

			// There are two possible ways to copy the GameObject and both have advantages and disadvantages.
			//
			// 1. Instantiation
			// The GameObject can be Instantiated as a whole, including all of its components and children.
			// Some components may have logic that affects the rest of the scene when active, in which case they need to be destroyed.
			// All unknown components have to be destroyed therefor. Not all components are destroyable though.
			// If they can be deactivated instead, an IComponentTypeExtension Interface implementing component
			// has to be attached to the SceneObserver holding GameObject, to skip the destruction and deactivate the component instead.
			// If neither is possible, the GameObject can not be instantiated.
			//
			// 2. Recreation
			// The GameObject can also be recreated by creating empty GameObjects for every GameObject with a Renderer component (which makes it visible).
			// These empty GameObjects receive standard Renderers with the same Materials as the original
			// and ImitateMovements components to mimic the movements of the original.
			// Non-standard Renderer components can not be recreated this way though.
			//
			// If non-standard Renderers are involved, instantiation must be chosen.
			// If non-destroyable and disable-able components are involved, recreation must be chosen.
			// In the rare cases where both are true, the GameObject will not be recorded.
			// You can choose manually between both options by attaching ForceDuplicationByRecreation or ForceDuplicationByInstantiation components
			// to the original root GameObject or to a GameObject which is marked as to be recorded individually (see DoRecordIndividually).

			// Only try instantiation if it is forced
			if (forceDuplicationByInstantiation)
			{
				// Try instantiation 
				returnedComponents = TryToDuplicateByInstantiation(observedGameObject, recordedObjectContainer);
				duplicatedGameObject = recordedObjectContainer.transform.GetChild(0).gameObject;
				//If the record container is empty, duplication by instantiation failed
			}
			if (returnedComponents.Count > 0 && duplicatedGameObject != null)
			{
				// If it was successful, add the Recorders.

				// Append "_Instantiation" to the name of the recorded object container.
				recordedObjectContainer.name = recordedObjectContainer.name + "_Instantiation";

				// Find all IRecorderExtensions in the duplicated GameObject.
				List<IRecorderExtension> copiedExtensions = duplicatedGameObject.GetComponentsInChildren<IRecorderExtension>(true).ToList();
				foreach (IRecorderExtension recorderExtension in copiedExtensions)
					recorderExtension.GetsCalledAfterInstantiation();

				// Add a Recording component to the duplicated GameObject, recording the original GameObject.
				newRecordings.Add(Recording.Constructor(duplicatedGameObject, observedGameObject, returnedComponents, copiedExtensions, animate, sceneObserver.fps));

				// Add an ImitateMovement component to the record container
				// and register the original GameObject to collapse the record container, when the original gets destroyed or disabled
				ImitateMovement imitateMovement = recordedObjectContainer.AddComponent<ImitateMovement>();
				imitateMovement.RegisterObjectToBeCheckedIfNullOrDisabled(observedGameObject);
				// Register the ImitateMovement component
				List<IRecorderExtension> containerExtensions = new List<IRecorderExtension>();
				containerExtensions.Add(imitateMovement);
				// If the original GameObject had a parent, imitate this parent with the record container
				Transform observedGameObjectParentTransform = observedGameObject.transform.parent;
				if (observedGameObjectParentTransform != null && observedGameObjectParentTransform.gameObject != null)
				{
					GameObject observedGameObjectParent = observedGameObjectParentTransform.gameObject;
					imitateMovement.RegisterObjectToBeImitated(observedGameObjectParent);
					imitateMovement.RegisterObjectToBeCheckedIfNullOrDisabled(observedGameObjectParent);
				}
				// Register the transform of the container
				List<Component> singleTransformInList = new List<Component>();
				singleTransformInList.Add(recordedObjectContainer.GetComponent<Transform>());
				// Add a Recording for the record container only 
				newRecordings.Add(Recording.Constructor(recordedObjectContainer, recordedObjectContainer, singleTransformInList, containerExtensions, animate, sceneObserver.fps));

			}
			else 
			{
				// If the duplication was unsuccessful:

				// Try to duplicate the GameObject by recreation.
				returnedComponents = TryToDuplicateByRecreation(observedGameObject, recordedObjectContainer);

				// If the duplication was successful,
				if (returnedComponents.Count > 0)
				{
					// Append "_Recreation" to the name of the recorded object container.
					recordedObjectContainer.name = recordedObjectContainer.name + "_Recreation";

					// Find all IRecorderExtensions in the record container.
					List<IRecorderExtension> copiedExtensions = recordedObjectContainer.GetComponentsInChildren<IRecorderExtension>(true).ToList();

					// Add the transform of the record container to the components to bind by recorder
					returnedComponents.Add(recordedObjectContainer.GetComponent<Transform>());

					// Add a Recording for the recorded object container.
					newRecordings.Add(Recording.Constructor(recordedObjectContainer, recordedObjectContainer, returnedComponents, copiedExtensions, animate, sceneObserver.fps));
				}
				else
				{
					// If duplication by instantiation and recreation both failed, return an empty list of Recordings.
					// This can happen if the original GameObject was empty or had no Recorder.
					return new List<Recording>();
				}
			}

			// Add all new Recordings to the Recorder
			recordings.AddRange(newRecordings);
			// Return the new Recordings for further optional action.
			// Do not add them to recordings twice!
			return newRecordings;
		}


		// Post-registers new ObjectObserver to already recording Recorders
		// This happens when new GameObjects spawn and are found by the SceneRecorder
		// or when new child GameObjects get attached and registered by the parents ObjectObserver
		public void PostRegisterObject(ObjectObserver objectObserver)
		{
			if (ifRecordingEnded)
				return;

			// Create a new Recording
			List<Recording> newRecordings = CreateIndividualRecording(objectObserver, true);

			// Find all ImitateMovement components of the duplicated GameObjects
			List<ImitateMovement> imitateMovements = new List<ImitateMovement>();
			foreach (Recording rec in newRecordings)
				foreach (Component component in rec.extensions)
					if (component is ImitateMovement)
						imitateMovements.Add(component as ImitateMovement);

			// Make them invisible 
			foreach (ImitateMovement imitateMovement in imitateMovements)
				imitateMovement.forceCollapse = true;

			// Record the new GameObjects as long as they are late to synchronize the new Recordings with the other Recordings.
			foreach (Recording rec in newRecordings)
			{
				// Create two snapshots between which the recorded object is invisible to prevent linear interpolation from causing
				// the object to grow linearly until the moment it should actually appear.
				rec.SaveSnapshot(0.00001f);
				rec.SaveSnapshot(recordLength - 0.00001f);
			}

			// Make them visible again
			foreach (ImitateMovement imitateMovement in imitateMovements)
				imitateMovement.forceCollapse = false;
		}


		// Copies the GameObject by instantiation and tries to clean it of unknown components
		private List<Component> TryToDuplicateByInstantiation(GameObject originalGameObject, GameObject recordedObjectContainer)
		{
			// Find all IRecorderExtension components in the original GameObject and update them before instantiation.
			IRecorderExtension[] recorderExtensions = originalGameObject.GetComponentsInChildren<IRecorderExtension>(true);
			foreach (IRecorderExtension recorderExtension in recorderExtensions)
				recorderExtension.GetsCalledBeforeInstantiation();

			// Finds and renames all original sibling GameObjects with the same name
			// GameObjectRecorder identifies GameObjects by name, which makes this workaround necessary to apply the recording to the right GameObject
			RenameSiblingsWithSameName(originalGameObject);

			// Instantiate the original GameObject
			GameObject duplicatedGameObject = Instantiate(originalGameObject);
			duplicatedGameObject.SetActive(true);

			// Move the duplicated GameObject into the record container.
			duplicatedGameObject.transform.SetParent(recordedObjectContainer.transform);

			// Collect recorded components, such as Transforms, from the original GameObject
			// and destroy any child GameObjects in the duplicated version that should not be recorded.
			List<Component> components = GetAnimatedComponentsAndDestroyNonVisibleGameObjects(duplicatedGameObject, originalGameObject);

			// Check if there are components to be recorded and if the deletion of unknown components was successful.
			if (components.Count > 0 && TryToDestroyUnknownComponents(duplicatedGameObject))
				return components;
			else
			{
				// If deletion was not successful, clean up and return an empty List.
				// An empty list will be interpreted as fail
				// and duplication by recreation will be tried next.
				Destroy(duplicatedGameObject);
				return new List<Component>();
			}
		}


		// Tries to recreate the original GameObject as a duplicate GameObject, and continues recursively for its children that are configured to be recorded.
		private List<Component> TryToDuplicateByRecreation(GameObject originalGameObject, GameObject recordedObjectContainer)
		{
			// Get the ObjectObserver component of the original GameObject
			ObjectObserver objectObserver = originalGameObject.GetComponent<ObjectObserver>();

			List<Component> ret = new List<Component>();

			// If the ObjectObserver is not present or the GameObject is not configured to be recorded, return an empty list
			if (objectObserver == null || !objectObserver.IfRecord())
				return ret;

			// Get potential Renderer and MeshFilter components of the original GameObject
			Renderer renderer = originalGameObject.GetComponent<Renderer>();
			MeshFilter meshFilter = originalGameObject.GetComponent<MeshFilter>();

			// Rename all original sibling GameObjects with the same name
			// GameObjectRecorder identifies GameObjects by name, which makes this workaround necessary to apply the recording to the right GameObject
			RenameSiblingsWithSameName(originalGameObject);

			// If both a Renderer and MeshFilter were found in the original GameObject
			if (renderer != null && meshFilter != null)
			{
				// Create a new GameObject
				GameObject duplicatedGameObject = new GameObject();
				// Move it into the record container
				duplicatedGameObject.transform.SetParent(recordedObjectContainer.transform);
				// And rename it to avoid siblings with the same name in the duplicated GameObject
				duplicatedGameObject.name = originalGameObject.name + "_" + Random.Range(1, 9999999);
				//Random.Range(1, 9999999) is a workaround. GameObjectRecorder will ignore Objects with the same name.

				// Try to recreate the Renderer und copy the shared mesh
				if (TryToCopySharedMesh(originalGameObject, duplicatedGameObject))
				{
					// Add an ImitateMovement component to the new GameObject, to make it imitate the movement of the original GameObject
					ImitateMovement imitateMovement = duplicatedGameObject.AddComponent<ImitateMovement>();
					imitateMovement.RegisterObjectToBeCheckedIfNullOrDisabled(originalGameObject);
					imitateMovement.RegisterObjectToBeImitated(originalGameObject);

					// Copy all IRecorderExtension components from the original GameObject to the new GameObject
					var componentsWithRecorderExtension = originalGameObject.GetComponents<IRecorderExtension>();
					foreach (var comp in componentsWithRecorderExtension)
						comp.CopyComponentToGameObject(duplicatedGameObject);

					// If the GameObject is configured to be animated in the recording,
					// add its Transform component to the list of duplicated components
					if (objectObserver.IfAnimate())
					{
						ret.Add(duplicatedGameObject.transform);
						// TODO figure out which components would also be interesting to record
					}
				}
				else
				{
					// If TryToCopySharedMesh failed, clean up 
					Destroy(duplicatedGameObject);
				}
			}

			// Continue recursively for every child which is configured as to be recorded and not as to be recorded individually
			foreach (Transform child in originalGameObject.transform)
			{
				ObjectObserver childObjectObserver = child.gameObject.GetComponent<ObjectObserver>();
				if(childObjectObserver != null && !childObjectObserver.IfRoot() && childObjectObserver.IfRecord())
					ret.AddRange(TryToDuplicateByRecreation(child.gameObject, recordedObjectContainer));
			}
			return ret;
		}


		// This function renames siblings with the same name as a workaround for GameObjectRecorder.
		// GameObjectRecorder binds GameObjects by name and ignores Objects with the same name.
		// I hope this does not mess with any other functionality of your project,
		// but I expect that if you allow GameObjects having the same name, the name is not important to identify it.
		// Usually it just happens when you spawn copies of a GameObject, that they get all named "Name(Clone)".
		private void RenameSiblingsWithSameName(GameObject _gameObject)
		{
			foreach (Transform _transform in _gameObject.GetComponentsInChildren<Transform>(true))
			{
				if (_transform.parent == null)
				{
					// If it is a root GameObject, rename all other root GameObjects with the same name
					foreach (GameObject rootGameObject in gameObject.scene.GetRootGameObjects())
					{
						if (rootGameObject == _transform.gameObject)
							continue;
						
						if (rootGameObject.name == _transform.gameObject.name)
							rootGameObject.name = rootGameObject.name + "_renamed_" + Random.Range(1, 9999999);
						//Random.Range(1, 9999999) is a workaround. GameObjectRecorder will ignore Objects with the same name.
					}
				}
				else
				{
					// If it has a parent, rename all other children of the the parent with the same name
					foreach (Transform child in _transform.parent.transform)
					{
						if (_transform == child)
							continue;

						if (child.gameObject.name == _transform.gameObject.name)
							child.gameObject.name = child.gameObject.name + "_renamed_" + Random.Range(1, 9999999);
						//Random.Range(1, 9999999) is a workaround. GameObjectRecorder will ignore Objects with the same name.
					}
				}
			}
		}

		// The function is used to get all animated components and remove non-visible GameObjects.
		// It returns List of all Transform components
		// and it destroys duplicated children which should not be recorded or which are part of an individual recording.
		// If destroyNextLevelRoot is true, it means that this function was called by itself and if this child is also configured as root,
		// it is part of another independent recording, so it has to be destroyed.
		private List<Component> GetAnimatedComponentsAndDestroyNonVisibleGameObjects(GameObject duplicatedGameObject, GameObject originalGameObject, bool destroyNextLevelRoot = false)
		{
			// Get ObjectObserver
			ObjectObserver objectObserver = originalGameObject.GetComponent<ObjectObserver>();
			List<Component> ret = new List<Component>();

			// Destroy duplicated GameObject if original is configured to be not recorded or if its part of an individual Recording
			if (objectObserver == null || !objectObserver.IfRecord() || (destroyNextLevelRoot && objectObserver.IfRoot()))
			{
				Destroy(duplicatedGameObject);
				return ret;
			}

			// If the GameObject is configured to be animated in the recording
			if (objectObserver.IfAnimate())
			{
				// Add its Transform component
				ret.Add(originalGameObject.transform);
				// TODO figure out which components would also be interesting to record
			}

			// Continue recursively for every child
			for (int i = 0; i < duplicatedGameObject.transform.childCount; i++)
			{
				Transform originalChild = originalGameObject.transform.GetChild(i);
				Transform duplicatedChild = duplicatedGameObject.transform.GetChild(i);
				if (originalChild != null && duplicatedChild != null)
					ret.AddRange(GetAnimatedComponentsAndDestroyNonVisibleGameObjects(duplicatedChild.gameObject, originalChild.gameObject, true));
			}
			return ret;
		}


		// Tries to destroy all unknown components in a duplicated GameObject and returns false if it fails to do so.
		bool TryToDestroyUnknownComponents(GameObject duplicatedGameObject)
		{
			// Disable any components which have to be disabled instead of destroyed, based on IComponentTypeExtension.
			// For example, the Interactable component of SteamVR can't be destroyed without error.
			foreach (IComponentTypeExtension componentTypeExtension in sceneObserver.GetComponentTypeExtensions()) 
				componentTypeExtension.DisableComponentsInDuplicatedChildren(duplicatedGameObject);

			// Get all components of the duplicated GameObject.
			Component[] components = duplicatedGameObject.GetComponentsInChildren<Component>(true);

			// Some components may only be destroyable after others have been destroyed
			// If these dependencies form an acyclic graph. It will take 'components.Length' tries in the worst case scenario.
			// If dependencies are cyclic or lay outside the GameObject, this approach will fail.
			int lastRoundNumberOfUnknownComponents = components.Length;
			for (int i = components.Length; i > 0; i--)
			{
				int remainingNumberOfUnknownComponents = 0;
				foreach (Component component in components)
				{
					if (component is null)
						continue;

					// Check if the component type should not be destroyed based on IComponentTypeExtension.
					bool isComponentTypeOfExtension = false;
					foreach (IComponentTypeExtension componentTypeExtension in sceneObserver.GetComponentTypeExtensions())
						isComponentTypeOfExtension = isComponentTypeOfExtension || componentTypeExtension.DoNotDestroyComponentInDuplicatedChildren(component);

					// Check if it is one of the standard Component types that shouldn't be destroyed.
					if (!( isComponentTypeOfExtension
						|| (component is Transform)
						|| (component is MeshRenderer)
						|| (component is MeshFilter)
						|| (component is Renderer)
						|| (component is IRecorderExtension)))
					{
						try
						{
							// Try to destroy the component if it is unknown.
							Destroy(component);
						}
						catch
						{
							// If Destroy caused an error, it means the component is not destroyable yet.
							remainingNumberOfUnknownComponents++;
						}
					}
				}

				// All unknown components destroyed -> success
				if (remainingNumberOfUnknownComponents == 0)
					return true; 

				// If no unknown components destroyable -> failed
				if(remainingNumberOfUnknownComponents == lastRoundNumberOfUnknownComponents)
					return false; 

				// If only some unknown components destroyed -> go on
				lastRoundNumberOfUnknownComponents = remainingNumberOfUnknownComponents;
			}
			// Has no unknown component anymore
			return true;
		}


		// If the source GameObject has a standard Renderer with Mesh, copy the Mesh and its materials to the destination GameObject.
		private bool TryToCopySharedMesh(GameObject source, GameObject destination)
		{
			// Returns false if the source GameObject doesn't have a MeshFilter and thus is not a standard Renderer.
			MeshFilter meshFilter = source.GetComponent<MeshFilter>();
			if (meshFilter == null)
				return false;

			// Checking a shared mesh against null will freeze the engine. Using a try-catch block won't.
			try
			{
				// Instantiate a copy of the shared mesh and assign it to the MeshFilter component of the duplicated GameObject.
				destination.AddComponent<MeshFilter>().sharedMesh = Instantiate(meshFilter.sharedMesh);
				// Copy the shared materials to the MeshRenderer component of the duplicated GameObject.
				destination.AddComponent<MeshRenderer>().sharedMaterials = meshFilter.GetComponent<MeshRenderer>().sharedMaterials;
			}
			catch
			{
				// If the shared mesh is null, return false.
				return false;
			}
			// If the copying is successful, return true.
			return true;
		}
	}
}