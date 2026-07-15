using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
 
public class Networkmanager : MonoBehaviourPunCallbacks
{
    [Header("Buttons (from Canvas)")]
    public Button createRoomButton;   // Canvas > Createroom
    public Button joinRoomButton;     // Canvas > Joinroom
    public Button backButton;         // Canvas > Back
 
    [Header("Scene Names")]
    public string lobbySceneName = "Lobby";
    public string joiningSceneName = "Joining";
    public string mainMenuSceneName = "MainMenu";

    [Header("Photon Settings")]
    [Tooltip("Optional fixed Photon region to use for all clients. Set this to the same region on host and joiner builds.")]
    public string defaultPhotonRegion = "us";
    [Tooltip("Optional shared game version. Host and joiner must use the same version to see the same Photon rooms.")]
    public string defaultGameVersion = "1.0";
 
    private bool isCreatingRoom = false;

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }
 
    // True only once OnConnectedToMaster() has actually fired.
    // IsConnectedAndReady is NOT enough - it's also true while still on the
    // NameServer, before Photon has routed us to the Master Server, which is
    // what was causing "Client is on NameServer (must be Master Server)".
    private bool isReadyForMatchmaking = false;
 
    void Start()
    {
        createRoomButton.onClick.AddListener(() => { PlayClickSound(); CreateRoom(); });
        joinRoomButton.onClick.AddListener(() => { PlayClickSound(); OnJoinRoomClicked(); });
        backButton.onClick.AddListener(() => { PlayClickSound(); GoBack(); });
 
        // Disable buttons until we're actually connected to the Master Server.
        SetButtonsInteractable(false);

        bool settingsChanged = ApplyPhotonSettings();

        if (!PhotonNetwork.IsConnected || settingsChanged)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.NetworkClientState == ClientState.ConnectedToMasterServer)
        {
            // Already connected from a previous scene (e.g. came back from Lobby).
            PhotonNetwork.AutomaticallySyncScene = true;
            isReadyForMatchmaking = true;
            SetButtonsInteractable(true);
        }
    }
 
    private void SetButtonsInteractable(bool value)
    {
        if (createRoomButton != null) createRoomButton.interactable = value;
        if (joinRoomButton != null) joinRoomButton.interactable = value;
        // backButton intentionally left out - player should always be able to go back
    }
 
    // ================= CONNECT =================
    public override void OnConnectedToMaster()
    {
        Debug.Log($"Connected to Photon Master Server; region={PhotonNetwork.CloudRegion}, gameVersion={PhotonNetwork.GameVersion}");
        PhotonNetwork.AutomaticallySyncScene = true;
        isReadyForMatchmaking = true;
        SetButtonsInteractable(true);
    }
 
    // ================= CREATE ROOM =================
    public void CreateRoom()
    {
        if (!isReadyForMatchmaking)
        {
            Debug.LogWarning("Still connecting to Photon - please wait a moment and try again.");
            return;
        }
 
        isCreatingRoom = true;
        SetButtonsInteractable(false); // prevent double-clicks while creating
 
        GameSession.Instance.CurrentMode = GameSession.GameMode.MultiplayerHost;
 
        string roomName = "Room_" + Random.Range(1000, 9999);
 
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = 4;
        options.IsVisible = true;
        options.IsOpen = true;
 
        PhotonNetwork.CreateRoom(roomName, options);
        if (GameSession.Instance != null)
        {
            GameSession.Instance.CreatedRoomName = roomName;
        }
        Debug.Log("Creating Room: " + roomName);
    }

    // ================= JOIN ROOM BUTTON =================
    // Just switches to the JoiningRoom scene.
    // The actual JoinRandomRoom() call happens over there (see JoiningRoomManager.cs)
    public void OnJoinRoomClicked()
    {
        SceneManager.LoadScene(joiningSceneName);
    }

    // ================= BACK BUTTON =================
    public void GoBack()
    {
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ================= ROOM CREATED SUCCESS =================
    public override void OnCreatedRoom()
    {
        Debug.Log("Room created successfully: " + PhotonNetwork.CurrentRoom.Name);
    }

    public override void OnJoinedRoom()
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.CreatedRoomName = PhotonNetwork.CurrentRoom.Name;
        }

        // Only auto-load Lobby if WE are the one who created the room
        if (isCreatingRoom)
        {
            isCreatingRoom = false;
            Debug.Log("Joined own created Room: " + PhotonNetwork.CurrentRoom.Name);
            PhotonNetwork.LoadLevel(lobbySceneName);
        }
    }

    // ================= FAIL =================
    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        isCreatingRoom = false;
        Debug.LogWarning("Create Room Failed: " + message);
        SetButtonsInteractable(true);
    }

    private bool ApplyPhotonSettings()
    {
        if (PhotonNetwork.PhotonServerSettings == null) return false;

        var appSettings = PhotonNetwork.PhotonServerSettings.AppSettings;
        bool changed = false;

        if (!string.IsNullOrEmpty(defaultPhotonRegion) && appSettings.FixedRegion != defaultPhotonRegion)
        {
            appSettings.FixedRegion = defaultPhotonRegion;
            appSettings.UseNameServer = true;
            Debug.Log($"Photon fixed region set to: {defaultPhotonRegion}");
            changed = true;
        }

        if (!string.IsNullOrEmpty(defaultGameVersion) && appSettings.AppVersion != defaultGameVersion)
        {
            appSettings.AppVersion = defaultGameVersion;
            Debug.Log($"Photon game version set to: {defaultGameVersion}");
            changed = true;
        }

        if (!string.IsNullOrEmpty(defaultGameVersion) && PhotonNetwork.GameVersion != defaultGameVersion)
        {
            Debug.LogWarning($"PhotonGameVersion mismatch: current={PhotonNetwork.GameVersion}, expected={defaultGameVersion}");
        }

        return changed;
    }
}