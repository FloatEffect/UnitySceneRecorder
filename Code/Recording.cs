using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;
using UnitySceneRecorder;

namespace UnitySceneRecorder{
	public class Recording : MonoBehaviour
	{
		// A Recording is a component that records the movements of a single GameObject and its children.
		// It creates an AnimationClip using a GameObjectRecorder, which can be played back later by a Replay object.
		// Multiple Recordings can be handled by a single Recorder, and they are given to the Replay after processing.
		// New recordings can also be registered while the Recorder is already recording.
		// In this case a long frame is recorded to synchronize it with other recordings of the same Recorder.


		// Records the movement of one GameObject and it's children during the recording. Gets destroyed after finishing.
		private GameObjectRecorder gameObjectRecorder;
		// Finished animation to be applied to an equivalent GameObject as the recorded one, to replay the movements of the original.
		private AnimationClip animationClip;
		// If the original GameObject gets destroyed, no further snapshots will be taken.
		private bool disableTakeSnapshot = false;
		// Recording frames per second adjustable in the Constructor
		private float fps = 5f;
		// By the Recorder duplicated GameObject to display the replay
		public GameObject duplicatedGameObject { get; private set; }
		// Is true if the recording got ended
		public bool ifRecordingEnded { get; private set; } = false;
		// Is true as soon the ended recording got processed
		private bool ifReplayReady = false; 
		// Record length in seconds
		public float recordLength { get; private set; } = 0f;
		// List of all materials found in the duplicatedGameObject and all children at the end of the recording
		public List<Material> materials { get; private set; } = new List<Material>();
		// List of all registered extensions which have to be called during recording or playback
		public List<IRecorderExtension> extensions { get; private set; } = new List<IRecorderExtension>();


		// Constructs a new Recording as component of the _duplicatedGameObject,
		// The Recording component requires a duplicatedGameObject, which will be used to display the replay.
		// Also needed are the originalGameObject to record from,
		// a List of all componentsToRecord (e.g. transforms) of the originalGameObject and its children,
		// and a List of all IRecorderExtension, which should be updated during recording and replay.
		// A Recording component is also required when recording a still snapshot,
		// but only if animate is true, a GameObjectRecorder will be created to record changing movements.
		// The fps of the recording can be adjusted.
		public static Recording Constructor(GameObject _duplicatedGameObject, GameObject originalGameObject, List<Component> componentsToRecord, List<IRecorderExtension> _extensions, bool animate = true, float _fps = 5)
		{
			// As Unity doesn't support true object-oriented programming constructors in MonoBehaviour components,
			// this static function emulates constructor functionality.

			// Return empty Recording if _duplicatedGameObject is null
			if (_duplicatedGameObject == null)
			{
				Debug.LogWarning("Recording Constructor parameter duplicatedGameObject was NULL. The recording was lost.");
				GameObject emptyComponentHolder = new GameObject();
				emptyComponentHolder.name = "If_you_see_this_GameObject,_Recording_Constructor_parameter_was_NULL";
				return emptyComponentHolder.AddComponent<Recording>();
			}

			// If a Recording component already exists, return it instead of creating a new one
			Recording ret = _duplicatedGameObject.GetComponent<Recording>();
			if (ret != null)
			{
				Debug.LogWarning("Recording Constructor parameter recordContainer had already a Recording Component. The new recording was lost.");
				return ret;
			}

			// Create a Recording component and start the recording
			ret = _duplicatedGameObject.AddComponent<Recording>();
			ret.StartRecording(originalGameObject, componentsToRecord, _extensions, animate, _fps);
			return ret;
		}


		// To create a still snapshot, call this function to get already finished Recording back.
		// See Recording.Constructor for parameter
		public static Recording ConstructNonAnimatedRecording(GameObject _duplicatedGameObject, GameObject originalGameObject, List<IRecorderExtension> _extensions)
		{
			// Create a new Recording component and end the recording
			Recording ret = Recording.Constructor(_duplicatedGameObject, originalGameObject, new List<Component>(), _extensions, false);
			ret.EndRecording();

			// Load the first snapshot and return the Recording
			ret.LoadSnapshot(0);
			return ret;
		}


		// Initializes the Recording, preparing the extensions and creating a GameObjectRecorder if animate is true
		public void StartRecording(GameObject originalGameObject, List<Component> componentsToRecord, List<IRecorderExtension> _extensions, bool animate = true, float _fps = 5f)
		{

			duplicatedGameObject = gameObject;
			extensions = _extensions;
			fps = _fps;

			// Prepare extensions for recording
			foreach (IRecorderExtension Extension in extensions)
				Extension.GetsCalledBeforeRecording();

			// If animated, create a GameObjectRecorder and bind all animated components of the original GameObject
			if (animate)
			{
				gameObjectRecorder = new GameObjectRecorder(originalGameObject);
				
				foreach (Component comp in componentsToRecord)
					gameObjectRecorder.BindComponent(comp);
			}
		}


		// Create a new snapshot with deltaTime as time passed since last frame in seconds.
		public void SaveSnapshot(float deltaTime)
		{
			// If the recording has ended, return immediately
			if (ifRecordingEnded)
				return;

			// Add the deltaTime to the total length of the recording
			recordLength += deltaTime;

			// Update all extensions
			foreach (IRecorderExtension Extension in extensions)
				Extension.GetsCalledDuringRecording(deltaTime);

			// Try to record a new snapshot
			if (gameObjectRecorder != null && !disableTakeSnapshot)
			{
				try
				{
					gameObjectRecorder.TakeSnapshot(deltaTime);
				}
				catch
				{
					// If recording frame by GameObjectRecorder fails, disable further TakeSnapshot calls for performance reasons
					// This can happen when the original GameObject is destroyed during recording, which is intended behavior
					disableTakeSnapshot = true;
					Debug.Log("GameObject got destroyed during recording.");
				}
			}
		}


		// Ends the recording and starts preparing it for playback
		public void EndRecording()
		{
			if (ifRecordingEnded || duplicatedGameObject == null)
				return;

			ifRecordingEnded = true;

			// If an animation was recorded process the recording to an animation
			// For long and large recordings this will take some time and can causes the rendering to freeze for several seconds
			// Till now I haven't found a way to move this into a parallel process in Unity,
			// as the SaveToClip call can not be split up to be continued over several frames in a Coroutine 
			// and it needs Unity API support and thus cannot utilize the Job System for parallelization.
			if (gameObjectRecorder != null)
			{
				// Disabling Curve-Filtering by Setting up CurveFilterOptions 
				// - If the scene is small these filter options can be set to 0.5f
				// - If the scene is large this would cause misaligned movement though
				CurveFilterOptions maxError = new CurveFilterOptions();
				maxError.floatError = 0.001f;
				maxError.positionError = 0.001f;
				maxError.rotationError = 0.001f;
				maxError.scaleError = 0.001f;
				maxError.keyframeReduction = true;
				maxError.unrollRotation = true;

				// Process the recording to an animation clip
				animationClip = new AnimationClip();
				gameObjectRecorder.SaveToClip(animationClip, fps, maxError);
				animationClip.wrapMode = WrapMode.ClampForever;

				// Destroy gameObjectRecorder to free space
				gameObjectRecorder = null;
			}

			// Mark the recording as ready for replay
			ifReplayReady = true;

			// Update all extensions
			foreach (IRecorderExtension extension in extensions)
				extension.GetsCalledBeforePlayback();

			// Find all Materials in the duplicatedGameObject and it's children
			materials = new List<Material>();
			Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
			foreach (Renderer renderer in renderers)
				foreach (Material material in renderer.materials)
					materials.Add(material);
		}


		// Returns true if the recording has been processed and is ready for playback
		public bool IfReplayReady()
		{
			if (!ifRecordingEnded)
				return false;
			if (ifReplayReady)
				return true;

			// Check if playback is ready. See comment in EndRecording()

			return ifReplayReady;
		}


		// Load the snapshot at the given second of the recording
		public void LoadSnapshot(float atSecond)
		{
			// Skip if replay isn't ready or if the duplicated GameObject is missing
			if (!ifReplayReady || duplicatedGameObject == null)
			{
				Debug.Log("LoadSnapshot failed. ifReplayReady: " + ifReplayReady + "; duplicatedGameObject missing: " + (duplicatedGameObject == null) + "");
				return;
			}

			// Clip replay time at the end of the recording
			atSecond = Mathf.Min(atSecond, recordLength);
			/*if (atSecond > recordLength)
			{
				// Uncomment this block to check for overshooting timeline (Wow, I'm impressed which comment GPT suggests here :D )
				Debug.Log("Overshooting timeline: " + (atSecond - recordLength) + " GameObject: " + duplicatedGameObject.name + "");
				return;
			}*/

			// Update all properties which were bound by GameObjectRecorder
			if (animationClip != null)
				animationClip.SampleAnimation(duplicatedGameObject, atSecond);
			else
				Debug.LogWarning("LoadSnapshot failed. animationClip is null");

			// Call frame update for Extensions
			foreach (IRecorderExtension extension in extensions)
				extension.GetsCalledDuringPlayback(atSecond);
		}
	}
}