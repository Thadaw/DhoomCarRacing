using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    private GameObject settingsPanel;
    private Slider musicSlider;
    private Slider sfxSlider;
    private Toggle musicMuteToggle;
    private Toggle sfxMuteToggle;
    private Toggle fullscreenToggle;
    private TMP_Dropdown resolutionDropdown;
    private TextMeshProUGUI musicValueText;
    private TextMeshProUGUI sfxValueText;

    private Resolution[] resolutions;
    private int currentResolutionIndex;

    private const string FullscreenKey = "Fullscreen";
    private const string ResolutionKey = "ResolutionIndex";

    public void Init(Canvas canvas)
    {
        CreateSettingsPanel(canvas.transform);
        Hide();
        LoadSettings();
    }

    private void CreateSettingsPanel(Transform parent)
    {
        // Full screen dark overlay
        GameObject overlay = new GameObject("SettingsOverlay", typeof(RectTransform));
        overlay.transform.SetParent(parent, false);
        RectTransform overlayRT = overlay.GetComponent<RectTransform>();
        overlayRT.anchorMin = Vector2.zero;
        overlayRT.anchorMax = Vector2.one;
        overlayRT.offsetMin = Vector2.zero;
        overlayRT.offsetMax = Vector2.zero;
        Image overlayImg = overlay.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.8f);
        overlayImg.raycastTarget = true;

        // Panel background - racing theme dark with orange border
        settingsPanel = CreateUIPanel("SettingsPanel", overlay.transform);
        RectTransform panelRT = settingsPanel.GetComponent<RectTransform>();
        panelRT.anchorMin = new Vector2(0.5f, 0.5f);
        panelRT.anchorMax = new Vector2(0.5f, 0.5f);
        panelRT.sizeDelta = new Vector2(800, 700);
        AddImage(settingsPanel, new Color(0.08f, 0.08f, 0.1f, 0.97f));
        AddBorder(settingsPanel, 4f, new Color(1f, 0.65f, 0f, 1f));

        // Decorative top bar
        GameObject topBar = new GameObject("TopBar", typeof(RectTransform));
        topBar.transform.SetParent(settingsPanel.transform, false);
        RectTransform topBarRT = topBar.GetComponent<RectTransform>();
        topBarRT.anchorMin = new Vector2(0, 1);
        topBarRT.anchorMax = new Vector2(1, 1);
        topBarRT.pivot = new Vector2(0.5f, 1);
        topBarRT.sizeDelta = new Vector2(0, 6);
        Image topBarImg = topBar.AddComponent<Image>();
        topBarImg.color = new Color(1f, 0.65f, 0f, 1f);

        // Decorative bottom bar
        GameObject botBar = new GameObject("BotBar", typeof(RectTransform));
        botBar.transform.SetParent(settingsPanel.transform, false);
        RectTransform botBarRT = botBar.GetComponent<RectTransform>();
        botBarRT.anchorMin = new Vector2(0, 0);
        botBarRT.anchorMax = new Vector2(1, 0);
        botBarRT.pivot = new Vector2(0.5f, 0);
        botBarRT.sizeDelta = new Vector2(0, 6);
        Image botBarImg = botBar.AddComponent<Image>();
        botBarImg.color = new Color(1f, 0.65f, 0f, 1f);

        // Title
        CreateText(settingsPanel.transform, "SettingsTitle", "SETTINGS",
            new Vector2(0, 280), new Vector2(600, 55), 44,
            new Color(1f, 0.75f, 0f), TextAlignmentOptions.Center);

        // --- Music Volume ---
        CreateText(settingsPanel.transform, "MusicLabel", "Music Volume",
            new Vector2(0, 210), new Vector2(600, 35), 26,
            Color.white, TextAlignmentOptions.Center);
        musicSlider = CreateSlider(settingsPanel.transform, "MusicSlider",
            new Vector2(0, 170), 0f, 1f, OnMusicVolumeChanged);
        musicValueText = CreateText(settingsPanel.transform, "MusicValue", "50%",
            new Vector2(0, 140), new Vector2(100, 30), 22,
            new Color(1f, 0.75f, 0f), TextAlignmentOptions.Center);

        // --- SFX Volume ---
        CreateText(settingsPanel.transform, "SFXLabel", "SFX Volume",
            new Vector2(0, 90), new Vector2(600, 35), 26,
            Color.white, TextAlignmentOptions.Center);
        sfxSlider = CreateSlider(settingsPanel.transform, "SFXSlider",
            new Vector2(0, 50), 0f, 1f, OnSFXVolumeChanged);
        sfxValueText = CreateText(settingsPanel.transform, "SFXValue", "50%",
            new Vector2(0, 20), new Vector2(100, 30), 22,
            new Color(1f, 0.75f, 0f), TextAlignmentOptions.Center);

        // --- Mute toggles row ---
        musicMuteToggle = CreateToggle(settingsPanel.transform, "MusicMuteToggle",
            "Mute Music", new Vector2(-170, -40));
        musicMuteToggle.onValueChanged.AddListener(OnMusicMuteChanged);

        sfxMuteToggle = CreateToggle(settingsPanel.transform, "SFXMuteToggle",
            "Mute SFX", new Vector2(170, -40));
        sfxMuteToggle.onValueChanged.AddListener(OnSFXMuteChanged);

        // --- Fullscreen toggle ---
        fullscreenToggle = CreateToggle(settingsPanel.transform, "FullscreenToggle",
            "Fullscreen", new Vector2(0, -90));
        fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        CreateText(settingsPanel.transform, "FullscreenHint", "Toggle between fullscreen and windowed mode",
            new Vector2(0, -118), new Vector2(500, 24), 16,
            new Color(0.6f, 0.6f, 0.6f, 1f), TextAlignmentOptions.Center);

        // --- Resolution dropdown ---
        CreateText(settingsPanel.transform, "ResLabel", "Resolution",
            new Vector2(0, -155), new Vector2(500, 30), 22,
            Color.white, TextAlignmentOptions.Center);
        resolutionDropdown = CreateDropdown(settingsPanel.transform, "ResDropdown",
            new Vector2(0, -195));
        resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        // --- Back button ---
        CreateButton(settingsPanel.transform, "BackBtn", "BACK",
            new Vector2(0, -270), new Vector2(240, 55),
            new Color(0.85f, 0.15f, 0.1f), OnBackClicked);

        settingsPanel.SetActive(false);
    }

    // --- Callbacks ---
    private void OnMusicVolumeChanged(float val)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.SetMusicVolume(val);
        if (musicValueText != null)
            musicValueText.text = Mathf.RoundToInt(val * 100) + "%";
        if (musicMuteToggle != null && val > 0f)
            musicMuteToggle.isOn = false;
    }

    private void OnSFXVolumeChanged(float val)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.SetSFXVolume(val);
        if (sfxValueText != null)
            sfxValueText.text = Mathf.RoundToInt(val * 100) + "%";
        if (sfxMuteToggle != null && val > 0f)
            sfxMuteToggle.isOn = false;
    }

    private void OnMusicMuteChanged(bool muted)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.SetMusicMute(muted);
    }

    private void OnSFXMuteChanged(bool muted)
    {
        if (AudioManager.instance != null)
            AudioManager.instance.SetSFXMute(muted);
    }

    private void OnFullscreenChanged(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(FullscreenKey, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnResolutionChanged(int index)
    {
        if (index >= 0 && index < resolutions.Length)
        {
            Resolution res = resolutions[index];
            Screen.SetResolution(res.width, res.height, Screen.fullScreen);
            PlayerPrefs.SetInt(ResolutionKey, index);
            PlayerPrefs.Save();
        }
    }

    private void OnBackClicked()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
        Hide();
    }

    // --- Settings Load/Save ---
    private void LoadSettings()
    {
        float musicVol = AudioManager.instance != null ? AudioManager.instance.GetMusicVolume() : 0.12f;
        float sfxVol = AudioManager.instance != null ? AudioManager.instance.GetSFXVolume() : 0.7f;
        bool musicMuted = AudioManager.instance != null && AudioManager.instance.IsMusicMuted();
        bool sfxMuted = AudioManager.instance != null && AudioManager.instance.IsSFXMuted();

        if (musicSlider != null)
        {
            musicSlider.value = musicVol;
            if (musicValueText != null)
                musicValueText.text = Mathf.RoundToInt(musicVol * 100) + "%";
        }
        if (sfxSlider != null)
        {
            sfxSlider.value = sfxVol;
            if (sfxValueText != null)
                sfxValueText.text = Mathf.RoundToInt(sfxVol * 100) + "%";
        }
        if (musicMuteToggle != null) musicMuteToggle.isOn = musicMuted;
        if (sfxMuteToggle != null) sfxMuteToggle.isOn = sfxMuted;

        // Fullscreen
        bool isFullscreen = PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        Screen.fullScreen = isFullscreen;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;

        // Resolution
        PopulateResolutions();
    }

    private void PopulateResolutions()
    {
        resolutions = Screen.resolutions;
        if (resolutionDropdown == null) return;

        resolutionDropdown.ClearOptions();
        int savedIndex = PlayerPrefs.GetInt(ResolutionKey, 0);
        currentResolutionIndex = 0;

        var options = new System.Collections.Generic.List<string>();
        for (int i = 0; i < resolutions.Length; i++)
        {
            Resolution res = resolutions[i];
            string option = res.width + " x " + res.height;
            options.Add(option);

            if (res.width == Screen.currentResolution.width &&
                res.height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);
        int indexToUse = (savedIndex >= 0 && savedIndex < resolutions.Length) ? savedIndex : currentResolutionIndex;
        resolutionDropdown.value = indexToUse;
        currentResolutionIndex = indexToUse;
        resolutionDropdown.RefreshShownValue();
    }

    public void Show()
    {
        if (settingsPanel != null)
        {
            Transform overlay = settingsPanel.transform.parent;
            if (overlay != null)
                overlay.gameObject.SetActive(true);
            settingsPanel.SetActive(true);
            LoadSettings();
        }
    }

    public void Hide()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Transform overlay = settingsPanel.transform.parent;
            if (overlay != null)
                overlay.gameObject.SetActive(false);
        }
    }

    // --- UI Helpers ---
    private GameObject CreateUIPanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        return panel;
    }

    private void SetOverlayActive(bool active)
    {
        Transform overlay = settingsPanel.transform.parent;
        if (overlay != null)
        {
            Image img = overlay.GetComponent<Image>();
            if (img != null)
                img.raycastTarget = active;
            overlay.gameObject.SetActive(active);
        }
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, int fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        return tmp;
    }

    private Slider CreateSlider(Transform parent, string name, Vector2 pos,
        float min, float max, UnityEngine.Events.UnityAction<float> onValueChanged)
    {
        // Background track
        GameObject trackGO = new GameObject(name + "_Track", typeof(RectTransform));
        trackGO.transform.SetParent(parent, false);
        RectTransform trackRT = trackGO.GetComponent<RectTransform>();
        trackRT.anchorMin = new Vector2(0.5f, 0.5f);
        trackRT.anchorMax = new Vector2(0.5f, 0.5f);
        trackRT.anchoredPosition = pos;
        trackRT.sizeDelta = new Vector2(550, 20);
        Image trackImg = trackGO.AddComponent<Image>();
        trackImg.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        // Fill area
        GameObject fillArea = new GameObject("FillArea", typeof(RectTransform));
        fillArea.transform.SetParent(trackGO.transform, false);
        RectTransform fillAreaRT = fillArea.GetComponent<RectTransform>();
        fillAreaRT.anchorMin = new Vector2(0, 0.5f);
        fillAreaRT.anchorMax = new Vector2(1, 0.5f);
        fillAreaRT.sizeDelta = new Vector2(-20, 16);
        fillAreaRT.anchoredPosition = new Vector2(10, 0);

        GameObject fill = new GameObject("Fill", typeof(RectTransform));
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRT = fill.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = new Vector2(0, 1);
        fillRT.sizeDelta = new Vector2(0, 0);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(1f, 0.7f, 0f, 1f);

        // Handle area
        GameObject handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(trackGO.transform, false);
        RectTransform handleAreaRT = handleArea.GetComponent<RectTransform>();
        handleAreaRT.anchorMin = new Vector2(0, 0.5f);
        handleAreaRT.anchorMax = new Vector2(1, 0.5f);
        handleAreaRT.sizeDelta = new Vector2(-20, 20);
        handleAreaRT.anchoredPosition = Vector2.zero;

        GameObject handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20, 20);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;

        // Slider component
        Slider slider = trackGO.AddComponent<Slider>();
        slider.fillRect = fillRT;
        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = (min + max) / 2f;
        slider.onValueChanged.AddListener(onValueChanged);

        return slider;
    }

    private Toggle CreateToggle(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject rowGO = new GameObject(name + "_Row", typeof(RectTransform));
        rowGO.transform.SetParent(parent, false);
        RectTransform rowRT = rowGO.GetComponent<RectTransform>();
        rowRT.anchorMin = new Vector2(0.5f, 0.5f);
        rowRT.anchorMax = new Vector2(0.5f, 0.5f);
        rowRT.anchoredPosition = pos;
        rowRT.sizeDelta = new Vector2(350, 40);

        // Background checkbox
        GameObject bgGO = new GameObject("Background", typeof(RectTransform));
        bgGO.transform.SetParent(rowGO.transform, false);
        RectTransform bgRT = bgGO.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.5f);
        bgRT.anchorMax = new Vector2(0, 0.5f);
        bgRT.anchoredPosition = new Vector2(15, 0);
        bgRT.sizeDelta = new Vector2(28, 28);
        Image bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);

        // Checkmark
        GameObject checkGO = new GameObject("Checkmark", typeof(RectTransform));
        checkGO.transform.SetParent(bgGO.transform, false);
        RectTransform checkRT = checkGO.GetComponent<RectTransform>();
        checkRT.anchorMin = Vector2.zero;
        checkRT.anchorMax = Vector2.one;
        checkRT.offsetMin = new Vector2(4, 4);
        checkRT.offsetMax = new Vector2(-4, -4);
        Image checkImg = checkGO.AddComponent<Image>();
        checkImg.color = new Color(1f, 0.65f, 0f, 1f);

        // Label
        GameObject labelGO = new GameObject("Label", typeof(RectTransform));
        labelGO.transform.SetParent(rowGO.transform, false);
        RectTransform labelRT = labelGO.GetComponent<RectTransform>();
        labelRT.anchorMin = new Vector2(0, 0.5f);
        labelRT.anchorMax = new Vector2(1, 0.5f);
        labelRT.offsetMin = new Vector2(45, -15);
        labelRT.offsetMax = new Vector2(-10, 15);
        TextMeshProUGUI labelTmp = labelGO.AddComponent<TextMeshProUGUI>();
        labelTmp.text = label;
        labelTmp.fontSize = 22;
        labelTmp.color = Color.white;
        labelTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Toggle component
        Toggle toggle = rowGO.AddComponent<Toggle>();
        toggle.targetGraphic = bgImg;
        toggle.graphic = checkImg;
        toggle.isOn = false;

        return toggle;
    }

    private TMP_Dropdown CreateDropdown(Transform parent, string name, Vector2 pos)
    {
        GameObject dropGO = new GameObject(name, typeof(RectTransform));
        dropGO.transform.SetParent(parent, false);
        RectTransform dropRT = dropGO.GetComponent<RectTransform>();
        dropRT.anchorMin = new Vector2(0.5f, 0.5f);
        dropRT.anchorMax = new Vector2(0.5f, 0.5f);
        dropRT.anchoredPosition = pos;
        dropRT.sizeDelta = new Vector2(350, 40);

        // Background
        Image bgImg = dropGO.AddComponent<Image>();
        bgImg.color = new Color(0.15f, 0.15f, 0.15f, 1f);

        // Template (dropdown list)
        GameObject template = new GameObject("Template", typeof(RectTransform));
        template.transform.SetParent(dropGO.transform, false);
        RectTransform templateRT = template.GetComponent<RectTransform>();
        templateRT.anchorMin = new Vector2(0, 0);
        templateRT.anchorMax = new Vector2(1, 0);
        templateRT.pivot = new Vector2(0.5f, 1f);
        templateRT.sizeDelta = new Vector2(0, 150);
        templateRT.anchoredPosition = new Vector2(0, -40);
        Image templateBg = template.AddComponent<Image>();
        templateBg.color = new Color(0.15f, 0.15f, 0.15f, 0.95f);
        ScrollRect scroll = template.AddComponent<ScrollRect>();
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask));
        viewport.transform.SetParent(template.transform, false);
        RectTransform viewportRT = viewport.GetComponent<RectTransform>();
        viewportRT.anchorMin = Vector2.zero;
        viewportRT.anchorMax = Vector2.one;
        viewportRT.sizeDelta = Vector2.zero;
        viewportRT.offsetMin = Vector2.zero;
        viewportRT.offsetMax = Vector2.zero;
        Mask viewportMask = viewport.GetComponent<Mask>();
        viewportMask.showMaskGraphic = false;
        Image viewportImg = viewport.AddComponent<Image>();
        viewportImg.color = Color.white;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0, 28);

        scroll.viewport = viewportRT;
        scroll.content = contentRT;

        // Item template
        GameObject itemTemplate = new GameObject("Item", typeof(RectTransform));
        itemTemplate.transform.SetParent(content.transform, false);
        RectTransform itemRT = itemTemplate.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f);
        itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 40);
        Image itemBg = itemTemplate.AddComponent<Image>();
        itemBg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
        Toggle itemToggle = itemTemplate.AddComponent<Toggle>();
        itemToggle.targetGraphic = itemBg;

        GameObject itemLabel = new GameObject("Item Label", typeof(RectTransform));
        itemLabel.transform.SetParent(itemTemplate.transform, false);
        RectTransform itemLabelRT = itemLabel.GetComponent<RectTransform>();
        itemLabelRT.anchorMin = Vector2.zero;
        itemLabelRT.anchorMax = Vector2.one;
        itemLabelRT.offsetMin = new Vector2(10, 0);
        itemLabelRT.offsetMax = new Vector2(-10, 0);
        TextMeshProUGUI itemLabelTmp = itemLabel.AddComponent<TextMeshProUGUI>();
        itemLabelTmp.fontSize = 18;
        itemLabelTmp.color = Color.white;
        itemLabelTmp.alignment = TextAlignmentOptions.MidlineLeft;
        itemToggle.graphic = itemLabelTmp;

        template.SetActive(false);

        // Caption text
        GameObject captionGO = new GameObject("Label", typeof(RectTransform));
        captionGO.transform.SetParent(dropGO.transform, false);
        RectTransform captionRT = captionGO.GetComponent<RectTransform>();
        captionRT.anchorMin = Vector2.zero;
        captionRT.anchorMax = Vector2.one;
        captionRT.offsetMin = new Vector2(15, 0);
        captionRT.offsetMax = new Vector2(-35, 0);
        TextMeshProUGUI captionTmp = captionGO.AddComponent<TextMeshProUGUI>();
        captionTmp.fontSize = 18;
        captionTmp.color = Color.white;
        captionTmp.alignment = TextAlignmentOptions.MidlineLeft;

        // Arrow
        GameObject arrowGO = new GameObject("Arrow", typeof(RectTransform));
        arrowGO.transform.SetParent(dropGO.transform, false);
        RectTransform arrowRT = arrowGO.GetComponent<RectTransform>();
        arrowRT.anchorMin = new Vector2(1, 0.5f);
        arrowRT.anchorMax = new Vector2(1, 0.5f);
        arrowRT.anchoredPosition = new Vector2(-15, 0);
        arrowRT.sizeDelta = new Vector2(20, 20);
        TextMeshProUGUI arrowTmp = arrowGO.AddComponent<TextMeshProUGUI>();
        arrowTmp.text = "▼";
        arrowTmp.fontSize = 16;
        arrowTmp.color = Color.white;
        arrowTmp.alignment = TextAlignmentOptions.Center;

        // Dropdown component
        TMP_Dropdown dropdown = dropGO.AddComponent<TMP_Dropdown>();
        dropdown.targetGraphic = bgImg;
        dropdown.captionText = captionTmp;
        dropdown.template = templateRT;
        dropdown.itemText = itemLabelTmp;

        return dropdown;
    }

    private Button CreateButton(Transform parent, string name, string text,
        Vector2 pos, Vector2 size, Color color, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGO = new GameObject(name, typeof(RectTransform));
        btnGO.transform.SetParent(parent, false);
        RectTransform btnRT = btnGO.GetComponent<RectTransform>();
        btnRT.anchorMin = new Vector2(0.5f, 0.5f);
        btnRT.anchorMax = new Vector2(0.5f, 0.5f);
        btnRT.anchoredPosition = pos;
        btnRT.sizeDelta = size;
        Image btnImg = btnGO.AddComponent<Image>();
        btnImg.color = color;
        Button btn = btnGO.AddComponent<Button>();
        btn.targetGraphic = btnImg;
        btn.onClick.AddListener(onClick);

        // Button text
        GameObject textGO = new GameObject("Text", typeof(RectTransform));
        textGO.transform.SetParent(btnGO.transform, false);
        RectTransform textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = Vector2.zero;
        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 22;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return btn;
    }

    private void AddImage(GameObject go, Color color)
    {
        Image img = go.AddComponent<Image>();
        img.color = color;
    }

    private void AddOutline(GameObject go, Color color, float width)
    {
        Outline outline = go.AddComponent<Outline>();
        outline.effectColor = color;
        outline.effectDistance = new Vector2(width, width);
    }

    private void AddBorder(GameObject go, float width, Color color)
    {
        // Top border
        CreateBorderEdge(go.transform, "BorderTop", new Vector2(0, 1), new Vector2(0, 1),
            new Vector2(0, 1), Vector2.zero, new Vector2(0, -width), color);
        // Bottom border
        CreateBorderEdge(go.transform, "BorderBot", new Vector2(0, 0), new Vector2(1, 0),
            new Vector2(0, 0), Vector2.zero, new Vector2(0, width), color);
        // Left border
        CreateBorderEdge(go.transform, "BorderLeft", new Vector2(0, 0), new Vector2(0, 1),
            new Vector2(0, 0), Vector2.zero, new Vector2(width, 0), color);
        // Right border
        CreateBorderEdge(go.transform, "BorderRight", new Vector2(1, 0), new Vector2(1, 1),
            new Vector2(1, 0), Vector2.zero, new Vector2(-width, 0), color);
    }

    private void CreateBorderEdge(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        GameObject edge = new GameObject(name, typeof(RectTransform));
        edge.transform.SetParent(parent, false);
        RectTransform rt = edge.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        Image img = edge.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
    }
}
