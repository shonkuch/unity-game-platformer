using UnityEngine;

// Flies straight. Damages the first Damageable hitbox it touches (never its shooter's) and is destroyed by it,
// by any other solid collider such as walls, or after its lifetime. Other triggers (sensors, vision zones) are passed.
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Projectile : MonoBehaviour
{
    [SerializeField, Min(0f)] private float speed = 12f;
    [SerializeField, Min(1)] private int damage = 1;
    [SerializeField, Min(0.1f)] private float lifetime = 3f;
    [Tooltip("Layers it reacts to; colliders on other layers are passed through.")]
    [SerializeField] private LayerMask hitLayers = ~0;
    [Tooltip("Optional looping child (World simulation space); left behind to fade out when the projectile ends.")]
    [SerializeField] private ParticleSystem trail;
    [Tooltip("Optional prefab spawned where it hits something (not when its lifetime runs out). " +
             "Authored for a projectile flying right; spawned with the projectile's rotation.")]
    [SerializeField] private ParticleSystem impactEffect;

    private Rigidbody2D _rigidbody;
    private Damageable _shooter;
    private float _scale = 1f;
    private bool _spent;
    private float _expiresAt = float.PositiveInfinity;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
    }

    // direction: -1 left, 1 right. shooter may be null. scale multiplies its size: sprite, collider, trail and impact.
    public void Launch(int direction, Damageable shooter, float scale = 1f)
    {
        _shooter = shooter;
        _scale = scale;
        if (!Mathf.Approximately(scale, 1f))
        {
            transform.localScale *= scale;
            Vfx.FollowHierarchyScale(trail);
        }

        _rigidbody.linearVelocity = Vector2.right * (direction * speed);
        if (direction < 0) transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        _expiresAt = Time.time + lifetime;
    }

    private void Update()
    {
        if (!_spent && Time.time >= _expiresAt) End();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_spent || (hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var target = other.GetComponentInParent<Damageable>();
        if (target != null && target == _shooter) return;

        if (target != null && target.IsHitbox(other)) target.TakeDamage(damage);
        else if (other.isTrigger) return;

        Vfx.Spawn(impactEffect, transform.position, transform.rotation, _scale);
        End();
    }

    private void End()
    {
        _spent = true;
        Vfx.Release(trail);
        Destroy(gameObject);
    }
}
