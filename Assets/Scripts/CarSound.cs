using UnityEngine;

public class CarSound : MonoBehaviour
{
    private AudioSource startSource;
    private AudioSource runSource;
    private AudioSource crashSource;
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

        AudioClip startClip = Resources.Load<AudioClip>("Sounds/start acceleration");
        AudioClip runClip = Resources.Load<AudioClip>("Sounds/caracceleration");
        AudioClip crashClip = Resources.Load<AudioClip>("Sounds/carcrash");
        AudioClip bgmClip = Resources.Load<AudioClip>("Sounds/SadenessBGM");

        if (startClip != null)
        {
            startSource = gameObject.AddComponent<AudioSource>();
            startSource.clip = startClip;
            startSource.loop = true;
            startSource.spatialBlend = 0f;
            startSource.volume = 0.85f;
            startSource.Play();
            Debug.Log("CarSound: Playing start acceleration");
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

        if (crashClip != null)
        {
            crashSource = gameObject.AddComponent<AudioSource>();
            crashSource.clip = crashClip;
            crashSource.loop = false;
            crashSource.spatialBlend = 0f;
            crashSource.volume = 1f;
            Debug.Log("CarSound: Loaded carcrash");
        }
        else
        {
            Debug.LogWarning("CarSound: Could not load carcrash clip");
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

        if (crashSource != null)
        {
            crashSource.Stop();
            crashSource.volume = 0f;
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

            if (startSource != null)
                startSource.Stop();
        }

        if (!raceStarted) return;

        float throttle = Input.GetAxis("Vertical");
        bool braking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Space);
        bool accelerating = throttle > 0.1f && !braking;

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

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed > 2f && crashSource != null && !crashSource.isPlaying)
        {
            crashSource.volume = Mathf.Clamp01(impactSpeed / 20f);
            crashSource.pitch = Random.Range(0.9f, 1.1f);
            crashSource.Play();
            Debug.Log("CarSound: Hit " + collision.gameObject.name + " at " + impactSpeed.ToString("F1"));
        }

        if (runSource != null && raceStarted)
        {
            runSource.Stop();
            runSource.Play();
        }
    }
}
