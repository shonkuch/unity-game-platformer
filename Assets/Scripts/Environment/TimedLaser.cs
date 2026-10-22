using UnityEngine;

public class TimedLaser : MonoBehaviour
{
    [SerializeField] private SpriteRenderer laserBeam;
    [SerializeField] private float interval;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField] private Transform castPoint;
    [SerializeField] private Vector2 castSize = new(1f, 1f);
    [SerializeField] private LayerMask hitLayers = ~0;
    private bool _isActive;

    private float _timer;
    
    private void Start()
    {
        if (castPoint == null)
            Debug.LogError($"{nameof(TimedLaser)} on {name} has no cast point; attacks won't hit.", this);
    }

    private void Update()
    {
        _timer += Time.deltaTime;

        if (_timer >= interval)
        {
            _timer = 0;
            SetState(!_isActive);
        }
    }

    void SetState(bool isActive)
    {
        _isActive = isActive;
        _timer  = 0;
        laserBeam.enabled = isActive;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_isActive)
            return;
        
        if ((hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var target = other.GetComponentInParent<Damageable>();
        if (target == null) 
            return;

        if (target.IsHitbox(other))
        {
            target.TakeDamage(damage);
            SetState(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (castPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(castPoint.position, castSize);
    }
}
