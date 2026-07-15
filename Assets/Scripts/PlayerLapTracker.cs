using UnityEngine;
using TMPro;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Collections.Generic;

public class PlayerLapTracker : MonoBehaviour
{
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

    private bool raceCompleted = false;
    private float raceStartTime = -1f;
    private float lapStartTime = -1f;

    private void Start()
    {
        UpdateUI();
    }

    private void Update()
    {
        if (raceStartTime < 0f && RaceManager.Instance != null && RaceManager.Instance.raceStarted)
        {
            raceStartTime = Time.time;
            lapStartTime = Time.time;

            // Start tracking play time
            PlayTimeTracker.EnsureExists();
            PlayTimeTracker.Instance.StartTracking();

        }
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

        // Player completed all checkpoints of this lap
        nextCheckpointIndex = 0;

        // Record lap time
        float lapTime = Time.time - lapStartTime;
        lapTimes.Add(lapTime);
        lapStartTime = Time.time;

        // If this was the final lap, finish the race immediately
        if (currentLap >= totalLaps)
        {
            FinishRace();
            return;
        }

        // Otherwise move to next lap
        currentLap++;

        Debug.Log("Lap completed. Current lap: " + currentLap);

        UpdateUI();
    }

    private void FinishRace()
    {
        raceCompleted = true;
        finishTime = Time.time - raceStartTime;

        // Stop tracking play time for this race
        if (PlayTimeTracker.Instance != null)
            PlayTimeTracker.Instance.StopTracking();

        Debug.Log("Race Finished! Time: " + finishTime.ToString("F2"));

        if (RaceManager.Instance != null)
        {
            RaceManager.Instance.raceFinished = true;
        }

        // Store finish time in Photon custom properties (multiplayer)
        PhotonView pv = GetComponentInParent<PhotonView>();
        if (pv != null && pv.IsMine && PhotonNetwork.InRoom)
        {
            Hashtable props = new Hashtable();
            props["FinishTime"] = finishTime;
            PhotonNetwork.LocalPlayer.SetCustomProperties(props);
        }

        // Submit time to Firebase leaderboard
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
    }

    private void UpdateUI()
    {
        if (lapText != null)
            lapText.text = "Lap: " + currentLap + " / " + totalLaps;

        if (checkpointText != null)
            checkpointText.text = "Checkpoint: " + nextCheckpointIndex + " / " + totalCheckpoints;
    }
}