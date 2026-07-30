using System.Collections;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleInfo : MonoBehaviour
{
    public GameObject overlay;
    // public AudioListener mainListener;

    private void Awake()
    {
        // mainListener.enabled = false;
        overlay.SetActive(true);
    }

    public void StartGame()
    {
        overlay.SetActive(false);
        // mainListener.enabled = true;
        SceneManager.LoadScene("BattleScene");
    }
}
