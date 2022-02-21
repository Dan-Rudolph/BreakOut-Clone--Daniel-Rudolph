using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public bool blockArraySpawned = false;
    public bool canLaunch = false;
    public bool loadedBall = false;
    public List<string> sceneBalls = new List<string>();
    void Awake()
    {
        if (instance != null)
            GameObject.Destroy(instance);
        else
            instance = this;
        DontDestroyOnLoad(this);
    }
}

