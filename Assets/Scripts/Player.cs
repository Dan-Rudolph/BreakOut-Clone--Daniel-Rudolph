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
    NetworkIdentity ballNetId;
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

    [SyncVar(hook = nameof(OnColourChange))]
    public bool colourChanging;
    public void OnColourChange(bool active, bool notActive)
    {
        ColourBlockArray();
    }
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        cameraBounds = FindObjectOfType<CameraBounds>();
        blockArray = new GameObject[blockArrayWidth, blockArrayHeight];
        renderer = GetComponent<Renderer>();
        transform.name = GetComponent<NetworkIdentity>().netId.ToString();
        Invoke("SetName", 0.5f);
        

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
           CmdSpawnBlockArray(GetComponent<NetworkIdentity>());
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
            if (GameManager.instance.loadedBall)
            {
                CmdLaunchBall(transform.GetComponent<NetworkIdentity>());
                launchedBall = true;
            }
            else 
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

   
    [Command]
    public void CmdLoadBall(NetworkIdentity ballId)
    {

        GameObject ball = Instantiate(Resources.Load("Ball"), transform.GetChild(0).transform.position, Quaternion.identity) as GameObject;
        NetworkServer.Spawn(ball);
        ballId.gameObject.GetComponent<Player>().ballNetId = ball.GetComponent<NetworkIdentity>();
        ball.GetComponent<Ball>().ballNetId = ball.GetComponent<NetworkIdentity>();
        ball.GetComponent<Ball>().ownerId = ballId;
       

        RpcLoadBall(ballId, ball.GetComponent<NetworkIdentity>());

    }
    [ClientRpc]
    public void RpcLoadBall(NetworkIdentity ballId,NetworkIdentity ball)
    {

        ballId.gameObject.GetComponent<Player>().ballNetId = ball.GetComponent<NetworkIdentity>();
        ball.GetComponent<Ball>().ballNetId = ball.GetComponent<NetworkIdentity>();
        ball.GetComponent<Ball>().ownerId = ballId;
        ball.transform.parent = ballId.gameObject.transform;
        ball.transform.position = ballId.transform.GetChild(0).position;

    }
    [Command]
    public void CmdLaunchBall(NetworkIdentity ballId)
    {

        
        
        GameObject ball = ballNetId.gameObject;
        ball.GetComponent<Ball>().player = ballId.GetComponent<Player>();
        ball.transform.parent = null;
        RpcLaunchBall(ballId);
         ball.GetComponent<Ball>().hasLaunched = true;
    }
    [ClientRpc]
    public void RpcLaunchBall(NetworkIdentity ballId)
    {

        Vector2 dir = RandomVector2(2.35619f, 0.785398f);//calculates a random angle between 135 and 45 degrees in radians
       
        GameObject ball = ballNetId.gameObject;
        ball.GetComponent<Ball>().movementDirection = dir;
        ball.GetComponent<Ball>().player = ballId.GetComponent<Player>();
        ball.transform.parent = null;
        GameManager.instance.canLaunch = false;   
        ball.GetComponent<Ball>().hasLaunched = true;
    }

    [Command(requiresAuthority =false)]
    public void CmdReturnBall(NetworkIdentity netId, NetworkIdentity ownerId)
    {
        netId.GetComponent<Ball>().hasLaunched = false;
        netId.transform.position = ownerId.transform.GetChild(0).position;
        netId.transform.parent = ownerId.transform;
        netId.GetComponent<Rigidbody>().Sleep();
        RpcReturnBall(netId, ownerId);
    }
    [ClientRpc]
    public void RpcReturnBall(NetworkIdentity netId,NetworkIdentity ownerId)
    {
        netId.transform.position = ownerId.transform.GetChild(0).position;
        netId.transform.parent = ownerId.transform;
        netId.GetComponent<Rigidbody>().Sleep();
        netId.GetComponent<Ball>().hasLaunched = false;
    }
    [Command]
    public void CmdSpawnBlockArray(NetworkIdentity netid)
    {
        GameObject go;
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 5; y++)
            {
                 go = Instantiate(Resources.Load("Block"), new Vector3(spawnPosition.x + x * blockOffsetX,
                spawnPosition.y + y * blockOffsetY, 0), Quaternion.identity) as GameObject;
                go.transform.name = "Block " + go.transform.position.ToString();
                GameManager.instance.blockArray[x, y] = go;
                
               
                
                NetworkServer.Spawn(go);
               
            }
        }

        ColourBlockArray();
        colourChanging = true;
    }
    
    public void ColourBlockArray()
    {
       
        for (int i = 0; i <= GameManager.instance.blockArray.GetUpperBound(0); i++)//iterate through each row and set the material color with hex codes
        {

            if (ColorUtility.TryParseHtmlString("#14145B", out color))
            {
                GameManager.instance.blockArray[i, 0].GetComponent<MeshRenderer>().material.color = color;
            }
            if (ColorUtility.TryParseHtmlString("#0C9E9D", out color))
            {
                GameManager.instance.blockArray[i, 1].GetComponent<MeshRenderer>().material.color = color;
            }
            if (ColorUtility.TryParseHtmlString("#76C2BC", out color))
            {
                GameManager.instance.blockArray[i, 2].GetComponent<MeshRenderer>().material.color = color;
            }
            if (ColorUtility.TryParseHtmlString("#97CB82", out color))
            {
                GameManager.instance.blockArray[i, 3].GetComponent<MeshRenderer>().material.color = color;
            }
            if (ColorUtility.TryParseHtmlString("#F4F4E1", out color))
            {
                GameManager.instance.blockArray[i, 4].GetComponent<MeshRenderer>().material.color = color;
            }


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
