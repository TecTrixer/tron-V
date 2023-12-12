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
using UnityEngine.UI;

public class playerControllerWheels : NetworkBehaviour
{
    // Private Game Logic Variables
    [SyncVar(hook = nameof(KillPlayer))]
    private bool isAlive = true;
    [SyncVar(hook = nameof(ResetPlayers))]
    public bool gameActive = false;
    [SyncVar]
    public bool trailActive = false;
    private bool isSpectator = false;
    
    // Private Physics Variables
    private Rigidbody rb;
    private Vector2 movement;
    private Vector3 movementSpectator;
    private Vector3 velocity;
    public GameObject wheels;
    public WheelCollider wFrontL;
    public WheelCollider wFrontR;
    public WheelCollider wBackL;
    public WheelCollider wBackR;

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

    // Public Tune Variables Spectator
    public float spectatorSpeed = 0.3f;
    public float spectatorAng = 20f;
    
    // Public refrences for Trail
    public GameObject trailEmL;   // Lower emissive Trail
    public GameObject trail;
    public GameObject trailEmU;   // Upper emissive Trail
    private GameObject[] trails;
    public GameObject trailSpawn;
    private GameObject deathScreen, specScreen;
    private Button btnSpectate, btnQuit;
    private GameObject gameStartScreen;
    private Button btnGameStart, btnQuitSpec;
    private GameObject networkManager;

    // Player Prefab references
    public GameObject model;
    public GameObject headLight;
    [SyncVar(hook = nameof(OnColorChange))]
    private Color playerColor = new Color();
    [SyncVar(hook = nameof(OnNameChange))]
    private string playerName = "Anonymous";
    public TextMeshPro nameTextMesh;
    public GameObject nameContainer;
    public GameObject emissivePolygon; // Used to set player color;

    // Game Physics
    public float maxSpeed = 5;
    public float defaultSpeed = 4f;
    public float minSpeed = 3f;

    public float maxLean = 45f;
    public float leanInSpeed = 4f;
    public float leanOutSpeed = 3f;
    public float leanToTurn = 1f;
    public float turnSlowDown = 0.2f;
    private float curLean = 0f;
    private int frameCounter = 0;
    private float[] speeds;

    // Trail Tune Variables
    public float trailScale = 0.1f;
    public float trailScaleDistance = 0.2f;
    public int trailDiag = 10;
    public float trailTransparency = 0.8f;
    public float trailGlowScale = 0.01f;
    public float trailLength = 1800;
    private Vector3 initialPos;
    private Quaternion initialRot;


    // Start is called before the first frame update
    void Start()
    {
        // Setup Trail
        InitPlayer();
    }

    public override void OnStartLocalPlayer()
    {
        this.movement = Vector2.zero;
        this.rb = this.gameObject.GetComponent<Rigidbody>();
        initialPos = this.rb.transform.position;
        initialRot = this.rb.transform.rotation;

        // Setup screen canvases
        foreach (GameObject screenContainer in GameObject.FindGameObjectsWithTag("screenContainer"))
        {
            // Get Death Screen
            foreach (Transform child in screenContainer.transform) {
                if (child.gameObject.tag == "deathScreen") this.deathScreen = child.gameObject;
            }
            foreach (Transform child in this.deathScreen.transform) {
                if (child.gameObject.tag == "btnSpectate") {
                    this.btnSpectate = (Button)child.gameObject.GetComponent<Button>();
                }
                if (child.gameObject.tag == "btnQuit") {
                    this.btnQuit = child.gameObject.GetComponent<Button>();
                }
            }
            // Get Start Button for Host
            if (isServer) {
                foreach (Transform child in screenContainer.transform) {
                    if (child.gameObject.tag == "gameStartScreen") this.gameStartScreen = child.gameObject;
                }
                foreach (Transform child in this.gameStartScreen.transform) {
                    if (child.gameObject.tag == "btnGameStart") {
                        this.btnGameStart = (Button)child.gameObject.GetComponent<Button>();
                    }
                }
                this.gameStartScreen.SetActive(true);
                this.btnGameStart.onClick.AddListener(StartGame);
            }

            // Get Spectator screen
            foreach (Transform child in screenContainer.transform) {
                if (child.gameObject.tag == "screenSpectator") this.specScreen = child.gameObject;
            }
            foreach (Transform child in this.specScreen.transform) {
                if (child.gameObject.tag == "btnQuit") {
                    this.btnQuitSpec = child.gameObject.GetComponent<Button>();
                }
            }
            this.btnQuitSpec.onClick.AddListener(OnQuit);
        }

        [Command]
        void StartGame() {
            foreach (GameObject player in GameObject.FindGameObjectsWithTag("player")) {
                player.GetComponent<playerControllerWheels>().gameActive = true;
            }
            this.gameStartScreen.SetActive(false);
        }

        // Get netowrk Manager
        foreach (GameObject networkManager in GameObject.FindGameObjectsWithTag("networkManager")) {
            this.networkManager = networkManager;
        }

        // Set Camera 
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0.5f, -1);
        Camera.main.transform.localEulerAngles = new Vector3(15, 0, 0);
        InitLocalPlayer(startScreenController.playerColor, startScreenController.playerName);
        this.nameContainer.SetActive(false);

        // Set speed controller
        speeds = new float[10];
        for (int i = 0; i < speeds.Length; i++) {
            speeds[i] = 1;
        }
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
        if (isAlive && trailActive) DrawTrail();
        if (!isLocalPlayer)
        {
            nameContainer.transform.LookAt(Camera.main.transform);
            Quaternion rot = Quaternion.AngleAxis(180, Camera.main.transform.up);
            nameContainer.transform.rotation = rot * nameContainer.transform.rotation;
            return;
        }
        this.movement = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
        this.movementSpectator = new Vector3(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"), -1* Input.GetAxis("Fire3") + Input.GetAxis("Jump"));
        if (isAlive && trailActive) UpdateTrail();
    }

    // Update physics
    void FixedUpdate()
    {
        if (!isLocalPlayer) return;
        if (this.isAlive) DrivePhysics();
        if (this.isAlive && this.gameActive) HandleRampCollisions();
        if (this.isSpectator) SpectatorPhysics();
        if (this.isAlive && this.gameActive && frameCounter == 30) InitMesh();
    }

    void HandleRampCollisions() {
        // Check for terrain collisions
        float sumSpeed = 0;
        for (int i = 1; i < speeds.Length; i++) {
            speeds[i-1] = speeds[i];
            sumSpeed += speeds[i];
        }
        speeds[speeds.Length-1] = rb.velocity.magnitude;
        sumSpeed += rb.velocity.magnitude;
        if (sumSpeed < 10 && isLocalPlayer && frameCounter > 20) {
            // terrain collision
            Camera.main.transform.SetParent(null);
            Camera.main.transform.position = new Vector3(0, 7, 0);
            Camera.main.transform.eulerAngles = new Vector3(0, 0, 0);
            setDead();
            SpectateScreen();
        }
    }

    void DrivePhysics() {
        // Handling lean and turn
        if (movement[1] == 0) {
            if (Math.Abs(curLean) < leanOutSpeed) {
                curLean = 0.0f;
            } else {
                curLean -= Math.Sign(curLean) * leanOutSpeed;
            }
        } else {
            curLean += movement[1] * leanInSpeed;
        }
        curLean = Math.Clamp(curLean, -maxLean, maxLean);

        wFrontL.steerAngle = leanToTurn * curLean;
        wFrontR.steerAngle = leanToTurn * curLean;

        // Handling forward speed
        float curMaxSpeed = maxSpeed * (1.0f - turnSlowDown * Math.Abs(curLean) / maxLean);
        float speed = defaultSpeed;
        if (movement[0] < 0.0f) {
            speed = Mathf.Lerp(minSpeed, defaultSpeed, movement[0] + 1.0f);
        } else if (movement[0] > 0.0f) {
            speed = Mathf.Lerp(defaultSpeed, curMaxSpeed, movement[0]);
        }
        wFrontL.motorTorque = speed;
        wFrontR.motorTorque = speed;
        wBackL.motorTorque = speed;
        wBackR.motorTorque = speed;
        float speedPerc = (speed - minSpeed) / (maxSpeed - minSpeed);

        model.transform.localRotation = Quaternion.Euler(0, 0, -curLean);
        Camera.main.fieldOfView = Mathf.Lerp(60, 75, speedPerc);
        Camera.main.transform.localRotation = Quaternion.Euler(15 - 10 * speedPerc, 0, -0.25f * curLean);
        Camera.main.transform.localPosition = new Vector3(0, 0.5f, -0.8f - 0.2f * speedPerc);

        if (gameActive) frameCounter++;
    }


    void SpectatorPhysics() {    
        Camera.main.transform.Rotate(0, movementSpectator[1]*spectatorAng, 0);
        Camera.main.transform.Translate(0, movementSpectator[2]*spectatorSpeed, movementSpectator[0]*spectatorSpeed);
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

    void OnQuit() {
        networkManager.GetComponent<MainGameHUD>().Stop();
        SceneManager.LoadScene("startScreen");
    }

    void OnSpectate() {
        this.deathScreen.SetActive(false);
        this.specScreen.SetActive(true);
        this.isSpectator = true;
    }

    // Game Logic Functions
    void KillPlayer(bool _Old, bool _New)
    {
        Debug.Log("killing: " + netId);
        // Kill Player
        //this.rb.isKinematic = false;
        //this.rb.detectCollisions = false;
        this.model.SetActive(false);
        this.headLight.SetActive(false);
        this.nameContainer.SetActive(false);
        this.wheels.SetActive(false);
        for (int i = 0; i < trails.Length; i++) {
            this.trailFilter[i].gameObject.SetActive(false);
            this.trailRenderer[i].gameObject.SetActive(false);
            this.trailCollider[i].gameObject.SetActive(false);
        }
    }

    void SpectateScreen() {
        this.deathScreen.SetActive(true);
        this.btnSpectate.onClick.AddListener(OnSpectate);
        this.btnQuit.onClick.AddListener(OnQuit);
    }


    // Trail Logic Initialization
    void ResetPlayers(bool _Old, bool _New)
    {
        if (!_New) {
            return;
        }

        // Reset player position
        if (isLocalPlayer) {
            this.rb.transform.position = initialPos;
            this.rb.transform.rotation = initialRot;
            this.rb.velocity = new Vector3(0, 0, 0);
            this.curLean = 0f;
        }
        InitTrail();
    }

    void InitTrail()  {
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
            //InitMesh();
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
        trailActive = true;
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

    // Trail Collisions
    void OnCollisionEnter(Collision collision)
    {
        if (!isLocalPlayer || !gameActive || frameCounter < 30) {
            return;
        }
        if (collision.gameObject.CompareTag("trail") || collision.gameObject.CompareTag("walls")) {
            Camera.main.transform.SetParent(null);
            Camera.main.transform.position = new Vector3(0, 7, 0);
            Camera.main.transform.eulerAngles = new Vector3(0, 0, 0);
            setDead();
            SpectateScreen();
        }
    }

    // Remote Call kill player
    [Command]
    void setDead() {
        this.isAlive = false;
    }
}
