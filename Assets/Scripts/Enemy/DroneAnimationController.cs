using UnityEngine;

// Plays the drone's animations for its current state: Move by default, the attack animation once per shot (restarted
// on each wind-up, see EnemyAttackState) and the explosion once on death. When the explosion ends, the visuals object
// (sprite and body collider) is deactivated, since nothing of the drone is left.
public class DroneAnimationController : EnemyAnimationController
{
    [Tooltip("The EnemyVisuals Animator; found on this object or its children if empty.")]
    [SerializeField] private Animator animator;

    private readonly int _move = Animator.StringToHash("Drone_Move");
    private readonly int _attack = Animator.StringToHash("Drone_Attack");
    private readonly int _death = Animator.StringToHash("Drone_Explosion");

    private int _current;
    private int _playedOnFrame = -1;

    private void Awake()
    {
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    public override void PlayAnimation(EnemyStateId state, int value)
    {
        if (animator == null || !animator.isActiveAndEnabled) return;

        if (state == EnemyStateId.Death)
        {
            if (_current != _death) Play(_death);
            else if (HasFinished(_death)) animator.gameObject.SetActive(false);
            return;
        }

        // A shot's wind-up restarts the attack animation; it plays to its end even if the state changes meanwhile.
        if (state == EnemyStateId.Attack && value != 0) Play(_attack);
        else if (_current != _move && (_current != _attack || HasFinished(_attack))) Play(_move);
    }

    private void Play(int state)
    {
        animator.Play(state, 0, 0f);
        _current = state;
        _playedOnFrame = Time.frameCount;
    }

    // Play() only takes effect on the Animator's next update, so until then the state info still describes the
    // previous state.
    private bool HasFinished(int state)
    {
        if (Time.frameCount == _playedOnFrame) return false;

        AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0);
        return info.shortNameHash == state && info.normalizedTime >= 1f;
    }
}
