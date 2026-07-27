using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using System.Collections.Generic;

public class RacePlayerList : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform playerListParent;
    [SerializeField] private GameObject playerRowPrefab;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool showInMultiplayerOnly = true;

    private float nextUpdateTime = 0f;
    private List<GameObject> spawnedRows = new List<GameObject>();
    private PlayerLapTracker[] cachedTrackers;

    private void Start()
    {
        TryFindUI();
        EnsureLayout();
        RefreshTrackers();

        if (playerListParent != null)
            playerListParent.gameObject.SetActive(false);
    }

    private void RefreshTrackers()
    {
        cachedTrackers = FindObjectsByType<PlayerLapTracker>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval;
            UpdatePlayerList();
        }
    }

    private void TryFindUI()
    {
        if (playerListParent == null)
        {
            GameObject go = GameObject.Find("playerlist");
            if (go == null) go = GameObject.Find("PlayerList");
            if (go != null) playerListParent = go.transform;
        }

        if (playerRowPrefab == null && playerListParent != null)
        {
            Transform found = FindChildRecursive(playerListParent, "PlayerRow");
            if (found == null) found = FindChildRecursive(playerListParent, "playerrow 1");
            if (found != null)
            {
                playerRowPrefab = found.gameObject;
                playerRowPrefab.SetActive(false);
            }
        }
    }

    private void EnsureLayout()
    {
        if (playerListParent == null) return;

        if (playerListParent.GetComponent<Mask>() == null)
            playerListParent.gameObject.AddComponent<Mask>();

        if (playerListParent.GetComponent<Image>() == null)
        {
            Image img = playerListParent.gameObject.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.01f);
        }

        VerticalLayoutGroup vlg = playerListParent.GetComponent<VerticalLayoutGroup>();
        if (vlg == null) vlg = playerListParent.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.spacing = 4f;
        vlg.padding = new RectOffset(8, 8, 8, 8);

        ContentSizeFitter csf = playerListParent.GetComponent<ContentSizeFitter>();
        if (csf == null) csf = playerListParent.gameObject.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void UpdatePlayerList()
    {
        if (showInMultiplayerOnly && !PhotonNetwork.InRoom)
        {
            if (playerListParent != null && playerListParent.gameObject.activeSelf)
                playerListParent.gameObject.SetActive(false);
            return;
        }

        if (playerListParent == null) return;

        if (cachedTrackers == null || cachedTrackers.Length == 0)
            RefreshTrackers();

        List<PlayerRaceInfo> players = CollectPlayers();

        if (players.Count == 0)
        {
            if (playerListParent.gameObject.activeSelf)
                playerListParent.gameObject.SetActive(false);
            return;
        }

        if (!playerListParent.gameObject.activeSelf)
            playerListParent.gameObject.SetActive(true);

        players.Sort((a, b) =>
        {
            if (a.isFinished && b.isFinished)
                return a.finishTime.CompareTo(b.finishTime);
            if (a.isFinished) return -1;
            if (b.isFinished) return 1;
            if (a.currentLap != b.currentLap)
                return b.currentLap.CompareTo(a.currentLap);
            return b.nextCheckpoint.CompareTo(a.nextCheckpoint);
        });

        ClearRows();

        for (int i = 0; i < players.Count; i++)
        {
            string pos = (i + 1) + ".";
            string name = players[i].playerName;
            string timeStr = players[i].isFinished
                ? FormatTime(players[i].finishTime)
                : FormatTime(players[i].elapsedTime);
            string status = GetStatusText(players[i]);

            SpawnRow(pos, name, timeStr, status, players[i].isLocal);
        }
    }

    private List<PlayerRaceInfo> CollectPlayers()
    {
        List<PlayerRaceInfo> players = new List<PlayerRaceInfo>();

        if (cachedTrackers == null)
            RefreshTrackers();

        foreach (PlayerLapTracker tracker in cachedTrackers)
        {
            PhotonView pv = tracker.GetComponentInParent<PhotonView>();
            string name;
            bool isLocal = false;

            if (pv != null)
            {
                isLocal = pv.IsMine;
                if (isLocal)
                    name = PlayerNameHelper.GetPlayerName();
                else
                {
                    Photon.Realtime.Player owner = pv.Owner;
                    name = (owner != null && !string.IsNullOrEmpty(owner.NickName))
                        ? owner.NickName
                        : "Player " + (owner != null ? owner.ActorNumber : "?");
                }
            }
            else
            {
                isLocal = true;
                name = PlayerNameHelper.GetPlayerName();
            }

            float elapsed = 0f;
            if (tracker.finishTime > 0f)
                elapsed = tracker.finishTime;
            else if (RaceManager.Instance != null && RaceManager.Instance.raceStarted && GetRaceStartTime() > 0f)
                elapsed = Time.time - GetRaceStartTime();

            players.Add(new PlayerRaceInfo
            {
                playerName = name,
                currentLap = tracker.currentLap,
                totalLaps = tracker.totalLaps,
                nextCheckpoint = tracker.nextCheckpointIndex,
                totalCheckpoints = tracker.totalCheckpoints,
                finishTime = tracker.finishTime,
                elapsedTime = elapsed,
                isLocal = isLocal,
                isFinished = tracker.finishTime > 0f
            });
        }

        if (PhotonNetwork.InRoom)
        {
            foreach (Photon.Realtime.Player player in PhotonNetwork.PlayerList)
            {
                if (player.IsLocal) continue;

                bool found = false;
                foreach (PlayerRaceInfo p in players)
                {
                    string pname = string.IsNullOrEmpty(player.NickName)
                        ? "Player " + player.ActorNumber : player.NickName;
                    if (!p.isLocal && p.playerName == pname)
                    {
                        found = true;
                        break;
                    }
                }
                if (found) continue;

                string pName = string.IsNullOrEmpty(player.NickName)
                    ? "Player " + player.ActorNumber : player.NickName;

                float fTime = 0f;
                if (player.CustomProperties.TryGetValue("FinishTime", out object ft) && ft is float fVal)
                    fTime = fVal;

                players.Add(new PlayerRaceInfo
                {
                    playerName = pName,
                    currentLap = 0,
                    totalLaps = 3,
                    nextCheckpoint = 0,
                    totalCheckpoints = 3,
                    finishTime = fTime,
                    elapsedTime = fTime > 0f ? fTime : 0f,
                    isLocal = false,
                    isFinished = fTime > 0f
                });
            }
        }

        return players;
    }

    private float GetRaceStartTime()
    {
        if (RaceManager.Instance != null)
            return RaceManager.Instance.raceStartTime;
        return 0f;
    }

    private string GetStatusText(PlayerRaceInfo info)
    {
        if (info.isFinished)
            return "Finished";

        if (info.totalLaps > 0 && info.currentLap > 0)
            return "Lap " + info.currentLap + "/" + info.totalLaps;

        return "Racing...";
    }

    private void SpawnRow(string position, string playerName, string time, string status, bool isLocal)
    {
        if (playerRowPrefab != null)
        {
            GameObject row = Instantiate(playerRowPrefab, playerListParent);
            row.SetActive(true);

            bool foundAny = false;
            foreach (Transform child in row.transform)
            {
                TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
                if (tmp == null)
                    tmp = child.gameObject.AddComponent<TextMeshProUGUI>();

                if (child.name == "playername")
                {
                    tmp.text = position + " " + playerName;
                    if (isLocal) tmp.color = new Color(0.3f, 1f, 0.3f);
                    foundAny = true;
                }
                else if (child.name == "finishtime")
                {
                    tmp.text = time;
                    foundAny = true;
                }
                else if (child.name == "status")
                {
                    tmp.text = status;
                    foundAny = true;
                }
            }

            if (!foundAny)
            {
                foreach (Transform child in row.transform)
                {
                    TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
                    if (tmp == null) continue;
                    tmp.text = position + " " + playerName + "  |  " + time + "  |  " + status;
                    if (isLocal) tmp.color = new Color(0.3f, 1f, 0.3f);
                    break;
                }
            }

            LayoutElement le = row.GetComponent<LayoutElement>();
            if (le == null) le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 36f;

            spawnedRows.Add(row);
        }
        else
        {
            GameObject go = new GameObject("PlayerRow", typeof(RectTransform));
            go.transform.SetParent(playerListParent, false);

            HorizontalLayoutGroup hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            hlg.padding = new RectOffset(8, 8, 2, 2);

            CreateCellText(go.transform, "Position", position, 40f, isLocal);
            CreateCellText(go.transform, "Name", playerName, 180f, isLocal);
            CreateCellText(go.transform, "Time", time, 110f, isLocal);
            CreateCellText(go.transform, "Status", status, 100f, isLocal);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 32f;

            Image bg = go.AddComponent<Image>();
            bg.color = isLocal ? new Color(0.2f, 0.5f, 0.2f, 0.4f) : new Color(0.2f, 0.2f, 0.2f, 0.3f);

            spawnedRows.Add(go);
        }
    }

    private void CreateCellText(Transform parent, string childName, string text, float width, bool highlight)
    {
        GameObject go = new GameObject(childName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 18;
        tmp.color = highlight ? new Color(0.3f, 1f, 0.3f) : Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;

        LayoutElement le = go.AddComponent<LayoutElement>();
        le.preferredWidth = width;
        le.minWidth = 40f;
    }

    private void ClearRows()
    {
        foreach (GameObject row in spawnedRows)
        {
            if (row != null)
                Destroy(row);
        }
        spawnedRows.Clear();
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            Transform found = FindChildRecursive(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        float seconds = time % 60f;
        return minutes + ":" + seconds.ToString("00.00");
    }

    private class PlayerRaceInfo
    {
        public string playerName;
        public int currentLap;
        public int totalLaps;
        public int nextCheckpoint;
        public int totalCheckpoints;
        public float finishTime;
        public float elapsedTime;
        public bool isLocal;
        public bool isFinished;
    }
}
