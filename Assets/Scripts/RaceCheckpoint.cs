using UnityEngine;

public class RaceCheckpoint : MonoBehaviour
{
    public int checkpointIndex;
    public bool isFinishLine = false;

    private void Start()
    {
        string type = isFinishLine ? "FINISH" : "CHECKPOINT";
        Debug.Log($"RaceCheckpoint [{type}]: index={checkpointIndex}, position={transform.position}, name={gameObject.name}");
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerLapTracker tracker = other.GetComponentInParent<PlayerLapTracker>();
        if (tracker == null)
            tracker = other.GetComponent<PlayerLapTracker>();
        if (tracker == null)
            return;

        if (isFinishLine)
        {
            tracker.CrossFinishLine();
        }
        else
        {
            tracker.PassCheckpoint(checkpointIndex);
        }
    }
}