using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using Unity.VisualScripting;

using UnityEditor;
using UnityEngine;

public class reformatController : MonoBehaviour
{
    // Private Physics Variables
    private Rigidbody rb;
    private Vector2 movement;
    private Vector3 velocity;
    private float lean;

    // Trail Containers
    private MeshFilter trailFilter;
    private MeshRenderer trailRenderer;
    private MeshCollider trailCollider;
    private List<Vector3> vertices;
    private List<int> triangles;
    private float yAngle = 0f;
    private float zLean = 0f;
    private float curSpeed = 2.25f;

    // Public refrences
    public GameObject trail;
    public GameObject trailSpawn;
    public GameObject model;

    // Public tune variables
    public float maxSpeed = 5;
    public float defaultSpeed = 4f;
    public float minSpeed = 3f;
    public float linAcc = 25;
    public float linDec = 40;
    
    public float turnRad = 0.05f;
    public float turnRadMax = 1f;

    public float maxLean = 45f;

    // Trail Vars
    public float trailScale = 0.1f;
    public float trailScaleDistance = 0.2f;
    public int trailDiag = 10;

    // Start is called before the first frame update
    void Start()
    {
        this.movement = Vector2.zero;
        this.rb = this.gameObject.GetComponent<Rigidbody>();
        // Set Camera 
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0.5f, -1);
        Camera.main.transform.localEulerAngles = new Vector3(15, 0, 0);

        // Initializing trail handler
        this.InitTrail();
    }

    // Update is called once per frame
    void Update()
    {
        this.movement = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
        this.UpdateTrail();
    }

    // Update physics
    void FixedUpdate()
    {
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

    // Trail Initialization
    void InitTrail() {
        // Reset Trail
        trail.transform.position = Vector3.zero;
        trail.transform.rotation = Quaternion.identity;

        // Set Parent
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("trailContainer")) trail.transform.SetParent(go.transform);

        // Get Material
        Material tmp = trail.GetComponent<Renderer>().material;
        tmp.color = new Color(0, 0, 0.5f, 0.2f);
        trail.GetComponent<Renderer>().material = tmp;
        this.trailFilter = this.trail.GetComponent<MeshFilter>();
        this.trailRenderer = this.trail.GetComponent<MeshRenderer>();
        this.trailCollider = this.trail.GetComponent<MeshCollider>();

        vertices = new List<Vector3>
        {
            trailSpawn.transform.position - model.transform.forward * 0.01f,
            trailSpawn.transform.position + model.transform.up * trailScale - model.transform.forward * 0.01f,
            trailSpawn.transform.position,
            trailSpawn.transform.position + model.transform.up * trailScale
        };
        triangles = new List<int> 
        {
            0,1,3,
            0,3,2,
            0,3,1,
            0,2,3,
        };
        this.trailFilter.mesh.vertices = vertices.ToArray();
        this.trailFilter.mesh.triangles = triangles.ToArray();
    }

    // Trail Update
    void UpdateTrail() {
        int index = vertices.Count;
        float scale = (trailScaleDistance - trailScale)/(trailDiag-1);
        for (int i = 0; i < Math.Min(trailDiag, index/2); i++) {
            int indexGnd = index - 2 - 2*i;
            int indexAir = index - 1 - 2*i;
            vertices[indexAir] += Vector3.Normalize(vertices[indexAir] - vertices[indexGnd]) * scale;
        }


        vertices.Add(trailSpawn.transform.position);
        vertices.Add(trailSpawn.transform.position + model.transform.up * trailScale);

        //Front face
        triangles.Add(index-2);
        triangles.Add(index-1);
        triangles.Add(index+1);
        triangles.Add(index-2);
        triangles.Add(index+1);
        triangles.Add(index);

        //Back face
        triangles.Add(index-2);
        triangles.Add(index+1);
        triangles.Add(index-1);
        triangles.Add(index-2);
        triangles.Add(index);
        triangles.Add(index+1);

        trailFilter.mesh.vertices = vertices.ToArray();
        trailFilter.mesh.triangles = triangles.ToArray();

        trailCollider.sharedMesh = trailFilter.mesh;
    }
}
