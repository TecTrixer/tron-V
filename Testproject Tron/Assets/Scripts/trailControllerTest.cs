using System;
using System.Collections;
using System.Collections.Generic;
//using System.Numerics;
using Unity.VisualScripting;

using UnityEditor;
using UnityEngine;

public class trailController : MonoBehaviour
{
    // Private Physics Variables
    private Vector2 movement;
    private MeshFilter trailFilter;
    private MeshRenderer trailRenderer;
    private MeshCollider trailCollider;
    private List<Vector3> vertices;
    private List<int> triangles;

    // Public refrences
    public GameObject trail;
    public GameObject Trails;
    public GameObject Model;

    // Start is called before the first frame update
    void Start()
    {
        trail.transform.SetParent(Trails.transform);
        this.movement = Vector2.zero;
        trail.transform.position = Vector3.zero;
        trail.transform.rotation = Quaternion.identity;
        Material tmp = trail.GetComponent<Renderer>().material;
        tmp.color = new Color(0, 0, 0.5f, 0.5f);
        trail.GetComponent<Renderer>().material = tmp;
        this.trailFilter = this.trail.GetComponent<MeshFilter>();
        this.trailRenderer = this.trail.GetComponent<MeshRenderer>();
        this.trailCollider = this.trail.GetComponent<MeshCollider>();

        vertices = new List<Vector3>
        {
            Model.transform.position - Vector3.up * 0.5f - Vector3.forward * 0.1f - Model.transform.forward * 1.0f,
            Model.transform.position + Vector3.up * 0.5f - Vector3.forward * 0.1f - Model.transform.forward * 1.0f,
            Model.transform.position - Vector3.up * 0.5f - Model.transform.forward * 1.0f,
            Model.transform.position + Vector3.up * 0.5f - Model.transform.forward * 1.0f
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

        // Set Camera 
        Camera.main.transform.SetParent(this.transform);
        Camera.main.transform.localPosition = new Vector3(0, 1f, -2);
        Camera.main.transform.localEulerAngles = new Vector3(15, 0, 0);
        
    }

    // Update is called once per frame
    void Update()
    {
        this.movement = new Vector2(Input.GetAxis("Vertical"),Input.GetAxis("Horizontal"));
        this.transform.Rotate(0, movement[1], 0);
        this.transform.Translate(0, 0, movement[0]);
        CreateTrail();
    }

    // Update physics
    void FixedUpdate()
    {
    }

    void CreateTrail() {
        int index = vertices.Count;
        vertices.Add(Model.transform.position - Vector3.up * 0.5f - Model.transform.forward * 1.0f);
        vertices.Add(Model.transform.position + Vector3.up * 0.5f - Model.transform.forward * 1.0f);

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
    
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("trail")) {
            this.transform.position = Vector3.zero;
            this.vertices = new List<Vector3>
            {
                Model.transform.position - Vector3.up * 0.5f - Vector3.forward * 0.1f - Model.transform.forward * 1.0f,
                Model.transform.position + Vector3.up * 0.5f - Vector3.forward * 0.1f - Model.transform.forward * 1.0f,
                Model.transform.position - Vector3.up * 0.5f - Model.transform.forward * 1.0f,
                Model.transform.position + Vector3.up * 0.5f - Model.transform.forward * 1.0f
            };
            this.triangles = new List<int> 
            {
                0,1,3,
                0,3,2,
                0,3,1,
                0,2,3,
            };
            CreateTrail();
        }
    }
}
