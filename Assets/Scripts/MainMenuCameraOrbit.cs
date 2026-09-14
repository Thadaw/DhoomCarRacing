using UnityEngine;

public class MainMenuCameraOrbit : MonoBehaviour
{
    [Header("Orbit Center")]
    public Vector3 orbitCenter = Vector3.zero;

    [Header("Orbit Settings")]
    public float distance = 7f;
    public float height = 2f;
    public float orbitSpeed = 3f;
    public float heightAmplitude = 0.3f;
    public float heightFrequency = 0.15f;

    [Header("Look Settings")]
    public float lookAtHeightOffset = 0.5f;
    public float tiltAngle = 2f;

    private float time = 0f;

    void Start()
    {
        transform.position = orbitCenter + new Vector3(0, height, -distance);
    }

    void LateUpdate()
    {
        time += Time.deltaTime;

        float zoomCycle = Mathf.Sin(time * heightFrequency * Mathf.PI * 2f);
        float currentDistance = distance + zoomCycle * 0.8f;

        float xOffset = Mathf.Sin(time * 0.3f) * 0.5f;
        float yOffset = Mathf.Sin(time * heightFrequency * Mathf.PI * 2f) * heightAmplitude;

        Vector3 orbitPos = orbitCenter + new Vector3(
            xOffset,
            height + yOffset,
            -currentDistance
        );

        transform.position = orbitPos;

        Vector3 lookTarget = orbitCenter + Vector3.up * lookAtHeightOffset;
        Vector3 dir = lookTarget - transform.position;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
            transform.Rotate(Vector3.right, tiltAngle, Space.Self);
        }
    }
}
