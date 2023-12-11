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
    private readonly SyncList<List<Vector3>> vertices = new SyncList<List<Vector3>>(){};
    private readonly SyncList<List<int>> triangles = new SyncList<List<int>>(){};

    // Public refrences for Trail
    public GameObject trailEmL;   // Lower emissive Trail
    public GameObject trail;
    public GameObject trailEmU;   // Upper emissive Trail
    private GameObject[] trails;
    public GameObject trailSpawn;

    // Player Prefab references
    public GameObject model;
    [SyncVar(hook = nameof(OnColorChange))]
    private Color playerColor;
    [SyncVar(hook = nameof(OnNameChange))]
    private string playerName;
    //public TextMeshPro nameTextMesh;
    //public GameObject nameContainer;
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
    void Start() {
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
        InitLocalPlayer();
    }

    [Command]
    void InitLocalPlayer() {
        // Set Player Colour
        this.playerColor = startScreenController.playerColor;
        this.playerName = startScreenController.playerName;
    }

    // Update is called once per frame
    void Update()
    {
        DrawTrail();
        if (!isLocalPlayer) {
            //nameContainer.transform.LookAt(Camera.main.transform);
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
        if (movement[1] == 0 && zLean != 0) {
            if (Math.Abs(zLean) < 2f) {
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
        if (Math.Abs(curSpeed - defaultSpeed) < Time.deltaTime * minAcc * 0.25f) {
            curSpeed = defaultSpeed;
        }
        else if (curSpeed < defaultSpeed) {
            curSpeed += Time.deltaTime * minAcc * 0.25f;
        } else if (curSpeed > defaultSpeed) {
            curSpeed -= Time.deltaTime * minAcc * 0.25f;
        }
        if (movement[0] < 0) {
            curSpeed += movement[0] * linDec * Time.deltaTime; 
        } else {
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
    void InitPlayer() {
        this.isAlive = true;
        SetPlayerColor();
    }

    void SetPlayerColor() {
        this.emissivePolygon.GetComponent<Renderer>().materials[1].SetColor("_EmissionColor", this.playerColor);
    }

    void OnColorChange(Color _Old, Color _New) {
        //nameTextMesh.color = _New;
        SetPlayerColor();
        SetTrailColor();
    }

    void OnNameChange(string _Old, string _New) {
        //this.nameTextMesh.text = _New;
    }

    // Game Logic Functions
    void KillPlayer() {
        // Kill Player
        SceneManager.LoadScene("startScreen");
    }


    // Trail Logic Initialization
    void InitTrail() {
        // Define Trails
        trails = new GameObject [3]{
            trailEmL,
            trail,
            trailEmU
        };

        // Reset Trails
        foreach (GameObject trailElem in trails) {
            trailElem.transform.position = Vector3.zero;
            trailElem.transform.rotation = Quaternion.identity;
        }

        // Set Parent
        foreach (GameObject trailContainer in GameObject.FindGameObjectsWithTag("trailContainer")) {
            foreach (GameObject trailElem in trails) trailElem.transform.SetParent(trailContainer.transform);
        }

        SetTrailColor();

        // Access trail Meshes
        for (int i = 0; i < trails.Length; i++) {
            this.trailFilter[i] = this.trails[i].GetComponent<MeshFilter>();
            this.trailRenderer[i] = this.trails[i].GetComponent<MeshRenderer>();
            this.trailCollider[i] = this.trails[i].GetComponent<MeshCollider>();
        }

        // initialize Meshes for trail
        InitMesh();
    }

    void SetTrailColor() {
        // Get Material
        Material mat = trail.GetComponent<Renderer>().material;

        // Set Trail Color
        mat.color = new Color(playerColor.r, playerColor.g, playerColor.b, trailTransparency);
        trail.GetComponent<Renderer>().material = mat;
        trailEmL.GetComponent<Renderer>().material.SetColor("_EmissionColor", playerColor);
        trailEmU.GetComponent<Renderer>().material.SetColor("_EmissionColor", playerColor);
    }

    [Command]
    void InitMesh() {
        // Initialize Lists in Sync List
        for (int i = 0; i < trails.Length; i++) {
            vertices.Add(new List<Vector3>(){});
            triangles.Add(new List<int>(){});
        }

        // Initialize individual meshes
        // Create Vertices lower emissive
        vertices[0].Add(trailSpawn.transform.position - model.transform.forward * 0.01f);
        vertices[0].Add(trailSpawn.transform.position + model.transform.up * trailGlowScale - model.transform.forward * 0.01f);
        vertices[0].Add(trailSpawn.transform.position);
        vertices[0].Add(trailSpawn.transform.position + model.transform.up * trailGlowScale);

        // Create Vertices central transparent 
        vertices[1].Add(trailSpawn.transform.position + model.transform.up * trailGlowScale - model.transform.forward * 0.01f);
        vertices[1].Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale) - model.transform.forward * 0.01f);
        vertices[1].Add(trailSpawn.transform.position + model.transform.up * trailGlowScale);
        vertices[1].Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale));
        // Create Vertices upper emissive       
        vertices[2].Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale) - model.transform.forward * 0.01f);
        vertices[2].Add(trailSpawn.transform.position + model.transform.up * trailScale - model.transform.forward * 0.01f);
        vertices[2].Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale));
        vertices[2].Add(trailSpawn.transform.position + model.transform.up * trailScale);

        // Create initial triangles
        for (int i = 0; i < trails.Length; i++) {
            triangles[i].Add(0);
            triangles[i].Add(1);
            triangles[i].Add(3);
            triangles[i].Add(0);
            triangles[i].Add(3);
            triangles[i].Add(2);
            triangles[i].Add(0);
            triangles[i].Add(3);
            triangles[i].Add(1);
            triangles[i].Add(0);
            triangles[i].Add(2);
            triangles[i].Add(3);
        }
    }

    // Update Trail Lists
    [Command]
    void UpdateTrail() {
        // Current index
        int index = vertices[0].Count;        
        
        // Scale Back Vertices
        float scale = (trailScaleDistance - trailScale)/(trailDiag-1);
        for (int i = 0; i < Math.Min(trailDiag, index/2); i++) {
            int indexGnd = index - 2 - 2*i;
            int indexAir = index - 1 - 2*i;
            // Center Transparent Trail
            vertices[1][indexAir] += Vector3.Normalize(vertices[1][indexAir] - vertices[1][indexGnd]) * scale;
            // Upper Emissive Trail
            vertices[2][indexGnd] += Vector3.Normalize(vertices[1][indexAir] - vertices[1][indexGnd]) * scale;
            vertices[2][indexAir] += Vector3.Normalize(vertices[1][indexAir] - vertices[1][indexGnd]) * scale;
        }

        // Add new Vertices
        // Lower Emissive Trail
        vertices[0].Add(trailSpawn.transform.position);
        vertices[0].Add(trailSpawn.transform.position + model.transform.up * trailGlowScale);
        // Center Transparent Trail
        vertices[1].Add(trailSpawn.transform.position + model.transform.up * trailGlowScale);
        vertices[1].Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale));
        // Upper Emissive Trail
        vertices[2].Add(trailSpawn.transform.position + model.transform.up * (trailScale - trailGlowScale));
        vertices[2].Add(trailSpawn.transform.position + model.transform.up * trailScale);

        // Add new Triangles
        for (int i = 0; i < trails.Length; i++) {
            //Front face
            triangles[i].Add(index-2);
            triangles[i].Add(index-1);
            triangles[i].Add(index+1);
            triangles[i].Add(index-2);
            triangles[i].Add(index+1);
            triangles[i].Add(index);

            //Back face
            triangles[i].Add(index-2);
            triangles[i].Add(index+1);
            triangles[i].Add(index-1);
            triangles[i].Add(index-2);
            triangles[i].Add(index);
            triangles[i].Add(index+1);
        }


        // Shorten trail if necessary
        if (vertices[1].Count >= trailLength) {
            for (int i = 0; i < trails.Length; i++) {
                // remove Triangles
                for (int j = 0; j < 12; j++) {
                    triangles[i].RemoveAt(0);
                }
            }
        }
    }

    // Draw Trail Function
    void DrawTrail() {
        for (int i = 0; i < trails.Length; i++) {
            trailFilter[i].mesh.vertices = vertices[i].ToArray();
            trailFilter[i].mesh.triangles = triangles[i].ToArray();

            trailCollider[i].sharedMesh = trailFilter[i].mesh;
        }
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
