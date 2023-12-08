using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class star_controller : MonoBehaviour
{
    private float time = 0.0f;
    private Color color = Color.black;
    public Material material;
    public float speed = 1.0f;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        float r = (float) Math.Max(Math.Sin(speed * time), 0);
        float g = (float) Math.Max(Math.Sin(speed * (time + 2.0/3*Math.PI)), 0);
        float b = (float) Math.Max(Math.Sin(speed * (time + 4.0/3*Math.PI)), 0);
        this.color = new Color(r, g, b, 0.5f);
        this.material.color = color;
        foreach (Transform child in gameObject.transform) {
            for (int i = 0; i < child.gameObject.GetComponent<Renderer>().materials.Length; i++) {
                child.gameObject.GetComponent<Renderer>().materials[i].SetColor("_EmissionColor", this.color);
                child.gameObject.GetComponent<Renderer>().materials[i].SetColor("_Color", this.color);
                Debug.Log(child.gameObject.GetComponent<Renderer>().materials[i].name);
            }
            child.gameObject.GetComponent<Renderer>().material = this.material;
        }
    }

    void FixedUpdate() {
        time += Time.fixedDeltaTime;
    }
}
