using System;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

[RequireComponent(typeof(CarStateMachine))]
public class Audio : MonoBehaviour {

    CarStateMachine stateMachine;

    [Header("low pitch")]
    public AudioClip lowAccelClip;
    [Range(.5f, 1.5f)] public float lowPitchMin = 1f;

    [Header("high pitch")]
    public AudioClip highAccelClip;
    [Range(4, 10)] public float lowPitchMax = 6f;
    [Range(.1f, .5f)] public float highPitchMultiplier = 0.25f;

    [Header("EQ / Filter Effects")]
    public AnimationCurve lowPassCurve;    // Control "Air/Clatter" (Highs) time : 0 , value : 10000 - time : 1 , value : 22000
    public AnimationCurve distortionCurve; // Control "Growl/Bite" (Mids)   time : 0 , value : 0 - time : 1 , value : 0.3
    public AnimationCurve highPassCurve;   // Control "Rumble" (Lows)       time : 0 , value : 10 - time 1 , value :200
    
    [Range(0, 20)] public float eqLerpSpeed = 5f;
    private float loadLerpIndex;

    // Components
    private AudioSource m_LowAccel;
    private AudioSource m_HighAccel;
    private AudioLowPassFilter lowPassFilter;
    private AudioHighPassFilter highPassFilter;
    private AudioDistortionFilter midDistortion;
    
    private bool m_StartedSound;

    [Header("debug")]
    public float pitch;
    private float maxRolloffDistance = 700;

    void Start() {
        stateMachine = GetComponent<CarStateMachine>();
    }

    private void FixedUpdate() {
        if (Camera.main == null) return;
        float camDist = (Camera.main.transform.position - transform.position).sqrMagnitude;

        if (m_StartedSound && camDist > maxRolloffDistance * maxRolloffDistance) {
            StopSound();
        }

        if (!m_StartedSound && camDist < maxRolloffDistance * maxRolloffDistance) {
            StartSound();
        }

        if (m_StartedSound) {
            CalculatePitch();
            CalculateEQ();
        }
    }

    void CalculateEQ() {
        // 'Load' is usually your vertical move input (W or Trigger)
        float targetLoad = Math.Clamp(stateMachine.moveInput.y, 0, 1);
        loadLerpIndex = Mathf.Lerp(loadLerpIndex, targetLoad, Time.deltaTime * eqLerpSpeed);

        // BAND 1: HIGHS (Low Pass Filter)
        // Under load, we open this up (e.g., 2000Hz to 15000Hz) to let the engine "scream"
        lowPassFilter.cutoffFrequency = lowPassCurve.Evaluate(loadLerpIndex);

        // BAND 2: MIDS/GROWL (Distortion)
        // Adding subtle distortion at 300Hz-2kHz range simulates the "stress" of the engine load
        midDistortion.distortionLevel = distortionCurve.Evaluate(loadLerpIndex);

        // BAND 3: LOWS (High Pass Filter)
        // We use this to cut out the muddy sub-bass when the engine is screaming at high RPMs
        highPassFilter.cutoffFrequency = highPassCurve.Evaluate(stateMachine.engineController.engineRPM / stateMachine.engineController.maxRPM);
    }

    void CalculatePitch() {
        float rpmNormalized = stateMachine.engineController.engineRPM / stateMachine.engineController.maxRPM;
        pitch = ULerp(lowPitchMin, lowPitchMax, rpmNormalized);
        pitch = Mathf.Min(lowPitchMax, pitch);

        m_LowAccel.pitch = pitch ;
        m_HighAccel.pitch = pitch * highPitchMultiplier;

        float highFade = Mathf.InverseLerp(0.2f, 0.8f, rpmNormalized);
        float lowFade = 1 - highFade;

        highFade = 1 - ((1 - highFade) * (1 - highFade));
        lowFade = 1 - ((1 - lowFade) * (1 - lowFade));

        // Injecting a little extra volume boost when under load
        float loadVolumeBoost = Mathf.Lerp(0.7f, 1.0f, loadLerpIndex);
        m_LowAccel.volume = lowFade * loadVolumeBoost;
        m_HighAccel.volume = highFade * loadVolumeBoost;
    }

    private static float ULerp(float from, float to, float value) {
        return (1.0f - value) * from + value * to;
    }

    private AudioSource SetUpEngineAudioSource(AudioClip clip) {
        AudioSource source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.volume = 0;
        source.spatialBlend = 1;
        source.loop = true;
        source.time = Random.Range(0f, clip.length);
        source.Play();
        source.minDistance = 5;
        source.maxDistance = maxRolloffDistance;
        source.dopplerLevel = 0;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        return source;
    }

    private void StartSound() {
        m_HighAccel = SetUpEngineAudioSource(highAccelClip);
        m_LowAccel = SetUpEngineAudioSource(lowAccelClip);

        // Add the "EQ" components to the High Accel source (where the engine detail is)
        lowPassFilter = m_HighAccel.gameObject.AddComponent<AudioLowPassFilter>();
        highPassFilter = m_HighAccel.gameObject.AddComponent<AudioHighPassFilter>();
        midDistortion = m_HighAccel.gameObject.AddComponent<AudioDistortionFilter>();

        // Set some safe defaults so it's not silent/distorted at start
        lowPassFilter.cutoffFrequency = 5000;
        highPassFilter.cutoffFrequency = 10; 
        midDistortion.distortionLevel = 0;

        m_StartedSound = true;
    }

    private void StopSound() {
        // Cleanup all audio components
        var sources = GetComponents<AudioSource>();
        foreach (var s in sources) Destroy(s);
        
        var lp = GetComponents<AudioLowPassFilter>();
        foreach (var f in lp) Destroy(f);

        var hp = GetComponents<AudioHighPassFilter>();
        foreach (var f in hp) Destroy(f);

        var dist = GetComponents<AudioDistortionFilter>();
        foreach (var d in dist) Destroy(d);

        m_StartedSound = false;
    }
}