using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class StatsController : MonoBehaviour
{
    [SerializeField] private SceneSwitcher sceneSwitcher;

    private Transform leaderboardListParent;
    private bool leaderboardLoading = false;
    private TMP_FontAsset cachedFont;
    private GameObject playerRowPrefab;

    private TMP_Text playerNameText;
    private TMP_Text playTimeText;

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    private void Start()
    {
        FirebaseManager.EnsureExists();
        LeaderboardManager.EnsureExists();

        playerRowPrefab = Resources.Load<GameObject>("PlayerRow");
        Debug.Log($"StatsController: PlayerRow prefab loaded: {(playerRowPrefab != null ? "YES" : "NO - will use fallback")}");

        SetupTitle();
        SetupPlayerName();
        SetupPlayTime();
        SetupBackButton();
        SetupLogoutButton();
        CreateLeaderboardListArea();
        RefreshLeaderboard();
    }

    private void SetupTitle()
    {
        GameObject statsObj = GameObject.Find("stats");
        if (statsObj != null)
        {
            TMP_Text titleText = statsObj.GetComponent<TMP_Text>();
            if (titleText != null)
            {
                titleText.text = "Stats";
                titleText.fontSize = 36;
                titleText.fontStyle = FontStyles.Bold;
                titleText.alignment = TextAlignmentOptions.Center;
                if (cachedFont == null) cachedFont = titleText.font;
            }
        }

        GameObject youObj = GameObject.Find("you");
        if (youObj != null)
        {
            TMP_Text youText = youObj.GetComponent<TMP_Text>();
            if (youText != null)
            {
                youText.text = "Your Name:";
                if (cachedFont == null) cachedFont = youText.font;
            }
        }
    }

    private GameObject nameEditPanel;
    private TMP_InputField nameEditInput;

    private void SetupPlayerName()
    {
        GameObject playerNameObj = GameObject.Find("playername");
        if (playerNameObj == null)
        {
            Debug.LogWarning("StatsController: 'playername' object not found in scene.");
            return;
        }

        playerNameText = playerNameObj.GetComponent<TMP_Text>();
        if (playerNameText == null)
        {
            Debug.LogWarning("StatsController: 'playername' has no TMP_Text component.");
            return;
        }

        playerNameText.text = PlayerNameHelper.GetPlayerName();
        if (cachedFont == null) cachedFont = playerNameText.font;

        Debug.Log($"StatsController: Player name set to '{playerNameText.text}'");

        TMP_InputField existingInput = playerNameObj.GetComponent<TMP_InputField>();
        if (existingInput != null)
        {
            existingInput.onValueChanged.AddListener(val =>
            {
                if (string.IsNullOrEmpty(val)) return;
                PlayerNameHelper.SetPlayerName(val);
            });
        }

        RectTransform nameRt = playerNameObj.GetComponent<RectTransform>();
        float btnX = nameRt.anchoredPosition.x + nameRt.sizeDelta.x / 2f + 8f;
        float btnY = nameRt.anchoredPosition.y;
        GameObject editGo = new GameObject("EditBtn", typeof(Image), typeof(Button));
        editGo.transform.SetParent(playerNameObj.transform.parent, false);
        RectTransform eRt = editGo.GetComponent<RectTransform>();
        eRt.anchorMin = nameRt.anchorMin;
        eRt.anchorMax = nameRt.anchorMax;
        eRt.sizeDelta = new Vector2(56, 30);
        eRt.anchoredPosition = new Vector2(btnX, btnY);
        editGo.GetComponent<Image>().color = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        Button editBtn = editGo.GetComponent<Button>();
        editBtn.targetGraphic = editGo.GetComponent<Image>();
        editBtn.transition = Selectable.Transition.None;
        GameObject eLabel = new GameObject("Label", typeof(RectTransform));
        eLabel.transform.SetParent(editGo.transform, false);
        RectTransform eLRt = eLabel.GetComponent<RectTransform>();
        eLRt.anchorMin = Vector2.zero;
        eLRt.anchorMax = Vector2.one;
        eLRt.sizeDelta = Vector2.zero;
        TMP_Text eTmp = eLabel.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) eTmp.font = cachedFont;
        eTmp.text = "EDIT";
        eTmp.fontSize = 12;
        eTmp.fontStyle = FontStyles.Bold;
        eTmp.alignment = TextAlignmentOptions.Center;
        eTmp.color = Color.white;

        nameEditPanel = new GameObject("NameEditPanel", typeof(RectTransform), typeof(Image));
        nameEditPanel.transform.SetParent(playerNameObj.transform.parent, false);
        RectTransform epRt = nameEditPanel.GetComponent<RectTransform>();
        epRt.anchorMin = new Vector2(0, 1);
        epRt.anchorMax = new Vector2(0, 1);
        epRt.sizeDelta = new Vector2(300, 140);
        epRt.anchoredPosition = new Vector2(146, btnY - 70);
        nameEditPanel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        nameEditPanel.SetActive(false);

        GameObject inputGo = new GameObject("InputField", typeof(RectTransform), typeof(Image));
        inputGo.transform.SetParent(nameEditPanel.transform, false);
        RectTransform iRt = inputGo.GetComponent<RectTransform>();
        iRt.anchorMin = new Vector2(0.5f, 0.8f);
        iRt.anchorMax = new Vector2(0.5f, 0.8f);
        iRt.sizeDelta = new Vector2(240, 36);
        iRt.anchoredPosition = Vector2.zero;
        inputGo.GetComponent<Image>().color = new Color(1, 1, 1, 0.2f);
        GameObject taGo = new GameObject("TextArea", typeof(RectTransform));
        taGo.transform.SetParent(inputGo.transform, false);
        RectTransform taRt = taGo.GetComponent<RectTransform>();
        taRt.anchorMin = Vector2.zero;
        taRt.anchorMax = Vector2.one;
        taRt.sizeDelta = new Vector2(-10, -6);
        taRt.anchoredPosition = Vector2.zero;
        GameObject txGo = new GameObject("Text", typeof(RectTransform));
        txGo.transform.SetParent(taGo.transform, false);
        RectTransform txRt = txGo.GetComponent<RectTransform>();
        txRt.anchorMin = Vector2.zero;
        txRt.anchorMax = Vector2.one;
        txRt.sizeDelta = Vector2.zero;
        TMP_Text txTmp = txGo.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) txTmp.font = cachedFont;
        txTmp.text = playerNameText.text;
        txTmp.fontSize = 24;
        txTmp.color = Color.white;
        txTmp.alignment = TextAlignmentOptions.MidlineLeft;
        nameEditInput = inputGo.AddComponent<TMP_InputField>();
        nameEditInput.textViewport = taRt;
        nameEditInput.textComponent = txTmp;
        nameEditInput.text = playerNameText.text;
        nameEditInput.characterLimit = 16;

        Button saveBtn = CreateSimpleButton(nameEditPanel.transform, "Save", new Vector2(-60, -30), new Vector2(80, 36), new Color(0.2f, 0.6f, 0.2f));
        saveBtn.onClick.AddListener(() =>
        {
            PlayClickSound();
            string val = nameEditInput.text;
            if (string.IsNullOrEmpty(val)) return;
            PlayerNameHelper.SetPlayerName(val);
            playerNameText.text = val;
            nameEditPanel.SetActive(false);
        });

        Button cancelBtn = CreateSimpleButton(nameEditPanel.transform, "Cancel", new Vector2(60, -30), new Vector2(80, 36), new Color(0.6f, 0.2f, 0.2f));
        cancelBtn.onClick.AddListener(() => { PlayClickSound(); nameEditPanel.SetActive(false); });

        editBtn.onClick.AddListener(() =>
        {
            PlayClickSound();
            nameEditInput.text = playerNameText.text;
            nameEditPanel.SetActive(true);
        });
    }

    private Button CreateSimpleButton(Transform parent, string label, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(label + "Btn", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
        Image img = go.AddComponent<Image>();
        img.color = color;
        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        GameObject lGo = new GameObject("Label", typeof(RectTransform));
        lGo.transform.SetParent(go.transform, false);
        RectTransform lRt = lGo.GetComponent<RectTransform>();
        lRt.anchorMin = Vector2.zero;
        lRt.anchorMax = Vector2.one;
        lRt.sizeDelta = Vector2.zero;
        TMP_Text tmp = lGo.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) tmp.font = cachedFont;
        tmp.text = label;
        tmp.fontSize = 20;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        return btn;
    }

    private void SetupPlayTime()
    {
        GameObject timeCount = GameObject.Find("Totalplaytimecount");
        if (timeCount == null)
        {
            Debug.LogWarning("StatsController: 'Totalplaytimecount' not found.");
            return;
        }

        playTimeText = timeCount.GetComponent<TMP_Text>();
        if (playTimeText == null) return;

        UpdatePlayTime();
    }

    private void UpdatePlayTime()
    {
        if (playTimeText == null) return;

        float totalSeconds = PlayTimeTracker.TotalPlayTime;
        int hours = Mathf.FloorToInt(totalSeconds / 3600f);
        int minutes = Mathf.FloorToInt((totalSeconds % 3600f) / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        string timeStr;
        if (hours > 0)
            timeStr = $"{hours}h {minutes}m {seconds}s";
        else if (minutes > 0)
            timeStr = $"{minutes}m {seconds}s";
        else
            timeStr = $"{seconds}s";

        playTimeText.text = timeStr;
    }

    private SceneSwitcher FindSceneSwitcher()
    {
        if (sceneSwitcher != null) return sceneSwitcher;
        sceneSwitcher = FindFirstObjectByType<SceneSwitcher>();
        return sceneSwitcher;
    }

    private void SetupBackButton()
    {
        GameObject backObj = GameObject.Find("back");
        if (backObj == null) return;

        Button backButton = backObj.GetComponent<Button>();
        if (backButton == null) return;

        backButton.onClick.RemoveAllListeners();
        backButton.onClick.AddListener(() =>
        {
            PlayClickSound();
            if (AudioManager.instance != null)
                AudioManager.instance.playMenuMusic();
            SceneSwitcher ss = FindSceneSwitcher();
            if (ss != null)
                ss.SceneLoder("MainMenu");
            else
                SceneManager.LoadScene("MainMenu");
        });
    }

    private void SetupLogoutButton()
    {
        GameObject logoutObj = GameObject.Find("logout");
        if (logoutObj == null) return;

        Button logoutButton = logoutObj.GetComponent<Button>();
        if (logoutButton == null) return;

        logoutButton.onClick.RemoveAllListeners();
        logoutButton.onClick.AddListener(() =>
        {
            PlayClickSound();
            PlayerNameHelper.Logout();
            if (AudioManager.instance != null)
                AudioManager.instance.playMenuMusic();
            SceneSwitcher ss = FindSceneSwitcher();
            if (ss != null)
                ss.SceneLoder("MainMenu");
            else
                SceneManager.LoadScene("MainMenu");
        });
    }

    private void CreateLeaderboardListArea()
    {
        GameObject statsObj = GameObject.Find("stats");
        Transform parent;

        if (statsObj != null)
        {
            parent = statsObj.transform;
            TMP_Text existingText = statsObj.GetComponent<TMP_Text>();
            if (existingText != null)
                existingText.text = "";
        }
        else
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            parent = canvas != null ? canvas.transform : transform;
        }

        GameObject scrollGo = new GameObject("LeaderboardScroll", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(parent, false);
        RectTransform scrollRt = scrollGo.GetComponent<RectTransform>();
        scrollRt.anchorMin = new Vector2(0f, 0f);
        scrollRt.anchorMax = new Vector2(1f, 1f);
        scrollRt.offsetMin = new Vector2(10f, 10f);
        scrollRt.offsetMax = new Vector2(-10f, 10f);
        scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.01f);
        if (scrollGo.GetComponent<Mask>() == null)
            scrollGo.AddComponent<Mask>().showMaskGraphic = false;

        ScrollRect scrollRect = scrollGo.GetComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.scrollSensitivity = 30f;

        GameObject contentGo = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(ContentSizeFitter), typeof(VerticalLayoutGroup));
        contentGo.transform.SetParent(scrollGo.transform, false);
        RectTransform contentRt = contentGo.GetComponent<RectTransform>();
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = new Vector2(0f, 0f);
        ContentSizeFitter csf = contentGo.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        VerticalLayoutGroup vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        scrollRect.content = contentRt;

        leaderboardListParent = contentGo.transform;
        Debug.Log("StatsController: Leaderboard scroll area created inside 'stats' object.");
    }

    private async void RefreshLeaderboard()
    {
        try
        {
            if (leaderboardListParent == null)
            {
                Debug.LogError("StatsController: leaderboardListParent is null.");
                return;
            }
            if (leaderboardLoading) return;
            leaderboardLoading = true;

            foreach (Transform child in leaderboardListParent)
                Destroy(child.gameObject);

            CreateLayoutText(leaderboardListParent, "Loading...", 20, FontStyles.Normal, TextAlignmentOptions.Center);

            if (LeaderboardManager.Instance == null)
            {
                LeaderboardManager.EnsureExists();
                await Task.Delay(500);
            }

            if (LeaderboardManager.Instance == null)
            {
                ClearAndShow("Leaderboard unavailable.\nCheck your internet connection.");
                return;
            }

            string trackId = GameSession.Instance?.SelectedTrackId ?? "";
            Debug.Log($"StatsController: SelectedTrackId = '{trackId}'");

            List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

            if (!string.IsNullOrEmpty(trackId))
            {
                Debug.Log($"StatsController: Fetching leaderboard for track '{trackId}'");
                entries = await LeaderboardManager.Instance.GetLeaderboard(trackId, 20);
                Debug.Log($"StatsController: Got {entries.Count} entries for track '{trackId}'");
            }

            if (entries.Count == 0)
            {
                Debug.Log("StatsController: No track-specific entries, fetching ALL entries");
                entries = await LeaderboardManager.Instance.GetAllLeaderboard(50);
                Debug.Log($"StatsController: Got {entries.Count} total entries");
            }

            List<GameObject> toDestroy = new List<GameObject>();
            foreach (Transform child in leaderboardListParent)
                toDestroy.Add(child.gameObject);
            foreach (GameObject go in toDestroy)
                Destroy(go);

            if (entries.Count == 0)
            {
                ClearAndShow("No times recorded yet.\nComplete a race to see stats here.");
                return;
            }

            string myName = PlayerNameHelper.GetPlayerName();
            float myBestTime = float.MaxValue;
            int myPosition = -1;

            for (int i = 0; i < entries.Count; i++)
            {
                bool isMe = entries[i].playerName == myName;
                CreateEntry(leaderboardListParent, i + 1, entries[i].playerName, entries[i].finishTime, entries[i].trackId, isMe);
                if (isMe && entries[i].finishTime < myBestTime)
                {
                    myBestTime = entries[i].finishTime;
                    myPosition = i + 1;
                }
            }

            string footerText;
            if (myPosition > 0)
                footerText = $"Your Position: {GetPositionStr(myPosition)} | Best Time: {FormatTime(myBestTime)}";
            else
                footerText = !string.IsNullOrEmpty(trackId) ? $"Track: {trackId}" : "All Tracks";

            CreateFooterText(leaderboardListParent, footerText);
        }
        catch (Exception ex)
        {
            Debug.LogError($"StatsController: RefreshLeaderboard failed: {ex.Message}\n{ex.StackTrace}");
            ClearAndShow("Failed to load leaderboard.");
        }
        finally
        {
            leaderboardLoading = false;
        }
    }

    private void ClearAndShow(string message)
    {
        if (leaderboardListParent == null) return;
        List<GameObject> toDestroy = new List<GameObject>();
        foreach (Transform child in leaderboardListParent)
            toDestroy.Add(child.gameObject);
        foreach (GameObject go in toDestroy)
            Destroy(go);
        CreateLayoutText(leaderboardListParent, message, 20, FontStyles.Normal, TextAlignmentOptions.Center);
    }

    private void CreateEntry(Transform parent, int position, string playerName, float time, string trackId, bool isCurrentPlayer = false)
    {
        string prefix = isCurrentPlayer ? "★ " : "";
        string posStr = GetPositionStr(position);
        string timeStr = FormatTime(time);

        Color textColor = position switch
        {
            1 => new Color(1f, 0.84f, 0f),
            2 => new Color(0.75f, 0.75f, 0.75f),
            3 => new Color(0.8f, 0.5f, 0.2f),
            _ => Color.white
        };
        if (isCurrentPlayer)
            textColor = new Color(0.3f, 1f, 0.3f);

        string entryText = $"{prefix}{posStr}. {playerName}  -  {timeStr}";

        // Larger entry height (~130px) so ~5 entries fit in view
        float entryHeight = 130f;
        float entryFontSize = 36;

        if (playerRowPrefab != null)
        {
            GameObject go = Instantiate(playerRowPrefab, parent);
            go.name = $"Entry_{position}";
            go.SetActive(true);

            LayoutElement le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.preferredHeight = entryHeight;
            le.preferredWidth = 880f;

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.sizeDelta = new Vector2(0f, entryHeight);
            }

            TMP_Text tmp = go.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
            {
                tmp.text = entryText;
                tmp.color = textColor;
                tmp.fontSize = entryFontSize;
                tmp.alignment = TextAlignmentOptions.MidlineLeft;
            }
            else
            {
                Debug.LogWarning("StatsController: PlayerRow has no TMP_Text child.");
            }
        }
        else
        {
            Debug.Log("StatsController: Using fallback text creation (no prefab).");
            GameObject go = new GameObject("Entry_" + position, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            LayoutElement entryLe = go.GetComponent<LayoutElement>();
            entryLe.preferredHeight = entryHeight;
            entryLe.preferredWidth = 880f;

            RectTransform rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(0f, entryHeight);

            TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
            if (cachedFont != null) tmp.font = cachedFont;
            tmp.text = entryText;
            tmp.fontSize = entryFontSize;
            tmp.color = textColor;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.rectTransform.offsetMin = new Vector2(20f, 0f);
            tmp.rectTransform.offsetMax = new Vector2(-20f, 0f);
        }
    }

    private static string GetPositionStr(int pos) => pos switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => pos + "th" };

    private static string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;
        return $"{minutes}:{seconds:00.00}";
    }

    private void CreateFooterText(Transform parent, string text)
    {
        GameObject go = new GameObject("Footer", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredHeight = 50f;
        le.preferredWidth = 880f;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 50f);

        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        if (cachedFont != null) tmp.font = cachedFont;
        tmp.text = text;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    private TMP_Text CreateLayoutText(Transform parent, string text, int fontSize, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI), typeof(LayoutElement));
        go.transform.SetParent(parent, false);

        LayoutElement le = go.GetComponent<LayoutElement>();
        le.preferredHeight = 50f;
        le.preferredWidth = 880f;

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, 50f);

        TMP_Text tmp = go.GetComponent<TextMeshProUGUI>();
        if (cachedFont != null) tmp.font = cachedFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = Color.white;

        return tmp;
    }

    private TMP_Text CreateText(Transform parent, string text, Vector2 pos, int fontSize, FontStyles style, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 60f);
        rt.anchoredPosition = pos;

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) tmp.font = cachedFont;
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = Color.white;

        return tmp;
    }
}
