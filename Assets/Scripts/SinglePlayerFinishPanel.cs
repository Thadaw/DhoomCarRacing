using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class SinglePlayerFinishPanel : MonoBehaviour
{
    private GameObject finishPanel;
    private TextMeshProUGUI positionText;
    private TextMeshProUGUI playerNameText;
    private TextMeshProUGUI finishTimeText;
    private TextMeshProUGUI bestLapText;
    private TextMeshProUGUI topSpeedText;
    private TextMeshProUGUI averageSpeedText;
    private Button garageButton;
    private Button mainMenuButton;
    private Button profileButton;

    private float showDelay = 2f;

    public void SetupRefs(GameObject panel, TextMeshProUGUI posText, TextMeshProUGUI nameText, TextMeshProUGUI timeText, TextMeshProUGUI lapText, TextMeshProUGUI speedText, TextMeshProUGUI avgSpeedText, Button garage, Button mainMenu, Button profile)
    {
        finishPanel = panel;
        positionText = posText;
        playerNameText = nameText;
        finishTimeText = timeText;
        bestLapText = lapText;
        topSpeedText = speedText;
        averageSpeedText = avgSpeedText;
        garageButton = garage;
        mainMenuButton = mainMenu;
        profileButton = profile;

        if (finishPanel != null)
            finishPanel.SetActive(false);

        Debug.Log("SinglePlayer SetupRefs: panel=" + (finishPanel != null) + " pos=" + (positionText != null) + " name=" + (playerNameText != null) + " time=" + (finishTimeText != null) + " lap=" + (bestLapText != null) + " speed=" + (topSpeedText != null) + " avg=" + (averageSpeedText != null));
    }

    private void OnEnable()
    {
        PlayerLapTracker.OnLocalPlayerFinished += OnRaceFinished;
    }

    private void OnDisable()
    {
        PlayerLapTracker.OnLocalPlayerFinished -= OnRaceFinished;
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    private void BindButtons()
    {
        if (garageButton != null)
        {
            garageButton.onClick.RemoveAllListeners();
            garageButton.onClick.AddListener(() => { PlayClickSound(); GoToGarage(); });
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() => { PlayClickSound(); GoToMainMenu(); });
        }
        if (profileButton != null)
        {
            profileButton.onClick.RemoveAllListeners();
            profileButton.onClick.AddListener(() => { PlayClickSound(); GoToProfile(); });
        }
    }

    private void OnRaceFinished()
    {
        if (PhotonNetwork.InRoom)
            return;

        StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(showDelay);
        ShowStats();
    }

    public void ShowStats()
    {
        PlayerLapTracker tracker = FindLocalTracker();
        if (tracker == null) return;
        if (finishPanel == null) return;

        finishPanel.SetActive(true);
        BindButtons();

        string playerName = PlayerNameHelper.GetPlayerName();

        float bestLap = 0f;
        if (tracker.lapTimes != null && tracker.lapTimes.Count > 0)
        {
            bestLap = tracker.lapTimes[0];
            for (int i = 1; i < tracker.lapTimes.Count; i++)
            {
                if (tracker.lapTimes[i] < bestLap)
                    bestLap = tracker.lapTimes[i];
            }
        }

        if (positionText != null)
        {
            positionText.text = "POSITION: #1";
            positionText.fontSize = 36;
            positionText.color = new Color(1f, 0.8f, 0f);
        }
        if (playerNameText != null)
        {
            playerNameText.text = "RACER: " + playerName;
            playerNameText.fontSize = 28;
            playerNameText.color = Color.white;
        }
        if (finishTimeText != null)
        {
            finishTimeText.text = "TIME: " + (tracker.finishTime > 0f ? FormatTime(tracker.finishTime) : "DNF");
            finishTimeText.fontSize = 28;
            finishTimeText.color = new Color(0f, 0.9f, 1f);
        }
        if (bestLapText != null)
        {
            bestLapText.text = "BEST LAP: " + (bestLap > 0f ? FormatTime(bestLap) : "--");
            bestLapText.fontSize = 28;
            bestLapText.color = new Color(0f, 1f, 0.4f);
        }
        if (topSpeedText != null)
        {
            topSpeedText.text = "TOP SPEED: " + (tracker.topSpeed > 0f ? tracker.topSpeed.ToString("0") + " KM/H" : "--");
            topSpeedText.fontSize = 28;
            topSpeedText.color = new Color(1f, 0.4f, 0f);
        }
        if (averageSpeedText != null)
        {
            averageSpeedText.text = "AVG SPEED: " + (tracker.averageSpeed > 0f ? tracker.averageSpeed.ToString("0") + " KM/H" : "--");
            averageSpeedText.fontSize = 28;
            averageSpeedText.color = new Color(1f, 0.6f, 0.2f);
        }
    }

    private PlayerLapTracker FindLocalTracker()
    {
        PlayerLapTracker[] trackers = FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None);
        PlayerLapTracker fallback = null;
        foreach (PlayerLapTracker t in trackers)
        {
            PhotonView pv = t.GetComponentInParent<PhotonView>();
            if (pv != null && pv.IsMine)
                return t;
            if (pv == null && fallback == null)
                fallback = t;
        }
        return fallback;
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;
        return minutes + ":" + seconds.ToString("00.00");
    }

    private void GoToGarage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Garage");
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (AudioManager.instance != null)
            AudioManager.instance.playMenuMusic();
        SceneManager.LoadScene("MainMenu");
    }

    private void GoToProfile()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("stats");
    }
}
