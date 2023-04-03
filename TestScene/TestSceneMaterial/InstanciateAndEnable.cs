using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneRecorder;

namespace UnitySceneRecorder{
	public class InstanciateAndEnable : MonoBehaviour
	{
		public GameObject target;
		float pseSec = 0.7f;
		float lastTrigger = -100;
		public float shifted = 0.01f;
		public bool makeRoot = false;

		// Start is called before the first frame update
		void Start()
		{
			
		}

		// Update is called once per frame
		void Update()
		{
			if (target == null)
				return;

			if(lastTrigger + pseSec < Time.time)
			{
				lastTrigger = Time.time;

				GameObject newGameObject;
				if (!makeRoot)
					newGameObject = Object.Instantiate(target, gameObject.transform);
				else
					newGameObject = Object.Instantiate(target);
				//newGameObject.name = newGameObject.name + "_" + Random.Range(1, 9999999); // Unity will ignore Objects with the same name in some cases. Random.Range(1, 9999999) is added as a workarround. 
				newGameObject.transform.position = target.transform.position + new Vector3(shifted * Mathf.Sin(Time.time-2), 0f, shifted * Mathf.Cos(Time.time-2));
				newGameObject.SetActive(true);
			}
		}
	}
}