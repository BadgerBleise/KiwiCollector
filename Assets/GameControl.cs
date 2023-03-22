using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class GameControl : MonoBehaviour
{
    public GameObject Text, Button;
    public CoinCollector Script;
    public TMP_Text TextContent;
    
    public void OnButtonClick()
    {
        // Store the current scene's build index in a variable
        int sceneId = SceneManager.GetActiveScene().buildIndex;
        Debug.Log(sceneId);
        // Load the next scene using the sceneId variable
        SceneManager.LoadScene(sceneId + 1); 
    }
    
    
    private void OnTriggerEnter2D(Collider2D other) 
    {
        if(other.gameObject.CompareTag("Finish"))
        {
            TextContent.text = "You have gathered " + Script.score.ToString() + " Kiwis";

            Text.SetActive(true);
            Button.SetActive(true);
        }
         
        if(other.gameObject.CompareTag("Spikes"))
        {
            RefreshScene();
        }
    }
    private void RefreshScene()
    {
         // Store the current scene's build index in a variable
        int sceneId = SceneManager.GetActiveScene().buildIndex;
         
        // Load the next scene using the sceneId variable
        SceneManager.LoadScene(sceneId); 
    }
}
