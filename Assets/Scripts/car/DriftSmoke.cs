using UnityEngine;
using UnityEngine.Rendering;

// White tyre smoke while drifting.
// Everything is built at runtime (procedural puff texture + a ParticleSystem per wheel) so no prefab setup is needed.
// Particles live in world space and keep living for their lifetime, so the cloud also lingers briefly after the drift ends.
// Created at runtime via DriftSmoke.Ensure(gameObject, controller) by the car controller.
public class DriftSmoke : MonoBehaviour {

    [Header("when it smokes")]
    [Tooltip("driftAmount below this counts as gripping , no smoke")]
    [Range(0f, 0.5f)] public float smokeStartAmount = 0.1f;
    [Tooltip("driftAmount where the smoke reaches full density")]
    [Range(0.1f, 1f)] public float smokeFullAmount = 0.7f;
    [Tooltip("smoke only from the rear wheels (the ones that break loose in a drift)")]
    public bool rearWheelsOnly = true;

    [Header("look")]
    [Tooltip("particles per second at a full drift")]
    [Range(1f, 60f)] public float maxRate = 28f;
    [Range(0.1f, 1f)] public float minSize = 0.45f;
    [Range(0.3f, 3f)] public float maxSize = 1.5f;
    [Tooltip("seconds a puff stays alive — this is what makes the smoke linger after the drift ends")]
    [Range(0.3f, 5f)] public float lifetime = 1.8f;
    public Color smokeColor = new Color(0.82f, 0.82f, 0.82f, 0.5f);

    private PhotonCarController controller;
    private WheelCollider[] wheels;
    private ParticleSystem[] systems;
    private static Texture2D puffTexture;
    private static Material puffMaterial;

    // adds (or reuses) the component on the given GameObject and wires the car controller
    public static DriftSmoke Ensure(GameObject go, PhotonCarController car) {
        if (go == null || car == null) return null;

        DriftSmoke smoke = go.GetComponent<DriftSmoke>();
        if (smoke == null) smoke = go.AddComponent<DriftSmoke>();
        smoke.controller = car;

        if (smoke.systems == null) smoke.Initialize(car);
        return smoke;
    }

    void Initialize(PhotonCarController car) {
        WheelCollider[] candidates =
            rearWheelsOnly && (car.rearLeftWheel != null || car.rearRightWheel != null)
                ? new[] { car.rearLeftWheel, car.rearRightWheel }
                : new[] { car.frontLeftWheel, car.frontRightWheel, car.rearLeftWheel, car.rearRightWheel };

        int count = 0;
        foreach (WheelCollider wc in candidates) if (wc != null) count++;
        if (count == 0) return;

        wheels = new WheelCollider[count];
        systems = new ParticleSystem[count];

        int j = 0;
        foreach (WheelCollider wc in candidates) {
            if (wc == null) continue;
            wheels[j] = wc;
            systems[j] = CreateSystem(j);
            j++;
        }
    }

    ParticleSystem CreateSystem(int index) {
        GameObject holder = new GameObject("DriftSmoke_" + index);
        holder.transform.SetParent(transform, false);

        ParticleSystem ps = holder.AddComponent<ParticleSystem>();
        Configure(ps);

        ParticleSystemRenderer renderer = holder.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = PuffMaterial();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        ps.Play();
        return ps;
    }

    void Configure(ParticleSystem ps) {
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.playOnAwake = true;
        main.startLifetime = lifetime;
        main.startSpeed = 0f;                 // motion comes from velocityOverLifetime below
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = smokeColor;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // stays behind as the car drives off
        main.gravityModifier = 0f;
        main.maxParticles = 256;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;           // driven from Update while drifting

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.3f;

        // gentle rise with a little sideways wander so it reads as smoke , not sparks
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.y = new ParticleSystem.MinMaxCurve(0.35f, 0.8f);
        velocity.x = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);
        velocity.z = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

        ParticleSystem.LimitVelocityOverLifetimeModule slow = ps.limitVelocityOverLifetime;
        slow.enabled = true;
        slow.space = ParticleSystemSimulationSpace.World;
        slow.limit = 1.2f;                    // smoke slows down instead of drifting forever
        slow.dampen = 0.6f;

        // the puff grows as it fades away
        ParticleSystem.SizeOverLifetimeModule size = ps.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.4f, 1f, 1f));

        // fade in fast , fade out over the whole life
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
        color.color = new ParticleSystem.MinMaxGradient(gradient);
    }

    void Update() {
        if (controller == null || wheels == null || systems == null) return;

        // 0 = gripping , 1 = full drift — same gate the skid sound uses
        float gate = Mathf.InverseLerp(smokeStartAmount, smokeFullAmount, controller.driftAmount);
        float rate = gate > 0f ? maxRate * gate : 0f;

        for (int i = 0; i < wheels.Length; i++) {
            if (wheels[i] == null || systems[i] == null) continue;

            // no smoke from a wheel hanging in the air
            bool grounded = wheels[i].GetGroundHit(out WheelHit _);

            wheels[i].GetWorldPose(out Vector3 pos, out _);
            systems[i].transform.position = pos - wheels[i].transform.up * wheels[i].radius;

            ParticleSystem.EmissionModule emission = systems[i].emission;
            emission.rateOverTime = grounded ? rate : 0f;
        }
    }

    // soft round blob generated in code — no art asset required
    static Texture2D PuffTexture() {
        if (puffTexture != null) return puffTexture;

        const int size = 64;
        puffTexture = new Texture2D(size, size, TextureFormat.RGBA32, false) {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        float half = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++) {
            for (int x = 0; x < size; x++) {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(half, half)) / half;
                float a = Mathf.Clamp01(1f - d);
                a *= a;                              // soft edge
                puffTexture.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }

        puffTexture.Apply();
        return puffTexture;
    }

    static Material PuffMaterial() {
        if (puffMaterial != null) return puffMaterial;

        // shaders in order of preference , first one found wins
        Shader shader = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("UI/Default");
        if (shader == null) return null;

        puffMaterial = new Material(shader) { mainTexture = PuffTexture() };
        return puffMaterial;
    }
}
