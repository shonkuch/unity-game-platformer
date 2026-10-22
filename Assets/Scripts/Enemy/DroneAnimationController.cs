using UnityEngine;

public class DroneAnimationController : EnemyAnimationController
{
    private Animator _animator;

    private readonly int _move = Animator.StringToHash("Drone_Move");
    private readonly int _attack = Animator.StringToHash("Drone_Attack");
    private readonly int _death = Animator.StringToHash("Drone_Explosion");

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public override void PlayAnimation(EnemyStateId state, int value)
    {
        if (_animator == null) return;

        switch (state)
        {
            case EnemyStateId.Patrol:
                _animator.Play(_move);
                break;
            case EnemyStateId.Attack:
                _animator.Play(_attack);
                break;
            case EnemyStateId.Death:
                _animator.Play(_death);
                break;
        }
    }
}