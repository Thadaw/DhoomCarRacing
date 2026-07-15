using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using Photon.Pun;
using System;

public class MainMenuController : MonoBehaviour
{
    private const string GarageSceneName = "Garage";
    private const string StatsSceneName = "stats";

    [SerializeField] private SceneSwitcher sceneSwitcher;

    private Canvas mainCanvas;
    private Button googleSignInBtn;
    private Button multiplayerBtn;
    private TMP_Text playerNameText;

    private void Start()
    {
        FirebaseManager.EnsureExists();
        LeaderboardManager.EnsureExists();

        GameObject pnObj = GameObject.Find("playername");
        if (pnObj != null)
            playerNameText = pnObj.GetComponent<TMP_Text>();

        GameObject profileBtn = GameObject.Find("Profile");
        if (profileBtn != null)
        {
            Button btn = profileBtn.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => { PlayClickSound(); OnProfilePressed(); });
        }

        GameObject garageBtn = GameObject.Find("Garage");
        if (garageBtn != null)
        {
            Button btn = garageBtn.GetComponent<Button>();
            if (btn != null)
                btn.onClick.AddListener(() => { PlayClickSound(); OnGaragePressed(); });
        }

        GameObject mpBtn = GameObject.Find("Multiplayer");
        if (mpBtn != null)
            multiplayerBtn = mpBtn.GetComponent<Button>();

        BuildGoogleSignInUI();
    }

    private void PlayClickSound()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.playButtonSound();
    }

    private void BuildGoogleSignInUI()
    {
        mainCanvas = FindFirstObjectByType<Canvas>();
        if (mainCanvas == null) return;

        string currentName = PlayerPrefs.GetString("PlayerName", "Player");
        PhotonNetwork.NickName = currentName;
        if (playerNameText != null)
            playerNameText.text = currentName;

        googleSignInBtn = CreateButton(mainCanvas.transform, "Google Sign In", new Vector2(0f, 300f), 360f, 55f, new Color(0.2f, 0.5f, 0.2f));

        bool signedIn = currentName != "Player";
        if (signedIn)
        {
            googleSignInBtn.gameObject.SetActive(false);
            if (multiplayerBtn != null) multiplayerBtn.interactable = true;
            return;
        }
        if (multiplayerBtn != null) multiplayerBtn.interactable = false;

        googleSignInBtn.onClick.RemoveAllListeners();
        googleSignInBtn.onClick.AddListener(async () =>
        {
            PlayClickSound();
            googleSignInBtn.interactable = false;

            string googleName = await GoogleDesktopAuth.GetUserName();

            if (string.IsNullOrEmpty(googleName))
            {
                googleSignInBtn.interactable = true;
                return;
            }

            PlayerPrefs.SetString("PlayerName", googleName);
            PhotonNetwork.NickName = googleName;
            if (playerNameText != null)
                playerNameText.text = googleName;
            googleSignInBtn.gameObject.SetActive(false);
            if (multiplayerBtn != null) multiplayerBtn.interactable = true;
            WindowHelper.BringToFront();
        });
    }

    public void OnSinglePlayerPressed()
    {
        PlayClickSound();
        GameSession.Instance.IsSelectingFromLobby = false;
        GameSession.Instance.CurrentMode = GameSession.GameMode.SinglePlayer;
        sceneSwitcher.SceneLoder(GarageSceneName);
    }

    public void OnMultiplayerPressed()
    {
        PlayClickSound();
        GameSession.Instance.IsSelectingFromLobby = false;
        GameSession.Instance.CurrentMode = GameSession.GameMode.MultiplayerHost;
        sceneSwitcher.SceneLoder("MultiplayerMenu");
    }

    public void OnProfilePressed()
    {
        PlayClickSound();
        sceneSwitcher.SceneLoder(StatsSceneName);
    }

    public void OnGaragePressed()
    {
        PlayClickSound();
        sceneSwitcher.SceneLoder(GarageSceneName);
    }

    public void OnQuitPressed()
    {
        PlayClickSound();
        sceneSwitcher.QuitGame();
    }

    private TMP_Text CreateText(Transform parent, string text, Vector2 pos, int fontSize, TextAlignmentOptions align)
    {
        GameObject go = new GameObject("Text", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(600f, 40f);
        rt.anchoredPosition = pos;

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.color = Color.white;

        return tmp;
    }

    private Button CreateButton(Transform parent, string label, Vector2 pos, float width, float height, Color color)
    {
        GameObject go = new GameObject(label + "Button", typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(width, height);
        rt.anchoredPosition = pos;

        Image img = go.AddComponent<Image>();
        img.color = color;

        Button btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        GameObject labelGo = new GameObject("Label", typeof(RectTransform));
        labelGo.transform.SetParent(go.transform, false);

        RectTransform labelRt = labelGo.GetComponent<RectTransform>();
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.sizeDelta = Vector2.zero;

        TMP_Text tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = 24;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;

        return btn;
    }
}
