using UnityEngine;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class LeaderboardEntry
{
    public string playerName;
    public string trackId;
    public float finishTime;
    public string userId;
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private FirebaseFirestore db;
    private bool ready = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (Instance != null) return;
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void EnsureExists()
    {
        if (Instance == null)
        {
            GameObject go = new GameObject("LeaderboardManager");
            Instance = go.AddComponent<LeaderboardManager>();
        }
    }

    private void Start()
    {
        FirebaseManager.EnsureExists();
        if (FirebaseManager.Instance != null && FirebaseManager.Instance.Initialized)
            InitDb();
    }

    private void InitDb()
    {
        db = FirebaseFirestore.DefaultInstance;
        ready = true;
    }

    private bool EnsureReady()
    {
        if (!ready)
        {
            if (FirebaseManager.Instance != null && FirebaseManager.Instance.Initialized)
                InitDb();
        }
        return ready;
    }

    public async void SubmitTime(string playerName, string trackId, float finishTime)
    {
        try
        {
            if (!EnsureReady())
            {
                Debug.LogWarning("LeaderboardManager: Firebase not ready, skipping submit.");
                return;
            }

            Dictionary<string, object> data = new Dictionary<string, object>
            {
                { "playerName", playerName },
                { "trackId", trackId },
                { "finishTime", finishTime },
                { "userId", FirebaseManager.Instance.UserId },
                { "timestamp", Timestamp.GetCurrentTimestamp() }
            };

            await db.Collection("leaderboards").AddAsync(data);
            Debug.Log($"LeaderboardManager: Submitted time {finishTime:F2} for {playerName} on {trackId}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"LeaderboardManager: SubmitTime failed: {ex.Message}");
        }
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboard(string trackId, int limit = 20)
    {
        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        if (!EnsureReady())
        {
            Debug.LogWarning("LeaderboardManager: Firebase not ready, returning empty.");
            return entries;
        }

        try
        {
            Query query = db.Collection("leaderboards")
                .WhereEqualTo("trackId", trackId)
                .OrderBy("finishTime")
                .Limit(limit);

            QuerySnapshot snapshot = await query.GetSnapshotAsync();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                if (doc.Exists)
                {
                    Dictionary<string, object> data = doc.ToDictionary();
                    LeaderboardEntry entry = new LeaderboardEntry
                    {
                        playerName = data.ContainsKey("playerName") ? data["playerName"].ToString() : "Unknown",
                        trackId = data.ContainsKey("trackId") ? data["trackId"].ToString() : trackId,
                        finishTime = data.ContainsKey("finishTime") ? System.Convert.ToSingle(data["finishTime"]) : 0f,
                        userId = data.ContainsKey("userId") ? data["userId"].ToString() : ""
                    };
                    entries.Add(entry);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"LeaderboardManager: GetLeaderboard failed: {ex.Message}");
        }

        return entries;
    }
}
