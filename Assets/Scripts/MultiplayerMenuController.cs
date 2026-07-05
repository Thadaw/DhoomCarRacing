using UnityEngine;

/// <summary>
/// Attach to the Canvas in the MultiplayerMenu scene.
/// Hook Createroom/Joinroom/Back button OnClick() events to these methods.
/// </summary>
public class MultiplayerMenuController : MonoBehaviour
{
    private const string MainMenuSceneName = "MainMenu";
    private const string CreateRoomSceneName = "Createroom"; // adjust to your real scene name if different
    private const string JoinRoomSceneName = "Joinroom";     // adjust to your real scene name if different

    [SerializeField] private SceneSwitcher sceneSwitcher; // drag the SceneSwitcher GameObject here in Inspector

    // Hook to: Createroom -> OnClick()
    public void OnCreateRoomPressed()
    {
        GameSession.Instance.CurrentMode = GameSession.GameMode.MultiplayerHost;
        sceneSwitcher.SceneLoder(CreateRoomSceneName);
    }

    // Hook to: Joinroom -> OnClick()
    public void OnJoinRoomPressed()
    {
        GameSession.Instance.CurrentMode = GameSession.GameMode.MultiplayerJoin;
        sceneSwitcher.SceneLoder(JoinRoomSceneName);
    }

    // Hook to: Back -> OnClick()
    public void OnBackPressed()
    {
        sceneSwitcher.SceneLoder(MainMenuSceneName);
    }
}