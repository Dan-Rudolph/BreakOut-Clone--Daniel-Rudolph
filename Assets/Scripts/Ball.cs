using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
public class Ball : NetworkBehaviour
{
    [SerializeField]
    float BallSpeed;
    public Vector3 movementDirection;
    Rigidbody rb;
    SphereCollider sphereCollider;
    bool hasCollided = false;
    public bool hasLaunched = false;
    new Renderer renderer;
    public Player player;
    CameraBounds cameraBounds;
    public NetworkIdentity ballNetId;
    
    void Start()
    {
        cameraBounds = FindObjectOfType<CameraBounds>();
        renderer = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        sphereCollider = GetComponent<SphereCollider>();
        ballNetId = GetComponent<NetworkIdentity>();
    }

    // Update is called once per frame
    void Update()
    {
        DeflectIfAtCameraEdge();
    }
    private void FixedUpdate()
    {
        if (hasLaunched)
        {
            rb.velocity = movementDirection * BallSpeed * Time.deltaTime;
        }
    }
    private void DeflectIfAtCameraEdge()
    {
        if (hasCollided)//stops collision from retriggering
            return;
        if (rb.position.y >= cameraBounds.topCameraBounds - renderer.bounds.size.y / 2)
        {
            StartCoroutine(DeflectBall(Vector3.up));
        }
        else if ((rb.position.y <= cameraBounds.bottomCameraBounds + renderer.bounds.size.y / 2))
        {
            StartCoroutine(DeflectBall(-Vector3.up));
        }
        if (rb.position.x <= cameraBounds.LeftCameraBounds + renderer.bounds.size.x / 2)
        {
            StartCoroutine(DeflectBall(Vector3.left));
        }
        else if (rb.position.x >= cameraBounds.RightCameraBounds - renderer.bounds.size.x / 2)
        {
            StartCoroutine(DeflectBall(-Vector3.left));
        }
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (hasCollided)
            return;
        if (collision.gameObject.CompareTag("Block"))
        {
            StartCoroutine(DeflectBall(collision.GetContact(0).normal));
            Mirror.NetworkServer.Destroy(collision.gameObject);
            player.playerScore += 100;
            player.SpawnNetworkObject("impact", transform.position);
        }
        if (collision.gameObject.CompareTag("Player"))
        {
            StartCoroutine(DeflectBall(collision.GetContact(0).normal));
        }
        if (collision.gameObject.CompareTag("DeathTrigger"))
        {
            StartCoroutine(BallDeathTimer());
        }

    }
    private void OnCollisionExit(Collision collision)
    {
        if (!hasCollided)
            return;
        hasCollided = false;
    }
    private IEnumerator DeflectBall(Vector3 deflectAngleDirection) //deflects the ball off the hit normal
    {
        movementDirection = Vector3.Reflect(movementDirection, deflectAngleDirection);
        hasCollided = true;
        yield return new WaitForSeconds(0.1f);
        hasCollided = false;
    }
    private IEnumerator BallDeathTimer()// timer for destroying the ball
    {
        hasLaunched = false;
        rb.velocity = Vector3.zero;
        sphereCollider.enabled = false;
        GameManager.instance.canLaunch = true;
        yield return new WaitForSeconds(1);
        NetworkServer.Destroy(gameObject);
    }


}
