using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class startScreenController : MonoBehaviour
{
    // Public References
    public TMP_InputField playerField, hostField;
    public Button JoinBn, HostBn;
    public FlexibleColorPicker fcp;
    // Private References
    public static String playerName;
    public static String hostAddress;
    public static Color playerColor;
    public static int mode;

    void Start() {
        JoinBn.onClick.AddListener(Join);
        HostBn.onClick.AddListener(Host);
        if (!(this.hostField.text == null)) this.hostField.text = startScreenController.hostAddress;
        if (!(this.playerField.text == null)) this.playerField.text = startScreenController.playerName;
    }
    void Join() {
        GetValues();
        startScreenController.mode = 0;
        SceneManager.LoadScene("MainGame");
    }

    void Host() {
        GetValues();
        startScreenController.mode = 1;
        SceneManager.LoadScene("MainGame");
    }

    void GetValues() {
        startScreenController.playerName = this.playerField.text;
        startScreenController.hostAddress = this.hostField.text;
        startScreenController.playerColor = this.fcp.color;
        startScreenController.playerColor.a = 1;
    }
}
