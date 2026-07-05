using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowCar : MonoBehaviour
{
    public Transform carTransform;
    public Transform cameraPointTransform;

    private Vector3 velocity = Vector3.zero;
    [SerializeField] private float smoothTime = 0.15f;

    void LateUpdate()
    {
        if (carTransform == null || cameraPointTransform == null) return;

        transform.LookAt(carTransform);
        transform.position = Vector3.SmoothDamp(transform.position, cameraPointTransform.position, ref velocity, smoothTime);
    }
    
}