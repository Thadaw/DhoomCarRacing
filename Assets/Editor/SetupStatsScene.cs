using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;

public static class SetupStatsScene
{
    [MenuItem("Tools/Setup Stats Scene")]
    public static void Setup()
    {
        // 1. Add stats.unity to build settings if not already there
        string statsScenePath = "Assets/Scenes/stats.unity";
        bool foundInBuild = false;
        var buildScenes = EditorBuildSettings.scenes;
        foreach (var scene in buildScenes)
        {
            if (scene.path == statsScenePath)
            {
                foundInBuild = true;
                break;
            }
        }

        if (!foundInBuild)
        {
            var newScenes = new EditorBuildSettingsScene[buildScenes.Length + 1];
            System.Array.Copy(buildScenes, newScenes, buildScenes.Length);
            newScenes[newScenes.Length - 1] = new EditorBuildSettingsScene(statsScenePath, true);
            EditorBuildSettings.scenes = newScenes;
            Debug.Log($"Added {statsScenePath} to Build Settings.");
        }
        else
        {
            Debug.Log($"{statsScenePath} is already in Build Settings.");
        }

        // 2. Open the stats scene and set it up
        Scene statsScene = EditorSceneManager.OpenScene(statsScenePath, OpenSceneMode.Additive);

        // Find the Canvas
        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
        {
            Debug.LogError("Stats scene has no Canvas! Create a Canvas first.");
            return;
        }

        // Create SceneSwitcher GameObject if it doesn't exist
        GameObject sceneSwitcherGO = GameObject.Find("SceneSwiitcher");
        if (sceneSwitcherGO == null)
        {
            sceneSwitcherGO = new GameObject("SceneSwiitcher");
            sceneSwitcherGO.transform.position = Vector3.zero;
        }

        // Ensure SceneSwitcher component exists
        SceneSwitcher sw = sceneSwitcherGO.GetComponent<SceneSwitcher>();
        if (sw == null)
            sw = sceneSwitcherGO.AddComponent<SceneSwitcher>();

        // Create or find FadeImage
        GameObject fadeImage = GameObject.Find("FadeImage");
        if (fadeImage == null)
        {
            fadeImage = new GameObject("FadeImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image), typeof(CanvasGroup));
            fadeImage.transform.SetParent(canvas.transform, false);
            RectTransform rt = fadeImage.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            UnityEngine.UI.Image img = fadeImage.GetComponent<UnityEngine.UI.Image>();
            img.color = new Color(0.07f, 0.07f, 0.07f, 1f);
            img.raycastTarget = false;

            CanvasGroup cg = fadeImage.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        // Wire SceneSwitcher to FadeImage
        CanvasGroup fadeCG = fadeImage.GetComponent<CanvasGroup>();
        SerializedObject so = new SerializedObject(sw);
        so.FindProperty("canvasGroup").objectReferenceValue = fadeCG;
        so.FindProperty("speed").floatValue = 1f;
        so.ApplyModifiedProperties();

        // Create StatsController GameObject if it doesn't exist
        GameObject statsCtrlGO = GameObject.Find("StatsController");
        if (statsCtrlGO == null)
        {
            statsCtrlGO = new GameObject("StatsController");
            statsCtrlGO.transform.position = Vector3.zero;
        }

        // Ensure StatsController component exists
        StatsController sc = statsCtrlGO.GetComponent<StatsController>();
        if (sc == null)
            sc = statsCtrlGO.AddComponent<StatsController>();

        // Wire the SceneSwitcher reference
        SerializedObject scSo = new SerializedObject(sc);
        scSo.FindProperty("sceneSwitcher").objectReferenceValue = sw;
        scSo.ApplyModifiedProperties();

        // Mark scene as dirty and save
        EditorSceneManager.MarkSceneDirty(statsScene);
        EditorSceneManager.SaveScene(statsScene);
        EditorSceneManager.CloseScene(statsScene, true);

        Debug.Log("Stats scene setup complete!");
    }
}
