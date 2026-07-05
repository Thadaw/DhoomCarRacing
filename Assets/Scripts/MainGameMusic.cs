using UnityEngine;

public class MainGameMusic : MonoBehaviour
{
    void Start()
    {
        if (AudioManager.instance == null)
        {
            Debug.LogWarning("MainGameMusic: AudioManager.instance is null - no AudioManager " +
                "has run its Awake() yet. Make sure you started Play from the MainMenu scene, " +
                "which is where the persistent AudioManager lives.");
            return;
        }

        AudioManager.instance.playMainGame();
    }
}