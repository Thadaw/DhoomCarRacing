using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class RaceResultsUI : MonoBehaviour
{
    private Canvas canvas;
    private GameObject panel;
    private GameObject openButton;
    private Transform playerListParent;
    private bool isRaceComplete = false;

    private void EnsureCanvas()
    {
        if (canvas != null) return;
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var go = new GameObject("Canvas");
            canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            go.AddComponent<CanvasScaler>();
            go.AddComponent<GraphicRaycaster>();
        }
    }

    public void ShowResults()
    {
        isRaceComplete = true;
        OpenPanel();
    }

    public void OpenPanel()
    {
        EnsureCanvas();

        if (panel == null)
            CreatePanel();

        panel.SetActive(true);
        PopulateResults();
    }

    private void CreatePanel()
    {
        panel = new GameObject("RaceResultsPanel", typeof(RectTransform));
        panel.transform.SetParent(canvas.transform, false);
        panel.SetActive(false);

        RectTransform panelRt = panel.GetComponent<RectTransform>();
        panelRt.anchorMin = Vector2.zero;
        panelRt.anchorMax = Vector2.one;
        panelRt.sizeDelta = Vector2.zero;
        panelRt.anchoredPosition = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        // Title
        resultsTitleText = CreateText(panel.transform, "Race Complete!", new Vector2(0f, 240f), 42, FontStyles.Bold, TextAlignmentOptions.Center);

        // Close button (X) top-right
        CreateButton(panel.transform, "X", new Vector2(540f, 300f), () => panel.SetActive(false), 50f, 50f, new Color(0.6f, 0.1f, 0.1f));

        // Player list parent
        GameObject listGo = new GameObject("PlayerList", typeof(RectTransform));
        listGo.transform.SetParent(panel.transform, false);
        playerListParent = listGo.transform;

        RectTransform listRt = listGo.GetComponent<RectTransform>();
        listRt.anchorMin = new Vector2(0.5f, 0.5f);
        listRt.anchorMax = new Vector2(0.5f, 0.5f);
        listRt.sizeDelta = new Vector2(500f, 300f);
        listRt.anchoredPosition = new Vector2(0f, 60f);

        // Buttons
        CreateButton(panel.transform, "Restart", new Vector2(0f, -120f), () => RestartRace());
        CreateButton(panel.transform, "Lobby", new Vector2(0f, -200f), () => GoToLobby());
        CreateButton(panel.transform, "Main Menu", new Vector2(0f, -280f), () => GoToMainMenu());
    }

    private TMP_Text resultsTitleText;

    public void CreateOpenButton()
    {
        EnsureCanvas();
        if (openButton != null) return;

        openButton = new GameObject("ResultsOpenButton", typeof(RectTransform));
        openButton.transform.SetParent(canvas.transform, false);

        RectTransform rt = openButton.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(1f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(50f, 50f);
        rt.anchoredPosition = new Vector2(-60f, -60f);

        Image img = openButton.AddComponent<Image>();
        img.color = new Color(0.2f, 0.4f, 0.8f, 0.8f);

        Button btn = openButton.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(OpenPanel);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(openButton.transform, false);

        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;

        TMP_Text tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = "R";
        tmp.fontSize = 28;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
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
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.color = Color.white;

        return tmp;
    }

    private void CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction action, float width = 300f, float height = 50f, Color? color = null)
    {
        GameObject go = new GameObject(label + "Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.color = color ?? new Color(0.2f, 0.4f, 0.8f);

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        btn.onClick.AddListener(action);

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);

        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;

        TMP_Text tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 28;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
    }

    private void CreatePlayerEntry(Transform parent, int position, string playerName, float time)
    {
        string posStr = position switch
        {
            1 => "1st",
            2 => "2nd",
            3 => "3rd",
            _ => position + "th"
        };

        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;
        string timeStr = $"{minutes}:{seconds:00.00}";

        Color textColor = position switch
        {
            1 => new Color(1f, 0.84f, 0f),     // gold
            2 => new Color(0.75f, 0.75f, 0.75f), // silver
            3 => new Color(0.8f, 0.5f, 0.2f),    // bronze
            _ => Color.white
        };

        GameObject go = new GameObject("Entry", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(0f, 40f);
        rt.anchoredPosition = new Vector2(0f, -position * 45f);

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{posStr}. {playerName}  -  {timeStr}";
        tmp.fontSize = 26;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.rectTransform.offsetMin = new Vector2(20f, 0f);
        tmp.rectTransform.offsetMax = new Vector2(-20f, 0f);
    }

    private void CreateInProgressEntry(Transform parent, string playerName)
    {
        GameObject go = new GameObject("Entry", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(0f, 40f);

        int count = parent.childCount;
        rt.anchoredPosition = new Vector2(0f, -count * 45f);

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{playerName}  -  In Progress";
        tmp.fontSize = 24;
        tmp.color = new Color(0.6f, 0.6f, 0.6f);
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.rectTransform.offsetMin = new Vector2(20f, 0f);
        tmp.rectTransform.offsetMax = new Vector2(-20f, 0f);
    }

    private void PopulateResults()
    {
        foreach (Transform child in playerListParent)
            Destroy(child.gameObject);

        // Update title
        if (resultsTitleText != null)
            resultsTitleText.text = isRaceComplete ? "Race Complete!" : "Race Results";

        List<PlayerResult> results = new List<PlayerResult>();

        // Find all player trackers
        PlayerLapTracker[] trackers = FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None);

        // Collect finished players from all trackers (multiplayer: each tracked car)
        foreach (PlayerLapTracker t in trackers)
        {
            PhotonView pv = t.GetComponentInParent<PhotonView>();
            bool isLocal = pv != null && pv.IsMine;

            string name;
            if (isLocal)
                name = "You";
            else if (pv != null)
            {
                Photon.Realtime.Player owner = pv.Owner;
                name = owner != null && !string.IsNullOrEmpty(owner.NickName)
                    ? owner.NickName : "Player " + (owner != null ? owner.ActorNumber : "?");
            }
            else
                name = "Player";

            if (t.finishTime > 0f)
            {
                results.Add(new PlayerResult
                {
                    playerName = name,
                    finishTime = t.finishTime,
                    isLocal = isLocal,
                    isFinished = true
                });
            }
            else if (isLocal || pv != null)
            {
                results.Add(new PlayerResult
                {
                    playerName = name,
                    finishTime = 0f,
                    isLocal = isLocal,
                    isFinished = false
                });
            }
        }

        // Also check Photon custom properties for other players not found via trackers
        if (PhotonNetwork.InRoom)
        {
            foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal) continue;

                bool alreadyAdded = false;
                foreach (PlayerResult r in results)
                {
                    if (!r.isLocal && r.playerName.Contains("Player " + player.ActorNumber))
                    {
                        alreadyAdded = true;
                        break;
                    }
                }
                if (alreadyAdded) continue;

                string name = string.IsNullOrEmpty(player.NickName)
                    ? "Player " + player.ActorNumber : player.NickName;

                if (player.CustomProperties.TryGetValue("FinishTime", out object ft) && ft is float fTime)
                {
                    results.Add(new PlayerResult
                    {
                        playerName = name,
                        finishTime = fTime,
                        isLocal = false,
                        isFinished = true
                    });
                }
                else
                {
                    results.Add(new PlayerResult
                    {
                        playerName = name,
                        finishTime = 0f,
                        isLocal = false,
                        isFinished = false
                    });
                }
            }
        }

        // Sort: finished first by time, then unfinished
        results.Sort((a, b) =>
        {
            if (a.isFinished != b.isFinished)
                return a.isFinished ? -1 : 1;
            if (a.isFinished)
                return a.finishTime.CompareTo(b.finishTime);
            return 0;
        });

        // Display
        if (results.Count == 0)
        {
            CreateText(playerListParent, "No results available.", Vector2.zero, 24, FontStyles.Normal, TextAlignmentOptions.Center);
            return;
        }

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].isFinished)
                CreatePlayerEntry(playerListParent, i + 1, results[i].playerName, results[i].finishTime);
            else
                CreateInProgressEntry(playerListParent, results[i].playerName);
        }
    }

    private void RestartRace()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("MainGame");
        }
        else if (!PhotonNetwork.InRoom)
        {
            SceneManager.LoadScene("MainGame");
        }
        else
        {
            GoToMainMenu();
        }
    }

    private void GoToLobby()
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("Lobby");
        }
        else if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("MultiplayerMenu");
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    private void GoToMainMenu()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainMenu");
    }

    private class PlayerResult
    {
        public string playerName;
        public float finishTime;
        public bool isLocal;
        public bool isFinished;
    }
}
