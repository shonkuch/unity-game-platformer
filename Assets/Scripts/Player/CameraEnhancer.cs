using UnityEngine;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineCamera))]

public class CameraEnhancer : MonoBehaviour
{
    [Header("References")]
    public Transform target;

    [Header("Tilt Settings")]
    public float maxTiltAngle = 3f;
    
    public float tiltSmoothSpeed = 5f;
    public float velocitySensitivity = 0.2f;

    private CinemachineCamera vcam;
    private Rigidbody2D targetRb;
    private Vector3 lastPosition;
    private float currentTilt = 0f;

    void Start()
    {
        vcam = GetComponent<CinemachineCamera>();
        if (target != null)
        {
            targetRb = target.GetComponent<Rigidbody2D>();
            lastPosition = target.position;
        }
    }

    void LateUpdate()
    {
        if (vcam == null || target == null) return;

        float targetVelocityX = 0f;
        if (targetRb != null)
        {
            targetVelocityX = targetRb.linearVelocity.x;
        }
        else
        {
            targetVelocityX = (target.position.x - lastPosition.x) / Time.deltaTime;
            lastPosition = target.position;
        }

        float targetTilt = -targetVelocityX * velocitySensitivity;
        targetTilt = Mathf.Clamp(targetTilt, -maxTiltAngle, maxTiltAngle);

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, Time.deltaTime * tiltSmoothSpeed);

        var lens = vcam.Lens;
        lens.Dutch = currentTilt;
        vcam.Lens = lens;
    }
}
