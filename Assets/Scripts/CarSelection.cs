using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

public class CarSelection : MonoBehaviour
{
    [SerializeField] private GameObject[] cars;

    [Header("Scene Names")]
    [SerializeField] private string trackSelectSceneName = "TrackSelection";
    [SerializeField] private string lobbySceneName = "Lobby";
    [SerializeField] private string multiplayerMenuSceneName = "MultiplayerMenu";

    private int currentCarIndex = 0;

    void Start()
    {
        if (cars == null || cars.Length == 0)
        {
            Debug.LogError("No cars assigned in Inspector!");
            return;
        }

        for (int i = 0; i < cars.Length; i++)
        {
            if (cars[i] != null)
                cars[i].SetActive(false);
        }

        currentCarIndex = GameSession.Instance != null ? GameSession.Instance.SelectedCarId : 0;
        if (currentCarIndex < 0 || currentCarIndex >= cars.Length)
            currentCarIndex = 0;

        cars[currentCarIndex].SetActive(true);

        GameObject backBtn = GameObject.Find("BackButton");
        if (backBtn != null)
        {
            Button btn = backBtn.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => { PlayClickSound(); GoBack(); });
        }

        Debug.Log("Car Selection Started. Mode: " +
            (GameSession.Instance != null ? GameSession.Instance.CurrentMode.ToString() : "none"));
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    public void NextCar()
    {
        PlayClickSound();
        Debug.Log("NEXT BUTTON WORKING");

        if (cars == null || cars.Length == 0)
        {
            Debug.LogError("Cars array empty!");
            return;
        }

        cars[currentCarIndex].SetActive(false);

        currentCarIndex++;

        if (currentCarIndex >= cars.Length)
            currentCarIndex = 0;

        cars[currentCarIndex].SetActive(true);
    }

    public void PreviousCar()
    {
        PlayClickSound();
        if (cars == null || cars.Length == 0)
            return;

        cars[currentCarIndex].SetActive(false);

        currentCarIndex--;

        if (currentCarIndex < 0)
            currentCarIndex = cars.Length - 1;

        cars[currentCarIndex].SetActive(true);

        Debug.Log("Previous Car: " + currentCarIndex);
    }

    public void SelectCar()
    {
        PlayClickSound();
        PlayerPrefs.SetInt("CarIndexValue", currentCarIndex);
        Debug.Log("Selected Car Index: " + currentCarIndex);

        if (GameSession.Instance != null)
        {
            GameSession.Instance.SelectedCarId = currentCarIndex;
        }

        if (GameSession.Instance != null && GameSession.Instance.IsSelectingFromLobby)
        {
            SyncCarToPhoton();
            GameSession.Instance.IsSelectingFromLobby = false;

            if (PhotonNetwork.InRoom)
            {
                Debug.Log("Routing back to Lobby.");
                SceneManager.LoadScene(lobbySceneName);
            }
            else
            {
                Debug.LogWarning("Cannot return to Lobby because the client is not in a room. Returning to MultiplayerMenu.");
                SceneManager.LoadScene(multiplayerMenuSceneName);
            }
        }
        else
        {
            Debug.Log("Routing to TrackSelection.");
            SceneManager.LoadScene(trackSelectSceneName);
        }
    }

    public void GoBack()
    {
        if (GameSession.Instance != null && GameSession.Instance.IsSelectingFromLobby)
        {
            GameSession.Instance.IsSelectingFromLobby = false;
            if (PhotonNetwork.InRoom)
            {
                SceneManager.LoadScene(lobbySceneName);
                return;
            }
        }
        SceneManager.LoadScene("MainMenu");
    }

    private void SyncCarToPhoton()
    {
        if (!PhotonNetwork.InRoom) return;

        Hashtable props = new Hashtable();
        props["CarId"] = currentCarIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }
}