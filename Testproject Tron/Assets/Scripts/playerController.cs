using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using Unity.VisualScripting;

using UnityEditor;
using UnityEngine;

public class playerController : MonoBehaviour
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

    // Public refrences
    public GameObject trail;
    public GameObject Trails;
    public GameObject model;

    // Public tune variables
    public float maxSpeed = 1;
    public float maxLean = 15;
    public float linAccel = 1;
    public float angAccel = 1;
    public float turnRad = 0.1f;

    // Start is called before the first frame update
    void Start()
    {
        trail.transform.SetParent(Trails.transform);
        this.movement = Vector2.zero;
        this.rb = this.gameObject.GetComponent<Rigidbody>();
        this.trailFilter = this.trail.GetComponent<MeshFilter>();
        this.trailRenderer = this.trail.GetComponent<MeshRenderer>();
        this.trailCollider = this.trail.GetComponent<MeshCollider>();

        // Set Camera 
        Camera.main.transform.SetParent(transform);
        Camera.main.transform.localPosition = new Vector3(0, 0.5f, -1);
        Camera.main.transform.localEulerAngles = new Vector3(15, 0, 0);
        
    }

    // Update is called once per frame
    void Update()
    {
        this.movement = new Vector2(Input.GetAxis("Vertical"), Input.GetAxis("Horizontal"));

    }

    // Update physics
    void FixedUpdate()
    {
        if (rb.velocity.magnitude > maxSpeed) {
            rb.velocity = rb.velocity.normalized * maxSpeed;
        } else {
        //Quaternion quat = Quaternion.AngleAxis(this.movement[1] * angAccel, Vector3.up * fToggle);
        rb.AddRelativeForce(Vector3.forward * this.linAccel * this.movement[0]);
        }
        //rb.AddForceAtPosition(this.movement[1] * this.angAccel * rb.transform.right,rb.transform.position + rb.transform.forward * 0.2f);
        rb.AddTorque(Vector3.up * this.angAccel * this.movement[1]);
        
        /*
        // Get inputs
        float leanPrev = this.lean;
        if (Input.GetAxis("Horizontal") != 0) {
            this.lean = Math.Clamp(this.lean + Input.GetAxis("Horizontal") * angAccel * Time.fixedDeltaTime, -maxLean, maxLean);
        } else {
            if (this.lean != 0.0) {
                int sign = Math.Sign(this.lean);
                this.lean = this.lean - Math.Sign(this.lean) * 2 * angAccel * Time.fixedDeltaTime;
                if (Math.Sign(this.lean) != sign) {
                    this.lean = 0;
                }
            }
        }

        this.accel = Input.GetAxis("Vertical") * linAccel * Time.fixedDeltaTime;

        // Update physics
        this.pos += this.velocity * Time.fixedDeltaTime;
        this.velocity = gameObject.transform.forward.normalized * (this.velocity.magnitude + this.accel);
        this.velocity = Vector3.Normalize(this.velocity + gameObject.transform.right.normalized*this.turnRad*this.lean)*this.velocity.magnitude;
        
        // Set Positions
        gameObject.transform.position = this.pos;
        if (this.velocity != Vector3.zero) {
            gameObject.transform.forward = this.velocity.normalized;
        }
        gameObject.transform.Rotate(new Vector3(0,0,this.lean - leanPrev));
        //Camera.main.transform.localEulerAngles = new Vector3(0, 0, -lean);
        */
    }
}
