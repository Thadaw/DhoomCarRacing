using UnityEngine;

/// <summary>
/// Persists across scene loads. Stores the player's choices (mode, car, track)
/// so Garage, TrackSelect, CreateRoom, JoinRoom, and MainGame can all read/write
/// the same data without passing it manually scene-to-scene.
/// Self-creates if no instance exists yet, so it works even if you start
/// Play from a scene other than MainMenu.
/// </summary>
public class GameSession : MonoBehaviour
{
    private static GameSession _instance;

    public static GameSession Instance
    {
        get
        {
            if (_instance == null)
            {
                // No GameSession in the scene yet (e.g. we started Play from
                // a scene other than MainMenu) - create one on the fly.
                var go = new GameObject("GameSession");
                _instance = go.AddComponent<GameSession>();
                DontDestroyOnLoad(go);
                Debug.LogWarning("GameSession.Instance was null - auto-created one. " +
                                 "Make sure MainMenu is the first scene loaded in normal play.");
            }
            return _instance;
        }
    }

    public enum GameMode { None, SinglePlayer, MultiplayerHost, MultiplayerJoin }

    [Header("Set by MainMenu buttons")]
    public GameMode CurrentMode = GameMode.None;

    [Header("Set by Garage scene")]
    public int SelectedCarId;

    [Header("Set by TrackSelect scene")]
    public string SelectedTrackId;

    [Header("Set by JoinRoom scene (multiplayer only)")]
    public string RoomCodeToJoin;
    public string CreatedRoomName;
    public bool IsHost;

    [Header("Set when selecting car/track from Lobby")]
    public bool IsSelectingFromLobby = false;

    private void Awake()
    {
        // If one already exists (e.g. we looped back to MainMenu), destroy this duplicate.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }
}