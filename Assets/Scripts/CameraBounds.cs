using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class CameraBounds:MonoBehaviour
{

    
   new Camera camera;
    public float LeftCameraBounds, RightCameraBounds, topCameraBounds, bottomCameraBounds;
   
    private void Start()
    {
        camera = GetComponent<Camera>();
          
    }
    private void Update()
    {
        PlayAreaLimits();
    }

    public void PlayAreaLimits()
    {
        LeftCameraBounds = camera.ViewportToWorldPoint(new Vector3(0, 0, 0)).x; //sets bounds of camera
        RightCameraBounds = camera.ViewportToWorldPoint(new Vector3(1, 0, 0)).x;
        topCameraBounds = camera.ViewportToWorldPoint(new Vector3(0, 1, 0)).y ;
        bottomCameraBounds = camera.ViewportToWorldPoint(new Vector3(0, -0.5f, 0)).y;
    }
}
