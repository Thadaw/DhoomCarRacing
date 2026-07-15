using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class TrackSelection : MonoBehaviour
{
    [Header("Track Preview Images")]
    public GameObject track1Map;
    public GameObject track2Map;
    public GameObject track3Map;

    [Header("Track Buttons (for highlight visuals)")]
    public Button track1Button;
    public Button track2Button;
    public Button track3Button;

    [Header("Highlight Colors")]
    public Color selectedColor = Color.white;
    public Color unselectedColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [Header("Play Button")]
    public Button playButton;

    [Header("Scene Names")]
    [SerializeField] private string multiplayerMenuSceneName = "MultiplayerMenu";
    [SerializeField] private string garageSceneName = "Garage";
    [SerializeField] private string lobbySceneName = "Lobby";

    private string selectedTrackId = null;

    void Start()
    {
        ShowAllMaps();
        UpdateButtonHighlights();
        SetPlayInteractable(false);

        GameObject backBtn = GameObject.Find("Back");
        if (backBtn != null)
        {
            Button btn = backBtn.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => { PlayClickSound(); GoBack(); });
        }
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    public void SelectTrack(string trackId)
    {
        PlayClickSound();
        if (string.IsNullOrEmpty(trackId))
        {
            Debug.LogError("TrackSelection.SelectTrack() called with an empty trackId!");
            return;
        }

        selectedTrackId = trackId;
        Debug.Log("Track highlighted: " + selectedTrackId);

        UpdateButtonHighlights();
        SetPlayInteractable(true);
    }

    void ShowAllMaps()
    {
        if (track1Map != null) track1Map.SetActive(true);
        if (track2Map != null) track2Map.SetActive(true);
        if (track3Map != null) track3Map.SetActive(true);
    }

    void ShowOnlyMap(string trackId)
    {
        if (track1Map != null) track1Map.SetActive(trackId == "Track1");
        if (track2Map != null) track2Map.SetActive(trackId == "Track2");
        if (track3Map != null) track3Map.SetActive(trackId == "Track3");
    }

    void UpdateButtonHighlights()
    {
        SetButtonColor(track1Button, selectedTrackId == "Track1");
        SetButtonColor(track2Button, selectedTrackId == "Track2");
        SetButtonColor(track3Button, selectedTrackId == "Track3");
    }

    void SetButtonColor(Button button, bool isSelected)
    {
        if (button == null) return;

        Image img = button.GetComponent<Image>();
        if (img != null)
            img.color = isSelected ? selectedColor : unselectedColor;
    }

    void SetPlayInteractable(bool state)
    {
        if (playButton != null)
            playButton.interactable = state;
    }

    string GetSceneNameForTrack(string trackId)
    {
        switch (trackId)
        {
            case "Track1": return "Track1";
            case "Track2": return "Track1";
            case "Track3": return "Track3";
            default:
                Debug.LogWarning("Unknown trackId: " + trackId + ". Defaulting to Track1.");
                return "Track1";
        }
    }

    public void ConfirmSelection()
    {
        PlayClickSound();
        if (string.IsNullOrEmpty(selectedTrackId))
        {
            Debug.LogWarning("Play pressed but no track was selected yet.");
            return;
        }

        Debug.Log("Track confirmed: " + selectedTrackId);

        string sceneToLoad = GetSceneNameForTrack(selectedTrackId);

        if (GameSession.Instance == null)
        {
            Debug.LogError("GameSession.Instance is null! Loading track scene directly: " + sceneToLoad);
            SceneManager.LoadScene(sceneToLoad);
            return;
        }

        GameSession.Instance.SelectedTrackId = selectedTrackId;

        if (GameSession.Instance.IsSelectingFromLobby)
        {
            GameSession.Instance.IsSelectingFromLobby = false;

            if (PhotonNetwork.InRoom)
            {
                SyncTrackToRoom();
                Debug.Log("Routing back to Lobby.");
                SceneManager.LoadScene(lobbySceneName);
                return;
            }

            Debug.LogWarning("Cannot return to Lobby because the client is not in a room. Returning to MultiplayerMenu.");
            SceneManager.LoadScene(multiplayerMenuSceneName);
            return;
        }

        switch (GameSession.Instance.CurrentMode)
        {
            case GameSession.GameMode.SinglePlayer:
                Debug.Log("Loading track scene: " + sceneToLoad);
                SceneManager.LoadScene(sceneToLoad);
                break;

            case GameSession.GameMode.MultiplayerHost:
            case GameSession.GameMode.MultiplayerJoin:
                Debug.Log("Routing to MultiplayerMenu (Multiplayer).");
                SceneManager.LoadScene(multiplayerMenuSceneName);
                break;

            default:
                Debug.LogWarning("GameSession.CurrentMode was None when Play was pressed! " +
                    "Defaulting to track scene: " + sceneToLoad);
                SceneManager.LoadScene(sceneToLoad);
                break;
        }
    }

    private void SyncTrackToRoom()
    {
        if (!PhotonNetwork.InRoom) return;

        Hashtable props = new Hashtable();
        props["TrackId"] = selectedTrackId;
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);
    }

    public void GoBack()
    {
        if (GameSession.Instance != null && GameSession.Instance.IsSelectingFromLobby)
        {
            GameSession.Instance.IsSelectingFromLobby = false;
            SceneManager.LoadScene(lobbySceneName);
            return;
        }
        SceneManager.LoadScene(garageSceneName);
    }
}