using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLogic : MonoBehaviour
{
    public GameObject hud;
    // Start is called before the first frame update
    void Start()
    {
        hud.GetComponent<MainGameHUD>().StartPlayer();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
