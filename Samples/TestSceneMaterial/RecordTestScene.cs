using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneRecorder;



//WARNING: You leave commented and cleand up territory! Spaghetti code ahead! 




namespace UnitySceneRecorder{
	public class RecordTestScene : MonoBehaviour
	{
		Recorder recorder;
		Replay replay;
		public GameObject sceneObserverHolder;
		public GameObject camHolder;
		public GameObject loadingBar;
		SceneObserver sceneObserver;

		void Update()
		{
			if(loadingBar != null)
			{
				loadingBar.transform.localScale = new Vector3(7.54f * (Time.time/5), 0.06f, 0.58f);
				if(Time.time > 5)
					loadingBar.SetActive(false);
			}

			if(camHolder != null)
			{
				if(Time.time < 5f){
					float camTime = Time.time / 5;

					Vector3 startLocalPosition = new Vector3(-41, 35, 11.8f);
					Vector3 startLocalEulerAngles = new Vector3(61, 180, 0);

					Vector3 endLocalPosition = new Vector3(-9.57f, 17.9f, 6.23f);
					Vector3 endLocalEulerAngles = new Vector3(52.4f, 195.9f, 0);

					camHolder.transform.localPosition = new Vector3(
						Mathf.SmoothStep(startLocalPosition.x, endLocalPosition.x, camTime),
						Mathf.SmoothStep(startLocalPosition.y, endLocalPosition.y, camTime),
						Mathf.SmoothStep(startLocalPosition.z, endLocalPosition.z, camTime));

					camHolder.transform.localEulerAngles = new Vector3(
						Mathf.SmoothStep(startLocalEulerAngles.x, endLocalEulerAngles.x, camTime),
						Mathf.SmoothStep(startLocalEulerAngles.y, endLocalEulerAngles.y, camTime),
						Mathf.SmoothStep(startLocalEulerAngles.z, endLocalEulerAngles.z, camTime));

				}
				else if((Time.time - 5f) / 15f < 1)
				{
					float camTime = (Time.time - 5f) / 15f;// Mathf.Max(0f,Mathf.Min((Time.time-5f)/10f),1f);
														   //camHolder.transform.localEulerAngles = new Vector3(57.4f, 179.9f, 0);
					camHolder.transform.localEulerAngles = new Vector3(52.4f, 195.9f, 0);
					float x = Mathf.SmoothStep(-9.57f, -77.9f, camTime);
					camHolder.transform.localPosition = new Vector3(x, 17.9f, 6.23f);
				}
				else
				{

					float camTime = (Time.time / 3)-7.5f;

					Vector3 startLocalPosition = new Vector3(-41, 35, 11.8f);
					Vector3 startLocalEulerAngles = new Vector3(61, 180, 0);

					Vector3 endLocalPosition = new Vector3(-77.9f, 17.9f, 6.23f);
					Vector3 endLocalEulerAngles = new Vector3(52.4f, 195.9f, 0);

					camHolder.transform.localPosition = new Vector3(
						Mathf.SmoothStep(endLocalPosition.x, startLocalPosition.x, camTime),
						Mathf.SmoothStep(endLocalPosition.y, startLocalPosition.y, camTime),
						Mathf.SmoothStep(endLocalPosition.z, startLocalPosition.z, camTime));

					camHolder.transform.localEulerAngles = new Vector3(
						Mathf.SmoothStep(endLocalEulerAngles.x, startLocalEulerAngles.x, camTime),
						Mathf.SmoothStep(endLocalEulerAngles.y, startLocalEulerAngles.y, camTime),
						Mathf.SmoothStep(endLocalEulerAngles.z, startLocalEulerAngles.z, camTime));
				}
				
			}
			


			if (recorder == null)
			{
				if (sceneObserverHolder == null)
					return;

				sceneObserver = sceneObserverHolder.GetComponent<SceneObserver>();
				if (sceneObserver == null)
					return;

				recorder = sceneObserver.CreateNewSceneRecording();
				return;
			}

			if (replay == null && Time.time > 5f)
			{
				recorder.EndRecording();
				replay = recorder.GetReplayIfFinished();
				if (replay != null)
				{
					sceneObserver.gameObject.transform.localPosition = new Vector3(0, 3.6f, -11.6f);
					sceneObserver.gameObject.transform.localEulerAngles = new Vector3(32.9f, 0, 0);
				}
			}


			if (replay != null && Time.time > 5f)
			{
				float replayTime = (Time.time+1f) % 6;
				if (replayTime > 5)
					replayTime = Mathf.SmoothStep(0, 5, 6 - replayTime);

				//Debug.Log(replayTime);
				replay.SetTime(replayTime);
			}
		}
	}
}