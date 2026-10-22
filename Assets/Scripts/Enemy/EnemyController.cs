using System.Collections.Generic;
using UnityEngine;

// Runs the enemy state machine (priority = EnemyStateId value) and its perception. Each physics step, before the
// states are polled, it looks for a target: a living, uncloaked Damageable hitbox inside the vision trigger
// with a clear line of sight.
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D), typeof(Damageable))]
public class EnemyController : StateMachine<EnemyStateBase, EnemyStateId>
{
    [Header("Bindings")]
    [Tooltip("Child rotated 180° on Y to face left; authored facing right. Holds the sprite, vision sensor and muzzle.")]
    [SerializeField] private Transform facingPivot;
    [Tooltip("Trigger in front of the enemy, under the facing pivot. Set its detect layers to the player's layer.")]
    [SerializeField] private TriggerSensor visionSensor;
    [Tooltip("Line-of-sight origin; the enemy's own position if empty.")]
    [SerializeField] private Transform eye;
    [Tooltip("Layers that block line of sight (walls). Leave out the player's and enemies' layers.")]
    [SerializeField] private LayerMask sightBlockers;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float acceleration = 30f;
    [SerializeField, Min(0f)] private float deceleration = 40f;

    public Damageable Damageable { get; private set; }
    public float Acceleration => acceleration;
    public float Deceleration => deceleration;
    // -1 left, 1 right.
    public int Facing { get; private set; } = 1;
    // The target seen this step, or null; TargetPosition is the center of the hitbox that was seen.
    public Damageable Target { get; private set; }
    public Vector2 TargetPosition { get; private set; }

    private readonly List<Collider2D> _seen = new();
    private readonly List<RaycastHit2D> _sightHits = new();
    private ContactFilter2D _sightFilter;
    private Quaternion _pivotRotation;

    private Vector2 EyePosition => eye != null ? eye.position : transform.position;

    private void Reset()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    protected override void Awake()
    {
        base.Awake();
        Damageable = GetComponent<Damageable>();

        if (facingPivot == null || visionSensor == null)
        {
            Fail("is missing bindings");
            return;
        }

        if (facingPivot == transform)
        {
            Fail("needs the facing pivot on a child object");
            return;
        }

        _pivotRotation = facingPivot.localRotation;
        _sightFilter = new ContactFilter2D { useTriggers = false };
        _sightFilter.SetLayerMask(sightBlockers);
        InitializeStates();
    }

    protected override void InitializeState(EnemyStateBase state) => state.Initialize(this);

    protected override void FixedUpdate()
    {
        UpdateTarget();
        base.FixedUpdate();
    }

    // Rotates the facing pivot to face direction (-1 left, 1 right); 0 keeps the current facing.
    public void Face(int direction)
    {
        if (direction == 0 || direction == Facing) return;

        Facing = direction;
        facingPivot.localRotation = _pivotRotation * Quaternion.Euler(0f, direction < 0 ? 180f : 0f, 0f);
    }

    private void UpdateTarget()
    {
        Target = null;
        if (Damageable.IsDead) return;

        visionSensor.GetOverlaps(_seen);
        foreach (Collider2D seen in _seen)
        {
            if (seen == null) continue;

            var target = seen.GetComponentInParent<Damageable>();
            if (target == null || target == Damageable || target.IsDead || !target.IsHitbox(seen)) continue;

            var stealth = target.GetComponent<PlayerStealth>();
            if (stealth != null && stealth.IsStealthActive) continue;

            Vector2 point = seen.bounds.center;
            if (Physics2D.Linecast(EyePosition, point, _sightFilter, _sightHits) > 0) continue;

            Target = target;
            TargetPosition = point;
            return;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (Target == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(EyePosition, TargetPosition);
    }
}
