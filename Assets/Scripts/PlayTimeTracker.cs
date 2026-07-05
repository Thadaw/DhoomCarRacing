using UnityEngine;

public class PlayTimeTracker : MonoBehaviour
{
    private static PlayTimeTracker _instance;

    public static PlayTimeTracker Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("PlayTimeTracker");
                _instance = go.AddComponent<PlayTimeTracker>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    public static void EnsureExists()
    {
        if (_instance == null)
        {
            var go = new GameObject("PlayTimeTracker");
            _instance = go.AddComponent<PlayTimeTracker>();
            DontDestroyOnLoad(go);
        }
    }

    public bool IsTracking { get; private set; } = false;

    private float accumulatedTime = 0f;
    private const string PrefKey = "TotalPlayTime";

    public static float TotalPlayTime
    {
        get => PlayerPrefs.GetFloat(PrefKey, 0f);
        set => PlayerPrefs.SetFloat(PrefKey, value);
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        accumulatedTime = 0f;
    }

    private void Update()
    {
        if (IsTracking)
        {
            accumulatedTime += Time.deltaTime;
        }
    }

    public void StartTracking()
    {
        if (!IsTracking)
        {
            IsTracking = true;
            accumulatedTime = 0f;
            Debug.Log("PlayTimeTracker: Started tracking.");
        }
    }

    public void StopTracking()
    {
        if (IsTracking)
        {
            IsTracking = false;
            float currentTotal = TotalPlayTime;
            currentTotal += accumulatedTime;
            TotalPlayTime = currentTotal;
            PlayerPrefs.Save();
            Debug.Log($"PlayTimeTracker: Stopped tracking. Added {accumulatedTime:F2}s. Total: {currentTotal:F2}s");
            accumulatedTime = 0f;
        }
    }

    private void OnDestroy()
    {
        if (IsTracking)
        {
            StopTracking();
        }
    }

    private void OnApplicationQuit()
    {
        if (IsTracking)
        {
            float currentTotal = TotalPlayTime;
            currentTotal += accumulatedTime;
            TotalPlayTime = currentTotal;
            PlayerPrefs.Save();
        }
    }
}
