using UnityEngine;

/// <summary>
/// Attach to the Canvas in the MultiplayerMenu scene.
/// Hook Createroom/Joinroom/Back button OnClick() events to these methods.
/// </summary>
public class MultiplayerMenuController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string CreateRoomSceneName = "Lobby";
    private const string JoinRoomSceneName = "Joining";

    [SerializeField] private SceneSwitcher sceneSwitcher; // drag the SceneSwitcher GameObject here in Inspector

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    // Hook to: Createroom -> OnClick()
    public void OnCreateRoomPressed()
    {
        PlayClickSound();
        GameSession.Instance.CurrentMode = GameSession.GameMode.MultiplayerHost;
        sceneSwitcher.SceneLoder(CreateRoomSceneName);
    }

    // Hook to: Joinroom -> OnClick()
    public void OnJoinRoomPressed()
    {
        PlayClickSound();
        GameSession.Instance.CurrentMode = GameSession.GameMode.MultiplayerJoin;
        sceneSwitcher.SceneLoder(JoinRoomSceneName);
    }

    // Hook to: Back -> OnClick()
    public void OnBackPressed()
    {
        PlayClickSound();
        sceneSwitcher.SceneLoder(MainMenuSceneName);
    }
}