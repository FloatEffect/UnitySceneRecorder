using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyMeBelowMinus10 : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (gameObject.transform.position.y < -10)
            Destroy(gameObject);
    }
}
