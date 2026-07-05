using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

// Attach this to an empty GameObject in your "Joining" scene.
// Player types the room code given by the host, then presses JoinButton.
//
// POPUP SETUP:
// 1. Create a UI Panel as a child of your Canvas, name it "PopupPanel".
//    - Add an Image (semi-transparent background covering the screen is fine).
//    - Add a child TMP_Text for the message - assign it to popupMessageText.
//    - Add a child Button labeled "OK" - assign it to popupOkButton.
//    - Set PopupPanel inactive by default (uncheck the checkbox next to its name).
// 2. Drag PopupPanel itself into the popupPanel field below.
public class JoiningRoomManager : MonoBehaviourPunCallbacks
{
    [Header("UI (from Canvas)")]
    public TMP_InputField roomCodeInput;  // Canvas > roomcode
    public Button joinButton;             // Canvas > JoinButton
    public Button backButton;             // Optional
    public TMP_Text statusText;           // Optional - small inline status label

    [Header("Popup UI")]
    public GameObject popupPanel;         // Canvas > PopupPanel (inactive by default)
    public TMP_Text popupMessageText;     // PopupPanel > Message
    public Button popupOkButton;          // PopupPanel > OkButton (optional - closes popup)

    [Header("Scene Names")]
    public string lobbySceneName = "Lobby";
    public string menuSceneName = "MultiplayerMenu";

    [Header("Photon Settings")]
    [Tooltip("Optional fixed Photon region to use for all clients. Set this to the same region on host and joiner builds.")]
    public string defaultPhotonRegion = "us";

    private bool isJoining = false;
    private bool isReadyForMatchmaking = false;

    void Start()
    {
        if (joinButton != null)
        {
            joinButton.onClick.AddListener(OnJoinButtonClicked);
            Debug.Log("[Joining] Join button listener attached.");
        }
        else
        {
            Debug.LogError("[Joining] joinButton is NOT assigned in the Inspector!");
        }

        if (backButton != null)
        {
            backButton.onClick.AddListener(GoBack);
            Debug.Log("[Joining] Back button listener attached.");
        }

        if (roomCodeInput == null)
            Debug.LogError("[Joining] roomCodeInput is NOT assigned in the Inspector!");

        if (popupOkButton != null)
            popupOkButton.onClick.AddListener(HidePopup);

        HidePopup(); // make sure popup starts hidden

        SetStatus("Connecting...");

        ApplyFixedPhotonRegion();

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            if (!string.IsNullOrEmpty(defaultPhotonRegion) && PhotonNetwork.CloudRegion != defaultPhotonRegion)
            {
                Debug.LogWarning($"[Joining] Connected to wrong region ({PhotonNetwork.CloudRegion}). Reconnecting to {defaultPhotonRegion}.");
                PhotonNetwork.Disconnect();
                SetStatus("Reconnecting to Photon...");
            }
            else
            {
                HandleExistingConnectionState();
            }
        }
    }

    private void ApplyFixedPhotonRegion()
    {
        if (PhotonNetwork.PhotonServerSettings == null) return;

        var appSettings = PhotonNetwork.PhotonServerSettings.AppSettings;
        if (string.IsNullOrEmpty(appSettings.FixedRegion) && !string.IsNullOrEmpty(defaultPhotonRegion))
        {
            appSettings.FixedRegion = defaultPhotonRegion;
            appSettings.UseNameServer = true;
            Debug.Log($"Photon fixed region set to: {defaultPhotonRegion}");
        }
    }

    private void HandleExistingConnectionState()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        switch (PhotonNetwork.NetworkClientState)
        {
            case ClientState.ConnectedToMasterServer:
                Debug.Log("[Joining] Already connected to Master Server.");
                isReadyForMatchmaking = true;
                SetStatus("Enter room code to join");
                break;

            case ClientState.JoinedLobby:
                Debug.Log("[Joining] Already in lobby.");
                isReadyForMatchmaking = true;
                SetStatus("Enter room code to join");
                break;

            default:
                Debug.Log("[Joining] Connected but not ready for matchmaking yet: " + PhotonNetwork.NetworkClientState);
                SetStatus("Connecting...");
                break;
        }
    }

    void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;

        Debug.Log("[Joining] " + msg);
    }

    // ================= POPUP HELPERS =================
    void ShowPopup(string message)
    {
        if (popupPanel == null)
        {
            Debug.LogWarning("[Joining] popupPanel not assigned - message was: " + message);
            return;
        }

        popupPanel.SetActive(true);
        if (popupMessageText != null)
            popupMessageText.text = message;
    }

    void HidePopup()
    {
        if (popupPanel != null)
            popupPanel.SetActive(false);
    }

    public override void OnConnectedToMaster()
    {
        var appVersion = PhotonNetwork.GameVersion;
        var region = PhotonNetwork.CloudRegion;
        Debug.Log($"[Joining] Connected to Photon Master Server; region={region}, gameVersion={appVersion}");
        PhotonNetwork.AutomaticallySyncScene = true;
        isReadyForMatchmaking = true;
        SetStatus("Enter room code to join");
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        isReadyForMatchmaking = false;
        SetStatus("Disconnected: " + cause);

        // If we disconnected because the region was wrong, reconnect with the corrected settings.
        if (!PhotonNetwork.IsConnected && !string.IsNullOrEmpty(defaultPhotonRegion))
        {
            Debug.Log("[Joining] Reconnecting to Photon after region mismatch or disconnect.");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    // ================= JOIN BUTTON =================
    public void OnJoinButtonClicked()
    {
        Debug.Log("[Joining] Join button clicked.");

        if (isJoining) return; // prevent double-clicks

        if (roomCodeInput == null)
        {
            Debug.LogError("[Joining] Cannot join - roomCodeInput is not assigned.");
            return;
        }

        string enteredRoomName = roomCodeInput.text.Trim();

        if (string.IsNullOrEmpty(enteredRoomName))
        {
            ShowPopup("Please enter a room code.");
            return;
        }

        if (!isReadyForMatchmaking)
        {
            ShowPopup("Still connecting to Photon. Please wait a moment and try again.");
            Debug.LogWarning($"[Joining] Not ready for matchmaking: state={PhotonNetwork.NetworkClientState}, connected={PhotonNetwork.IsConnected}, ready={PhotonNetwork.IsConnectedAndReady}");
            return;
        }

        string roomName;
        if (enteredRoomName.StartsWith("Room_", System.StringComparison.OrdinalIgnoreCase))
        {
            string suffix = enteredRoomName.Substring(5);
            roomName = "Room_" + suffix;
        }
        else
        {
            roomName = "Room_" + enteredRoomName;
        }

        isJoining = true;
        if (joinButton != null) joinButton.interactable = false;

        // Record intent before attempting the join.
        GameSession.Instance.CurrentMode = GameSession.GameMode.MultiplayerJoin;
        GameSession.Instance.IsHost = false;
        GameSession.Instance.RoomCodeToJoin = roomName;

        PhotonNetwork.AutomaticallySyncScene = true;
        SetStatus("Joining " + roomName + " ...");
        Debug.Log("[Joining] Attempting join with room name: " + roomName + " (entered: " + enteredRoomName + ")");
        PhotonNetwork.JoinRoom(roomName);
    }

    // ================= SUCCESS =================
    public override void OnJoinedRoom()
    {
        isJoining = false;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.CreatedRoomName = PhotonNetwork.CurrentRoom.Name;
        }

        Debug.Log($"Joined Room: {PhotonNetwork.CurrentRoom.Name}. Loading lobby scene.");
        PhotonNetwork.AutomaticallySyncScene = true;
        PhotonNetwork.LoadLevel(lobbySceneName);
    }

    // ================= FAIL =================
    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        isJoining = false;
        if (joinButton != null) joinButton.interactable = true;

        string region = PhotonNetwork.CloudRegion;
        string popupMessage = "Failed to join room:\n" + message;

        if (message.Contains("Game does not exist"))
        {
            popupMessage += "\n\nMake sure the host is still in the room and that you typed the exact room code shown in their lobby.";
        }

        Debug.LogWarning($"Join Room Failed ({returnCode}): {message}; region={region}");
        ShowPopup(popupMessage);
    }

    // ================= BACK BUTTON (optional) =================
    public void GoBack()
    {
        Debug.Log("[Joining] Back button clicked.");
        SceneManager.LoadScene(menuSceneName);
    }
}