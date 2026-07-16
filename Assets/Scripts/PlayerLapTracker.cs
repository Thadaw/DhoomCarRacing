using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class PlayerLapTracker : MonoBehaviour
{
    public static System.Action OnLocalPlayerFinished;

    [Header("Lap Settings")]
    public int totalLaps = 3;
    public int totalCheckpoints = 3;

    [Header("Current Progress")]
    public int currentLap = 1;
    public int nextCheckpointIndex = 0;

    [Header("UI")]
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI checkpointText;

    [Header("Timing")]
    public float finishTime;
    public List<float> lapTimes = new List<float>();

    [Header("Speed Tracking")]
    public float topSpeed;
    public float averageSpeed;
    public List<float> speedSamples = new List<float>();

    private bool raceCompleted = false;
    private float raceStartTime = -1f;
    private float lapStartTime = -1f;
    private PhotonCarController cachedCarController;

    private void Start()
    {
        AutoDetectCheckpoints();
        ApplyLapSettings();
        UpdateUI();
    }

    private void AutoDetectCheckpoints()
    {
        RaceCheckpoint[] allCheckpoints = FindObjectsByType<RaceCheckpoint>(FindObjectsSortMode.None);
        int count = 0;
        foreach (RaceCheckpoint cp in allCheckpoints)
        {
            if (!cp.isFinishLine)
                count++;
        }
        if (count > 0)
            totalCheckpoints = count;

        Debug.Log("Auto-detected " + totalCheckpoints + " checkpoints in scene.");
    }

    private void ApplyLapSettings()
    {
        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("TotalLaps", out object laps))
            {
                totalLaps = (int)laps;
                Debug.Log("Applied TotalLaps from Photon: " + totalLaps);
            }
        }
        else
        {
            if (GameSession.Instance != null)
            {
                totalLaps = GameSession.Instance.TotalLaps;
                Debug.Log("Applied TotalLaps from GameSession: " + totalLaps);
            }
        }
    }

    private void Update()
    {
        if (raceStartTime < 0f && RaceManager.Instance != null && RaceManager.Instance.raceStarted)
        {
            raceStartTime = Time.time;
            lapStartTime = Time.time;

            PlayTimeTracker.EnsureExists();
            PlayTimeTracker.Instance.StartTracking();
        }

        if (raceStartTime > 0f && !raceCompleted)
        {
            TrackSpeed();
        }
    }

    private void TrackSpeed()
    {
        if (cachedCarController == null)
            cachedCarController = GetComponentInParent<PhotonCarController>();

        if (cachedCarController == null)
            return;

        float currentSpeed = cachedCarController.CarSpeed();
        speedSamples.Add(currentSpeed);

        if (currentSpeed > topSpeed)
            topSpeed = currentSpeed;
    }

    public void PassCheckpoint(int checkpointIndex)
    {
        if (raceCompleted)
            return;

        if (RaceManager.Instance != null && !RaceManager.Instance.raceStarted)
            return;

        if (checkpointIndex == nextCheckpointIndex)
        {
            nextCheckpointIndex++;

            Debug.Log("Checkpoint passed: " + checkpointIndex);

            UpdateUI();
        }
        else
        {
            Debug.Log("Wrong checkpoint. Expected: " + nextCheckpointIndex + " but got: " + checkpointIndex);
        }
    }

    public void CrossFinishLine()
    {
        if (raceCompleted)
            return;

        if (RaceManager.Instance != null && !RaceManager.Instance.raceStarted)
            return;

        if (nextCheckpointIndex < totalCheckpoints)
        {
            Debug.Log("Finish line crossed too early. Missing checkpoints.");
            return;
        }

        nextCheckpointIndex = 0;

        float lapTime = Time.time - lapStartTime;
        lapTimes.Add(lapTime);
        lapStartTime = Time.time;

        if (currentLap >= totalLaps)
        {
            FinishRace();
            return;
        }

        currentLap++;

        Debug.Log("Lap completed. Current lap: " + currentLap);

        UpdateUI();
    }

    private void FinishRace()
    {
        raceCompleted = true;
        finishTime = Time.time - raceStartTime;

        ComputeAverageSpeed();

        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.StopTracking();

        Debug.Log("Race Finished! Time: " + finishTime.ToString("F2"));

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.raceFinished = true;
        }

        PhotonView pv = GetComponentInParent<PhotonView>();
        bool isLocal = (pv != null && pv.IsMine) || (pv == null);

        if (isLocal && pv != null && PhotonNetwork.InRoom)
        {
            float bestLap = 0f;
            if (lapTimes != null && lapTimes.Count > 0)
            {
                bestLap = lapTimes[0];
                for (int i = 1; i < lapTimes.Count; i++)
                {
                    if (lapTimes[i] < bestLap)
                        bestLap = lapTimes[i];
                }
            }

            Hashtable props = new Hashtable();
            props["FinishTime"] = finishTime;
            props["TopSpeed"] = topSpeed;
            props["AverageSpeed"] = averageSpeed;
            props["BestLap"] = bestLap;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        string playerName = PlayerPrefs.GetString("PlayerName", "Player");
        string trackId = GameSession.Instance != null ? GameSession.Instance.SelectedTrackId : "Unknown";
        FirebaseManager.EnsureExists();
        LeaderboardManager.EnsureExists();
        if (LeaderboardManager.Instance != null)
            LeaderboardManager.Instance.SubmitTime(playerName, trackId, finishTime);

        if (lapText != null)
            lapText.text = "FINISHED";

        if (checkpointText != null)
            checkpointText.text = "Race Complete";

        if (isLocal)
        {
            Debug.Log("Firing OnLocalPlayerFinished event.");
            OnLocalPlayerFinished?.Invoke();
        }
    }

    private void ComputeAverageSpeed()
    {
        if (speedSamples.Count == 0)
        {
            averageSpeed = 0f;
            return;
        }

        float sum = 0f;
        for (int i = 0; i < speedSamples.Count; i++)
            sum += speedSamples[i];

        averageSpeed = sum / speedSamples.Count;
    }

    private void UpdateUI()
    {
        if (lapText != null)
            lapText.text = "Lap: " + currentLap + " / " + totalLaps;

        if (checkpointText != null)
            checkpointText.text = "Checkpoint: " + nextCheckpointIndex + " / " + totalCheckpoints;
    }
}
