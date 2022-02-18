using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
  


public class GameManager:NetworkBehaviour
{
    new Camera camera;
    public float LeftCameraBounds, RightCameraBounds, topCameraBounds, bottomCameraBounds;
    public bool blockArraySpawned = false;
   
    private void Start()
    {
        camera = FindObjectOfType<Camera>();
          
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
