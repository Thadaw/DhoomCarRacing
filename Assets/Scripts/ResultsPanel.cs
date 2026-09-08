using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;

public class ResultsPanel : MonoBehaviour
{
    private GameObject resultsPanel;
    private Transform playerListParent;
    private GameObject playerRowPrefab;
    private TextMeshProUGUI positionText;
    private TextMeshProUGUI finishTimeText;
    private TextMeshProUGUI bestLapText;
    private TextMeshProUGUI topSpeedText;
    private TextMeshProUGUI averageSpeedText;
    private Button garageButton;
    private Button mainMenuButton;
    private Button profileButton;
    private float showDelay = 2f;
    private List<GameObject> spawnedRows = new List<GameObject>();

    public void SetupRefs(GameObject panel, Transform list, GameObject rowPrefab,
        TextMeshProUGUI pos, TextMeshProUGUI time, TextMeshProUGUI lap,
        TextMeshProUGUI topSpd, TextMeshProUGUI avgSpd,
        Button garage, Button mainMenu, Button profile)
    {
        resultsPanel = panel;
        playerListParent = list;
        playerRowPrefab = rowPrefab;
        positionText = pos;
        finishTimeText = time;
        bestLapText = lap;
        topSpeedText = topSpd;
        averageSpeedText = avgSpd;
        garageButton = garage;
        mainMenuButton = mainMenu;
        profileButton = profile;

        if (resultsPanel != null)
            resultsPanel.SetActive(false);
    }

    private void OnEnable()
    {
        PlayerLapTracker.OnLocalPlayerFinished += OnRaceFinished;
    }

    private void OnDisable()
    {
        PlayerLapTracker.OnLocalPlayerFinished -= OnRaceFinished;
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

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    public void ShowResults()
    {
        BindButtons();
        OpenPanel();
        PopulateLeaderboard();
        PopulatePerformance();
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
        Debug.Log("BindButtons: garage=" + (garageButton != null) + " mainmenu=" + (mainMenuButton != null) + " profile=" + (profileButton != null));
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
        {
            Debug.LogWarning("PopulateLeaderboard: playerListParent is null!");
            return;
        }

        List<PlayerResult> players = CollectPlayers();
        Debug.Log("PopulateLeaderboard: Found " + players.Count + " players");

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
            SpawnRow(i, pos, players[i].playerName, timeStr, status);
        }
    }

    private void PopulatePerformance()
    {
        PlayerResult localPlayer = GetLocalPlayerResult();

        if (localPlayer == null)
            return;

        int position = GetLocalPlayerPosition();

        if (positionText != null)
        {
            positionText.text = "POSITION: " + position;
            positionText.fontSize = 36;
            positionText.color = new Color(1f, 0.8f, 0f);
        }
        if (finishTimeText != null)
        {
            finishTimeText.text = "TIME: " + (localPlayer.isFinished ? FormatTime(localPlayer.finishTime) : "DNF");
            finishTimeText.fontSize = 32;
            finishTimeText.color = new Color(0f, 0.9f, 1f);
        }
        if (bestLapText != null)
        {
            bestLapText.text = "BEST LAP: " + (localPlayer.bestLap > 0f ? FormatTime(localPlayer.bestLap) : "--");
            bestLapText.fontSize = 32;
            bestLapText.color = new Color(0f, 1f, 0.4f);
        }
        if (topSpeedText != null)
        {
            topSpeedText.text = "TOP SPEED: " + (localPlayer.topSpeed > 0f ? localPlayer.topSpeed.ToString("0") + " KM/H" : "--");
            topSpeedText.fontSize = 32;
            topSpeedText.color = new Color(1f, 0.4f, 0f);
        }
        if (averageSpeedText != null)
        {
            averageSpeedText.text = "AVG SPEED: " + (localPlayer.averageSpeed > 0f ? localPlayer.averageSpeed.ToString("0") + " KM/H" : "--");
            averageSpeedText.fontSize = 32;
            averageSpeedText.color = new Color(1f, 0.6f, 0.2f);
        }
    }

    private List<PlayerResult> CollectPlayers()
    {
        List<PlayerResult> players = new List<PlayerResult>();
        HashSet<string> seen = new HashSet<string>();

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
                    name = PlayerNameHelper.GetPlayerName();
                else
                {
                    Photon.Realtime.Player owner = pv.Owner;
                    name = (owner != null && !string.IsNullOrEmpty(owner.NickName))
                        ? owner.NickName
                        : "Player " + (owner != null ? owner.ActorNumber : "?");
                }
            }
            else
            {
                isLocal = true;
                name = PlayerNameHelper.GetPlayerName();
            }

            if (seen.Contains(name)) continue;
            seen.Add(name);

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
                string pname = string.IsNullOrEmpty(player.NickName)
                    ? "Player " + player.ActorNumber : player.NickName;

                if (seen.Contains(pname)) continue;
                seen.Add(pname);

                float time = 0f;
                if (player.CustomProperties.TryGetValue("FinishTime", out object ft) && ft is float fTime)
                    time = fTime;

                float tSpeed = 0f;
                if (player.CustomProperties.TryGetValue("TopSpeed", out object ts) && ts is float topS)
                    tSpeed = topS;

                float aSpeed = 0f;
                if (player.CustomProperties.TryGetValue("AverageSpeed", out object av) && av is float avgS)
                    aSpeed = avgS;

                float bLap = 0f;
                if (player.CustomProperties.TryGetValue("BestLap", out object bl) && bl is float bestL)
                    bLap = bestL;

                players.Add(new PlayerResult
                {
                    playerName = pname,
                    finishTime = time,
                    bestLap = bLap,
                    topSpeed = tSpeed,
                    averageSpeed = aSpeed,
                    isLocal = player.IsLocal,
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

    private void SpawnRow(int rowIndex, string position, string playerName, string finishTime, string status)
    {
        Debug.Log("SpawnRow: row=" + rowIndex + " pos=" + position + " name=" + playerName + " time=" + finishTime + " status=" + status);

        if (playerRowPrefab != null)
        {
            GameObject row = Instantiate(playerRowPrefab, playerListParent);
            row.SetActive(true);

            int childCount = 0;
            bool foundAny = false;
            foreach (Transform child in row.transform)
            {
                childCount++;
                TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                    tmp = child.gameObject.AddComponent<TextMeshProUGUI>();

                if (child.name == "playername")
                {
                    tmp.text = position + " " + playerName;
                    foundAny = true;
                    Debug.Log("SpawnRow: Set playername to '" + tmp.text + "'");
                }
                else if (child.name == "finishtime")
                {
                    tmp.text = finishTime;
                    foundAny = true;
                    Debug.Log("SpawnRow: Set finishtime to '" + tmp.text + "'");
                }
                else if (child.name == "status")
                {
                    tmp.text = status;
                    foundAny = true;
                    Debug.Log("SpawnRow: Set status to '" + tmp.text + "'");
                }
            }

            Debug.Log("SpawnRow: prefab children=" + childCount + " foundAny=" + foundAny);

            if (!foundAny)
            {
                foreach (Transform child in row.transform)
                {
                    TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
                    if (tmp == null) continue;
                    tmp.text = position + " " + playerName + "  |  " + finishTime + "  |  " + status;
                    Debug.Log("SpawnRow: Fallback set text on '" + child.name + "' to '" + tmp.text + "'");
                    break;
                }
            }

            LayoutElement le = row.GetComponent<LayoutElement>();
            if (le == null) le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;

            spawnedRows.Add(row);
        }
        else
        {
            GameObject go = new GameObject("PlayerRow", typeof(RectTransform));
            go.transform.SetParent(playerListParent, false);

            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            CreateRowText(go.transform, "playername", position + " " + playerName, 200f);
            CreateRowText(go.transform, "finishtime", finishTime, 120f);
            CreateRowText(go.transform, "status", status, 100f);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 36f;

            spawnedRows.Add(go);
        }
    }

    private void CreateRowText(Transform parent, string childName, string text, float width)
    {
        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = 60f;
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
        if (AudioManager.instance != null)
            AudioManager.instance.playMenuMusic();
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainMenu");
    }

    private void GoToProfile()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("stats");
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
