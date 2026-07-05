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

    private void Start()
    {
        FirebaseManager.EnsureExists();
        LeaderboardManager.EnsureExists();

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
        GameObject leaderboardTitle = GameObject.Find("Leaderboard");
        if (leaderboardTitle != null)
        {
            TMP_Text titleText = leaderboardTitle.GetComponent<TMP_Text>();
            if (titleText != null)
            {
                titleText.text = "Stats";
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
    private TMP_Text playerNameText;

    private void SetupPlayerName()
    {
        GameObject playerNameObj = GameObject.Find("playername");
        if (playerNameObj == null) return;

        playerNameText = playerNameObj.GetComponent<TMP_Text>();
        if (playerNameText == null) return;

        playerNameText.text = PlayerPrefs.GetString("PlayerName", "Player");
        if (cachedFont == null) cachedFont = playerNameText.font;

        TMP_InputField existingInput = playerNameObj.GetComponent<TMP_InputField>();
        if (existingInput != null)
        {
            existingInput.onValueChanged.AddListener(val =>
            {
                if (string.IsNullOrEmpty(val)) return;
                PlayerPrefs.SetString("PlayerName", val);
                PhotonNetwork.NickName = val;
            });
        }

        // Edit button beside the name
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

        // Edit panel (hidden - below the name field)
        nameEditPanel = new GameObject("NameEditPanel", typeof(RectTransform), typeof(Image));
        nameEditPanel.transform.SetParent(playerNameObj.transform.parent, false);
        RectTransform epRt = nameEditPanel.GetComponent<RectTransform>();
        epRt.anchorMin = new Vector2(0, 1);
        epRt.anchorMax = new Vector2(0, 1);
        epRt.sizeDelta = new Vector2(300, 140);
        epRt.anchoredPosition = new Vector2(146, btnY - 70);
        nameEditPanel.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        nameEditPanel.SetActive(false);

        // Input field in panel
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

        // Save button
        Button saveBtn = CreateSimpleButton(nameEditPanel.transform, "Save", new Vector2(-60, -30), new Vector2(80, 36), new Color(0.2f, 0.6f, 0.2f));
        saveBtn.onClick.AddListener(() =>
        {
            string val = nameEditInput.text;
            if (string.IsNullOrEmpty(val)) return;
            PlayerPrefs.SetString("PlayerName", val);
            PhotonNetwork.NickName = val;
            playerNameText.text = val;
            nameEditPanel.SetActive(false);
        });

        // Cancel button
        Button cancelBtn = CreateSimpleButton(nameEditPanel.transform, "Cancel", new Vector2(60, -30), new Vector2(80, 36), new Color(0.6f, 0.2f, 0.2f));
        cancelBtn.onClick.AddListener(() => nameEditPanel.SetActive(false));

        editBtn.onClick.AddListener(() =>
        {
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
        if (timeCount == null) return;

        TMP_Text timeText = timeCount.GetComponent<TMP_Text>();
        if (timeText == null) return;

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

        timeText.text = timeStr;
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
            PlayerPrefs.SetString("PlayerName", "Player");
            PhotonNetwork.NickName = "Player";
            SceneSwitcher ss = FindSceneSwitcher();
            if (ss != null)
                ss.SceneLoder("MainMenu");
            else
                SceneManager.LoadScene("MainMenu");
        });
    }

    private void CreateLeaderboardListArea()
    {
        GameObject leaderboardTitle = GameObject.Find("Leaderboard");
        if (leaderboardTitle == null) return;

        GameObject listGo = new GameObject("LeaderboardEntries", typeof(RectTransform));
        listGo.transform.SetParent(leaderboardTitle.transform.parent, false);

        RectTransform lRt = listGo.GetComponent<RectTransform>();
        lRt.anchorMin = new Vector2(0.5f, 0.5f);
        lRt.anchorMax = new Vector2(0.5f, 0.5f);
        lRt.sizeDelta = new Vector2(600f, 250f);
        lRt.anchoredPosition = new Vector2(0f, -20f);

        leaderboardListParent = listGo.transform;
    }

    private async void RefreshLeaderboard()
    {
        try
        {
            if (leaderboardListParent == null || leaderboardLoading) return;
            leaderboardLoading = true;

            foreach (Transform child in leaderboardListParent)
                Destroy(child.gameObject);

            string trackId = !string.IsNullOrEmpty(GameSession.Instance?.SelectedTrackId)
                ? GameSession.Instance.SelectedTrackId
                : "Desert_Track";

            if (LeaderboardManager.Instance == null || FirebaseManager.Instance == null || !FirebaseManager.Instance.Initialized)
            {
                CreateText(leaderboardListParent, "Leaderboard unavailable.\nCheck your internet connection and Firebase setup.", Vector2.zero, 20, FontStyles.Normal, TextAlignmentOptions.Center);
                leaderboardLoading = false;
                return;
            }

            CreateText(leaderboardListParent, "Loading...", Vector2.zero, 20, FontStyles.Normal, TextAlignmentOptions.Center);

            List<LeaderboardEntry> entries = await LeaderboardManager.Instance.GetLeaderboard(trackId, 20);

            foreach (Transform child in leaderboardListParent)
                Destroy(child.gameObject);

            if (entries.Count == 0)
            {
                CreateText(leaderboardListParent, "No times recorded yet.", Vector2.zero, 20, FontStyles.Normal, TextAlignmentOptions.Center);
                leaderboardLoading = false;
                return;
            }

            string myName = PlayerPrefs.GetString("PlayerName", "Player");
            for (int i = 0; i < entries.Count; i++)
                CreateEntry(leaderboardListParent, i + 1, entries[i].playerName, entries[i].finishTime, entries[i].playerName == myName);

            CreateText(leaderboardListParent, $"Track: {trackId}", new Vector2(0f, -entries.Count * 40f - 20f), 14, FontStyles.Normal, TextAlignmentOptions.Center);
        }
        catch (Exception ex)
        {
            Debug.LogError($"StatsController: RefreshLeaderboard failed: {ex.Message}\n{ex.StackTrace}");
            foreach (Transform child in leaderboardListParent)
                Destroy(child.gameObject);
            CreateText(leaderboardListParent, "Failed to load leaderboard.", Vector2.zero, 20, FontStyles.Normal, TextAlignmentOptions.Center);
        }
        finally
        {
            leaderboardLoading = false;
        }
    }

    private void CreateEntry(Transform parent, int position, string playerName, float time, bool isCurrentPlayer = false)
    {
        string prefix = isCurrentPlayer ? "★ " : "";
        string posStr = position switch { 1 => "1st", 2 => "2nd", 3 => "3rd", _ => position + "th" };

        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;

        Color textColor = position switch
        {
            1 => new Color(1f, 0.84f, 0f),
            2 => new Color(0.75f, 0.75f, 0.75f),
            3 => new Color(0.8f, 0.5f, 0.2f),
            _ => Color.white
        };
        if (isCurrentPlayer)
            textColor = new Color(0.3f, 1f, 0.3f);

        GameObject go = new GameObject("Entry", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(0f, 34f);
        rt.anchoredPosition = new Vector2(0f, -position * 38f);

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        if (cachedFont != null) tmp.font = cachedFont;
        tmp.text = $"{prefix}{posStr}. {playerName}  -  {minutes}:{seconds:00.00}";
        tmp.fontSize = 20;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.rectTransform.offsetMin = new Vector2(20f, 0f);
        tmp.rectTransform.offsetMax = new Vector2(-20f, 0f);
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
