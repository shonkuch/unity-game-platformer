using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

// Hit points for anything that can take damage: the player, enemies, destructible environment.
// Flashes the sprite toward a tint and plays hit particles on every hit; disables its hitboxes and spawns
// the death effect on death.
// What else death means is up to whoever listens to Died.
[DisallowMultipleComponent]
public class Damageable : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHp = 3;
    [Tooltip("Colliders attacks and projectiles can hit; disabled on death. Empty = every non-trigger collider " +
             "of this object and its children (nothing is disabled on death then).")]
    [SerializeField] private Collider2D[] hitboxes = Array.Empty<Collider2D>();
    [Tooltip("Optional child particle system played on every hit.")]
    [SerializeField] private ParticleSystem hitParticles;
    [Tooltip("Optional prefab spawned on death at the sprite's center (or this object's position); outlives the object.")]
    [SerializeField] private ParticleSystem deathEffect;
    [Tooltip("Optional; tinted on every hit.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color flashColor = Color.red;
    [Tooltip("Tint strength (0 sprite's own color, 1 flash color) by time since the hit (s).")]
    [SerializeField] private AnimationCurve flashCurve = new(new Keyframe(0f, 1f), new Keyframe(0.25f, 0f));

    // Damage amount taken.
    public UnityEvent <int> damaged;
    public UnityEvent  died;

    public int MaxHp => maxHp;
    public int Hp { get; private set; }
    public bool IsDead => Hp <= 0;

    private Color _baseColor;
    private bool _flashing;
    private float _flashTime;

    private void Reset()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Awake()
    {
        Hp = maxHp;
        if (spriteRenderer != null) _baseColor = spriteRenderer.color;
    }

    private void OnDisable()
    {
        if (!_flashing) return;

        _flashing = false;
        spriteRenderer.color = _baseColor;
    }

    private void Update()
    {
        if (!_flashing) return;

        _flashTime += Time.deltaTime;
        float end = flashCurve.length > 0 ? flashCurve[flashCurve.length - 1].time : 0f;
        spriteRenderer.color = Color.Lerp(_baseColor, flashColor, flashCurve.Evaluate(Mathf.Min(_flashTime, end)));

        if (_flashTime >= end) _flashing = false;
    }

    // Whether collider counts as this object's hitbox (see hitboxes). Callers find the Damageable with GetComponentInParent.
    public bool IsHitbox(Collider2D collider) =>
        hitboxes.Length > 0 ? Array.IndexOf(hitboxes, collider) >= 0 : !collider.isTrigger;

    // Ignored once dead. Raises Damaged, then Died on the hit that brings hp to 0.
    public void TakeDamage(int amount)
    {
        if (IsDead || amount <= 0) return;

        Hp = Mathf.Max(0, Hp - amount);

        if (spriteRenderer != null && isActiveAndEnabled)
        {
            _flashing = true;
            _flashTime = 0f;
        }

        if (hitParticles != null) hitParticles.Play();

        damaged?.Invoke(Hp);
        if (!IsDead) return;

        foreach (Collider2D hitbox in hitboxes)
            if (hitbox != null) hitbox.enabled = false;

        Vector3 center = spriteRenderer != null ? spriteRenderer.bounds.center : transform.position;
        Vfx.Spawn(deathEffect, center, Quaternion.identity);

        died?.Invoke();
    }
}
