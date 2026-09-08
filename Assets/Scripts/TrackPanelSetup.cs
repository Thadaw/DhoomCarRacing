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
        SetupResultsPanelRefs(rp);

        SinglePlayerFinishPanel sp = FindObjectOfType<SinglePlayerFinishPanel>();
        if (sp == null)
        {
            GameObject go = new GameObject("singleplayerresultpanalmanager");
            sp = go.AddComponent<SinglePlayerFinishPanel>();
        }
        SetupSinglePlayerPanelRefs(sp);

        Transform resultPanelT = FindDeep(FindCanvas().transform, "resultpanal");
        if (resultPanelT != null) resultPanelT.gameObject.SetActive(false);

        Transform spPanelT = FindDeep(FindCanvas().transform, "Single Player Finish Panel");
        if (spPanelT != null) spPanelT.gameObject.SetActive(false);

        PauseMenu pm = FindObjectOfType<PauseMenu>();
        if (pm == null)
        {
            GameObject go = new GameObject("pausePanalManager");
            pm = go.AddComponent<PauseMenu>();
        }
        pm.RefreshUI();
    }

    private void SetupSinglePlayerPanelRefs(SinglePlayerFinishPanel sp)
    {
        Transform panelT = FindDeep(FindCanvas().transform, "Single Player Finish Panel");
        if (panelT == null) return;

        GameObject panel = panelT.gameObject;
        TextMeshProUGUI posText = FindDeep(panelT, "SPPosition")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI nameText = FindDeep(panelT, "SPPlayerName")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timeText = FindDeep(panelT, "SPFinishTime")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI lapText = FindDeep(panelT, "SPBestLap")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI speedText = FindDeep(panelT, "SPTopSpeed")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI avgSpeedText = FindDeep(panelT, "SPAverageSpeed")?.GetComponent<TextMeshProUGUI>();
        Button garage = FindDeep(panelT, "garage")?.GetComponent<Button>();
        Button mainMenu = FindDeep(panelT, "mainmenu")?.GetComponent<Button>();
        Button profile = FindDeep(panelT, "profile")?.GetComponent<Button>();

        sp.SetupRefs(panel, posText, nameText, timeText, lapText, speedText, avgSpeedText, garage, mainMenu, profile);
    }

    private Transform FindDeep(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform found = FindDeep(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private void SetupResultsPanelRefs(ResultsPanel rp)
    {
        Transform panelT = FindDeep(FindCanvas().transform, "resultpanal");
        if (panelT == null) return;

        GameObject panel = panelT.gameObject;
        Transform playerListT = FindDeep(panelT, "playerlist");
        GameObject rowPrefab = FindDeep(panelT, "playerrow 1")?.gameObject;
        if (rowPrefab != null) rowPrefab.SetActive(false);

        TextMeshProUGUI posText = FindDeep(panelT, "position")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI timeText = FindDeep(panelT, "finishtime")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI lapText = FindDeep(panelT, "bestlap")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI topSpeedText = FindDeep(panelT, "top speed")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI avgSpeedText = FindDeep(panelT, "avarage speed")?.GetComponent<TextMeshProUGUI>();
        Button garage = FindDeep(panelT, "garage")?.GetComponent<Button>();
        Button mainMenu = FindDeep(panelT, "mainmenu")?.GetComponent<Button>();
        Button profile = FindDeep(panelT, "profile")?.GetComponent<Button>();

        rp.SetupRefs(panel, playerListT, rowPrefab, posText, timeText, lapText, topSpeedText, avgSpeedText, garage, mainMenu, profile);
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

        CreatePlayerRow(listGO.transform);

        CreateButton(panel.transform, "garage", "Garage", new Vector2(-100, -300));
        CreateButton(panel.transform, "mainmenu", "Main Menu", new Vector2(100, -300));
        CreateButton(panel.transform, "profile", "Profile", new Vector2(0, -300));

        panel.SetActive(false);
    }

    private void SetupSinglePlayerPanel(Canvas canvas)
    {
        if (GameObject.Find("Single Player Finish Panel") != null) return;

        GameObject panel = CreateUIPanel("Single Player Finish Panel", canvas.transform);
        RectTransform panelRT = panel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.anchoredPosition = Vector2.zero;
        panelRT.sizeDelta = new Vector2(600, 500);
        AddImage(panel, new Color(0.05f, 0.05f, 0.1f, 0.95f));

        VerticalLayoutGroup vlg = panel.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 8;
        vlg.padding = new RectOffset(30, 30, 20, 20);

        CreateStatRow(panel.transform, "SPPosition", "POSITION", "#1", 36, new Color(1f, 0.8f, 0f));
        CreateStatRow(panel.transform, "SPPlayerName", "RACER", "", 28, Color.white);
        CreateStatRow(panel.transform, "SPFinishTime", "TIME", "", 28, new Color(0f, 0.9f, 1f));
        CreateStatRow(panel.transform, "SPBestLap", "BEST LAP", "", 28, new Color(0f, 1f, 0.4f));
        CreateStatRow(panel.transform, "SPTopSpeed", "TOP SPEED", "", 28, new Color(1f, 0.4f, 0f));
        CreateStatRow(panel.transform, "SPAverageSpeed", "AVG SPEED", "", 28, new Color(1f, 0.6f, 0.2f));

        GameObject buttonRow = new GameObject("ButtonRow", typeof(RectTransform));
        HorizontalLayoutGroup hlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.spacing = 15;
        LayoutElement brLE = buttonRow.AddComponent<LayoutElement>();
        brLE.preferredHeight = 50;
        brLE.minHeight = 50;
        buttonRow.transform.SetParent(panel.transform, false);

        CreateButton(buttonRow.transform, "garage", "GARAGE", Vector2.zero);
        CreateButton(buttonRow.transform, "mainmenu", "MAIN MENU", Vector2.zero);
        CreateButton(buttonRow.transform, "profile", "PROFILE", Vector2.zero);

        panel.SetActive(false);
    }

    private void CreateStatRow(Transform parent, string name, string title, string value, int fontSize, Color color)
    {
        GameObject row = new GameObject(name + "_row", typeof(RectTransform));
        HorizontalLayoutGroup rowHlg = row.AddComponent<HorizontalLayoutGroup>();
        rowHlg.childAlignment = TextAnchor.MiddleLeft;
        rowHlg.childControlWidth = true;
        rowHlg.childControlHeight = true;
        rowHlg.childForceExpandWidth = true;
        rowHlg.childForceExpandHeight = true;
        rowHlg.spacing = 10;
        LayoutElement rowLE = row.AddComponent<LayoutElement>();
        rowLE.preferredHeight = fontSize + 14;
        rowLE.minHeight = fontSize + 10;
        row.transform.SetParent(parent, false);

        GameObject titleGO = new GameObject(name + "_title", typeof(RectTransform));
        titleGO.transform.SetParent(row.transform, false);
        TextMeshProUGUI titleTmp = titleGO.AddComponent<TextMeshProUGUI>();
        titleTmp.text = title;
        titleTmp.fontSize = fontSize;
        titleTmp.color = new Color(color.r, color.g, color.b, 0.6f);
        titleTmp.alignment = TextAlignmentOptions.MidlineLeft;
        LayoutElement titleLE = titleGO.AddComponent<LayoutElement>();
        titleLE.preferredWidth = 180;
        titleLE.minWidth = 140;

        GameObject valueGO = new GameObject(name, typeof(RectTransform));
        valueGO.transform.SetParent(row.transform, false);
        TextMeshProUGUI valueTmp = valueGO.AddComponent<TextMeshProUGUI>();
        valueTmp.text = value;
        valueTmp.fontSize = fontSize;
        valueTmp.color = color;
        valueTmp.alignment = TextAlignmentOptions.MidlineRight;
        LayoutElement valueLE = valueGO.AddComponent<LayoutElement>();
        valueLE.flexibleWidth = 1;
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
        listRT.sizeDelta = new Vector2(250, 120);

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

    private void CreateText(Transform parent, string name, string text, Vector2 pos, Vector2 size, int fontSize = 24, Color? color = null)
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
        tmp.fontSize = fontSize;
        tmp.color = color ?? Color.white;
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
