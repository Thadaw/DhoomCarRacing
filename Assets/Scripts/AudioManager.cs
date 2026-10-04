using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager instance;

    [SerializeField] AudioClip buttonSfx;
    [SerializeField] AudioClip mainMenuMusic;
    [SerializeField] AudioClip MainGameMusic;

    private AudioSource musicSource;
    private AudioSource uiSource;

    private const string MusicVolumeKey = "MusicVolume";
    private const string SFXVolumeKey = "SFXVolume";
    private const string MusicMuteKey = "MusicMute";
    private const string SFXMuteKey = "SFXMute";

    private const float DefaultMusicVolume = 0.12f;
    private const float DefaultSFXVolume = 0.7f;

    // In-game track music stays low and clearly under the car sound (engine sits
    // at 0.85), so the slider value is capped while the race track is playing.
    private const float MainGameMusicVolumeCap = 0.15f;

    private bool playingGameMusic;

    public void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        else
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        AudioSource[] source = GetComponents<AudioSource>();

        if (source.Length < 2)
        {
            Debug.LogError("AudioManager: expected 2 AudioSource components on this GameObject " +
                "(index 0 = music, index 1 = UI sfx), but found " + source.Length +
                ". Add the missing AudioSource component(s) in the Inspector.");
            return;
        }

        musicSource = source[0];
        uiSource = source[1];

        LoadSettings();
    }

    private void LoadSettings()
    {
        if (!IsReady()) return;

        float musicVol = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
        float sfxVol = PlayerPrefs.GetFloat(SFXVolumeKey, DefaultSFXVolume);
        bool musicMute = PlayerPrefs.GetInt(MusicMuteKey, 0) == 1;
        bool sfxMute = PlayerPrefs.GetInt(SFXMuteKey, 0) == 1;

        musicSource.volume = musicMute ? 0f : musicVol;
        musicSource.mute = musicMute;
        uiSource.volume = sfxMute ? 0f : sfxVol;
        uiSource.mute = sfxMute;
    }

    private void ApplyMusicVolume()
    {
        if (!IsReady()) return;

        bool muted = PlayerPrefs.GetInt(MusicMuteKey, 0) == 1;
        float vol = PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
        if (playingGameMusic) vol = Mathf.Min(vol, MainGameMusicVolumeCap);

        musicSource.volume = muted ? 0f : vol;
        musicSource.mute = muted;
    }

    // --- Music Volume ---
    public void SetMusicVolume(float vol)
    {
        if (!IsReady()) return;
        PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(vol));
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public float GetMusicVolume()
    {
        return PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
    }

    // --- SFX Volume ---
    public void SetSFXVolume(float vol)
    {
        if (!IsReady()) return;
        vol = Mathf.Clamp01(vol);
        bool muted = PlayerPrefs.GetInt(SFXMuteKey, 0) == 1;
        uiSource.volume = muted ? 0f : vol;
        PlayerPrefs.SetFloat(SFXVolumeKey, vol);
        PlayerPrefs.Save();
    }

    public float GetSFXVolume()
    {
        return PlayerPrefs.GetFloat(SFXVolumeKey, DefaultSFXVolume);
    }

    // --- Music Mute ---
    public void SetMusicMute(bool muted)
    {
        if (!IsReady()) return;
        PlayerPrefs.SetInt(MusicMuteKey, muted ? 1 : 0);
        PlayerPrefs.Save();
        ApplyMusicVolume();
    }

    public bool IsMusicMuted()
    {
        return PlayerPrefs.GetInt(MusicMuteKey, 0) == 1;
    }

    // --- SFX Mute ---
    public void SetSFXMute(bool muted)
    {
        if (!IsReady()) return;
        uiSource.mute = muted;
        if (!muted)
            uiSource.volume = PlayerPrefs.GetFloat(SFXVolumeKey, DefaultSFXVolume);
        PlayerPrefs.SetInt(SFXMuteKey, muted ? 1 : 0);
        PlayerPrefs.Save();
    }

    public bool IsSFXMuted()
    {
        return PlayerPrefs.GetInt(SFXMuteKey, 0) == 1;
    }

    // --- Play Methods ---
    public void playMenuMusic()
    {
        if (!IsReady()) return;

        musicSource.Stop();
        musicSource.clip = mainMenuMusic;
        musicSource.playOnAwake = true;
        playingGameMusic = false;
        ApplyMusicVolume();
        musicSource.Play();
    }

    public void playMainGame()
    {
        if (!IsReady()) return;

        musicSource.Stop();
        musicSource.clip = MainGameMusic;
        musicSource.playOnAwake = true;
        playingGameMusic = true;
        ApplyMusicVolume();
        musicSource.Play();
    }

    public void playButtonSound()
    {
        if (!IsReady()) return;

        float sfxVol = PlayerPrefs.GetFloat(SFXVolumeKey, DefaultSFXVolume);
        bool muted = PlayerPrefs.GetInt(SFXMuteKey, 0) == 1;
        uiSource.volume = muted ? 0f : sfxVol;
        uiSource.mute = muted;
        uiSource.PlayOneShot(buttonSfx);
    }

    private bool IsReady()
    {
        if (musicSource == null || uiSource == null)
        {
            Debug.LogWarning("AudioManager: not ready (AudioSources missing).");
            return false;
        }
        return true;
    }
}
