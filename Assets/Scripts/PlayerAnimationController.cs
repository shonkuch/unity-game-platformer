using UnityEngine;

public class PlayerAnimationController : CharacterAnimationController<PlayerStateId>
{
    private Animator _animator;
    private PlayerStateId _currentState;

    private readonly int _idle = Animator.StringToHash("Player_Idle");
    private readonly int _run = Animator.StringToHash("Player_Run");
    private readonly int _jump = Animator.StringToHash("Player_Jump");
    private readonly int _crouch = Animator.StringToHash("Player_Crouch");
    private readonly int _wallGrab = Animator.StringToHash("Player_WallGrab");

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
    }

    public override void PlayAnimation(PlayerStateId state, int value)
    {
        if (_animator == null) return;

        if (_currentState == state) return;

        Debug.Log($"Аніматор перемикає: {_currentState} ---> {state}");

        _currentState = state;

        switch (state)
        {
            case PlayerStateId.Idle: 
                _animator.Play(_idle);
                break;
            case PlayerStateId.Move: 
                _animator.Play(_run);
                break;
            case PlayerStateId.Jump: 
            case PlayerStateId.Fall: 
                _animator.Play(_jump);
                break;
            case PlayerStateId.Crouch:
                _animator.Play(_crouch);
                break;
            case PlayerStateId.WallGrab: 
                _animator.Play(_wallGrab);
                break;
        }
    }
}