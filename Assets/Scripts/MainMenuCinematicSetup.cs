using UnityEngine;
using UnityEngine.UI;

public class BackgroundZoom : MonoBehaviour
{
    private Vector3 baseScale;
    private float time = 0f;

    void Start()
    {
        baseScale = transform.localScale;
    }

    void Update()
    {
        time += Time.deltaTime;
        float zoom = 1f + Mathf.Sin(time * 0.3f * Mathf.PI * 2f) * 0.05f;
        transform.localScale = baseScale * zoom;
    }
}

public class MainMenuCinematicSetup : MonoBehaviour
{
    void Awake()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        if (cam.GetComponent<Animator>() != null)
            cam.GetComponent<Animator>().enabled = false;

        CanvasGroup fadeGroup = FindFirstObjectByType<CanvasGroup>();
        if (fadeGroup != null) fadeGroup.alpha = 0f;

        Canvas originalCanvas = FindFirstObjectByType<Canvas>();

        GameObject bgCanvasGO = new GameObject("BackgroundCanvas");
        Canvas bgCanvas = bgCanvasGO.AddComponent<Canvas>();
        bgCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        bgCanvas.worldCamera = cam;
        bgCanvas.planeDistance = 20f;
        bgCanvasGO.AddComponent<CanvasScaler>();
        bgCanvasGO.AddComponent<GraphicRaycaster>();
        bgCanvasGO.AddComponent<BackgroundZoom>();

        if (originalCanvas != null)
        {
            Transform bgChild = FindDeep(originalCanvas.transform, "Background");
            if (bgChild != null)
            {
                bgChild.SetParent(bgCanvasGO.transform, false);
                RectTransform bgRT = bgChild.GetComponent<RectTransform>();
                if (bgRT != null)
                {
                    bgRT.anchorMin = Vector2.zero;
                    bgRT.anchorMax = Vector2.one;
                    bgRT.sizeDelta = Vector2.zero;
                    bgRT.anchoredPosition = Vector2.zero;
                }
            }
        }

        originalCanvas.renderMode = RenderMode.ScreenSpaceCamera;
        originalCanvas.worldCamera = cam;
        originalCanvas.planeDistance = 2f;

        GameObject carPrefab = Resources.Load<GameObject>("Car7");
        Vector3 carPos = new Vector3(4f, 0.5f, 8f);
        if (carPrefab != null)
        {
            GameObject car = Instantiate(carPrefab, carPos, Quaternion.Euler(0, 180, 0));
            foreach (var r in car.GetComponentsInChildren<Rigidbody>())
                Destroy(r);
            foreach (var c in car.GetComponentsInChildren<Collider>())
                Destroy(c);
            foreach (var p in car.GetComponentsInChildren<ParticleSystem>())
                p.Stop();
        }

        GameObject skeletonPrefab = Resources.Load<GameObject>("Skeleton");
        if (skeletonPrefab != null)
        {
            GameObject skeleton = Instantiate(skeletonPrefab, carPos + new Vector3(2f, 0, 0), Quaternion.Euler(0, 180, 0));
            skeleton.transform.localScale = Vector3.one * 0.01f;
            foreach (var r in skeleton.GetComponentsInChildren<Rigidbody>())
                Destroy(r);
            foreach (var c in skeleton.GetComponentsInChildren<Collider>())
                Destroy(c);
            foreach (var mr in skeleton.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.3f, 0.3f);
                mr.material = mat;
            }
            foreach (var mr in skeleton.GetComponentsInChildren<MeshRenderer>())
            {
                Material mat = new Material(Shader.Find("Standard"));
                mat.color = new Color(0.3f, 0.3f, 0.3f);
                mr.material = mat;
            }
        }

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.transform.position = new Vector3(4f, 0, 8f);
        ground.transform.localScale = new Vector3(8, 1, 8);
        Material groundMat = new Material(Shader.Find("Standard"));
        groundMat.color = new Color(0.15f, 0.15f, 0.18f);
        groundMat.SetFloat("_Metallic", 0.8f);
        groundMat.SetFloat("_Glossiness", 0.9f);
        ground.GetComponent<Renderer>().material = groundMat;
        Destroy(ground.GetComponent<Collider>());

        if (FindFirstObjectByType<Light>() == null)
        {
            GameObject lightGO = new GameObject("MenuLight");
            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.color = new Color(1f, 0.95f, 0.9f);
            lightGO.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        cam.transform.position = new Vector3(0, 2.5f, -1);
        cam.fieldOfView = 60;

        MainMenuCameraOrbit orbit = cam.gameObject.AddComponent<MainMenuCameraOrbit>();
        orbit.orbitCenter = carPos + Vector3.up * 0.8f;
        orbit.distance = 7f;
        orbit.height = 2f;
        orbit.orbitSpeed = 1.5f;
        orbit.heightAmplitude = 0.2f;
        orbit.heightFrequency = 0.1f;
        orbit.lookAtHeightOffset = 1f;
        orbit.tiltAngle = 0f;
    }

    Transform FindDeep(Transform parent, string name)
    {
        if (parent == null) return null;
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            Transform result = FindDeep(child, name);
            if (result != null) return result;
        }
        return null;
    }
}
