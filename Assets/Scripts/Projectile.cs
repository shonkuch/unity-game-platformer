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

    private Rigidbody2D _rigidbody;
    private Damageable _shooter;
    private bool _spent;

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

    // direction: -1 left, 1 right. shooter may be null.
    public void Launch(int direction, Damageable shooter)
    {
        _shooter = shooter;
        _rigidbody.linearVelocity = Vector2.right * (direction * speed);
        if (direction < 0) transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        Destroy(gameObject, lifetime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_spent || (hitLayers.value & (1 << other.gameObject.layer)) == 0) return;

        var target = other.GetComponentInParent<Damageable>();
        if (target != null && target == _shooter) return;

        if (target != null && target.IsHitbox(other)) target.TakeDamage(damage);
        else if (other.isTrigger) return;

        _spent = true;
        Destroy(gameObject);
    }
}
