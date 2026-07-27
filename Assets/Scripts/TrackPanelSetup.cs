using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class TrackPanelSetup : MonoBehaviour
{
    private static TrackPanelSetup instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        if (instance != null) return;
        GameObject go = new GameObject("TrackPanelSetup");
        instance = go.AddComponent<TrackPanelSetup>();
        DontDestroyOnLoad(go);
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instance == null) return;
        string name = scene.name;
        if (name != "Track1" && name != "Track2" && name != "Track3")
            return;

        instance.SetupPanels();
    }

    private Canvas FindCanvas()
    {
        Canvas c = FindObjectOfType<Canvas>();
        if (c == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            c = canvasGO.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        return c;
    }

    private void SetupPanels()
    {
        Canvas canvas = FindCanvas();

        SetupResultPanel(canvas);
        SetupSinglePlayerPanel(canvas);
        SetupPauseMenu(canvas);

        EnsureManagerScripts();
    }

    private void EnsureManagerScripts()
    {
        ResultsPanel rp = FindObjectOfType<ResultsPanel>();
        if (rp == null)
        {
            GameObject go = new GameObject("ResultPanalManager");
            rp = go.AddComponent<ResultsPanel>();
        }
        rp.RefreshUI();

        SinglePlayerFinishPanel sp = FindObjectOfType<SinglePlayerFinishPanel>();
        if (sp == null)
        {
            GameObject go = new GameObject("singleplayerresultpanalmanager");
            sp = go.AddComponent<SinglePlayerFinishPanel>();
        }
        sp.RefreshUI();

        PauseMenu pm = FindObjectOfType<PauseMenu>();
        if (pm == null)
        {
            GameObject go = new GameObject("pausePanalManager");
            pm = go.AddComponent<PauseMenu>();
        }
        pm.RefreshUI();
    }

    private void SetupResultPanel(Canvas canvas)
    {
        if (GameObject.Find("resultpanal") != null) return;

        GameObject panel = CreateUIPanel("resultpanal", canvas.transform);
        AddImage(panel, new Color(0.2f, 0.2f, 0.2f, 0.9f));

        CreateText(panel.transform, "position", "Position: ", new Vector2(20, -30), new Vector2(300, 40));
        CreateText(panel.transform, "finishtime", "Time: ", new Vector2(20, -80), new Vector2(300, 40));
        CreateText(panel.transform, "bestlap", "Best Lap: ", new Vector2(20, -130), new Vector2(300, 40));
        CreateText(panel.transform, "top speed", "Top Speed: ", new Vector2(20, -180), new Vector2(300, 40));
        CreateText(panel.transform, "avarage speed", "Avg Speed: ", new Vector2(20, -230), new Vector2(300, 40));

        GameObject listGO = new GameObject("playerlist", typeof(RectTransform));
        RectTransform listRT = listGO.GetComponent<RectTransform>();
        listRT.SetParent(panel.transform, false);
        listRT.anchorMin = new Vector2(0.5f, 0.5f);
        listRT.anchorMax = new Vector2(0.5f, 0.5f);
        listRT.anchoredPosition = new Vector2(200, 0);
        listRT.sizeDelta = new Vector2(300, 400);

        GameObject playerRow = CreatePlayerRow(listGO.transform);

        CreateButton(panel.transform, "garage", "Garage", new Vector2(-100, -300));
        CreateButton(panel.transform, "mainmenu", "Main Menu", new Vector2(100, -300));
        CreateButton(panel.transform, "profile", "Profile", new Vector2(0, -300));

        panel.SetActive(false);
    }

    private void SetupSinglePlayerPanel(Canvas canvas)
    {
        if (GameObject.Find("Single Player Finish Panel") != null) return;

        GameObject panel = CreateUIPanel("Single Player Finish Panel", canvas.transform);
        AddImage(panel, new Color(0.2f, 0.2f, 0.2f, 0.85f));

        CreateText(panel.transform, "SPPosition", "Position: ", new Vector2(20, -30), new Vector2(300, 40));
        CreateText(panel.transform, "SPPlayerName", "Player: ", new Vector2(20, -80), new Vector2(300, 40));
        CreateText(panel.transform, "SPFinishTime", "Time: ", new Vector2(20, -130), new Vector2(300, 40));
        CreateText(panel.transform, "SPBestLap", "Best Lap: ", new Vector2(20, -180), new Vector2(300, 40));
        CreateText(panel.transform, "SPTopSpeed", "Top Speed: ", new Vector2(20, -230), new Vector2(300, 40));
        CreateText(panel.transform, "SPAverageSpeed", "Avg Speed: ", new Vector2(20, -280), new Vector2(300, 40));

        CreateButton(panel.transform, "garage", "Garage", new Vector2(-100, -340));
        CreateButton(panel.transform, "mainmenu", "Main Menu", new Vector2(100, -340));
        CreateButton(panel.transform, "profile", "Profile", new Vector2(0, -340));

        panel.SetActive(false);
    }

    private void SetupPauseMenu(Canvas canvas)
    {
        if (GameObject.Find("pausepanal") != null) return;

        GameObject pausePanel = CreateUIPanel("pausepanal", canvas.transform);
        AddImage(pausePanel, new Color(0.15f, 0.15f, 0.15f, 0.92f));

        CreateButton(pausePanel.transform, "ResumeButton", "Resume", new Vector2(0, 80));
        CreateButton(pausePanel.transform, "RestartButton", "Restart", new Vector2(0, 0));
        CreateButton(pausePanel.transform, "MainMenuButton", "Main Menu", new Vector2(0, -80));

        GameObject listGO = new GameObject("PlayerList", typeof(RectTransform));
        RectTransform listRT = listGO.GetComponent<RectTransform>();
        listRT.SetParent(pausePanel.transform, false);
        listRT.anchorMin = new Vector2(0.5f, 0.5f);
        listRT.anchorMax = new Vector2(0.5f, 0.5f);
        listRT.anchoredPosition = new Vector2(250, 0);
        listRT.sizeDelta = new Vector2(200, 300);

        pausePanel.SetActive(false);

        CreatePauseToggle(canvas);
    }

    private void CreatePauseToggle(Canvas canvas)
    {
        if (GameObject.Find("PauseButton") != null) return;

        GameObject btnGO = CreateButton(canvas.transform, "PauseButton", "| |", new Vector2(-50, -30));
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.anchoredPosition = new Vector2(50, -50);
        rt.sizeDelta = new Vector2(60, 60);
    }

    private GameObject CreateUIPanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = panel.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        return panel;
    }

    private void AddImage(GameObject go, Color color)
    {
        Image img = go.GetComponent<Image>();
        if (img == null) img = go.AddComponent<Image>();
        img.color = color;
    }

    private void CreateText(Transform parent, string name, string text, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;
    }

    private GameObject CreateButton(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rt = btnGO.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(160, 50);

        Image img = btnGO.GetComponent<Image>();
        img.color = new Color(0.3f, 0.3f, 0.3f, 1f);

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(btnGO.transform, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btnGO;
    }

    private GameObject CreatePlayerRow(Transform parent)
    {
        GameObject row = new GameObject("playerrow 1", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rt = row.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 40);

        Image img = row.GetComponent<Image>();
        img.color = new Color(0.4f, 0.4f, 0.4f, 0.8f);

        GameObject textGO = new GameObject("Name", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.SetParent(row.transform, false);
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(10, 0);
        textRT.offsetMax = new Vector2(-10, 0);

        TextMeshProUGUI tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = "Player";
        tmp.fontSize = 20;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Left;

        row.SetActive(false);
        return row;
    }
}
