using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
public class Player : NetworkBehaviour
{

    [SerializeField]
    private float speed;
    [SerializeField]
    float blockOffsetY, blockOffsetX;
    Vector3 moveVector;
    new Renderer renderer;
    private int blockArrayWidth = 10;
    private int blockArrayHeight = 5;
    GameObject[,] blockArray;
    public Text playerNameText;
    public Text playerScoreText;
    public Vector3 spawnPosition;
    public bool launchedBall = false;
    CameraBounds cameraBounds;
    public RectTransform playerInfoRect;
    Color color;
    [SyncVar(hook = nameof(OnScoreUpdate))]
    public int playerScore;
    public void OnScoreUpdate(int currentScore, int newScore)
    {
        playerScoreText.text = playerScore.ToString();
    }

    [SyncVar(hook = nameof(OnNameUpdate))]
    public string playerName;
    public void OnNameUpdate(string oldString, string newString)
    {
        playerNameText.text = playerName.ToString();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        cameraBounds = FindObjectOfType<CameraBounds>();
        blockArray = new GameObject[blockArrayWidth, blockArrayHeight];
        renderer = GetComponent<Renderer>();
        transform.name = GetComponent<NetworkIdentity>().netId.ToString();
        Invoke("SetName", 0.5f);

        if (isLocalPlayer)
        {
            GameManager.instance.canLaunch = true;
        }
        if (NetworkManager.singleton.numPlayers > 1)
        {
            CmdRepositionPlayerPanel();
        }

       
        if (GameManager.instance.blockArraySpawned)
        {
            return;
        }
        else
        {
            StartCoroutine(SpawnBlockArray());
            GameManager.instance.blockArraySpawned = true;
        } 
    }

    void Update()
    {
        if (!isLocalPlayer)
            return;
        PlayerMovement();

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (GameManager.instance.loadedBall && GameManager.instance.canLaunch)
            {
                CmdLaunchBall(transform.GetComponent<NetworkIdentity>());
                launchedBall = true;
            }
            if (!GameManager.instance.loadedBall)
            {
                CmdLoadBall(transform.GetComponent<NetworkIdentity>());
                GameManager.instance.loadedBall = true;
            }

            
            
           
        }
    }
    void PlayerMovement()
    {
        moveVector = new Vector3(Input.GetAxisRaw("Mouse X"), 0, 0);//pass the mouse horizontal movement into the moveVector and normalise it.
        transform.position = (new Vector3(Mathf.Clamp(transform.position.x + moveVector.x * speed * Time.deltaTime,
       cameraBounds.LeftCameraBounds + renderer.bounds.size.x / 2, cameraBounds.RightCameraBounds - renderer.bounds.size.x / 2), -4, 0));//apply and clamp player movement to horizontal viewPort + player bounds 
    }

    [Command(requiresAuthority = false)]
    public void CmdLoadBall(NetworkIdentity ballId)
    {
       
        GameObject ball = Instantiate(Resources.Load("Ball"), transform.GetChild(0).transform.position, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(ball);
        Vector2 dir = RandomVector2(2.35619f, 0.785398f);//calculates a random angle between 135 and 45 degrees in radians
        ball.transform.parent = ballId.gameObject.transform;
        ball.transform.position = transform.GetChild(0).position;
       
    }
    [Command(requiresAuthority = false)]
    public void CmdLaunchBall(NetworkIdentity ballId)
    {

        
        Vector2 dir = RandomVector2(2.35619f, 0.785398f);//calculates a random angle between 135 and 45 degrees in radians
        GameObject ball = transform.GetChild(2).gameObject;
        ball.GetComponent<Ball>().player = ballId.GetComponent<Player>();
        ball.transform.parent = null;
        GameManager.instance.canLaunch = false;
        ball.GetComponent<Rigidbody>().velocity = dir * 600f;
        ball.GetComponent<Ball>().movementDirection = dir;
        ball.GetComponent<Ball>().hasLaunched = true;
    }
    public IEnumerator SpawnBlockArray()
    {
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                GameObject go = Instantiate(Resources.Load("Block"), new Vector3(spawnPosition.x + x * blockOffsetX,
                spawnPosition.y + y * blockOffsetY, 0), Quaternion.identity) as GameObject;
                go.transform.name = "Block " + go.transform.position.ToString();
                blockArray[x, y] = go;//adding spawned blocks to block array
                NetworkServer.Spawn(go);
                yield return new WaitForSeconds(0.001f);
            }
        }

        for (int i = 0; i <= blockArray.GetUpperBound(0); i++)//iterate through each row and set the material color with hex codes
        {

            if (ColorUtility.TryParseHtmlString("#14145B", out color))
            {
                blockArray[i, 0].GetComponent<MeshRenderer>().material.color = color;
            }
            if (ColorUtility.TryParseHtmlString("#0C9E9D", out color))
            {
                blockArray[i, 1].GetComponent<MeshRenderer>().material.color = color;
            }
            if (ColorUtility.TryParseHtmlString("#76C2BC", out color))
            {
                blockArray[i, 2].GetComponent<MeshRenderer>().material.color = color;
            }
            if (ColorUtility.TryParseHtmlString("#97CB82", out color))
            {
                blockArray[i, 3].GetComponent<MeshRenderer>().material.color = color;
            }
            if (ColorUtility.TryParseHtmlString("#F4F4E1", out color))
            {
                blockArray[i, 4].GetComponent<MeshRenderer>().material.color = color;
            }
            yield return new WaitForSeconds(0.1f);

        }
    }
    public Vector2 RandomVector2(float angleMax, float angleMin)//returns a random Vector2 using cos / sin / takes in radians
    {
        float random = Random.Range(angleMax, angleMin);
        return new Vector2(Mathf.Cos(random), Mathf.Sin(random));
    }
    [Client]
    public void SpawnNetworkObject(string obj, Vector3 pos) //clients tell server to spawn an object at position
    {
        CmdSpawnNetworkObject(obj, pos);
    }
    [Command(requiresAuthority = false)]
    public void CmdSpawnNetworkObject(string obj, Vector3 pos) //object spawned by server
    {
        GameObject objToSpawn = Instantiate(Resources.Load(obj), pos, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(objToSpawn);
    }
    void SetName()
    {
        playerName = GetComponent<NetworkIdentity>().netId.ToString();
    }


[Command(requiresAuthority = false)]
    public void CmdRepositionPlayerPanel()//server message to clients for repositioning of player score panel
    {
        RpcRepositionPlayerPanel();
    }

    [ClientRpc]
    public void RpcRepositionPlayerPanel()//tell clients to move their score panel
    {
        playerInfoRect.anchoredPosition = new Vector2(158.7f, 445.3f);
    }

}
