using UnityEngine;

// Stands and shoots straight projectiles in the facing direction while a target is visible.
// After losing sight it keeps holding position for the lose-sight delay, firing again if the target returns,
// before handing back to Patrol / Idle.
[DisallowMultipleComponent]
public class EnemyAttackState : EnemyStateBase
{
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("Spawn point; a child of the facing pivot so it flips with the enemy.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Optional child of the muzzle, played on every shot.")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [Tooltip("Time (s) between shots.")]
    [SerializeField, Min(0.05f)] private float fireInterval = 1f;
    [Tooltip("Time (s) from spotting the target to the first shot.")]
    [SerializeField, Min(0f)] private float firstShotDelay = 0.4f;
    [Tooltip("How long (s) to keep waiting after losing sight before going back to patrol.")]
    [SerializeField, Min(0f)] private float loseSightDelay = 2f;

    private float _lastSeenAt = float.NegativeInfinity;
    private float _nextShotAt;

    public override EnemyStateId Id => EnemyStateId.Attack;
    // 1 while the target is in sight, 0 while waiting for it to return.
    public override int AnimationValue => Controller.Target != null ? 1 : 0;

    public override void Initialize(EnemyController controller)
    {
        base.Initialize(controller);

        if (projectilePrefab == null || muzzle == null)
            Debug.LogError($"{nameof(EnemyAttackState)} on {name} needs a projectile prefab and a muzzle; it won't fire.", this);
    }

    public override bool CanBeActivated(EnemyStateId? currentState)
    {
        if (Controller.Target != null)
        {
            _lastSeenAt = Time.time;
            return true;
        }

        return currentState == Id && Time.time - _lastSeenAt < loseSightDelay;
    }

    public override void EnterState() => _nextShotAt = Time.time + firstShotDelay;

    public override void UpdateState()
    {
        BrakeHorizontal(Controller.Deceleration);
        if (Controller.Target == null) return;

        Controller.Face(DirectionOf(Controller.TargetPosition.x - Rigidbody.position.x));
        if (Time.time < _nextShotAt) return;

        Fire();
        _nextShotAt = Time.time + fireInterval;
    }

    public override void ResetState() => _lastSeenAt = float.NegativeInfinity;

    private void Fire()
    {
        if (projectilePrefab == null || muzzle == null) return;

        Projectile projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
        projectile.Launch(Controller.Facing, Controller.Damageable);
        if (muzzleFlash != null) muzzleFlash.Play();
    }
}
