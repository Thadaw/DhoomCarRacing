using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
  public static AudioManager instance;

  [SerializeField] AudioClip buttonSfx;
  [SerializeField] AudioClip mainMenuMusic;
  [SerializeField] AudioClip MainGameMusic;

  private AudioSource musicSource;
  private AudioSource uiSource;


  public void Awake()
  {
    if(instance != null)
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
  }

  public void playMenuMusic()
  {
    if (!IsReady()) return;

    if (musicSource.clip == mainMenuMusic && musicSource.isPlaying)
      return;

    musicSource.clip = mainMenuMusic;
    musicSource.playOnAwake = true;
    musicSource.volume = 0.2f;
    musicSource.Play();
  }

   public void playMainGame()
  {
    if (!IsReady()) return;

    musicSource.clip = MainGameMusic;
    musicSource.playOnAwake = true;
    musicSource.volume = 0.2f;
    musicSource.Play();
  }

  public void playButtonSound()
  {
    if (!IsReady()) return;

    uiSource.volume = 0.7f;
    uiSource.PlayOneShot(buttonSfx);
  }

  // Returns true only if this AudioManager has valid AudioSources set up.
  // Prevents NullReferenceExceptions if Awake() failed or hasn't run yet.
  private bool IsReady()
  {
    if (musicSource == null || uiSource == null)
    {
      Debug.LogWarning("AudioManager: not ready (AudioSources missing). " +
          "Make sure an AudioManager with 2 AudioSource components exists, " +
          "starting from the MainMenu scene.");
      return false;
    }
    return true;
  }
}