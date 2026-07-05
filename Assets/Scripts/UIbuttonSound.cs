using UnityEngine;

public class UIbuttonSound : MonoBehaviour
{
    public void ButtonSound()
    {
        // Guard against AudioManager.instance being null - this happens when
        // testing a scene directly (e.g. pressing Play from Garage instead of
        // MainMenu), since AudioManager.Awake() never gets a chance to run
        // and set up the singleton instance first.
        // Without this check, every button click would throw a
        // NullReferenceException here, which (with Error Pause enabled)
        // silently freezes the entire Editor on the very next click.
        if (AudioManager.instance == null)
        {
            Debug.LogWarning("UIbuttonSound: AudioManager.instance is null " +
                "(did you press Play from MainMenu, or skip straight to this scene?). " +
                "Skipping click sound.");
            return;
        }

        AudioManager.instance.playButtonSound();
    }
}