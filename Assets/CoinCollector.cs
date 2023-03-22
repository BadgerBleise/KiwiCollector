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
        score++;
        scoretext.text = "Kiwi Score: " + score.ToString();
        pickupSound.Play();
        Destroy(kiwi.gameObject);
     }
   }

}
