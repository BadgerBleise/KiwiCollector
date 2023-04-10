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
        // Load the next scene using the sceneId variable
        SceneManager.LoadScene(sceneId + 1); 
    }
    
    
    private void OnTriggerEnter2D(Collider2D other) 
    {
        if(other.gameObject.CompareTag("Finish"))
        {
            // Store the amount of Kiwis gathered and putting it at the end of each level
            TextContent.text = "You have gathered " + Script.score.ToString() + " Kiwis";

            Text.SetActive(true);
            Button.SetActive(true);
        }
         
        if(other.gameObject.CompareTag("Spikes"))
        {
            // Restart level if a player touches spikes
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
    
    public void EndGame()
    {
        // Quit the Game
        Application.Quit();
    }
}
