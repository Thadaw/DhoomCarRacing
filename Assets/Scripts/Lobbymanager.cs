using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using System.Collections.Generic;
using ExitGames.Client.Photon;

// Attach this to a GameObject in your "Lobby" scene.
// Shows room code, player count, live player list with ready status,
// a per-player Ready toggle, and a host-only Start button that only
// becomes interactable once every player has readied up.
public class LobbyManager : MonoBehaviourPunCallbacks
{
    [Header("Room Info UI")]
    public TMP_Text roomNameText;       // "Roomname" - shows the room code
    public TMP_Text playerCountText;    // "Playercount" - shows "2/4"

    [Header("Player List UI")]
    public Transform playerListParent;       // "Playerlist" (container) - rows get spawned here
    public GameObject playerListItemPrefab;  // Prefab with a single TMP_Text child

    [Header("Buttons")]
    public Button readyButton;     // "Ready" - toggles this client's ready state
    public TMP_Text readyButtonLabel; // Optional: text on the Ready button (Ready/Unready)
    public Button startButton;     // "Button" - host only, starts the game
    public Button leaveButton;     // "leave"
    public Button selectCarButton; // open car selection (returns to lobby)
    public Button selectTrackButton; // host-only: open track selection (returns to lobby)

    [Header("Scene Names")]
    public string menuSceneName = "MultiplayerMenu";
    public string trackSelectionSceneName = "TrackSelection";
    public string garageSceneName = "Garage";

    private const string READY_KEY = "IsReady";
    private List<GameObject> spawnedPlayerItems = new List<GameObject>();
    private bool isReady = false;
    private bool hasRoomState = false;

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    void Start()
    {
        if (readyButton != null)
            readyButton.onClick.AddListener(() => { PlayClickSound(); ToggleReady(); });

        if (startButton != null)
            startButton.onClick.AddListener(() => { PlayClickSound(); StartGame(); });

        if (leaveButton != null)
            leaveButton.onClick.AddListener(() => { PlayClickSound(); LeaveRoom(); });

        if (selectCarButton != null)
            selectCarButton.onClick.AddListener(() => { PlayClickSound(); OnSelectCarPressed(); });

        if (selectTrackButton != null)
            selectTrackButton.onClick.AddListener(() => { PlayClickSound(); OnSelectTrackPressed(); });

        // Fallback: try to auto-find buttons by name if not assigned in Inspector
        if (selectCarButton == null)
        {
            var go = GameObject.Find("SelectCarButton");
            if (go != null)
            {
                selectCarButton = go.GetComponent<Button>();
                if (selectCarButton != null)
                    selectCarButton.onClick.AddListener(() => { PlayClickSound(); OnSelectCarPressed(); });
            }
        }

        if (selectTrackButton == null)
        {
            var go = GameObject.Find("SelectTrackButton");
            if (go != null)
            {
                selectTrackButton = go.GetComponent<Button>();
                if (selectTrackButton != null)
                    selectTrackButton.onClick.AddListener(() => { PlayClickSound(); OnSelectTrackPressed(); });
            }
        }

        if (PhotonNetwork.InRoom)
        {
            InitializeLobby();
        }
        else
        {
            Debug.Log("[Lobby] Waiting to join room...");
        }
    }

    public override void OnJoinedRoom()
    {
        InitializeLobby();
    }

    void InitializeLobby()
    {
        if (hasRoomState) return;
        hasRoomState = true;

        // Make sure our own ready state starts false and is synced
        SetLocalReady(false);

        // If we're the host, auto-ready so the host can start immediately
        if (PhotonNetwork.IsMasterClient)
        {
            SetLocalReady(true);
        }

        SyncSelectedCarToPhoton();

        ShowRoomName();
        RefreshPlayerList();
        UpdateStartButtonState();
        UpdateSelectButtonState();
    }

    void SyncSelectedCarToPhoton()
    {
        if (GameSession.Instance == null) return;

        int selectedCarId = GameSession.Instance.SelectedCarId;
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("CarId", out object existingCarId)
            && existingCarId is int currentCarId
            && currentCarId == selectedCarId)
        {
            return;
        }

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props["CarId"] = selectedCarId;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    void UpdateSelectButtonState()
    {
        if (selectCarButton != null)
            selectCarButton.gameObject.SetActive(true);

        if (selectTrackButton != null)
            selectTrackButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    void ShowRoomName()
    {
        if (roomNameText != null)
        {
            string text = "Room: " + PhotonNetwork.CurrentRoom.Name;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("TrackId", out object track))
            {
                text += "   | Track: " + track.ToString();
            }
            roomNameText.text = text;
        }
    }

    void UpdatePlayerCount()
    {
        if (playerCountText != null)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            int max = PhotonNetwork.CurrentRoom.MaxPlayers;
            playerCountText.text = current + "/" + max;
        }
    }

    void RefreshPlayerList()
    {
        foreach (GameObject item in spawnedPlayerItems)
        {
            Destroy(item);
        }
        spawnedPlayerItems.Clear();

        // Ensure VerticalLayoutGroup on parent so items stack properly
        VerticalLayoutGroup vlg = playerListParent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = playerListParent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4;
        vlg.padding = new RectOffset(10, 10, 10, 10);

        // Build sorted list: host first, then rest by ActorNumber
        List<Player> sorted = new List<Player>(PhotonNetwork.PlayerList);
        sorted.Sort((a, b) =>
        {
            if (a.IsMasterClient) return -1;
            if (b.IsMasterClient) return 1;
            return a.ActorNumber.CompareTo(b.ActorNumber);
        });

        foreach (Player player in sorted)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListParent);
            TMP_Text label = item.GetComponentInChildren<TMP_Text>();

            string displayName = string.IsNullOrEmpty(player.NickName)
                ? "Player " + player.ActorNumber
                : player.NickName;

            if (player.IsMasterClient)
                displayName += " (Host)";

            displayName += IsPlayerReady(player) ? " - Ready" : " - Not Ready";

            // If the player has a selected car, show it
            if (player.CustomProperties.TryGetValue("CarId", out object carId))
            {
                displayName += " - Car: " + carId.ToString();
            }

            label.text = displayName;
            spawnedPlayerItems.Add(item);
        }

        UpdatePlayerCount();
        UpdateStartButtonState();
    }

    bool IsPlayerReady(Player player)
    {
        if (player.CustomProperties.TryGetValue(READY_KEY, out object value))
            return (bool)value;
        return false;
    }

    // ================= READY BUTTON =================
    void ToggleReady()
    {
        SetLocalReady(!isReady);
    }

    void SetLocalReady(bool ready)
    {
        isReady = ready;

        Hashtable props = new Hashtable();
        props[READY_KEY] = isReady;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);

        if (readyButtonLabel != null)
            readyButtonLabel.text = isReady ? "Unready" : "Ready";
    }

    // Called automatically by Photon whenever ANY player's custom properties change
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        RefreshPlayerList();
    }

    bool AreAllPlayersReady()
    {
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!IsPlayerReady(player))
                return false;
        }
        return true;
    }

    void UpdateStartButtonState()
    {
        if (startButton == null) return;

        // Only the host sees/uses the Start button
        startButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        if (PhotonNetwork.IsMasterClient)
        {
            startButton.interactable = AreAllPlayersReady() && PhotonNetwork.CurrentRoom.PlayerCount > 0;
        }
    }

    // ================= START GAME (HOST ONLY) =================
    void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (!AreAllPlayersReady())
        {
            Debug.LogWarning("Not all players are ready yet.");
            return;
        }

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.AutomaticallySyncScene = true;

        NetworkCarManager.EnsureExists();

        string sceneName = "MainGame";
        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("TrackId", out object trackId))
        {
            sceneName = GetSceneNameForTrack(trackId.ToString());
        }
        Debug.Log("Loading track scene: " + sceneName);
        PhotonNetwork.LoadLevel(sceneName);
    }

    string GetSceneNameForTrack(string trackId)
    {
        switch (trackId)
        {
            case "Track1": return "MainGame";
            case "Track2": return "Track1";
            case "Track3": return "Track3";
            default:
                Debug.LogWarning("Unknown trackId: " + trackId + ". Defaulting to MainGame.");
                return "MainGame";
        }
    }

    // ================= PHOTON CALLBACKS =================
    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayerList();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        RefreshPlayerList();
    }

    void UpdateSelectButtons()
    {
        if (selectCarButton != null)
            selectCarButton.gameObject.SetActive(true);

        if (selectTrackButton != null)
            selectTrackButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
    }

    void OnSelectCarPressed()
    {
        if (GameSession.Instance != null)
            GameSession.Instance.IsSelectingFromLobby = true;

        SceneManager.LoadScene(garageSceneName);
    }

    void OnSelectTrackPressed()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        if (GameSession.Instance != null)
            GameSession.Instance.IsSelectingFromLobby = true;

        SceneManager.LoadScene(trackSelectionSceneName);
    }

    // ================= LEAVE =================
    public void LeaveRoom()
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.IsSelectingFromLobby = false;
        }

        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        if (GameSession.Instance != null)
        {
            GameSession.Instance.IsSelectingFromLobby = false;
        }

        SceneManager.LoadScene(menuSceneName);
    }
}