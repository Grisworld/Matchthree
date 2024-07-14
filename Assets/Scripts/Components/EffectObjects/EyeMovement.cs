using System.Collections;
using System.Collections.Generic;
using Components;
using Unity.VisualScripting;
using UnityEngine;

public class EyeMovement : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
        if(transform.GetComponentInParent(typeof(Tile)))
            Debug.Log("my parent is Tile yes?");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
