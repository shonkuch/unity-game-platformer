using UnityEngine;

// Airborne without a jump: the fallback state. Pushing off a wall starts here with a horizontal boost.
[DisallowMultipleComponent]
public class FallState : MovingState
{
    private WallGrabState _wallGrab;
    private int _pushOffDirection;

    public override PlayerStateId Id => PlayerStateId.Fall;
    public override int AnimationValue => DirectionOf(Rigidbody.linearVelocity.y);

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
        _wallGrab = GetComponent<WallGrabState>();
    }

    public override bool CanBeActivated(PlayerStateId? currentState)
    {
        if (currentState != Id)
        {
            // Leaving a wall grab by pressing away from the wall is a push-off.
            bool pushingOff = currentState == PlayerStateId.WallGrab && Input.MoveAxis * _wallGrab.Side < 0f;
            _pushOffDirection = pushingOff ? -_wallGrab.Side : 0;
        }

        return true;
    }

    public override void EnterState()
    {
        base.EnterState();
        if (_pushOffDirection != 0) StartBoost(_pushOffDirection);
    }

    public override void EndState()
    {
        base.EndState();
        if (Controller.IsGrounded && Controller.Effects != null) Controller.Effects.PlayLanding();
    }
}
