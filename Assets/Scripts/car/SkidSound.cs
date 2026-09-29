using UnityEngine;

// Looping skid sound whose volume follows the wheels' slip amount.
// Created at runtime via SkidSound.Ensure(gameObject, controller) by the car controller.
public class SkidSound : MonoBehaviour {

    [Tooltip("average wheel slip where the sound is still silent")]
    [Range(0f, 1f)] public float silenceSlipThreshold = 0.15f;
    [Tooltip("average wheel slip where the sound reaches full volume")]
    [Range(0.1f, 3f)] public float fullSlipThreshold = 1f;
    [Range(0f, 1f)] public float maxVolume = 0.8f;
    [Tooltip("how fast the volume chases the slip amount")]
    [Range(1f, 30f)] public float volumeLerpSpeed = 8f;
    [Tooltip("pitch rises slightly with slip")]
    [Range(1f, 1.5f)] public float maxPitch = 1.15f;

    private PhotonCarController controller;
    private AudioSource source;
    private readonly WheelCollider[] wheels = new WheelCollider[4];
    private bool isLocal;

    // adds (or reuses) the component on the given GameObject and wires the car controller
    public static SkidSound Ensure(GameObject go, PhotonCarController car) {
        if (go == null || car == null) return null;

        SkidSound skid = go.GetComponent<SkidSound>();
        if (skid == null) skid = go.AddComponent<SkidSound>();
        skid.controller = car;
        return skid;
    }

    void Start() {
        // only the local player's car makes skid noise (same rule as CarSound)
        isLocal = controller != null && controller.isLocalPlayerCar && !controller.useExternalInput;
        if (!isLocal) {
            enabled = false;
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>("Sounds/SkidBreakMusic");
        if (clip == null) {
            Debug.LogWarning("SkidSound: Could not load Sounds/SkidBreakMusic");
            enabled = false;
            return;
        }

        source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = true;
        source.spatialBlend = 0f;
        source.volume = 0f;
        source.pitch = 1f;
        // armed but silent — starts on the first real skid
    }

    void Update() {
        if (source == null || controller == null) return;

        wheels[0] = controller.frontLeftWheel;
        wheels[1] = controller.frontRightWheel;
        wheels[2] = controller.rearLeftWheel;
        wheels[3] = controller.rearRightWheel;

        float slip = AverageSlip();

        // volume target follows the slip amount directly
        float targetVolume = slip <= silenceSlipThreshold
            ? 0f
            : Mathf.InverseLerp(silenceSlipThreshold, fullSlipThreshold, slip) * maxVolume;

        float normalized = maxVolume > 0f ? targetVolume / maxVolume : 0f;

        if (targetVolume > 0f && !source.isPlaying) source.Play();

        // chase the target so the sound swells in and out with the slide
        source.volume = Mathf.MoveTowards(
            source.volume,
            targetVolume,
            Time.deltaTime * volumeLerpSpeed * maxVolume);

        source.pitch = Mathf.Lerp(1f, maxPitch, normalized);

        // fully faded out — cut the loop
        if (source.volume <= 0.001f && targetVolume <= 0f && source.isPlaying)
            source.Stop();
    }

    private float AverageSlip() {
        float total = 0f;
        int count = 0;

        for (int i = 0; i < wheels.Length; i++) {
            if (wheels[i] != null && wheels[i].GetGroundHit(out WheelHit hit)) {
                total += Mathf.Abs(hit.forwardSlip) + Mathf.Abs(hit.sidewaysSlip);
                count++;
            }
        }

        return count > 0 ? total / count : 0f;
    }
}
