using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public bool blockArraySpawned = false;
    public bool canLaunch = false;
    public bool loadedBall = false;
 
    public GameObject[,] blockArray;
    void Awake()
    {
       blockArray = new GameObject[10, 5];
        if (instance != null)
            GameObject.Destroy(instance);
        else
            instance = this;
       // DontDestroyOnLoad(this);
    }
   
}

