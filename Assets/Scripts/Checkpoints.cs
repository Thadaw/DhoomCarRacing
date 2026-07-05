using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;

public class Checkpoints : MonoBehaviour
{
    [HideInInspector] public int lap = 0;
    [HideInInspector] public int checkpoint = -1;

    int checkpointCount;
    int nextCheckpoint = 0;
    Dictionary<int, bool> visited = new Dictionary<int, bool>();

    public Text lapText;

    [Header("Missing checkpoint UI")]
    [Tooltip("Shows when player hits a checkpoint out of order. Leave empty to disable UI.")]
    public Text missingCheckpointText;

    [HideInInspector] public bool missed = false;
    int expectedCheckpointAtMiss;

    void Start()
    {
        // Ensure lap UI exists and starts at Lap:0
        if (lapText != null)
            lapText.text = "Lap:" + lap;

        // Hide missing checkpoint UI at start
        if (missingCheckpointText != null)
            missingCheckpointText.gameObject.SetActive(false);

        GameObject[] checkpointObjects = GameObject.FindGameObjectsWithTag("checkpoint");
        if (checkpointObjects == null || checkpointObjects.Length == 0)
        {
            checkpointCount = 0;
            return;
        }

        // Determine checkpoint indices from object names (expects names like "0".."12")
        // Note: we don't actually need the list of indices; only the highest index.
        int maxIndex = int.MinValue;
        foreach (GameObject cp in checkpointObjects)
        {
            int idx = Int32.Parse(cp.name);
            if (idx > maxIndex) maxIndex = idx;
        }

        checkpointCount = maxIndex + 1;


        // Initialize visited for 0..maxIndex
        visited.Clear();
        for (int i = 0; i < checkpointCount; i++)
            visited.Add(i, false);

        nextCheckpoint = 0;
        missed = false;
        expectedCheckpointAtMiss = -1;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.gameObject.CompareTag("checkpoint"))
            return;

        int checkpointCurrent = int.Parse(other.gameObject.name);

        // Guard against unexpected names outside 0..maxIndex
        if (!visited.ContainsKey(checkpointCurrent))
            return;

        if (checkpointCurrent == nextCheckpoint)
        {
            visited[checkpointCurrent] = true;
            checkpoint = checkpointCurrent;

            // Lap rule:
            // - Hitting checkpoint "0" starts/advances to Lap 1 (Lap becomes 1).
            // - Completing the LAST checkpoint (expected: 12) finalizes the lap, but DOES NOT increment immediately.
            // - When checkpoint "0" is hit again after completing 12, Lap becomes 2, etc.
            if (checkpointCurrent == 0)
            {
                lap++;
                if (lapText != null)
                    lapText.text = "Lap:" + lap;
            }


            nextCheckpoint++;

            // Completed all checkpoints for this lap => reset for next lap
            if (nextCheckpoint >= checkpointCount)
            {
                var keys = new List<int>(visited.Keys);
                foreach (int key in keys)
                    visited[key] = false;

                nextCheckpoint = 0;
            }

            // If UI was showing a missed checkpoint, hide once player gets back on track
            if (missed)
            {
                missed = false;
                if (missingCheckpointText != null)
                    missingCheckpointText.gameObject.SetActive(false);
            }
        }
        else
        {
            // Player escaped/entered a checkpoint out of order.
            // Show expected (missing) checkpoint UI if they try to hit an unvisited checkpoint.
            if (!visited[checkpointCurrent] && checkpointCurrent != nextCheckpoint)
            {
                missed = true;
                expectedCheckpointAtMiss = nextCheckpoint;

                if (missingCheckpointText != null)
                {
                    missingCheckpointText.gameObject.SetActive(true);
                    missingCheckpointText.text = "Missing checkpoint: " + expectedCheckpointAtMiss;
                }
            }
        }
    }
}

