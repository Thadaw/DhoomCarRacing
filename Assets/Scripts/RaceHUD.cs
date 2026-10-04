using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

// Race HUD built entirely at runtime so every race scene (Track1/2/3 + aioponent)
// gets the same layout without hand-editing scene files:
//
//   top-right     dark rounded panel:  LAP  x/y   /   TIME  mm:ss.ff
//   bottom-right  round speedometer:   needle + big km/h number + gear box
//   bottom-left   round mini-map:      navigation-style: dark disc + track path
//                                      baked from the scene that ROTATES with the
//                                      car, fixed yellow arrow at the centre +
//                                      orbiting 'N' compass
//
// The legacy widgets (old "Speed" panel, old lap text) are hidden once this HUD
// is up, so nothing is drawn twice.
public class RaceHUD : MonoBehaviour
{
    // dial geometry (degrees measured CLOCKWISE from straight up)
    private const float GaugeMax = 220f;   // full scale printed on the dial
    private const float SweepStart = -125f;
    private const float Sweep = 250f;

    // mini-map: radius (in texture px) that all track points are fitted inside.
    // 126 keeps every point inside the 256px texture AND inside the 296px disc
    // (radius 148), so the baked path can never spill over the disc edge.
    private const float MapRadiusPx = 126f;

    private static readonly Color PanelBg = new Color(0.05f, 0.07f, 0.10f, 0.86f);
    private static readonly Color LabelColor = new Color(0.62f, 0.68f, 0.76f, 1f);
    private static readonly Color ValueColor = new Color(1f, 1f, 1f, 1f);
    private static readonly Color GearRed = new Color(0.88f, 0.22f, 0.16f, 0.95f);

    private static readonly Color MiniMapBg = new Color(0.08f, 0.10f, 0.14f, 0.92f);
    private static readonly Color MiniMapTrack = new Color(0.45f, 0.48f, 0.55f, 1f);
    private static readonly Color MiniMapArrow = new Color(1f, 0.92f, 0.24f, 1f);
    private static readonly Color MiniMapN = new Color(0.85f, 0.89f, 0.94f, 1f);
    private static readonly Color MiniMapLine = new Color(0.42f, 0.76f, 1f, 1f);     // route (nav blue)
    private static readonly Color MiniMapCasing = new Color(0.13f, 0.17f, 0.25f, 1f); // dark road casing
    private static readonly Color MiniMapStart = new Color(0.95f, 0.97f, 1f, 1f);

    private static Sprite roundedSprite;
    private static Sprite circleSprite;
    private static Sprite gaugeSprite;
    private static Sprite arrowSprite;

    private PhotonCarController car;
    private PlayerLapTracker tracker;
    private float findTimer;
    private bool timing;
    private float raceT0;
    private bool legacyHidden;
    private bool lapHidden;

    // results visibility — the whole race HUD is hidden while a results panel
    // (multiplayer/AI 'resultpanal' or the single-player finish panel) is up
    private GameObject hudCanvas;
    private ResultsPanel resultsPanel;
    private SinglePlayerFinishPanel singleFinishPanel;

    private TextMeshProUGUI lapValue;
    private TextMeshProUGUI timeValue;
    private TextMeshProUGUI speedValue;
    private TextMeshProUGUI gearValue;
    private RectTransform needle;

    // mini-map — baked track path + player arrowhead + 'N' heading
    private RectTransform mapArrow;
    private TextMeshProUGUI mapNorth;
    private Image trackImg;
    private bool mapBaked;
    private float mapRetryTimer;
    private int mapAttempts;
    private Vector2 mapCenter;   // world XZ centre the map is fitted around
    private float mapScale;      // texture px (== rect units) per world metre

    // race scenes only — Track1/2/3 and aioponent are the scenes with a CarSpawner.
    //
    // This MUST be driven by SceneManager.sceneLoaded, not by
    // RuntimeInitializeLoadType.AfterSceneLoad: that callback fires exactly once
    // after the FIRST scene of the session. In a build that first scene is
    // MainMenu, which has no CarSpawner, so the old version bailed out here and
    // never retried once the player reached a track scene — the HUD silently
    // never appeared in builds while still working in the Editor, where the
    // active scene at play time is usually a track.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterBootstrap()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (FindFirstObjectByType<CarSpawner>() == null) return;
        if (FindFirstObjectByType<RaceHUD>() != null) return;

        // deliberately NOT DontDestroyOnLoad — the HUD belongs to the race scene
        // and must disappear again when the player returns to the menu/garage.
        new GameObject("RaceHUD").AddComponent<RaceHUD>();
    }

    private void Awake()
    {
        BuildCanvas();
        ShiftPauseButtonAside();
    }

    private void Update()
    {
        if (car == null || tracker == null)
        {
            findTimer -= Time.deltaTime;
            if (findTimer <= 0f)
            {
                findTimer = 0.4f;
                FindRefs();
            }
        }

        HideLegacyWidgets();
        UpdateReadouts();
        UpdateMiniMap();
        UpdateResultsVisibility();
    }

    // ---------------------------------------------------------------- references

    private void FindRefs()
    {
        if (car == null)
        {
            PhotonCarController[] cars = FindObjectsByType<PhotonCarController>(FindObjectsSortMode.None);
            foreach (PhotonCarController c in cars)
                if (c.isLocalPlayerCar && !c.useExternalInput) { car = c; break; }

            if (car == null)
                foreach (PhotonCarController c in cars)
                    if (!c.useExternalInput) { car = c; break; }
        }

        if (tracker == null && car != null)
        {
            tracker = car.GetComponent<PlayerLapTracker>();
            if (tracker == null) tracker = car.GetComponentInChildren<PlayerLapTracker>();
        }
    }

    // the old speed widget (top-right), old lap text (top-left) and the legacy
    // "Checkpoint = 0" counter (top-left, object named "pointcount" in every race
    // scene) are replaced by this HUD — hide them once so nothing is drawn twice
    private void HideLegacyWidgets()
    {
        if (!legacyHidden)
        {
            legacyHidden = true;
            foreach (TextMeshProUGUI t in FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                // legacy checkpoint counter — the lap/time panel carries the race
                // info now; nothing re-enables this object, so hiding it is enough
                // (PlayerLapTracker keeps writing to it, which is harmless while
                // the GameObject is inactive)
                if (t.name == "pointcount")
                {
                    t.gameObject.SetActive(false);
                    continue;
                }

                if (t.name != "SpeedText") continue;
                Transform parent = t.transform.parent;
                GameObject kill = (parent != null && parent.name == "Speed") ? parent.gameObject : t.gameObject;
                kill.SetActive(false);
            }
        }

        // Legacy lap counter in the top-left corner (the scene object "lap",
        // bound by RaceUIBinder 0.2s AFTER start) — the lap/time panel at the
        // top-right is the only lap display now. Hidden by object reference, not
        // just via the tracker this HUD resolved: the binder picks its OWN
        // "local" tracker, which can be a different car — that's how the top-left
        // text survived. Re-runs for the first 0.6s so the late binding is
        // covered too.
        if (!lapHidden)
        {
            HideLegacyLapTexts();
            lapHidden = Time.timeSinceLevelLoad > 0.6f;
        }
    }

    // hide every lap counter this scene might drive: the binder's own TMP object,
    // whatever the trackers ended up bound to, and the old Checkpoints UI.Text
    private void HideLegacyLapTexts()
    {
        RaceUIBinder binder = FindFirstObjectByType<RaceUIBinder>();
        if (binder != null && binder.lapText != null)
            binder.lapText.gameObject.SetActive(false);

        foreach (PlayerLapTracker t in FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None))
            if (t.lapText != null) t.lapText.gameObject.SetActive(false);

        foreach (Checkpoints c in FindObjectsByType<Checkpoints>(FindObjectsSortMode.None))
            if (c.lapText != null) c.lapText.gameObject.SetActive(false);
    }

    // ---------------------------------------------------------------- readouts

    private void UpdateReadouts()
    {
        float speed = car != null ? car.CarSpeed() : 0f;

        if (speedValue != null)
            speedValue.text = speed.ToString("0");

        if (needle != null)
        {
            float t = Mathf.Clamp01(speed / GaugeMax);
            float angle = SweepStart + Sweep * t;
            needle.localRotation = Quaternion.Euler(0f, 0f, -angle);
        }

        if (gearValue != null)
        {
            float maxSpeed = car != null && car.maxSpeed > 10f ? car.maxSpeed : GaugeMax;
            int gear = speed < 1f ? 1 : Mathf.Clamp(Mathf.CeilToInt(speed / (maxSpeed / 6f)), 1, 6);
            gearValue.text = gear.ToString();
        }

        if (lapValue != null)
            lapValue.text = tracker != null
                ? tracker.currentLap + "/" + Mathf.Max(1, tracker.totalLaps)
                : "1/1";

        if (timeValue != null)
        {
            bool started = RaceManager.Instance != null && RaceManager.Instance.raceStarted;
            if (started && !timing)
            {
                timing = true;
                raceT0 = Time.time;
            }

            float elapsed = 0f;
            if (tracker != null && tracker.raceCompleted)
                elapsed = tracker.finishTime;
            else if (timing)
                elapsed = Time.time - raceT0;

            timeValue.text = FormatTime(elapsed);
        }
    }

    private static string FormatTime(float seconds)
    {
        if (seconds < 0f) seconds = 0f;
        int mins = (int)(seconds / 60f);
        float rest = seconds - mins * 60f;
        return string.Format("{0:00}:{1:00.00}", mins, rest);
    }

    // ---------------------------------------------------------------- layout

    private void BuildCanvas()
    {
        GameObject canvasGO = new GameObject("RaceHUD_Canvas", typeof(RectTransform));
        hudCanvas = canvasGO;
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;                       // above the scene HUD

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600f, 900f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        BuildLapTimePanel(canvasGO.transform);
        BuildGauge(canvasGO.transform);
        BuildMiniMap(canvasGO.transform);
    }

    // The pause icon is a scene object on the scene's own canvas (named "pause"
    // in Track1/2/3 + aioponent; "PauseButton" kept as a fallback name). It used
    // to sit flush in the top-right corner, where the lap/time panel now lives,
    // so shift its centre to 77.5% of the screen width — that leaves the panel
    // room on the same row with a small gap between the two:
    //
    //     [pause] [ LAP / TIME ]
    //
    // Anchors differ per scene (Track1/3/aioponent right-anchored, Track2
    // centre-anchored), so the target is computed in the icon's OWN anchor
    // space — otherwise Track2's icon lands near the middle of the screen.
    private static void ShiftPauseButtonAside()
    {
        GameObject go = GameObject.Find("pause");
        if (go == null) go = GameObject.Find("PauseButton");
        if (go == null) return;

        RectTransform rt = go.transform as RectTransform;
        if (rt == null) return;

        // only point-anchored rects can use the anchor-space formula below
        if (Mathf.Abs(rt.anchorMin.x - rt.anchorMax.x) > 0.001f) return;

        float refWidth = 1920f;
        Canvas canvas = go.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) refWidth = scaler.referenceResolution.x;
        }

        // icon centre at 77.5% of the screen width; anchoredX = target - anchor * width
        float anchorFraction = (rt.anchorMin.x + rt.anchorMax.x) * 0.5f;
        float anchoredX = (0.775f - anchorFraction) * refWidth;

        // keep the vertical placement, only move it aside
        rt.anchoredPosition = new Vector2(anchoredX, rt.anchoredPosition.y);
    }

    // top-right, on the SAME row as the pause icon:
    //
    //     [pause] [ LAP  1/1  ]
    //             [ TIME 00:05.67 ]
    //
    // -20,-10 in this canvas's 1600x900 reference space puts the panel at
    // x 1300..1580 (81.3%..98.8% of the width), y 10..118. The pause icon lives
    // on the scene's own 1920x1080 canvas and used to sit flush in the corner
    // (centred at ~83.6% of the width), which is where this panel now goes —
    // ShiftPauseButtonAside moves the icon centre to 77.5% of the width (right
    // edge ~80.1%), leaving a gap before the panel starts at 81.3%.
    // The top-left corner is not an option: the scene already has the legacy
    // widgets sitting there.
    private void BuildLapTimePanel(Transform canvas)
    {
        RectTransform panel = Rect("LapTimePanel", canvas,
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(-20f, -10f), new Vector2(280f, 108f));

        Image bg = panel.gameObject.AddComponent<Image>();
        bg.sprite = RoundedSprite();
        bg.type = Image.Type.Sliced;
        bg.color = PanelBg;

        // top row = LAP , bottom row = TIME
        RectTransform lapRow = Row(panel, true);
        RectTransform timeRow = Row(panel, false);

        // thin divider between the rows
        RectTransform divider = Rect("Divider", panel,
            new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(0f, 2f));
        Image line = divider.gameObject.AddComponent<Image>();
        line.color = new Color(1f, 1f, 1f, 0.12f);

        AddText(RowLabel(lapRow), "LAP", 26f, LabelColor, TextAlignmentOptions.Left, FontStyles.Bold);
        lapValue = AddText(RowValue(lapRow), "1/1", 40f, ValueColor, TextAlignmentOptions.Right, FontStyles.Bold);

        AddText(RowLabel(timeRow), "TIME", 26f, LabelColor, TextAlignmentOptions.Left, FontStyles.Bold);
        timeValue = AddText(RowValue(timeRow), "00:00.00", 34f, ValueColor, TextAlignmentOptions.Right, FontStyles.Bold);
    }

    private static RectTransform Row(RectTransform panel, bool top)
    {
        RectTransform row = Rect(top ? "Row_LAP" : "Row_TIME", panel,
            new Vector2(0f, top ? 0.5f : 0f), new Vector2(1f, top ? 1f : 0.5f),
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        return row;
    }

    private static RectTransform RowLabel(RectTransform row)
    {
        RectTransform rt = Rect("Label", row,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform r = rt;
        r.offsetMin = new Vector2(22f, 0f);
        r.offsetMax = new Vector2(-100f, 0f);
        return rt;
    }

    private static RectTransform RowValue(RectTransform row)
    {
        RectTransform rt = Rect("Value", row,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        RectTransform r = rt;
        r.offsetMin = new Vector2(90f, 0f);
        r.offsetMax = new Vector2(-20f, 0f);
        return rt;
    }

    // bottom-right: speedometer face, needle, big number, KM/H, gear box
    private void BuildGauge(Transform canvas)
    {
        RectTransform gauge = Rect("Speedometer", canvas,
            new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-50f, 40f), new Vector2(300f, 300f));

        // face (drawn texture: dark dial + cyan/red arc + ticks)
        RectTransform face = Rect("Face", gauge,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image faceImg = face.gameObject.AddComponent<Image>();
        faceImg.sprite = GaugeSprite();
        faceImg.raycastTarget = false;

        // labels around the dial: 0 / 50 / 100 / 150 / 200
        foreach (int v in new[] { 0, 50, 100, 150, 200 })
        {
            float angle = (SweepStart + Sweep * (v / GaugeMax)) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
            RectTransform lbl = Rect("Tick_" + v, gauge,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                dir * 96f, new Vector2(70f, 40f));
            AddText(lbl, v.ToString(), 24f, new Color(0.85f, 0.89f, 0.94f, 1f),
                TextAlignmentOptions.Center, FontStyles.Normal);
        }

        // needle — pivot sits on the dial centre, rect points up
        RectTransform needleRT = Rect("Needle", gauge,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0f),
            Vector2.zero, new Vector2(7f, 118f));
        Image needleImg = needleRT.gameObject.AddComponent<Image>();
        needleImg.color = new Color(1f, 1f, 1f, 0.95f);
        needleImg.raycastTarget = false;
        needle = needleRT;

        // hub over the needle base
        RectTransform hub = Rect("Hub", gauge,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(30f, 30f));
        Image hubImg = hub.gameObject.AddComponent<Image>();
        hubImg.sprite = CircleSprite();
        hubImg.color = new Color(0.10f, 0.12f, 0.16f, 1f);
        hubImg.raycastTarget = false;

        // big number + KM/H
        RectTransform numRT = Rect("SpeedNumber", gauge,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 10f), new Vector2(260f, 120f));
        speedValue = AddText(numRT, "0", 84f, ValueColor, TextAlignmentOptions.Center, FontStyles.Bold);

        RectTransform kphRT = Rect("SpeedUnit", gauge,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, -56f), new Vector2(200f, 40f));
        AddText(kphRT, "KM/H", 26f, new Color(0.72f, 0.78f, 0.86f, 1f),
            TextAlignmentOptions.Center, FontStyles.Normal);

        // gear box, hanging bottom-right of the dial
        RectTransform gearRT = Rect("GearBox", gauge,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(112f, -122f), new Vector2(62f, 52f));
        Image gearBg = gearRT.gameObject.AddComponent<Image>();
        gearBg.sprite = RoundedSprite();
        gearBg.type = Image.Type.Sliced;
        gearBg.color = GearRed;
        gearBg.raycastTarget = false;

        RectTransform gearInner = Rect("GearValue", gearRT,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        gearValue = AddText(gearInner, "1", 34f, ValueColor, TextAlignmentOptions.Center, FontStyles.Bold);
    }

    // bottom-left: circular dark mini-map, styled like a car navigation screen.
    // The track path is baked from the scene's real data (checkpoints / a live
    // generator / TrackData) into a texture once it exists. At runtime the whole
    // map ROTATES with the car so the driving direction always points up, the
    // yellow arrow stays fixed at the disc centre and 'N' orbits the rim.
    private void BuildMiniMap(Transform canvas)
    {
        // map frame: round dark disc, 320x320, bottom-left of the screen.
        //
        // Pivot MUST be (0,0) here. anchoredPosition is measured from the pivot,
        // so a centred pivot would place the rect's MIDDLE at the corner and push
        // half the 320x320 disc off the bottom-left of the canvas. With a (0,0)
        // pivot the offset is the rect's bottom-left corner, so the whole disc
        // stays inside the screen at any resolution the CanvasScaler produces.
        // Children are anchored to (0.5,0.5) of this rect, so they stay centred.
        RectTransform map = Rect("MiniMap", canvas,
            new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(24f, 24f), new Vector2(320f, 320f));

        // dark grey circular backdrop (round outline via a slightly larger inner ring)
        RectTransform bg = Rect("MapBg", map,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(296f, 296f));
        Image bgImg = bg.gameObject.AddComponent<Image>();
        bgImg.sprite = CircleSprite();
        bgImg.color = MiniMapBg;
        bgImg.raycastTarget = false;

        // track path: starts as a plain grey disc (what you see before track data
        // is available) and is swapped for the real baked track outline by
        // TryBakeMap. 256x256 matches the baked texture 1:1 so texture px == rect
        // units and the car's map position lines up exactly with the drawn path.
        RectTransform track = Rect("TrackPath", map,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(256f, 256f));
        trackImg = track.gameObject.AddComponent<Image>();
        trackImg.sprite = CircleSprite();
        trackImg.color = MiniMapTrack;
        trackImg.raycastTarget = false;

        // player arrowhead — real arrowhead sprite, pinned to the disc centre and
        // always pointing up; the map rotates underneath it (navigation style)
        RectTransform arrow = Rect("MapArrow", map,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            Vector2.zero, new Vector2(40f, 54f));
        Image arrowImg = arrow.gameObject.AddComponent<Image>();
        arrowImg.sprite = ArrowSprite();
        arrowImg.color = MiniMapArrow;
        arrowImg.raycastTarget = false;
        mapArrow = arrow;

        // 'N' compass marker — starts at the TOP of the disc (world +Z) and is
        // re-orbited every frame by UpdateMiniMap as the map rotates. It used to
        // sit at (0,-6), i.e. dead centre, covering the player arrow.
        RectTransform north = Rect("MapNorth", map,
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(0f, 122f), new Vector2(52f, 40f));
        mapNorth = AddText(north, "N", 22f, MiniMapN, TextAlignmentOptions.Center, FontStyles.Bold);
    }

    // Navigation-style update: the map ROTATES with the car so the driving
    // direction always points up, the yellow arrow stays fixed at the disc
    // centre and the 'N' compass orbits the rim — like a real car nav screen.
    private void UpdateMiniMap()
    {
        if (mapArrow == null) return;

        // TrackData builds in its own Awake during the scene load and the HUD is
        // created from SceneManager.sceneLoaded — usually after, but retry for a
        // while to be safe (and to catch generators that run a frame later).
        if (!mapBaked && mapAttempts < 40)
        {
            mapRetryTimer -= Time.deltaTime;
            if (mapRetryTimer <= 0f)
            {
                mapRetryTimer = 0.5f;
                mapAttempts++;
                TryBakeMap();
            }
        }

        if (car == null) return;

        // heading: clockwise angle of the car's forward from world +Z (map up)
        float theta = Mathf.Atan2(car.transform.forward.x, car.transform.forward.z) * Mathf.Rad2Deg;
        float rad = theta * Mathf.Deg2Rad;
        float cos = Mathf.Cos(rad);
        float sin = Mathf.Sin(rad);

        if (mapBaked)
        {
            // car position in unrotated map units (relative to the disc centre)
            Vector2 p = new Vector2(
                (car.transform.position.x - mapCenter.x) * mapScale,
                (car.transform.position.z - mapCenter.y) * mapScale);

            // Rotate the track CCW by theta so the car's heading points up, and
            // shift it so the car lands exactly under the centre arrow:
            // screen(q) = R * (q - p)  =>  rotation = R, position = -R * p.
            trackImg.rectTransform.localEulerAngles = new Vector3(0f, 0f, theta);
            trackImg.rectTransform.anchoredPosition = new Vector2(
                -(p.x * cos - p.y * sin),
                -(p.x * sin + p.y * cos));
        }

        // compass: world north (+Z) orbits the rim, glyph tilts to point at it
        if (mapNorth != null)
        {
            mapNorth.rectTransform.anchoredPosition = new Vector2(-sin, cos) * 122f;
            mapNorth.rectTransform.localEulerAngles = new Vector3(0f, 0f, theta);
        }

        // the car arrow never moves or rotates — the world turns around it
        mapArrow.anchoredPosition = Vector2.zero;
        mapArrow.localEulerAngles = Vector3.zero;
    }

    // Hide the race widgets (minimap / speedometer / lap-time panel) while a
    // results panel is on screen — they only make sense during the race. The
    // results panels live on always-active manager objects, so finding them is
    // cheap and they are there from the first frame.
    private void UpdateResultsVisibility()
    {
        if (resultsPanel == null)
            resultsPanel = FindFirstObjectByType<ResultsPanel>();
        if (singleFinishPanel == null)
            singleFinishPanel = FindFirstObjectByType<SinglePlayerFinishPanel>();

        bool resultsUp = (resultsPanel != null && resultsPanel.IsShown)
                      || (singleFinishPanel != null && singleFinishPanel.IsShown);

        bool desired = !resultsUp;
        if (hudCanvas != null && hudCanvas.activeSelf != desired)
            hudCanvas.SetActive(desired);
    }

    // ----------------------------------------------------------- mini-map path

    // Track centre-line in world space, best source first:
    // TrackData -> active checkpoints of THIS scene -> a live generator in this
    // scene -> the generator's static path (scene-checked).
    private static List<Vector3> CollectTrackPath()
    {
        // 1. TrackData centre-line (most complete source when a scene has it)
        if (TrackData.Instance != null && TrackData.Instance.points.Count >= 3)
        {
            List<Vector3> pts = new List<Vector3>(TrackData.Instance.points.Count);
            foreach (TrackData.TrackPoint tp in TrackData.Instance.points)
                pts.Add(tp.center);
            return pts;
        }

        // 2. Active checkpoints of the loaded scene — this is what Track1/2/3
        //    use. FindObjectsByType skips inactive objects, so aioponent (which
        //    disables its old checkpoints) falls through to the generator below.
        RaceCheckpoint[] cps = FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None);
        List<RaceCheckpoint> sorted = new List<RaceCheckpoint>();
        foreach (RaceCheckpoint cp in cps)
            if (!cp.isFinishLine) sorted.Add(cp);
        if (sorted.Count >= 3)
        {
            sorted.Sort((a, b) => a.checkpointIndex.CompareTo(b.checkpointIndex));

            // duplicate indices (stacked copies exist in Track2) would make the
            // line zigzag — keep the first entry of each index
            List<Vector3> cpPts = new List<Vector3>();
            int lastIdx = int.MinValue;
            foreach (RaceCheckpoint cp in sorted)
            {
                if (cp.checkpointIndex == lastIdx) continue;
                lastIdx = cp.checkpointIndex;
                cpPts.Add(cp.transform.position);
            }

            // sparse checkpoints -> smooth the polygon into a curved circuit
            if (cpPts.Count >= 3)
                return SmoothClosedPath(cpPts, 8);
        }

        // 3. a live generator in THIS scene (aioponent creates one at runtime)
        SimpleTrackGenerator gen = FindFirstObjectByType<SimpleTrackGenerator>();
        if (gen != null)
        {
            List<Vector3> genPts = gen.GetPathPoints();
            if (genPts.Count >= 3) return genPts;
        }

        // 4. the generator's static path — ONLY if it was generated in this
        //    scene. The static survives scene loads, and without this guard the
        //    AI scene's oval track got stamped onto every other track, which is
        //    why all of them showed the same circle.
        if (SimpleTrackGenerator.LastGeneratedPath != null
            && SimpleTrackGenerator.LastGeneratedPath.Count >= 3
            && SimpleTrackGenerator.LastGeneratedScene == SceneManager.GetActiveScene().name)
            return new List<Vector3>(SimpleTrackGenerator.LastGeneratedPath);

        return null;
    }

    // Closed-loop Catmull-Rom resample so a coarse checkpoint polygon becomes a
    // smooth, navigation-looking circuit.
    private static List<Vector3> SmoothClosedPath(List<Vector3> pts, int stepsPerSegment)
    {
        int n = pts.Count;
        List<Vector3> outPts = new List<Vector3>(n * stepsPerSegment);
        for (int i = 0; i < n; i++)
        {
            Vector3 p0 = pts[(i - 1 + n) % n];
            Vector3 p1 = pts[i];
            Vector3 p2 = pts[(i + 1) % n];
            Vector3 p3 = pts[(i + 2) % n];

            for (int s = 0; s < stepsPerSegment; s++)
            {
                float t = s / (float)stepsPerSegment;
                float t2 = t * t;
                float t3 = t2 * t;
                outPts.Add(0.5f * (2f * p1
                    + (-p0 + p2) * t
                    + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2
                    + (-p0 + 3f * p1 - 3f * p2 + p3) * t3));
            }
        }
        return outPts;
    }

    // Fit the track around its bounding-box centre and bake it once.
    // Returns silently while no track data exists yet — the grey placeholder
    // disc stays visible and UpdateMiniMap keeps retrying.
    private void TryBakeMap()
    {
        List<Vector3> path = CollectTrackPath();
        if (path == null) return;

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (Vector3 p in path)
        {
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.z < minZ) minZ = p.z;
            if (p.z > maxZ) maxZ = p.z;
        }

        Vector3 centre = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);

        // farthest point from the centre in world units
        float maxRad = 0f;
        foreach (Vector3 p in path)
        {
            float dx = p.x - centre.x;
            float dz = p.z - centre.z;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            if (d > maxRad) maxRad = d;
        }
        if (maxRad < 1f) return;

        mapCenter = new Vector2(centre.x, centre.z);
        mapScale = MapRadiusPx / maxRad;

        Sprite sprite = BakeTrackSprite(path);
        if (sprite == null) return;

        trackImg.sprite = sprite;
        trackImg.color = Color.white;   // texture is already coloured
        mapBaked = true;
    }

    // Draw the centre-line into a 512x512 texture (displayed inside a 256 rect,
    // so the GPU downsamples it — effectively free anti-aliasing). The route is
    // drawn navigation-style: dark casing underneath, bright blue route on top,
    // white start/finish dot last. Same fit as the arrow: +Z = up.
    private Sprite BakeTrackSprite(List<Vector3> path)
    {
        const int size = 512;
        const float baseHalf = 128f;   // centre in 256-space
        float k = size / 256f;         // 256-space -> texture-space scale (2x)

        Color32[] px = new Color32[size * size];   // all transparent

        int n = path.Count;
        Vector2[] texPts = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            texPts[i] = new Vector2(
                (path[i].x - mapCenter.x) * mapScale + baseHalf,
                (path[i].z - mapCenter.y) * mapScale + baseHalf) * k;
        }

        DrawPath(px, size, texPts, 6f * k, MiniMapCasing);     // road outline
        DrawPath(px, size, texPts, 3.5f * k, MiniMapLine);     // route line
        StampDisc(px, size, texPts[0], 6.5f * k, MiniMapStart); // start/finish

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels32(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
    }

    // stroke the closed polyline by stamping discs along every segment
    private static void DrawPath(Color32[] px, int size, Vector2[] pts, float radius, Color32 col)
    {
        int n = pts.Length;
        for (int i = 0; i < n; i++)
        {
            Vector2 a = pts[i];
            Vector2 b = pts[(i + 1) % n];

            // close the loop, unless the source list already ends on its start
            if (i == n - 1 && Vector2.Distance(a, b) < 1f) break;

            int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(a, b)));
            for (int s = 0; s <= steps; s++)
                StampDisc(px, size, Vector2.Lerp(a, b, s / (float)steps), radius, col);
        }
    }

    private static void StampDisc(Color32[] px, int size, Vector2 centre, float radius, Color32 col)
    {
        int r = Mathf.CeilToInt(radius);
        int r2 = Mathf.RoundToInt(radius * radius);
        int cx = Mathf.RoundToInt(centre.x);
        int cy = Mathf.RoundToInt(centre.y);

        for (int dy = -r; dy <= r; dy++)
        {
            int y = cy + dy;
            if (y < 0 || y >= size) continue;
            for (int dx = -r; dx <= r; dx++)
            {
                if (dx * dx + dy * dy > r2) continue;
                int x = cx + dx;
                if (x < 0 || x >= size) continue;
                px[y * size + x] = col;
            }
        }
    }

    // ---------------------------------------------------------------- helpers

    private static RectTransform Rect(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        RectTransform rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return rt;
    }

    private static TextMeshProUGUI AddText(RectTransform rt, string text, float fontSize,
        Color color, TextAlignmentOptions align, FontStyles style)
    {
        TextMeshProUGUI tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = align;
        tmp.fontStyle = style;
        tmp.raycastTarget = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    // arrowhead sprite: classic map pointer (triangle with a notched back),
    // pointing UP, edges smoothed with 4x4 supersampling.
    private static Sprite ArrowSprite()
    {
        if (arrowSprite != null) return arrowSprite;

        const int w = 64;
        const int h = 80;
        Color32[] px = new Color32[w * h];

        // normalised shape (y up): tip, bottom-left, notch, bottom-right
        Vector2 tip = new Vector2(0.50f, 0.97f);
        Vector2 left = new Vector2(0.05f, 0.05f);
        Vector2 notch = new Vector2(0.50f, 0.36f);
        Vector2 right = new Vector2(0.95f, 0.05f);

        const int ss = 4;
        const int hitsMax = ss * ss;

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int hits = 0;
                for (int sy = 0; sy < ss; sy++)
                    for (int sx = 0; sx < ss; sx++)
                    {
                        Vector2 p = new Vector2(
                            (x + (sx + 0.5f) / ss) / w,
                            (y + (sy + 0.5f) / ss) / h);

                        // arrowhead = big triangle MINUS the notch triangle
                        if (InTriangle(p, tip, left, right) && !InTriangle(p, left, notch, right))
                            hits++;
                    }

                byte a = (byte)(255 * hits / hitsMax);
                px[y * w + x] = new Color32(255, 255, 255, a);
            }
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels32(px);
        tex.Apply();

        arrowSprite = Sprite.Create(tex, new Rect(0f, 0f, w, h), new Vector2(0.5f, 0.5f));
        return arrowSprite;
    }

    private static bool InTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);
        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNeg && hasPos);
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
    }

    // ---------------------------------------------------------------- sprites

    private static Sprite RoundedSprite()
    {
        if (roundedSprite != null) return roundedSprite;

        const int size = 64;
        const int radius = 18;
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 clear = new Color32(255, 255, 255, 0);
        Color32[] px = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int dx = Mathf.Min(x, size - 1 - x);
                int dy = Mathf.Min(y, size - 1 - y);
                bool inside;
                if (dx >= radius || dy >= radius)
                {
                    inside = true;
                }
                else
                {
                    float ox = radius - dx;
                    float oy = radius - dy;
                    inside = ox * ox + oy * oy <= radius * radius;
                }
                px[y * size + x] = inside ? white : clear;
            }
        }

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels32(px);
        tex.Apply();

        roundedSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
            100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        return roundedSprite;
    }

    private static Sprite CircleSprite()
    {
        if (circleSprite != null) return circleSprite;

        const int size = 64;
        Color32 white = new Color32(255, 255, 255, 255);
        Color32 clear = new Color32(255, 255, 255, 0);
        Color32[] px = new Color32[size * size];
        float c = (size - 1) * 0.5f;
        float r = size * 0.5f - 1f;

        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                px[y * size + x] = d <= r ? white : clear;
            }

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels32(px);
        tex.Apply();

        circleSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return circleSprite;
    }

    // dial face: dark disc + cyan->red arc across the sweep + tick marks
    private static Sprite GaugeSprite()
    {
        if (gaugeSprite != null) return gaugeSprite;

        const int size = 512;
        float c = (size - 1) * 0.5f;
        float rout = size * 0.485f;
        float discR = rout * 0.86f;

        Color32 disc = new Color32(14, 18, 25, 238);
        Color32 cyan = new Color32(40, 196, 255, 255);
        Color32 red = new Color32(255, 59, 48, 255);
        Color32 tickDim = new Color32(150, 165, 185, 200);
        Color32 tickBright = new Color32(235, 240, 248, 255);

        Color32[] px = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - c;
                float dy = y - c;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                Color32 col = new Color32(0, 0, 0, 0);

                if (r <= discR)
                {
                    col = disc;
                }
                else if (r <= rout)
                {
                    float ang = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;   // clockwise from up
                    if (ang >= SweepStart && ang <= SweepStart + Sweep)
                    {
                        float t = (ang - SweepStart) / Sweep;
                        col = Color32Lerp(cyan, red, Mathf.SmoothStep(0.74f, 0.88f, t));
                    }
                }

                px[y * size + x] = col;
            }
        }

        // tick marks: every 10 km/h, brighter and longer every 50
        for (int v = 0; v <= (int)GaugeMax; v += 10)
        {
            bool major = v % 50 == 0;
            float ang = (SweepStart + Sweep * (v / GaugeMax)) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Sin(ang), Mathf.Cos(ang));
            float r0 = rout * (major ? 0.63f : 0.68f);
            float r1 = rout * 0.83f;
            Color32 col = major ? tickBright : tickDim;

            const int steps = 48;
            for (int s = 0; s <= steps; s++)
            {
                float rr = Mathf.Lerp(r0, r1, s / (float)steps);
                int px2 = Mathf.RoundToInt(c + dir.x * rr);
                int py2 = Mathf.RoundToInt(c + dir.y * rr);
                for (int oy = -1; oy <= 1; oy++)
                    for (int ox = -1; ox <= 1; ox++)
                    {
                        int xx = px2 + ox;
                        int yy = py2 + oy;
                        if (xx < 0 || yy < 0 || xx >= size || yy >= size) continue;
                        px[yy * size + xx] = col;
                    }
            }
        }

        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels32(px);
        tex.Apply();

        gaugeSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f));
        return gaugeSprite;
    }

    private static Color32 Color32Lerp(Color32 a, Color32 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Color32(
            (byte)(a.r + (b.r - a.r) * t),
            (byte)(a.g + (b.g - a.g) * t),
            (byte)(a.b + (b.b - a.b) * t),
            (byte)(a.a + (b.a - a.a) * t));
    }
}
