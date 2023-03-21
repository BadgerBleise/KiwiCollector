using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class GameControl : MonoBehaviour
{
    public GameObject Text,Button;
    public CoinCollector Script;
    public TMP_Text TextContent;
    
    public void OnButtonClick()
    {
        SceneManager.LoadScene("Level2");
    }
    
    
    private void OnTriggerEnter2D(Collider2D other) 
    {
        if(other.gameObject.CompareTag("Finish"))
        {
            
            
            TextContent.text = "You have gathered " + Script.score.ToString() + " Kiwis";
            
            Text.SetActive(true);
            Button.SetActive(true);


        }
        
    }
    

    
}
