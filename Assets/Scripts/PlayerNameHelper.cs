using UnityEngine;
using Photon.Pun;

public static class PlayerNameHelper
{
    private const string PrefKey = "PlayerName";
    private const string GoogleSignedInKey = "GoogleSignedIn";
    private const string DefaultName = "Player";

    public static string GetPlayerName()
    {
        return PlayerPrefs.GetString(PrefKey, DefaultName);
    }

    public static bool IsGoogleSignedIn()
    {
        return PlayerPrefs.GetInt(GoogleSignedInKey, 0) == 1;
    }

    public static void SetPlayerName(string name)
    {
        if (string.IsNullOrEmpty(name))
            name = DefaultName;

        PlayerPrefs.SetString(PrefKey, name);
        PlayerPrefs.Save();

        PhotonNetwork.NickName = name;
    }

    public static void SetGoogleName(string googleName)
    {
        if (string.IsNullOrEmpty(googleName))
            return;

        SetPlayerName(googleName);
        PlayerPrefs.SetInt(GoogleSignedInKey, 1);
        PlayerPrefs.Save();
    }

    public static void Logout()
    {
        PlayerPrefs.SetString(PrefKey, DefaultName);
        PlayerPrefs.SetInt(GoogleSignedInKey, 0);
        PlayerPrefs.Save();

        PhotonNetwork.NickName = DefaultName;
    }
}
