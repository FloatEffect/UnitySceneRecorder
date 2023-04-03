using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotateMeSlowly : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        gameObject.transform.localEulerAngles =new Vector3(0f, Time.time*66f, 0f);
    }
}
