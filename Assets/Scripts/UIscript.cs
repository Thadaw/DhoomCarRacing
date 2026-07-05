using System.Collections;
using UnityEngine;

public class UIscript : MonoBehaviour
{
    private GameObject playerObj;
    private Checkpoints checkpoints;

    [SerializeField] private GameObject missedcheckpointText;

    private Coroutine missedCoroutine;

    void Start()
    {
        playerObj = GameObject.FindGameObjectWithTag("Player");
        checkpoints = playerObj != null ? playerObj.GetComponent<Checkpoints>() : null;

        if (missedcheckpointText != null)
            missedcheckpointText.SetActive(false);
    }

    void Update()
    {
        if (checkpoints == null)
            return;

        if (checkpoints.missed)
        {
            checkpoints.missed = false;

            if (missedcheckpointText != null)
                missedcheckpointText.SetActive(true);

            if (missedCoroutine != null)
                StopCoroutine(missedCoroutine);

            missedCoroutine = StartCoroutine(HideMissedCheckpointTextAfterDelay(10f));
        }
    }

    IEnumerator HideMissedCheckpointTextAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (missedcheckpointText != null)
            missedcheckpointText.SetActive(false);
    }
}

