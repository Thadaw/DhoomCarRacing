using UnityEngine;

// Leaves black tire marks under the wheels while they are skidding.
// Works with any WheelCollider setup — create at runtime with TireMarks.Ensure(gameObject, wheels)
public class TireMarks : MonoBehaviour {

    [Tooltip("wheel slip needed before the tyre leaves a black mark")]
    [Range(0.05f, 1f)] public float skidSlipThreshold = 0.25f;
    [Range(0.05f, 0.6f)] public float markWidth = 0.22f;
    [Tooltip("seconds before the tire mark fades away")]
    [Range(1, 15)] public float markDuration = 6f;

    private WheelCollider[] wheels;
    private TrailRenderer[] trails;
    private static Material markMaterial;

    // adds (or reuses) the component on the given GameObject and wires up the wheels
    public static TireMarks Ensure(GameObject go, WheelCollider[] wheelColliders) {
        if (go == null || wheelColliders == null) return null;

        // filter out missing wheels
        int count = 0;
        foreach (WheelCollider wc in wheelColliders) if (wc != null) count++;
        if (count == 0) return null;

        TireMarks marks = go.GetComponent<TireMarks>();
        if (marks == null) marks = go.AddComponent<TireMarks>();

        if (marks.wheels == null || marks.wheels.Length != count) {
            WheelCollider[] clean = new WheelCollider[count];
            int j = 0;
            foreach (WheelCollider wc in wheelColliders) if (wc != null) clean[j++] = wc;
            marks.Initialize(clean);
        }

        return marks;
    }

    public void Initialize(WheelCollider[] wheelColliders) {
        wheels = wheelColliders;
        trails = new TrailRenderer[wheels.Length];
        for (int i = 0; i < wheels.Length; i++) trails[i] = CreateTrail(i);
    }

    TrailRenderer CreateTrail(int index) {
        GameObject mark = new GameObject("TireMark_" + index);
        TrailRenderer trail = mark.AddComponent<TrailRenderer>();
        trail.time = markDuration;
        trail.minVertexDistance = 0.03f;
        trail.startWidth = markWidth;
        trail.endWidth = markWidth;
        trail.numCapVertices = 2;
        trail.numCornerVertices = 2;
        trail.startColor = new Color(0f, 0f, 0f, 0.8f);
        trail.endColor = new Color(0f, 0f, 0f, 0f);
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;

        Material mat = MarkMaterial();
        if (mat != null) trail.material = mat;

        return trail;
    }

    static Material MarkMaterial() {
        if (markMaterial != null) return markMaterial;

        // unlit shader that supports vertex alpha so the mark can fade out
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) return null;

        markMaterial = new Material(shader);
        return markMaterial;
    }

    void Update() {
        if (wheels == null || trails == null) return;

        for (int i = 0; i < wheels.Length; i++) {
            if (wheels[i] == null || trails[i] == null) continue;

            bool grounded = wheels[i].GetGroundHit(out WheelHit hit);
            bool skidding = grounded &&
                (Mathf.Abs(hit.sidewaysSlip) > skidSlipThreshold || Mathf.Abs(hit.forwardSlip) > skidSlipThreshold);

            // contact patch = wheel centre pushed down to the ground
            wheels[i].GetWorldPose(out Vector3 pos, out _);
            trails[i].transform.position = pos - wheels[i].transform.up * wheels[i].radius;

            if (trails[i].emitting != skidding) trails[i].emitting = skidding;
        }
    }

    // clean the marks up with the car
    void OnDestroy() {
        if (trails == null) return;
        foreach (TrailRenderer trail in trails) {
            if (trail != null) Destroy(trail.gameObject);
        }
    }
}
