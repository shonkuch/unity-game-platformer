using UnityEngine;

// Stands and shoots straight projectiles in the facing direction while a target is visible. Each shot starts with a
// wind-up (the attack animation, played once per shot) and the projectile spawns the shot delay later, on the frame
// the animation fires. A started shot always completes, even if the target is lost meanwhile.
// After losing sight it keeps holding position for the lose-sight delay, firing again if the target returns,
// before handing back to Patrol / Idle.
[DisallowMultipleComponent]
public class EnemyAttackState : EnemyStateBase
{
    [SerializeField] private Projectile projectilePrefab;
    [Tooltip("Size multiplier for its projectiles: sprite, hitbox, trail and impact effect.")]
    [SerializeField, Min(0.1f)] private float projectileScale = 1f;
    [Tooltip("Spawn point; a child of the facing pivot so it flips with the enemy.")]
    [SerializeField] private Transform muzzle;
    [Tooltip("Optional child of the muzzle, played on every shot.")]
    [SerializeField] private ParticleSystem muzzleFlash;
    [Tooltip("Time (s) between shots.")]
    [SerializeField, Min(0.05f)] private float fireInterval = 1f;
    [Tooltip("Time (s) from spotting the target to the first shot.")]
    [SerializeField, Min(0f)] private float firstShotDelay = 0.4f;
    [Tooltip("Time (s) into the attack animation when it fires; the animation starts this long before each shot.")]
    [SerializeField, Min(0f)] private float shotDelay = 0.1667f;
    [Tooltip("How long (s) to keep waiting after losing sight before going back to patrol.")]
    [SerializeField, Min(0f)] private float loseSightDelay = 2f;

    private float _lastSeenAt = float.NegativeInfinity;
    private float _nextShotAt;
    private bool _windingUp;
    private bool _windUpStarted;

    public override EnemyStateId Id => EnemyStateId.Attack;
    // 1 on the step a shot's wind-up starts (the attack animation restarts), else 0.
    public override int AnimationValue => _windUpStarted ? 1 : 0;

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

    public override void EnterState()
    {
        _nextShotAt = Time.time + firstShotDelay;
        _windingUp = false;
    }

    public override void UpdateState()
    {
        _windUpStarted = false;
        BrakeHorizontal(Controller.Deceleration);
        if (Controller.Target != null) Controller.Face(DirectionOf(Controller.TargetPosition.x - Rigidbody.position.x));

        if (_windingUp)
        {
            if (Time.time < _nextShotAt) return;

            Fire();
            _windingUp = false;
            _nextShotAt = Time.time + fireInterval;
            return;
        }

        if (Controller.Target == null || Time.time < _nextShotAt - shotDelay) return;

        _windingUp = true;
        _windUpStarted = true;
    }

    // Leaving the state (lost sight, or death) cancels a shot that is still winding up.
    public override void EndState() => _windingUp = false;

    public override void ResetState() => _lastSeenAt = float.NegativeInfinity;

    private void Fire()
    {
        if (projectilePrefab == null || muzzle == null) return;

        Projectile projectile = Instantiate(projectilePrefab, muzzle.position, Quaternion.identity);
        projectile.Launch(Controller.Facing, Controller.Damageable, projectileScale);
        if (muzzleFlash != null) muzzleFlash.Play();
    }
}
