using System.Collections.Generic;
using UnityEngine;

// Red back lights while braking or drifting.
// Targets the "Taillights_*" renderers every car prefab ships with and swaps their materials
// between the normal look and a bright red glow — no prefab setup needed.
// Materials are cloned per renderer (.materials) so only this car's lamps change.
// Created at runtime via BrakeLight.Ensure(gameObject, controller) by the car controller.
public class BrakeLight : MonoBehaviour {

    [Header("when it lights up")]
    [Tooltip("driftAmount where the back lights come on without touching the brakes")]
    [Range(0f, 1f)] public float driftStartAmount = 0.1f;

    [Header("look")]
    [Tooltip("colour of the lit lamp")]
    public Color onColor = new Color(1f, 0.07f, 0.05f);
    [Tooltip("HDR multiplier — above 1 the lamp also blooms when post processing is on")]
    [Range(0.5f, 6f)] public float onIntensity = 2.5f;
    [Tooltip("how much the lit colour takes over the lamp texture")]
    [Range(0f, 1f)] public float tintAmount = 0.85f;

    private PhotonCarController controller;
    private Material[] mats;        // per-renderer clones , safe to recolour
    private Color[] baseColor;
    private Color[] baseEmission;
    private bool[] hadEmission;
    private bool lit;
    private bool wired;             // init runs once , a car without taillights stays dark

    // adds (or reuses) the component on the given GameObject and wires the car controller
    public static BrakeLight Ensure(GameObject go, PhotonCarController car) {
        if (go == null || car == null) return null;

        BrakeLight light = go.GetComponent<BrakeLight>();
        if (light == null) light = go.AddComponent<BrakeLight>();
        light.controller = car;

        if (!light.wired) light.Initialize();
        return light;
    }

    void Initialize() {
        wired = true;

        List<Material> found = new List<Material>();
        List<Color> colors = new List<Color>();
        List<Color> emissions = new List<Color>();
        List<bool> had = new List<bool>();

        foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) {
            if (r == null || !r.name.ToLower().Contains("taillight")) continue;

            // .materials clones the shared material , so only this car's lamps are recoloured
            foreach (Material m in r.materials) {
                if (m == null) continue;

                bool canEmit = m.HasProperty("_EmissionColor");

                found.Add(m);
                colors.Add(m.HasProperty("_Color") ? m.GetColor("_Color") : Color.white);
                emissions.Add(canEmit ? m.GetColor("_EmissionColor") : Color.black);
                had.Add(canEmit && m.IsKeywordEnabled("_EMISSION"));

                if (!m.HasProperty("_Color") && !canEmit)
                    Debug.LogWarning("BrakeLight: " + r.name + " material has neither _Color nor _EmissionColor — lamp cannot light up", m);
            }
        }

        if (found.Count == 0) {
            Debug.LogWarning("BrakeLight: no Taillights renderer found on " + gameObject.name);
            return;
        }

        mats = found.ToArray();
        baseColor = colors.ToArray();
        baseEmission = emissions.ToArray();
        hadEmission = had.ToArray();
    }

    void Update() {
        if (controller == null || mats == null) return;

        // foot brake / handbrake / AI brake , or an actual slide turns the lamps on
        bool on = controller.IsBraking || controller.driftAmount >= driftStartAmount;
        if (on == lit) return;
        lit = on;

        for (int i = 0; i < mats.Length; i++) {
            Material m = mats[i];
            if (m == null) continue;

            if (on) {
                if (m.HasProperty("_Color"))
                    m.SetColor("_Color", Color.Lerp(baseColor[i], onColor * onIntensity, tintAmount));

                if (m.HasProperty("_EmissionColor")) {
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", onColor * onIntensity);
                }
            }
            else {
                if (m.HasProperty("_Color"))
                    m.SetColor("_Color", baseColor[i]);

                if (m.HasProperty("_EmissionColor")) {
                    m.SetColor("_EmissionColor", baseEmission[i]);
                    if (!hadEmission[i]) m.DisableKeyword("_EMISSION");
                }
            }
        }
    }
}
