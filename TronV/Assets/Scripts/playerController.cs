using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using TMPro;
//using System.Numerics;
using Unity.VisualScripting;

using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class playerController : NetworkBehaviour
{
    // Private Game Logic Variables
    private bool isAlive = true;
    private bool trailActive = true;
    
    // Private Physics Variables
    private Rigidbody rb;
    private Vector2 movement;
    private Vector3 velocity;
    private float lean;
    private float yAngle = 0f;
    private float zLean = 0f;
    private float curSpeed = 2.25f;

    // Private Trail Variables
    private MeshFilter[] trailFilter = new MeshFilter[3];
    private MeshRenderer[] trailRenderer = new MeshRenderer[3];
    private MeshCollider[] trailCollider = new MeshCollider[3];
    private readonly SyncList<Vector3> vertices0 = new SyncList<Vector3>(){};
    private readonly SyncList<Vector3> vertices1 = new SyncList<Vector3>(){};
    private readonly SyncList<Vector3> vertices2 = new SyncList<Vector3>(){};
    private readonly SyncList<int> triangles0 = new SyncList<int>(){};
    private readonly SyncList<int> triangles1 = new SyncList<int>(){};
    private readonly SyncList<int> triangles2 = new SyncList<int>(){};

    // Public refrences for Trail
    public GameObject trailEmL;   // Lower emissive Trail
    public GameObject trail;
    public GameObject trailEmU;   // Upper emissive Trail
    private GameObject[] trails;
    public GameObject trailSpawn;

    // Player Prefab references
    public GameObject model;
    [SyncVar(hook = nameof(OnColorChange))]
    private Color playerColor = new Color();
    [SyncVar(hook = nameof(OnNameChange))]
    private string playerName = "Anonymous";
    public TextMeshPro nameTextMesh;
    public GameObject nameContainer;
    public GameObject emissivePolygon; // Used to set player color;

    // Public tune variables
    public float maxSpeed = 5;
    public float defaultSpeed = 4f;
    public float minSpeed = 3f;
    public float linAcc = 25;
    public float linDec = 40;

    public float turnRad = 0.05f;
    public float turnRadMax = 1f;

    public float maxLean = 45f;

    // Trail Tune Variables
    public float trailScale = 0.1f;
    public float trailScaleDistance = 0.2f;
    public int trailDiag = 10;
    public float trailTransparency = 0.8f;
    public float trailGlowScale = 0.01f;
    public float trailLength = 1800;


    // Start is called before the first frame update
    void Start()
    {
        // Setup Trail
        InitPlayer();
        InitTrail();
    }

    public override void OnStartLocalPlayer()
    {
        this.movement = Vector2.zero;
        this.rb = this.gameObject.GetComponent<Rigidbody>();
        this.yAngle = transform.rotation.y;
        // Set Camera 
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0.5f, -1);
        Camera.main.transform.localEulerAngles = new Vector3(15, 0, 0);
        InitLocalPlayer(startScreenController.playerColor, startScreenController.playerName);
    }

    [Command]
    void InitLocalPlayer(Color c, string playerName)
    {
        // Set Player Colour
        this.playerColor = c;
        this.playerName = playerName;
    }

    // Update is called once per frame
    void Update()
    {
        DrawTrail();
        if (!isLocalPlayer)
        {
            nameContainer.transform.LookAt(Camera.main.transform);
            return;
        }
        this.movement = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
        UpdateTrail();
    }

    // Update physics
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        // Handle lean
        zLean = Math.Clamp(zLean + 3f * movement[1], -maxLean, maxLean);
        if (movement[1] == 0 && zLean != 0)
        {
            if (Math.Abs(zLean) < 2f)
            {
                zLean = 0;
            }
            zLean -= Math.Sign(zLean) * 2f;
        }

        // Handle y-axis rotation
        float yAngleChange = zLean * (Math.Max(maxSpeed - curSpeed, 0) / (maxSpeed - minSpeed) + 0.5f);
        yAngle += Math.Clamp(turnRad * yAngleChange, -turnRadMax, turnRadMax);

        rb.transform.rotation = Quaternion.Euler(rb.transform.rotation.x, yAngle, -zLean);

        // Handle slow down / speed up
        float minAcc = Math.Min(linAcc, linDec);
        if (Math.Abs(curSpeed - defaultSpeed) < Time.deltaTime * minAcc * 0.25f)
        {
            curSpeed = defaultSpeed;
        }
        else if (curSpeed < defaultSpeed)
        {
            curSpeed += Time.deltaTime * minAcc * 0.25f;
        }
        else if (curSpeed > defaultSpeed)
        {
            curSpeed -= Time.deltaTime * minAcc * 0.25f;
        }
        if (movement[0] < 0)
        {
            curSpeed += movement[0] * linDec * Time.deltaTime;
        }
        else
        {
            curSpeed += movement[0] * linAcc * Time.deltaTime;
        }
        float curMaxSpeed = maxSpeed - 0.5f * Math.Abs(zLean) / maxLean;
        curSpeed = Math.Clamp(curSpeed, minSpeed, curMaxSpeed);

        // clamp velocity
        rb.velocity = new Vector3(transform.forward.x, 0, transform.forward.z) * curSpeed + new Vector3(0, rb.velocity.y, 0);

        // adjust cam based on speed and lean angle
        float speedPerc = (curSpeed - minSpeed) / (maxSpeed - minSpeed);
        Camera.main.fieldOfView = Mathf.Lerp(60, 75, speedPerc);
        Camera.main.transform.localRotation = Quaternion.Euler(15 - 10 * speedPerc, 0, 0.75f * zLean);
        Camera.main.transform.localPosition = new Vector3(0, 0.5f, -0.8f - 0.2f * speedPerc);
    }

    // Initialize Player
    void InitPlayer()
    {
        this.isAlive = true;
        SetPlayerColor();
    }

    void SetPlayerColor()
    {
        this.emissivePolygon.GetComponent<Renderer>().materials[1].SetColor("_EmissionColor", this.playerColor);
    }

    void OnColorChange(Color _Old, Color _New)
    {
        nameTextMesh.color = _New;
        SetPlayerColor();
        SetTrailColor();
    }

    void OnNameChange(string _Old, string _New)
    {
        this.nameTextMesh.text = _New;
    }

    // Game Logic Functions
    void KillPlayer()
    {
        // Kill Player
        SceneManager.LoadScene("startScreen");
    }


    // Trail Logic Initialization
    void InitTrail()
    {
        // Define Trails
        trails = new GameObject[3]{
            trailEmL,
            trail,
            trailEmU
        };

        // Reset Trails
        foreach (GameObject trailElem in trails)
        {
            trailElem.transform.position = Vector3.zero;
            trailElem.transform.rotation = Quaternion.identity;
        }

        // Set Parent
        foreach (GameObject trailContainer in GameObject.FindGameObjectsWithTag("trailContainer"))
        {
            foreach (GameObject trailElem in trails) trailElem.transform.SetParent(trailContainer.transform);
        }

        SetTrailColor();

        // Access trail Meshes
        for (int i = 0; i < trails.Length; i++)
        {
            this.trailFilter[i] = this.trails[i].GetComponent<MeshFilter>();
            this.trailRenderer[i] = this.trails[i].GetComponent<MeshRenderer>();
            this.trailCollider[i] = this.trails[i].GetComponent<MeshCollider>();
        }

        // initialize Meshes for trail
        if (isLocalPlayer)
        {
            InitMesh();
        }
    }

    void SetTrailColor()
    {
        // Get Material
        Material mat = new Material(trail.GetComponent<Renderer>().material);

        // Set Trail Color
        mat.color = new Color(playerColor.r, playerColor.g, playerColor.b, trailTransparency);
        trail.GetComponent<Renderer>().material = mat;
        trailEmL.GetComponent<Renderer>().material.SetColor("_EmissionColor", playerColor);
        trailEmU.GetComponent<Renderer>().material.SetColor("_EmissionColor", playerColor);
    }

    [Command]
    void InitMesh()
    {
        // Initialize individual meshes
        // Create Vertices lower emissive
        vertices0.Add(trailSpawn.transform.position - model.transform.forward * 0.01f);
        vertices0.Add(trailSpawn.transform.position + model.transform.up * trailGlowScale - model.transform.forward * 0.01f);
        vertices0.Add(trailSpawn.transform.position);
        vertices0.Add(trailSpawn.transform.position + model.transform.up * trailGlowScale);

        // Create Vertices central transparent 
        vertices1.Add(trailSpawn.transform.position + model.transform.up * trailGlowScale - model.transform.forward * 0.01f);
        vertices1.Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale) - model.transform.forward * 0.01f);
        vertices1.Add(trailSpawn.transform.position + model.transform.up * trailGlowScale);
        vertices1.Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale));
        // Create Vertices upper emissive       
        vertices2.Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale) - model.transform.forward * 0.01f);
        vertices2.Add(trailSpawn.transform.position + model.transform.up * trailScale - model.transform.forward * 0.01f);
        vertices2.Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale));
        vertices2.Add(trailSpawn.transform.position + model.transform.up * trailScale);

        // Create initial triangles
        int[] indices = {
            0,1,3,
            0,3,2,
            0,3,1,
            0,2,3,
        };

        foreach (int i in indices) {
            triangles0.Add(i);
            triangles1.Add(i);
            triangles2.Add(i);
        }
    }

    // Update Trail Lists
    [Command]
    void UpdateTrail()
    {
        // Current index
        int index = vertices0.Count;

        // Scale Back Vertices
        float scale = (trailScaleDistance - trailScale) / (trailDiag - 1);
        for (int i = 0; i < Math.Min(trailDiag, index / 2); i++)
        {
            int indexGnd = index - 2 - 2 * i;
            int indexAir = index - 1 - 2 * i;
            // Center Transparent Trail
            vertices1[indexAir] += Vector3.Normalize(vertices1[indexAir] - vertices1[indexGnd]) * scale;
            // Upper Emissive Trail
            vertices2[indexGnd] += Vector3.Normalize(vertices1[indexAir] - vertices1[indexGnd]) * scale;
            vertices2[indexAir] += Vector3.Normalize(vertices1[indexAir] - vertices1[indexGnd]) * scale;
        }

        // Add new Vertices
        // Lower Emissive Trail
        vertices0.Add(trailSpawn.transform.position);
        vertices0.Add(trailSpawn.transform.position + model.transform.up * trailGlowScale);
        // Center Transparent Trail
        vertices1.Add(trailSpawn.transform.position + model.transform.up * trailGlowScale);
        vertices1.Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale));
        // Upper Emissive Trail
        vertices2.Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale));
        vertices2.Add(trailSpawn.transform.position + model.transform.up * trailScale);

        int[] indices = {
            // Front face
            index-2, index-1, index+1,
            index-2, index+1, index,
            // Back face
            index-2, index+1, index-1,
            index-2, index, index+1
        };

        // Add new Triangles
        foreach (int i in indices)
        {
            triangles0.Add(i);
            triangles1.Add(i);
            triangles2.Add(i);
        }


        // Shorten trail if necessary
        if (vertices1.Count >= trailLength)
        {
            // remove Triangles
            for (int j = 0; j < 12; j++)
            {
                triangles0.RemoveAt(0);
                triangles1.RemoveAt(0);
                triangles2.RemoveAt(0);
            }
        }
        DrawTrail();
    }

    // Draw Trail Function
    void DrawTrail()
    {
        trailFilter[0].mesh.vertices = vertices0.ToArray();
        trailFilter[0].mesh.triangles = triangles0.ToArray();
        trailFilter[1].mesh.vertices = vertices1.ToArray();
        trailFilter[1].mesh.triangles = triangles1.ToArray();
        trailFilter[2].mesh.vertices = vertices2.ToArray();
        trailFilter[2].mesh.triangles = triangles2.ToArray();

        trailCollider[0].sharedMesh = trailFilter[0].mesh;
        trailCollider[1].sharedMesh = trailFilter[1].mesh;
        trailCollider[2].sharedMesh = trailFilter[2].mesh;
    }

    [ClientRpc]
    void RpcDrawTrail() {
        DrawTrail();
    }

    // Trail Collisions
    void OnCollisionEnter(Collision collision)
    {
        /*if (collision.gameObject.CompareTag("trail")) {
            this.isAlive = false;
            this.KillPlayer();
        }*/
    }
}
