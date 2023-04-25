using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System;

public class CoinCollector : MonoBehaviour
{
   public TMP_Text scoretext;
   public int score = 0;
   public AudioSource pickupSound;

   private void OnTriggerEnter2D(Collider2D kiwi) 
   {
     if(kiwi.gameObject.CompareTag("Kiwi"))
     {
        // Store the amount of gathered Kiwis
        score++;
        scoretext.text = "Kiwi Score: " + score.ToString();
        // Add the pickup sound
        pickupSound.Play();
        Destroy(kiwi.gameObject);
     }
   }

}
