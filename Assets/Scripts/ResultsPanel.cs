using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class ResultsPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject resultsPanel;
    [SerializeField] private Button closeButton;

    [Header("Leaderboard (Left Side)")]
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerRowPrefab;

    [Header("Performance (Right Side)")]
    [SerializeField] private TextMeshProUGUI positionText;
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

    private List<GameObject> spawnedRows = new List<GameObject>();

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
        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        TryFindUI();

        if (closeButton != null)
            closeButton.onClick.AddListener(() => { PlayClickSound(); ClosePanel(); });
        if (replayButton != null)
            replayButton.onClick.AddListener(() => { PlayClickSound(); Replay(); });
        if (nextRaceButton != null)
            nextRaceButton.onClick.AddListener(() => { PlayClickSound(); NextRace(); });
        if (garageButton != null)
            garageButton.onClick.AddListener(() => { PlayClickSound(); GoToGarage(); });
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => { PlayClickSound(); GoToMainMenu(); });
    }

    private void OnRaceFinished()
    {
        if (!PhotonNetwork.InRoom)
            return;

        StartCoroutine(ShowAfterDelay());
    }

    private IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSecondsRealtime(showDelay);
        ShowResults();
    }

    private void TryFindUI()
    {
        if (resultsPanel == null)
        {
            var go = GameObject.Find("ResultsPanel");
            if (go != null) resultsPanel = go;
        }
        if (closeButton == null)
        {
            var go = GameObject.Find("CloseButton");
            if (go != null) closeButton = go.GetComponent<Button>();
        }
        if (playerListParent == null)
        {
            var go = GameObject.Find("PlayerList");
            if (go != null) playerListParent = go.transform;
        }
        if (playerRowPrefab == null)
        {
            var go = GameObject.Find("playerrow1");
            if (go != null) playerRowPrefab = go;
        }
        if (positionText == null)
        {
            var go = GameObject.Find("position");
            if (go != null) positionText = go.GetComponent<TextMeshProUGUI>();
        }
        if (finishTimeText == null)
        {
            var go = GameObject.Find("finishtime");
            if (go != null) finishTimeText = go.GetComponent<TextMeshProUGUI>();
        }
        if (bestLapText == null)
        {
            var go = GameObject.Find("bestlap");
            if (go != null) bestLapText = go.GetComponent<TextMeshProUGUI>();
        }
        if (topSpeedText == null)
        {
            var go = GameObject.Find("topspeed");
            if (go != null) topSpeedText = go.GetComponent<TextMeshProUGUI>();
        }
        if (averageSpeedText == null)
        {
            var go = GameObject.Find("avarage speed");
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

    public void ShowResults()
    {
        OpenPanel();
        PopulateLeaderboard();
        PopulatePerformance();
    }

    public void OpenPanel()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(true);
    }

    public void ClosePanel()
    {
        if (resultsPanel != null)
            resultsPanel.SetActive(false);
    }

    private void PopulateLeaderboard()
    {
        ClearRows();

        if (playerListParent == null)
            return;

        List<PlayerResult> players = CollectPlayers();

        players.Sort((a, b) =>
        {
            if (a.isFinished && b.isFinished)
                return a.finishTime.CompareTo(b.finishTime);
            if (a.isFinished) return -1;
            if (b.isFinished) return 1;
            return 0;
        });

        for (int i = 0; i < players.Count; i++)
        {
            string pos = (i + 1) + ".";
            string timeStr = players[i].isFinished
                ? FormatTime(players[i].finishTime)
                : "DNF";
            string status = players[i].isFinished ? "Finished" : "Racing...";
            string entryText = pos + " " + players[i].playerName + "  -  " + timeStr + "  -  " + status;
            SpawnRow(entryText);
        }
    }

    private void PopulatePerformance()
    {
        PlayerResult localPlayer = GetLocalPlayerResult();

        if (localPlayer == null)
            return;

        int position = GetLocalPlayerPosition();

        if (positionText != null)
            positionText.text = position.ToString();
        if (finishTimeText != null)
            finishTimeText.text = localPlayer.isFinished ? FormatTime(localPlayer.finishTime) : "DNF";
        if (bestLapText != null)
            bestLapText.text = localPlayer.bestLap > 0f ? FormatTime(localPlayer.bestLap) : "--";
        if (topSpeedText != null)
            topSpeedText.text = localPlayer.topSpeed > 0f ? localPlayer.topSpeed.ToString("0") + " KM/H" : "--";
        if (averageSpeedText != null)
            averageSpeedText.text = localPlayer.averageSpeed > 0f ? localPlayer.averageSpeed.ToString("0") + " KM/H" : "--";
    }

    private List<PlayerResult> CollectPlayers()
    {
        List<PlayerResult> players = new List<PlayerResult>();

        PlayerLapTracker[] trackers = FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None);

        foreach (PlayerLapTracker tracker in trackers)
        {
            PhotonView pv = tracker.GetComponentInParent<PhotonView>();
            string name;
            bool isLocal = false;

            if (pv != null)
            {
                isLocal = pv.IsMine;
                if (isLocal)
                    name = "You";
                else
                {
                    Photon.Realtime.Player owner = pv.Owner;
                    name = (owner != null && !string.IsNullOrEmpty(owner.NickName))
                        ? owner.NickName
                        : "Player " + (owner != null ? owner.ActorNumber : "?");
                }
            }
            else
                name = "Player";

            float bestLap = 0f;
            if (tracker.lapTimes != null && tracker.lapTimes.Count > 0)
            {
                bestLap = tracker.lapTimes[0];
                for (int j = 1; j < tracker.lapTimes.Count; j++)
                {
                    if (tracker.lapTimes[j] < bestLap)
                        bestLap = tracker.lapTimes[j];
                }
            }

            players.Add(new PlayerResult
            {
                playerName = name,
                finishTime = tracker.finishTime,
                bestLap = bestLap,
                topSpeed = tracker.topSpeed,
                averageSpeed = tracker.averageSpeed,
                isLocal = isLocal,
                isFinished = tracker.finishTime > 0f
            });
        }

        if (PhotonNetwork.InRoom)
        {
            foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal) continue;

                bool found = false;
                foreach (PlayerResult p in players)
                {
                    string pname = string.IsNullOrEmpty(player.NickName)
                        ? "Player " + player.ActorNumber : player.NickName;
                    if (!p.isLocal && p.playerName == pname)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) continue;

                float time = 0f;
                if (player.CustomProperties.TryGetValue("FinishTime", out object ft) && ft is float fTime)
                    time = fTime;

                float tSpeed = 0f;
                if (player.CustomProperties.TryGetValue("TopSpeed", out object ts) && ts is float topS)
                    tSpeed = topS;

                float aSpeed = 0f;
                if (player.CustomProperties.TryGetValue("AverageSpeed", out object av) && av is float avgS)
                    aSpeed = avgS;

                string name = string.IsNullOrEmpty(player.NickName)
                    ? "Player " + player.ActorNumber : player.NickName;

                players.Add(new PlayerResult
                {
                    playerName = name,
                    finishTime = time,
                    bestLap = 0f,
                    topSpeed = tSpeed,
                    averageSpeed = aSpeed,
                    isLocal = false,
                    isFinished = time > 0f
                });
            }
        }

        return players;
    }

    private PlayerResult GetLocalPlayerResult()
    {
        List<PlayerResult> players = CollectPlayers();
        foreach (PlayerResult p in players)
        {
            if (p.isLocal)
                return p;
        }
        return null;
    }

    private int GetLocalPlayerPosition()
    {
        List<PlayerResult> players = CollectPlayers();

        players.Sort((a, b) =>
        {
            if (a.isFinished && b.isFinished)
                return a.finishTime.CompareTo(b.finishTime);
            if (a.isFinished) return -1;
            if (b.isFinished) return 1;
            return 0;
        });

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].isLocal)
                return i + 1;
        }
        return 0;
    }

    private void SpawnRow(string text)
    {
        if (playerRowPrefab != null)
        {
            GameObject row = Instantiate(playerRowPrefab, playerListParent);
            row.SetActive(true);

            RectTransform rowRt = row.GetComponent<RectTransform>();
            if (rowRt != null)
            {
                rowRt.anchorMin = new Vector2(0f, 1f);
                rowRt.anchorMax = new Vector2(1f, 1f);
                rowRt.pivot = new Vector2(0.5f, 1f);
                rowRt.sizeDelta = new Vector2(0f, 40f);
                rowRt.anchoredPosition = Vector2.zero;
                rowRt.offsetMin = new Vector2(0f, rowRt.offsetMin.y);
                rowRt.offsetMax = new Vector2(0f, rowRt.offsetMax.y);
            }

            TextMeshProUGUI tmp = row.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.text = text;
                tmp.overflowMode = TextOverflowModes.Ellipsis;
                tmp.enableWordWrapping = false;
                tmp.rectTransform.anchorMin = Vector2.zero;
                tmp.rectTransform.anchorMax = Vector2.one;
                tmp.rectTransform.sizeDelta = Vector2.zero;
                tmp.rectTransform.anchoredPosition = Vector2.zero;
                tmp.rectTransform.offsetMin = new Vector2(10f, 0f);
                tmp.rectTransform.offsetMax = new Vector2(-10f, 0f);
            }

            spawnedRows.Add(row);
        }
        else
        {
            GameObject go = new GameObject("PlayerRow", typeof(RectTransform));
            go.transform.SetParent(playerListParent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 22;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            tmp.enableWordWrapping = false;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, 36f);
            rt.offsetMin = new Vector2(10f, 0f);
            rt.offsetMax = new Vector2(-10f, 0f);

            spawnedRows.Add(go);
        }
    }

    private void ClearRows()
    {
        foreach (GameObject row in spawnedRows)
        {
            if (row != null)
                Destroy(row);
        }
        spawnedRows.Clear();
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
        string currentScene = GetCurrentSceneName();

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(currentScene);
        else if (!PhotonNetwork.InRoom)
            SceneManager.LoadScene(currentScene);
        else
            GoToMainMenu();
    }

    private void NextRace()
    {
        Time.timeScale = 1f;
        string nextScene = GetNextTrackScene();

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.LoadLevel(nextScene);
        }
        else if (!PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene(nextScene);
        }
        else
        {
            GoToMainMenu();
        }
    }

    private void GoToGarage()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("Garage");
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainMenu");
    }

    private class PlayerResult
    {
        public string playerName;
        public float finishTime;
        public float bestLap;
        public float topSpeed;
        public float averageSpeed;
        public bool isLocal;
        public bool isFinished;
    }
}
