using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class GameControl : MonoBehaviour {
    public GameObject Text, Button;
    public CoinCollector Script;
    public TMP_Text TextContent;

    [Header("Timer Settings")]
    public TMP_Text timerText;
    private static float elapsedTime = 0f;
    private static bool isTimerRunning = true;
    private static int lastSceneIndex = -1;

    void Start() {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (lastSceneIndex != currentSceneIndex) {
            elapsedTime = 0f;
            isTimerRunning = true;
            lastSceneIndex = currentSceneIndex;
        }
    }

    void Update() {
        if (isTimerRunning) {
            elapsedTime += Time.deltaTime;
            UpdateTimerUI();
        }
    }

    void UpdateTimerUI() {
        if (timerText != null) {
            int minutes = Mathf.FloorToInt(elapsedTime / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }

    public void OnButtonClick() {
        int sceneId = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(sceneId + 1);
    }


    private void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Finish")) {
            isTimerRunning = false;

            TextContent.text =
                "You have gathered " + Script.score.ToString() + " Kiwis" + "\n\n" +
                "Final Time: " + timerText.text;

            Text.SetActive(true);
            Button.SetActive(true);
        }

        if (other.gameObject.CompareTag("Spikes")) {
            RefreshScene();
        }
    }

    private void RefreshScene() {
        int sceneId = SceneManager.GetActiveScene().buildIndex;
        elapsedTime = 0f;
        isTimerRunning = true;
        SceneManager.LoadScene(sceneId);
    }

    public void EndGame() {
        Application.Quit();
    }
}
