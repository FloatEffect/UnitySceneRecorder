using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneRecorder;

namespace UnitySceneRecorder{
	public class ObjectObserver : MonoBehaviour
	{
		// ObjectObservers get attached as component to GameObjects to keep track of them.
		// They can be configured as not recorded and thus invisible in the recording, 
		// or as not animated and thus not moving in the recording (to save resources).
		// All child GameObjects will be configured identical unless they are configured
		// as to be recorded individually and thus being treated as root GameObjects.

		// Note that Unity is not able to distinguish instantiated GameObjects from it's original.
		// If you instantiate a GameObject owning a ObjectObserver, this new GameObject will not recognize that it is new
		// and only can be registered as a new object if you destroy the ObjectObserver component of the clone.


		// If the GameObject should be recorded
		private bool doNotRecord = false;
		// If the GameObject should be animated in the recording
		private bool doNotAnimate = false;
		// If the GameObject should be recorded individually as it was a root GameObject
		private bool treatAsRoot = false;
		// The SceneObserver tracking all ObjectObservers and Recorder
		private SceneObserver sceneObserver;

		[Header("Debug Output (doesn't change anything, only for display)")]
		public bool Debug_IfRecorded = true;
		public bool Debug_IfAnimated = true;
		public bool Debug_IfRoot = false;


		// Adds or updates an ObjectObserver component for a GameObject and links it to a SceneObserver.
		// If an ObjectObserver already exists for the GameObject, its configuration is updated if necessary.
		public static ObjectObserver Constructor(GameObject _gameObject, SceneObserver _sceneObserver, bool _treatAsRoot = true, bool doNotRecordParent = false, bool doNotAnimateParent = false)
		{
			// As Unity doesn't support true object-oriented programming constructors in MonoBehaviour components,
			// this static function emulates constructor functionality.

			// Try to find an existing ObjectObserver for the GameObject
			ObjectObserver objectObserver = _gameObject.GetComponent<ObjectObserver>();
			bool foundExistingObjectObserver = objectObserver != null;
			bool existingObjectObserverWasRecorded = false;

			// If an ObjectObserver already exists, check if it was previously being recorded
			if (foundExistingObjectObserver)
				existingObjectObserverWasRecorded = objectObserver.IfRecord();

			// If no ObjectObserver was found, create a new one and link the SceneObserver
			if (!foundExistingObjectObserver)
			{
				objectObserver = _gameObject.AddComponent<ObjectObserver>();
				objectObserver.SetSceneObserver(_sceneObserver);
			}

			// Check if this GameObject should be recorded individually
			if (_treatAsRoot == false && ( // by checking the input parameter
				_gameObject.GetComponent<DoRecordIndividually>() != null || // by checking for an attached marker
				_sceneObserver.individuallyRecordedGameObjects.Contains(_gameObject))) // by checking SceneObservers List 
			{
				// If it should be recorded individually, remember to modify it after the other configuration.
				// This order minimizes unnecessary reconfigurations.
				_treatAsRoot = true;
			}

			// Check if the GameObject should not be recorded
			if ((doNotRecordParent && !_treatAsRoot) || // by checking the input parameter
				_gameObject.GetComponent<DoNotRecord>() != null || // by checking for an attached marker
				_sceneObserver.nonRecordedGameObjects.Contains(_gameObject)) // by checking SceneObservers List 
			{
				//If so, update its configuration to not be recorded
				objectObserver.MakeNonRecorded();
			}

			// Check if the GameObject should not be animated in the recording
			if ((doNotAnimateParent && !_treatAsRoot) || // by checking the input parameter
				_gameObject.GetComponent<DoNotAnimateRecording>() != null || // by checking for an attached marker
				_sceneObserver.nonMovingGameObjects.Contains(_gameObject))// by checking SceneObservers List 
			{
				//If so, update its configuration to not be animated
				objectObserver.MakeNonAnimated();
			}

			// Propagate the new configuration to all children if it was updated
			if (objectObserver.IfRecord() || (!objectObserver.IfRecord() && existingObjectObserverWasRecorded))
			{
				objectObserver.AddObjectObserverToChildren();
			}

			// If this GameObject should be recorded individually, update its configuration
			if (_treatAsRoot && objectObserver.IfRecord())
			{
				objectObserver.MakeRoot();
			}

			return objectObserver;
		}


		// During initialization the SceneObserver has to be set
		public void SetSceneObserver(SceneObserver _sceneObserver)
		{
			sceneObserver = _sceneObserver;
		}


		// This method is called automatically when the children of the GameObject change.
		// It checks if there are new unregistered GameObjects and adds an ObjectObserver for each one.
		void OnTransformChildrenChanged()
		{
			if (doNotRecord || sceneObserver == null)
				return;

			// Check if there are new unregistered GameObjects
			foreach (Transform child in transform)
			{
				ObjectObserver _objectObserver = child.gameObject.GetComponent<ObjectObserver>();
				if (_objectObserver == null)
				{
					// If so, add an ObjectObserver and post-register it 
					_objectObserver = ObjectObserver.Constructor(child.gameObject, sceneObserver, false, doNotRecord, doNotAnimate);
					sceneObserver.RegisterNewObjectObserver(_objectObserver);
				}
			}
		}


		// This method updates all children by adding or updating their ObjectObservers.
		void AddObjectObserverToChildren()
		{
			if (sceneObserver == null)
				return;

			// Add or update the ObjectObservers for all children
			foreach (Transform child in transform)
				ObjectObserver.Constructor(child.gameObject, sceneObserver, false, doNotRecord, doNotAnimate);
		}


		// This method returns true if the GameObject is configured to be animated in recordings.
		public bool IfAnimate()
		{
			return !doNotAnimate;
		}


		// This method returns true if the GameObject is configured to be recorded.
		public bool IfRecord()
		{
			return !doNotRecord;
		}


		// This method returns true if the GameObject is configured to be recorded individually like a root GameObject.
		public bool IfRoot()
		{
			return treatAsRoot;
		}


		// Update the configuration to disable animation in the recording.
		public void MakeNonAnimated(bool forceUpdateChildren = false)
		{
			if (doNotAnimate)
				return;

			// Disable animation
			doNotAnimate = true;
			// Update debug display in the inspector
			Debug_IfAnimated = false;

			// Propagate the change to all children
			if (!doNotRecord || forceUpdateChildren)
				AddObjectObserverToChildren();
		}


		// Update the configuration to disable recording.
		public void MakeNonRecorded(bool forceUpdateChildren = false)
		{
			if (doNotRecord)
				return;

			// Disable recording
			doNotRecord = true;
			// Update debug display in the inspector
			Debug_IfRecorded = false;

			// Propagate the change to all children if requested
			if (forceUpdateChildren)
				AddObjectObserverToChildren();
		}


		// Update the configuration to be recorded individually like a root GameObject
		public void MakeRoot()
		{
			if (treatAsRoot || sceneObserver == null)
				return;

			// Update as recorded individually like a root GameObject
			treatAsRoot = true;
			// Update debug display in the inspector
			Debug_IfRoot = true;

			// Register this as new root GameObject
			sceneObserver.RegisterNewObjectObserver(this, true);
		}


		// Automatically called when this component is destroyed.
		void OnDestroy()
		{
			// If it was registered, unregister it
			if (treatAsRoot && sceneObserver != null)
				sceneObserver.UnregisterObjectObserver(this);
		}
	}
}