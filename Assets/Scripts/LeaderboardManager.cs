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
        Debug.Log("LeaderboardManager: Firestore DB ready.");
    }

    public async Task WaitForReadyAsync(float timeout = 10f)
    {
        float elapsed = 0f;
        while (!ready && elapsed < timeout)
        {
            if (FirebaseManager.Instance != null && FirebaseManager.Instance.Initialized && !ready)
                InitDb();
            if (!ready)
            {
                await Task.Delay(200);
                elapsed += 0.2f;
            }
        }
        Debug.Log($"LeaderboardManager: WaitForReady completed. ready={ready}");
    }

    public async void SubmitTime(string playerName, string trackId, float finishTime)
    {
        try
        {
            await WaitForReadyAsync();
            if (!ready)
            {
                Debug.LogWarning("LeaderboardManager: Firebase not ready after wait, skipping submit.");
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

    private LeaderboardEntry ParseEntry(DocumentSnapshot doc, string fallbackTrackId)
    {
        if (!doc.Exists) return null;
        try
        {
            Dictionary<string, object> data = doc.ToDictionary();
            if (data == null || data.Count == 0) return null;

            string pName = "Unknown";
            if (data.ContainsKey("playerName") && data["playerName"] != null)
                pName = data["playerName"].ToString();

            string tId = fallbackTrackId;
            if (data.ContainsKey("trackId") && data["trackId"] != null)
                tId = data["trackId"].ToString();

            float fTime = 0f;
            if (data.ContainsKey("finishTime") && data["finishTime"] != null)
                fTime = System.Convert.ToSingle(data["finishTime"]);

            string uId = "";
            if (data.ContainsKey("userId") && data["userId"] != null)
                uId = data["userId"].ToString();

            if (fTime <= 0f) return null;

            return new LeaderboardEntry
            {
                playerName = pName,
                trackId = tId,
                finishTime = fTime,
                userId = uId
            };
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"LeaderboardManager: ParseEntry failed for doc {doc.Id}: {ex.Message}");
            return null;
        }
    }

    public async Task<List<LeaderboardEntry>> GetLeaderboard(string trackId, int limit = 20)
    {
        await WaitForReadyAsync();

        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        if (!ready)
        {
            Debug.LogWarning("LeaderboardManager: Firebase not ready, returning empty.");
            return entries;
        }

        try
        {
            QuerySnapshot snapshot = await db.Collection("leaderboards")
                .WhereEqualTo("trackId", trackId)
                .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                LeaderboardEntry entry = ParseEntry(doc, trackId);
                if (entry != null) entries.Add(entry);
            }

            entries.Sort((a, b) => a.finishTime.CompareTo(b.finishTime));
            if (entries.Count > limit)
                entries.RemoveRange(limit, entries.Count - limit);

            Debug.Log($"LeaderboardManager: Got {entries.Count} entries for track '{trackId}'");
        }
        catch (Exception ex)
        {
            Debug.LogError($"LeaderboardManager: GetLeaderboard failed: {ex.Message}\n{ex.StackTrace}");
        }

        return entries;
    }

    public async Task<List<LeaderboardEntry>> GetAllLeaderboard(int limit = 50)
    {
        await WaitForReadyAsync();

        List<LeaderboardEntry> entries = new List<LeaderboardEntry>();

        if (!ready)
        {
            Debug.LogWarning("LeaderboardManager: Firebase not ready, returning empty.");
            return entries;
        }

        try
        {
            QuerySnapshot snapshot = await db.Collection("leaderboards")
                .GetSnapshotAsync();

            foreach (DocumentSnapshot doc in snapshot.Documents)
            {
                LeaderboardEntry entry = ParseEntry(doc, "Unknown");
                if (entry != null) entries.Add(entry);
            }

            entries.Sort((a, b) => a.finishTime.CompareTo(b.finishTime));
            if (entries.Count > limit)
                entries.RemoveRange(limit, entries.Count - limit);

            Debug.Log($"LeaderboardManager: Got {entries.Count} total entries across all tracks");
        }
        catch (Exception ex)
        {
            Debug.LogError($"LeaderboardManager: GetAllLeaderboard failed: {ex.Message}\n{ex.StackTrace}");
        }

        return entries;
    }
}
