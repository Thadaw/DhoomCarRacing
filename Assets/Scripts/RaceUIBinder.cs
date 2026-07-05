using UnityEngine;
using Photon.Pun;
using TMPro;

public class RaceUIBinder : MonoBehaviour
{
    public TextMeshProUGUI lapText;
    public TextMeshProUGUI checkpointText;

    private void Start()
    {
        Invoke(nameof(BindUIToPlayer), 0.2f);
    }

    private void BindUIToPlayer()
    {
        PlayerLapTracker[] allTrackers = FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None);
        PlayerLapTracker localTracker = null;

        foreach (PlayerLapTracker t in allTrackers)
        {
            PhotonView pv = t.GetComponentInParent<PhotonView>();
            if (pv != null && pv.IsMine)
            {
                localTracker = t;
                break;
            }
        }

        if (localTracker == null)
        {
            if (allTrackers.Length > 0)
                localTracker = allTrackers[0];
            else
            {
                Debug.LogWarning("No PlayerLapTracker found in scene.");
                return;
            }
        }

        localTracker.lapText = lapText;
        localTracker.checkpointText = checkpointText;

        Debug.Log("Race UI bound to player lap tracker.");
    }
}