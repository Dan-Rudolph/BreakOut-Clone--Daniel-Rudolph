using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public bool blockArraySpawned = false;
    public int totalBallCount;
    public bool canLaunch;
    void Awake()
    {
        if (instance != null)
            GameObject.Destroy(instance);
        else
            instance = this;
        DontDestroyOnLoad(this);
    }
}

