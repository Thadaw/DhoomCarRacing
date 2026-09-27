using UnityEngine;

public class CarSound : MonoBehaviour
{
    private AudioSource startSource;
    private AudioSource runSource;

    private AudioSource bgmSource;
    private bool raceStarted;
    private bool isLocal;

    void OnEnable()
    {
        PlayerLapTracker.OnLocalPlayerFinished += StopAllSounds;
    }

    void OnDisable()
    {
        PlayerLapTracker.OnLocalPlayerFinished -= StopAllSounds;
    }

    void Start()
    {
        PhotonCarController cc = GetComponent<PhotonCarController>();
        isLocal = cc != null && cc.isLocalPlayerCar;

        // Car sound (engine + music) belongs to the real player only.
        // AI cars and other players' cars must stay completely silent.
        if (!isLocal)
        {
            enabled = false;
            return;
        }

        AudioClip startClip = Resources.Load<AudioClip>("Sounds/start acceleration");
        AudioClip runClip = Resources.Load<AudioClip>("Sounds/caracceleration");

        AudioClip bgmClip = Resources.Load<AudioClip>("Sounds/SadenessBGM");

        if (startClip != null)
        {
            startSource = gameObject.AddComponent<AudioSource>();
            startSource.clip = startClip;
            startSource.loop = false;
            startSource.spatialBlend = 0f;
            startSource.volume = 0.85f;
            // Armed but silent — it plays when the countdown reaches GO.
            Debug.Log("CarSound: Start rev armed (plays at GO)");
        }
        else
        {
            Debug.LogWarning("CarSound: Could not load start acceleration clip");
        }

        if (runClip != null)
        {
            runSource = gameObject.AddComponent<AudioSource>();
            runSource.clip = runClip;
            runSource.loop = true;
            runSource.spatialBlend = 0f;
            runSource.volume = 0f;
            runSource.Play();
            Debug.Log("CarSound: Loaded caracceleration");
        }
        else
        {
            Debug.LogWarning("CarSound: Could not load caracceleration clip");
        }

        if (bgmClip != null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.clip = bgmClip;
            bgmSource.loop = true;
            bgmSource.spatialBlend = 0f;
            bgmSource.volume = 0.25f;
            bgmSource.Play();
            Debug.Log("CarSound: Playing background music");
        }
        else
        {
            Debug.LogWarning("CarSound: Could not load background music");
        }
    }

    void StopAllSounds()
    {
        if (startSource != null)
        {
            startSource.Stop();
            startSource.volume = 0f;
        }

        if (runSource != null)
        {
            runSource.Stop();
            runSource.volume = 0f;
        }


        if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.volume = 0f;
        }

        enabled = false;
        Debug.Log("CarSound: All sounds stopped - race finished");
    }

    void Update()
    {
        if (!isLocal) return;
        if (RaceManager.Instance == null) return;

        if (!raceStarted && RaceManager.Instance.raceStarted)
        {
            raceStarted = true;
            Debug.Log("CarSound: Race started");

            // Launch rev fires exactly when the countdown reaches GO.
            if (startSource != null)
                startSource.Play();
        }

        if (!raceStarted) return;

        float throttle = Input.GetAxis("Vertical");
        bool braking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Space);
        bool accelerating = throttle > 0.1f && !braking;

        // Hand the audio over to the running engine sound as soon as we drive off.
        if (accelerating && startSource != null && startSource.isPlaying)
            startSource.Stop();

        if (runSource != null)
        {
            if (accelerating)
            {
                if (!runSource.isPlaying)
                    runSource.Play();

                runSource.volume = 0.85f;

                PhotonCarController cc = GetComponent<PhotonCarController>();
                if (cc != null)
                {
                    float t = Mathf.Clamp01(cc.CarSpeed() / cc.maxSpeed);
                    runSource.pitch = Mathf.Lerp(0.8f, 1.5f, t);
                }
            }
            else
            {
                if (runSource.isPlaying)
                {
                    runSource.Stop();
                    Debug.Log("CarSound: Acceleration stopped - no throttle or braking");
                }
                runSource.volume = 0f;
            }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isLocal) return;

        string hitName = collision.gameObject.name.ToLower();

        if (hitName.Contains("checkpoint") || hitName.Contains("laptrigger"))
            return;

        if (collision.gameObject.GetComponent<CarSound>() != null)
            return;

        if (runSource != null && raceStarted)
        {
            runSource.Stop();
            runSource.Play();
        }
    }
}
