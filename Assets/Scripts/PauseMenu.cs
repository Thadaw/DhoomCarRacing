using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button toggleButton;

    [Header("Panel Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Player Rows")]
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerRowPrefab;

    private bool isPaused = false;
    private List<GameObject> spawnedRows = new List<GameObject>();

    private void Start()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        TryFindButtons();
        BindButtons();
    }

    public void RefreshUI()
    {
        TryFindButtons();
        BindButtons();
    }

    private void BindButtons()
    {
        if (toggleButton != null)
        {
            toggleButton.onClick.RemoveAllListeners();
            toggleButton.onClick.AddListener(() => { PlayClickSound(); TogglePause(); });
        }
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveAllListeners();
            resumeButton.onClick.AddListener(() => { PlayClickSound(); Resume(); });
        }
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(() => { PlayClickSound(); Restart(); });
        }
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveAllListeners();
            mainMenuButton.onClick.AddListener(() => { PlayClickSound(); GoToMainMenu(); });
        }
    }

    private void TryFindButtons()
    {
        if (pausePanel == null)
        {
            var go = GameObject.Find("pausepanal");
            if (go == null) go = GameObject.Find("PausePanel");
            if (go != null) pausePanel = go;
        }
        if (toggleButton == null)
        {
            var go = GameObject.Find("PauseButton");
            // the corner icon in Track1/2/3 + aioponent is named "pause" — without
            // this fallback, clicking it did nothing (only the ESC key worked)
            if (go == null) go = GameObject.Find("pause");
            if (go != null) toggleButton = go.GetComponent<Button>();
        }
        if (resumeButton == null)
        {
            var go = GameObject.Find("ResumeButton");
            if (go != null) resumeButton = go.GetComponent<Button>();
        }
        if (restartButton == null)
        {
            var go = GameObject.Find("RestartButton");
            if (go != null) restartButton = go.GetComponent<Button>();
        }
        if (mainMenuButton == null)
        {
            var go = GameObject.Find("MainMenuButton");
            if (go != null) mainMenuButton = go.GetComponent<Button>();
        }
        if (playerListParent == null)
        {
            var go = GameObject.Find("PlayerList");
            if (go != null) playerListParent = go.transform;
        }
        if (playerRowPrefab == null && playerListParent != null)
        {
            Transform found = FindChildRecursive(playerListParent, "PlayerRow");
            if (found != null)
            {
                playerRowPrefab = found.gameObject;
                playerRowPrefab.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {
        Time.timeScale = 1f;
        isPaused = false;
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null)
            pausePanel.SetActive(true);
        UpdatePlayerRows();
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void UpdatePlayerRows()
    {
        if (playerListParent == null)
            return;

        ClearPlayerRows();

        List<PlayerInfo> players = CollectPlayers();

        // local player first, everyone else alphabetically — FindObjects order
        // was arbitrary, so the AI names showed up as "1, 3, 2"
        players.Sort((a, b) =>
        {
            if (a.isLocal != b.isLocal) return a.isLocal ? -1 : 1;
            return string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase);
        });

        int count = players.Count;
        float rowHeight = count <= 1 ? 80f : 40f;
        float fontSize = count <= 1 ? 50f : 28f;

        // snap the list over the panel's dark background with padding and let the
        // VerticalLayoutGroup + ContentSizeFitter size it (the manual sizeDelta
        // below was fought by the fitter anyway)
        LayoutPlayerList();

        for (int i = 0; i < count; i++)
        {
            string entryText = players[i].name;
            if (players[i].isLocal) entryText += "  (YOU)";
            SpawnPlayerRow(entryText, i, rowHeight, fontSize);
        }
    }

    // Fit the list over the panel's dark 'bgforplayer' rectangle with padding so
    // the names sit nicely inside the frame instead of hugging its top-left
    // corner, and configure the layout components so rows stack centred with
    // comfortable spacing.
    private void LayoutPlayerList()
    {
        RectTransform list = playerListParent as RectTransform;
        if (list == null) return;

        // locate the dark background rectangle (a sibling inside pausepanal)
        RectTransform bg = null;
        Transform parent = list.parent;
        if (parent != null)
        {
            foreach (Transform child in parent)
            {
                if (child.name == "bgforplayer") { bg = child as RectTransform; break; }
            }
        }

        if (bg != null)
        {
            // both live under the same parent, so the bg rect can be copied
            // directly. The list keeps a centred pivot, so the ContentSizeFitter
            // grows the name block symmetrically around the dark area's centre —
            // it stays clear of the frame border for any number of players.
            list.anchorMin = bg.anchorMin;
            list.anchorMax = bg.anchorMax;
            list.pivot = new Vector2(0.5f, 0.5f);
            list.anchoredPosition = bg.anchoredPosition;
            list.sizeDelta = new Vector2(Mathf.Max(120f, bg.sizeDelta.x - 130f), list.sizeDelta.y);
        }

        VerticalLayoutGroup vlg = playerListParent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = playerListParent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.MiddleCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(8, 8, 10, 10);

        ContentSizeFitter csf = playerListParent.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = playerListParent.gameObject.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private List<PlayerInfo> CollectPlayers()
    {
        List<PlayerInfo> players = new List<PlayerInfo>();
        HashSet<string> seen = new HashSet<string>();

        if (!PhotonNetwork.InRoom)
        {
            string localName = PlayerNameHelper.GetPlayerName();
            players.Add(new PlayerInfo { name = localName, time = 0f, isLocal = true });
            seen.Add(localName);

            PlayerLapTracker[] trackers = FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None);
            foreach (PlayerLapTracker tracker in trackers)
            {
                if (tracker.aiName != "")
                {
                    if (seen.Contains(tracker.aiName)) continue;
                    seen.Add(tracker.aiName);
                    players.Add(new PlayerInfo { name = tracker.aiName, time = tracker.finishTime, isLocal = false });
                }
            }

            return players;
        }

        PlayerLapTracker[] allTrackers = FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None);

        foreach (PlayerLapTracker tracker in allTrackers)
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
                name = PlayerNameHelper.GetPlayerName();
                isLocal = true;
            }

            if (seen.Contains(name)) continue;
            seen.Add(name);

            players.Add(new PlayerInfo { name = name, time = 0f, isLocal = isLocal });
        }

        foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
        {
            string pname = string.IsNullOrEmpty(player.NickName)
                ? "Player " + player.ActorNumber : player.NickName;

            if (seen.Contains(pname)) continue;
            seen.Add(pname);

            float time = 0f;
            if (player.CustomProperties.TryGetValue("FinishTime", out object ft) && ft is float fTime)
                time = fTime;

            players.Add(new PlayerInfo { name = pname, time = time, isLocal = player.IsLocal });
        }

        return players;
    }

    private void SpawnPlayerRow(string text, int rowIndex, float rowHeight, float fontSize)
    {
        if (playerRowPrefab != null)
        {
            GameObject row = Instantiate(playerRowPrefab, playerListParent);
            row.SetActive(true);

            TextMeshProUGUI tmp = row.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                ApplyPlayerTextStyle(tmp, text, fontSize);
            }

            RectTransform rowRT = row.GetComponent<RectTransform>();
            if (rowRT != null)
            {
                rowRT.anchorMin = new Vector2(0f, 1f);
                rowRT.anchorMax = new Vector2(1f, 1f);
                rowRT.pivot = new Vector2(0.5f, 1f);
                rowRT.sizeDelta = new Vector2(0f, rowHeight);
                rowRT.anchoredPosition = new Vector2(0f, -rowIndex * rowHeight);
            }

            spawnedRows.Add(row);
        }
        else
        {
            GameObject go = new GameObject("PlayerRow", typeof(RectTransform));
            go.transform.SetParent(playerListParent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            ApplyPlayerTextStyle(tmp, text, fontSize);

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, rowHeight);
            rt.anchoredPosition = new Vector2(0f, -rowIndex * rowHeight);
            rt.offsetMin = new Vector2(10f, 0f);
            rt.offsetMax = new Vector2(-10f, 0f);

            spawnedRows.Add(go);
        }
    }

    private void ApplyPlayerTextStyle(TextMeshProUGUI tmp, string text, float fontSize)
    {
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.92f, 0.55f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        tmp.fontMaterial = new Material(tmp.fontMaterial);
        tmp.fontMaterial.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.15f);
        tmp.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 1f));
        tmp.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 2f);
        tmp.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -2f);
        tmp.fontMaterial.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.5f);
        tmp.fontMaterial.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.6f));
    }

    private void ClearPlayerRows()
    {
        foreach (GameObject row in spawnedRows)
        {
            if (row != null)
                Destroy(row);
        }
        spawnedRows.Clear();
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

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;
        return minutes + ":" + seconds.ToString("00.00");
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(currentScene);
        else if (!PhotonNetwork.InRoom)
            SceneManager.LoadScene(currentScene);
        else
            GoToMainMenu();
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

    private class PlayerInfo
    {
        public string name;
        public float time;
        public bool isLocal;
    }
}
