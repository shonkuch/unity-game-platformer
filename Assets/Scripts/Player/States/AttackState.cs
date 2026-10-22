using System.Collections.Generic;
using UnityEngine;

// One swing: after the hit delay, box-casts at the cast point and damages every Damageable it touches once,
// never the player's own. Lasts the attack duration while horizontal control continues.
// Starts from Idle, Move or Fall; not from Jump, Crouch or a wall grab.
[DisallowMultipleComponent]
public class AttackState : MovingState
{
    [SerializeField, Min(1)] private int damage = 1;
    [Tooltip("Center of the hit box: a child of the player (not of the sprite), placed for facing right. " +
             "Its local X is mirrored when facing left.")]
    [SerializeField] private Transform castPoint;
    [SerializeField] private Vector2 castSize = new(1f, 1f);
    [SerializeField] private LayerMask hitLayers = ~0;
    [Tooltip("How long (s) the attack state lasts.")]
    [SerializeField, Min(0f)] private float duration = 0.35f;
    [Tooltip("Time (s) into the attack when the hit box is cast; clamped to the duration.")]
    [SerializeField, Min(0f)] private float hitDelay = 0.1f;

    private readonly List<Collider2D> _overlaps = new();
    private readonly HashSet<Damageable> _hitThisSwing = new();
    private Damageable _self;
    private float _time;
    private bool _hasHit;

    public override PlayerStateId Id => PlayerStateId.Attack;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
        _self = GetComponent<Damageable>();

        if (castPoint == null)
            Debug.LogError($"{nameof(AttackState)} on {name} has no cast point; attacks won't hit.", this);
    }

    public override bool CanBeActivated(PlayerStateId? currentState)
    {
        if (currentState == Id) return _time < duration;

        return Input.AttackBuffered &&
               currentState is PlayerStateId.Idle or PlayerStateId.Move or PlayerStateId.Fall;
    }

    public override void EnterState()
    {
        base.EnterState();
        Input.ConsumeAttack();
        _time = 0f;
        _hasHit = false;

        // Face the held direction first so the swing effect points where the hit will land.
        Controller.Face(DirectionOf(Input.MoveAxis));
        if (Controller.Effects != null) Controller.Effects.PlayAttack(Controller.Facing);
    }

    protected override void UpdateMovement()
    {
        if (!_hasHit && _time >= Mathf.Min(hitDelay, duration))
        {
            _hasHit = true;
            Hit();
        }

        _time += Time.fixedDeltaTime;
    }

    private void Hit()
    {
        if (castPoint == null) return;

        var filter = new ContactFilter2D { useTriggers = true };
        filter.SetLayerMask(hitLayers);
        Physics2D.OverlapBox(CastCenter(), castSize, 0f, filter, _overlaps);

        // Only a target's hitboxes count (not e.g. an enemy's vision trigger); the player's own colliders and
        // repeat colliders of the same target are skipped, so each target is hit once.
        _hitThisSwing.Clear();
        foreach (Collider2D hit in _overlaps)
        {
            if (hit.attachedRigidbody == Rigidbody) continue;

            var target = hit.GetComponentInParent<Damageable>();
            if (target == null || target == _self || !target.IsHitbox(hit) || !_hitThisSwing.Add(target)) continue;

            target.TakeDamage(damage);
        }
    }

    private Vector2 CastCenter()
    {
        int facing = Controller != null ? Controller.Facing : 1;
        Vector3 local = transform.InverseTransformPoint(castPoint.position);
        local.x *= facing;
        return transform.TransformPoint(local);
    }

    private void OnDrawGizmosSelected()
    {
        if (castPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(CastCenter(), castSize);
    }
}
