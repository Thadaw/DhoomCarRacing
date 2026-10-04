using UnityEngine;

public class CarSound : MonoBehaviour
{
    private AudioSource startSource;
    private AudioSource runSource;

    private AudioSource bgmSource;
    private bool raceStarted;
    private bool isLocal;

    private PhotonCarController cc;
    private float prevSpeed;
    private int prevGear;
    private float shiftTimer;

    [Header("volume")]
    [Tooltip("fallback music volume when AudioManager is missing — race BGM stays well below the engine")]
    [Range(0f, 0.5f)] public float bgmVolume = 0.12f;
    [Range(0f, 1f)] public float carVolume = 0.85f;

    [Header("engine response")]
    [Tooltip("engine volume while coasting / idling — the loop never goes fully silent")]
    [Range(0f, 0.5f)] public float idleVolume = 0.15f;
    [Tooltip("engine pitch while idling")]
    [Range(0.4f, 1f)] public float idlePitch = 0.7f;
    [Tooltip("highest pitch at redline")]
    [Range(1f, 2f)] public float maxPitch = 1.6f;
    [Tooltip("how fast the volume chases throttle/speed")]
    [Range(1f, 40f)] public float volumeResponse = 14f;
    [Tooltip("how fast the pitch chases the engine RPM")]
    [Range(1f, 40f)] public float pitchResponse = 9f;
    [Tooltip("km/h per second of acceleration that counts as a full 'roar' boost")]
    [Range(20f, 200f)] public float fullRoarAcceleration = 80f;

    [Header("gear shift")]
    [Tooltip("number of simulated gears the pitch sweeps through")]
    [Range(2, 8)] public int gearCount = 5;
    [Tooltip("seconds the engine sound dips while the gearbox shifts")]
    [Range(0.05f, 0.5f)] public float shiftDuration = 0.2f;

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
        cc = GetComponent<PhotonCarController>();
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

        if (startClip != null)
        {
            startSource = gameObject.AddComponent<AudioSource>();
            startSource.clip = startClip;
            startSource.loop = false;
            startSource.spatialBlend = 0f;
            startSource.volume = carVolume;
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
            runSource.pitch = idlePitch;
            // Loops for the whole race — volume/pitch are shaped every frame below,
            // so the engine idles quietly instead of cutting out when off-throttle.
            runSource.Play();
            Debug.Log("CarSound: Loaded caracceleration");
        }
        else
        {
            Debug.LogWarning("CarSound: Could not load caracceleration clip");
        }

        // Race BGM normally comes from AudioManager (playMainGame), which respects
        // the player's music slider/mute settings. We only start our own low-volume
        // track when AudioManager is missing (Play pressed directly on the race
        // scene) — exactly one background music is ever playing, never two.
        if (AudioManager.instance == null)
        {
            AudioClip bgmClip = Resources.Load<AudioClip>("Sounds/SadenessBGM");
            if (bgmClip != null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.clip = bgmClip;
                bgmSource.loop = true;
                bgmSource.spatialBlend = 0f;
                bgmSource.volume = bgmVolume;
                bgmSource.Play();
                Debug.Log("CarSound: AudioManager missing — fallback BGM only");
            }
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
        if (runSource == null) return;

        // Throttle from the controller so external input is heard too.
        float throttle = cc != null ? cc.ThrottleInput : Input.GetAxis("Vertical");
        float gas = Mathf.Max(0f, throttle);

        float speed = cc != null ? cc.CarSpeed() : 0f;
        float maxSpeed = cc != null && cc.maxSpeed > 0f ? cc.maxSpeed : 1f;
        float speedRatio = Mathf.Clamp01(speed / maxSpeed);

        float dt = Time.deltaTime;
        float acceleration = dt > 0f ? (speed - prevSpeed) / dt : 0f;
        prevSpeed = speed;
        float roar = Mathf.Clamp01(acceleration / fullRoarAcceleration);

        // Launch rev hands over to the engine loop as soon as we open the throttle.
        if (gas > 0.1f && startSource != null && startSource.isPlaying)
            startSource.Stop();

        // --- Simulated gearbox -------------------------------------------------
        // Pitch climbs inside the current gear (gearT 0 -> 1) and wraps back down
        // at every shift point — that wrap IS the classic RPM drop. The smoothed
        // pitch chase turns the jump into a quick burble like a real gear change.
        float gearPos = Mathf.Clamp(speedRatio, 0f, 0.9999f) * gearCount;
        int gear = Mathf.Min((int)gearPos, gearCount - 1);
        float gearT = gearPos - gear;

        if (gear > prevGear && shiftTimer <= 0f)
            shiftTimer = shiftDuration;
        prevGear = gear;
        if (shiftTimer > 0f) shiftTimer -= dt;

        // RPM sweep in the gear + throttle revving (strongest at low speed).
        float rpmNorm = Mathf.Clamp01(0.1f + 0.9f * gearT + gas * 0.15f * (1f - speedRatio));
        // -----------------------------------------------------------------------

        // Loudness: throttle first, speed second, plus a roar while actually accelerating.
        float load = Mathf.Max(gas, speedRatio);
        float targetVolume = Mathf.Lerp(idleVolume, carVolume,
            Mathf.Clamp01(load + roar * 0.35f));

        float targetPitch = idlePitch + (maxPitch - idlePitch) * rpmNorm + roar * 0.15f;
        targetPitch = Mathf.Min(targetPitch, maxPitch);

        // Torque cut during the shift — a short volume dip sells the gear change.
        if (shiftTimer > 0f)
            targetVolume *= 0.7f;

        if (!runSource.isPlaying) runSource.Play();

        // Smooth chase — no volume/pitch popping.
        runSource.volume = Mathf.Lerp(runSource.volume, targetVolume, dt * volumeResponse);
        runSource.pitch = Mathf.Lerp(runSource.pitch, targetPitch, dt * pitchResponse);
    }
}
