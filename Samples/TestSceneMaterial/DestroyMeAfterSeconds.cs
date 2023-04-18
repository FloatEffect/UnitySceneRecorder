using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnitySceneRecorder;

namespace UnitySceneRecorder{
	public class DestroyMeAfterSeconds : MonoBehaviour
	{
		private float initialized = 0f;
		public float afterSeconds = 1f;
		// Start is called before the first frame update
		void Start()
		{
			initialized = Time.time;
		}

		// Update is called once per frame
		void Update()
		{
			if (Time.time > initialized + afterSeconds)
				Destroy(gameObject);
		}
	}
}