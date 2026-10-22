using System;
using UnityEngine;

public class VisionActivatedLaser : MonoBehaviour
{
    [SerializeField] private GameObject platform;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private float platformLeftTarget;
    [SerializeField] private float platformRightTarget;
    [SerializeField] private float platformSpeed;
    private bool _targetLeft;

    private float Target => _targetLeft ? platformLeftTarget : platformRightTarget;

    private void FixedUpdate()
    {
        float delta = Math.Clamp(platform.transform.position.x - Target, -platformSpeed, platformSpeed);
        float updatedX = platform.transform.position.x - delta;
        
        platform.transform.position = new Vector3(updatedX, platform.transform.position.y, platform.transform.position.z);

        if (Math.Abs(delta) < 0.1f)
        {
            _targetLeft = !_targetLeft;
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var target = other.GetComponentInParent<PlayerController>();
        if (target == null) 
            return;
        
        if (!target.Stealth.IsStealthActive)
        {
            _targetLeft = true;
        }
    }
}
