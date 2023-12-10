using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
//using System.Numerics;
using Unity.VisualScripting;

using UnityEditor;
using UnityEngine;

public class playerController : NetworkBehaviour
{
    // Private Physics Variables
    private Rigidbody rb;
    private Vector2 movement;
    private Vector3 velocity;
    private float lean;
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

    public float trailScale = 0.1f;
    // Start is called before the first frame update
    public override void OnStartLocalPlayer()
    {
        this.movement = Vector2.zero;
        this.rb = this.gameObject.GetComponent<Rigidbody>();
        this.yAngle = transform.rotation.y;
        // Set Camera 
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0.5f, -1);
        Camera.main.transform.localEulerAngles = new Vector3(15, 0, 0);

        // Initializing trail handler
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("trailContainer")) trail.transform.SetParent(go.transform);
        Material tmp = trail.GetComponent<Renderer>().material;
        tmp.color = new Color(0, 0, 0.5f, 0.5f);
        trail.GetComponent<Renderer>().material = tmp;
        this.trailFilter = this.trail.GetComponent<MeshFilter>();
        this.trailRenderer = this.trail.GetComponent<MeshRenderer>();
        this.trailCollider = this.trail.GetComponent<MeshCollider>();

        vertices = new List<Vector3>
        {
            rb.transform.position - rb.transform.up * trailScale - rb.transform.forward * 0.1f - rb.transform.forward * 0.33f,
            rb.transform.position + rb.transform.up * trailScale - rb.transform.forward * 0.1f - rb.transform.forward * 0.33f,
            rb.transform.position - rb.transform.up * trailScale - rb.transform.forward * 0.33f,
            rb.transform.position + rb.transform.up * trailScale - rb.transform.forward * 0.33f
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

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer) return;
        this.movement = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));
        CreateTrail();
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

    void CreateTrail() {
        int index = vertices.Count;
        vertices.Add(rb.transform.position - rb.transform.up * trailScale - rb.transform.forward * 0.33f);
        vertices.Add(rb.transform.position + rb.transform.up * trailScale - rb.transform.forward * 0.33f);

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
