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
    [SerializeField] private Button garageButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button profileButton;

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
        TryFindUI();

        if (resultsPanel != null)
            resultsPanel.SetActive(false);

        EnsurePlayerListMask();
    }

    public void RefreshUI()
    {
        TryFindUI();
        EnsurePlayerListMask();
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
            var go = GameObject.Find("resultpanal");
            if (go == null) go = GameObject.Find("ResultsPanel");
            if (go != null) resultsPanel = go;
        }
        if (playerListParent == null && resultsPanel != null)
        {
            Transform found = FindChildRecursive(resultsPanel.transform, "playerlist");
            if (found == null) found = FindChildRecursive(resultsPanel.transform, "PlayerList");
            if (found != null) playerListParent = found;
        }
        if (playerListParent == null)
        {
            var go = GameObject.Find("playerlist");
            if (go == null) go = GameObject.Find("PlayerList");
            if (go != null) playerListParent = go.transform;
        }
        if (playerRowPrefab == null && resultsPanel != null)
        {
            Transform found = FindChildRecursive(resultsPanel.transform, "playerrow 1");
            if (found == null) found = FindChildRecursive(resultsPanel.transform, "playerrow 1(Clone)");
            if (found != null)
            {
                playerRowPrefab = found.gameObject;
                playerRowPrefab.SetActive(false);
            }
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
            var go = GameObject.Find("top speed");
            if (go == null) go = GameObject.Find("topspeed");
            if (go != null) topSpeedText = go.GetComponent<TextMeshProUGUI>();
        }
        if (averageSpeedText == null)
        {
            var go = GameObject.Find("avarage speed");
            if (go != null) averageSpeedText = go.GetComponent<TextMeshProUGUI>();
        }
        if (garageButton == null)
        {
            Transform t = FindChildRecursive(resultsPanel != null ? resultsPanel.transform : transform, "garage");
            if (t == null) t = FindChildRecursive(resultsPanel != null ? resultsPanel.transform : transform, "GarageButton");
            if (t == null)
            {
                GameObject g = GameObject.Find("garage");
                if (g != null) t = g.transform;
            }
            if (t != null)
            {
                garageButton = t.GetComponent<Button>();
                if (garageButton == null)
                    garageButton = t.GetComponentInParent<Button>();
            }
        }
        if (mainMenuButton == null)
        {
            Transform t = FindChildRecursive(resultsPanel != null ? resultsPanel.transform : transform, "mainmenu");
            if (t == null) t = FindChildRecursive(resultsPanel != null ? resultsPanel.transform : transform, "MainMenuButton");
            if (t == null)
            {
                GameObject g = GameObject.Find("mainmenu");
                if (g != null) t = g.transform;
            }
            if (t != null)
            {
                mainMenuButton = t.GetComponent<Button>();
                if (mainMenuButton == null)
                    mainMenuButton = t.GetComponentInParent<Button>();
            }
        }
        if (profileButton == null)
        {
            Transform t = FindChildRecursive(resultsPanel != null ? resultsPanel.transform : transform, "profile");
            if (t == null) t = FindChildRecursive(resultsPanel != null ? resultsPanel.transform : transform, "ProfileButton");
            if (t == null)
            {
                GameObject g = GameObject.Find("profile");
                if (g != null) t = g.transform;
            }
            if (t != null)
            {
                profileButton = t.GetComponent<Button>();
                if (profileButton == null)
                    profileButton = t.GetComponentInParent<Button>();
            }
        }

        Debug.Log("TryFindUI: resultsPanel=" + (resultsPanel != null) + " [" + (resultsPanel != null ? resultsPanel.name : "null") + "] playerListParent=" + (playerListParent != null) + " [" + (playerListParent != null ? playerListParent.name : "null") + "] playerRowPrefab=" + (playerRowPrefab != null) + " [" + (playerRowPrefab != null ? playerRowPrefab.name : "null") + "]");
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private void EnsurePlayerListMask()
    {
        if (playerListParent == null)
            return;

        TextMeshProUGUI strayText = playerListParent.GetComponent<TextMeshProUGUI>();
        if (strayText != null)
            strayText.enabled = false;

        if (playerListParent.GetComponent<Mask>() == null)
            playerListParent.gameObject.AddComponent<Mask>();

        if (playerListParent.GetComponent<Image>() == null)
        {
            Image img = playerListParent.gameObject.AddComponent<Image>();
            if (img != null)
                img.color = new Color(0f, 0f, 0f, 0.01f);
        }

        VerticalLayoutGroup vlg = playerListParent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = playerListParent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        ContentSizeFitter csf = playerListParent.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = playerListParent.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    public void ShowResults()
    {
        TryFindUI();
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
        Debug.Log("CollectPlayers: Found " + trackers.Length + " PlayerLapTracker objects");

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

            Debug.Log("CollectPlayers: tracker=" + tracker.gameObject.name + " name=" + name + " isLocal=" + isLocal + " finishTime=" + tracker.finishTime);

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

                float bLap = 0f;
                if (player.CustomProperties.TryGetValue("BestLap", out object bl) && bl is float bestL)
                    bLap = bestL;

                string name = string.IsNullOrEmpty(player.NickName)
                    ? "Player " + player.ActorNumber : player.NickName;

                players.Add(new PlayerResult
                {
                    playerName = name,
                    finishTime = time,
                    bestLap = bLap,
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
