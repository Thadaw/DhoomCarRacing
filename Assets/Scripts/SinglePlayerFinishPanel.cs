using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class SinglePlayerFinishPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject finishPanel;

    [Header("Stats")]
    [SerializeField] private TextMeshProUGUI positionText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI finishTimeText;
    [SerializeField] private TextMeshProUGUI bestLapText;
    [SerializeField] private TextMeshProUGUI topSpeedText;
    [SerializeField] private TextMeshProUGUI averageSpeedText;

    [Header("Buttons")]
    [SerializeField] private Button replayButton;
    [SerializeField] private Button nextRaceButton;
    [SerializeField] private Button garageButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Timing")]
    [SerializeField] private float showDelay = 2f;

    private void OnEnable()
    {
        PlayerLapTracker.OnLocalPlayerFinished += OnRaceFinished;
    }

    private void OnDisable()
    {
        PlayerLapTracker.OnLocalPlayerFinished -= OnRaceFinished;
    }

    private void Start()
    {
        if (finishPanel != null)
            finishPanel.SetActive(false);

        TryFindUI();

        if (replayButton != null)
            replayButton.onClick.AddListener(() => { PlayClickSound(); Replay(); });
        if (nextRaceButton != null)
            nextRaceButton.onClick.AddListener(() => { PlayClickSound(); NextRace(); });
        if (garageButton != null)
            garageButton.onClick.AddListener(() => { PlayClickSound(); GoToGarage(); });
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => { PlayClickSound(); GoToMainMenu(); });
    }

    private void TryFindUI()
    {
        if (finishPanel == null)
        {
            var go = GameObject.Find("Single Player Finish Panel");
            if (go != null) finishPanel = go;
        }
        if (positionText == null)
        {
            var go = GameObject.Find("SPPosition");
            if (go != null) positionText = go.GetComponent<TextMeshProUGUI>();
        }
        if (playerNameText == null)
        {
            var go = GameObject.Find("SPPlayerName");
            if (go != null) playerNameText = go.GetComponent<TextMeshProUGUI>();
        }
        if (finishTimeText == null)
        {
            var go = GameObject.Find("SPFinishTime");
            if (go != null) finishTimeText = go.GetComponent<TextMeshProUGUI>();
        }
        if (bestLapText == null)
        {
            var go = GameObject.Find("SPBestLap");
            if (go != null) bestLapText = go.GetComponent<TextMeshProUGUI>();
        }
        if (topSpeedText == null)
        {
            var go = GameObject.Find("SPTopSpeed");
            if (go != null) topSpeedText = go.GetComponent<TextMeshProUGUI>();
        }
        if (averageSpeedText == null)
        {
            var go = GameObject.Find("SPAverageSpeed");
            if (go != null) averageSpeedText = go.GetComponent<TextMeshProUGUI>();
        }
        if (replayButton == null)
        {
            var go = GameObject.Find("ReplayButton");
            if (go != null) replayButton = go.GetComponent<Button>();
        }
        if (nextRaceButton == null)
        {
            var go = GameObject.Find("NextRaceButton");
            if (go != null) nextRaceButton = go.GetComponent<Button>();
        }
        if (garageButton == null)
        {
            var go = GameObject.Find("GarageButton");
            if (go != null) garageButton = go.GetComponent<Button>();
        }
        if (mainMenuButton == null)
        {
            var go = GameObject.Find("MainMenuButton");
            if (go != null) mainMenuButton = go.GetComponent<Button>();
        }
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
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

        if (finishPanel != null)
            finishPanel.SetActive(true);

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
            positionText.text = "1";
        if (playerNameText != null)
            playerNameText.text = playerName;
        if (finishTimeText != null)
            finishTimeText.text = tracker.finishTime > 0f ? FormatTime(tracker.finishTime) : "DNF";
        if (bestLapText != null)
            bestLapText.text = bestLap > 0f ? FormatTime(bestLap) : "--";
        if (topSpeedText != null)
            topSpeedText.text = tracker.topSpeed > 0f ? tracker.topSpeed.ToString("0") + " KM/H" : "--";
        if (averageSpeedText != null)
            averageSpeedText.text = tracker.averageSpeed > 0f ? tracker.averageSpeed.ToString("0") + " KM/H" : "--";
    }

    private PlayerLapTracker FindLocalTracker()
    {
        PlayerLapTracker[] trackers = FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None);
        foreach (PlayerLapTracker t in trackers)
        {
            PhotonView pv = t.GetComponentInParent<PhotonView>();
            if (pv == null)
                return t;
        }
        return null;
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;
        return minutes + ":" + seconds.ToString("00.00");
    }

    private string GetCurrentSceneName()
    {
        return SceneManager.GetActiveScene().name;
    }

    private string GetNextTrackScene()
    {
        string current = GetCurrentSceneName();
        switch (current)
        {
            case "Track1": return "Track2";
            case "Track2": return "Track3";
            case "Track3": return "Track1";
            default: return "Track1";
        }
    }

    private void Replay()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GetCurrentSceneName());
    }

    private void NextRace()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(GetNextTrackScene());
    }

    private void GoToGarage()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("Garage");
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
