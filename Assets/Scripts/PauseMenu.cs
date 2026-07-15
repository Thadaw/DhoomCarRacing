using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class PauseMenu : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private Button toggleButton;

    [Header("Panel Buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button lobbyButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Player Rows")]
    [SerializeField] private TextMeshProUGUI[] playerRows;

    private bool isPaused = false;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(() => { PlayClickSound(); TogglePause(); });

        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => { PlayClickSound(); Resume(); });
        if (restartButton != null)
            restartButton.onClick.AddListener(() => { PlayClickSound(); Restart(); });
        if (lobbyButton != null)
            lobbyButton.onClick.AddListener(() => { PlayClickSound(); GoToLobby(); });
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => { PlayClickSound(); GoToMainMenu(); });
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            TogglePause();
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if (pausePanel != null)
            pausePanel.SetActive(true);
        UpdatePlayerRows();
    }

    public void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        if (pausePanel != null)
            pausePanel.SetActive(false);
    }

    private void UpdatePlayerRows()
    {
        if (playerRows == null || playerRows.Length == 0)
            return;

        PlayerLapTracker[] trackers = FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None);
        List<PlayerInfo> players = new List<PlayerInfo>();

        foreach (PlayerLapTracker tracker in trackers)
        {
            PhotonView pv = tracker.GetComponentInParent<PhotonView>();
            string name;
            bool isLocal = false;

            if (pv != null)
            {
                isLocal = pv.IsMine;
                if (isLocal)
                    name = "You";
                else
                {
                    Photon.Realtime.Player owner = pv.Owner;
                    name = (owner != null && !string.IsNullOrEmpty(owner.NickName))
                        ? owner.NickName
                        : "Player " + (owner != null ? owner.ActorNumber : "?");
                }
            }
            else
                name = "Player";

            float bestLap = 0f;
            if (tracker.lapTimes != null && tracker.lapTimes.Count > 0)
            {
                bestLap = tracker.lapTimes[0];
                for (int j = 1; j < tracker.lapTimes.Count; j++)
                {
                    if (tracker.lapTimes[j] < bestLap)
                        bestLap = tracker.lapTimes[j];
                }
            }
            players.Add(new PlayerInfo { name = name, time = bestLap, isLocal = isLocal });
        }

        if (PhotonNetwork.InRoom)
        {
            foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal) continue;

                bool found = false;
                foreach (PlayerInfo p in players)
                {
                    string pname = string.IsNullOrEmpty(player.NickName)
                        ? "Player " + player.ActorNumber : player.NickName;
                    if (!p.isLocal && p.name == pname)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) continue;

                float time = 0f;
                if (player.CustomProperties.TryGetValue("FinishTime", out object ft) && ft is float fTime)
                    time = fTime;

                string name = string.IsNullOrEmpty(player.NickName)
                    ? "Player " + player.ActorNumber : player.NickName;

                players.Add(new PlayerInfo { name = name, time = time, isLocal = false });
            }
        }

        players.Sort((a, b) =>
        {
            if (a.time > 0f && b.time > 0f) return a.time.CompareTo(b.time);
            if (a.time > 0f) return -1;
            if (b.time > 0f) return 1;
            return 0;
        });

        for (int i = 0; i < playerRows.Length; i++)
        {
            if (playerRows[i] == null) continue;

            if (i < players.Count)
            {
                playerRows[i].gameObject.SetActive(true);
                string pos = (i + 1) + ".";
                string timeStr = players[i].time > 0f
                    ? FormatTime(players[i].time)
                    : "Racing...";
                playerRows[i].text = pos + " " + players[i].name + "  -  " + timeStr;
            }
            else
            {
                playerRows[i].gameObject.SetActive(false);
            }
        }
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;
        return minutes + ":" + seconds.ToString("00.00");
    }

    private void Restart()
    {
        Time.timeScale = 1f;
        string currentScene = SceneManager.GetActiveScene().name;

        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel(currentScene);
        else if (!PhotonNetwork.InRoom)
            SceneManager.LoadScene(currentScene);
        else
            GoToMainMenu();
    }

    private void GoToLobby()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
            PhotonNetwork.LoadLevel("Lobby");
        else if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            SceneManager.LoadScene("MultiplayerMenu");
        }
        else
            SceneManager.LoadScene("Lobby");
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();
        SceneManager.LoadScene("MainMenu");
    }

    private class PlayerInfo
    {
        public string name;
        public float time;
        public bool isLocal;
    }
}
