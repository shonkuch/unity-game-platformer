using UnityEngine;

// Runs the jump acceleration curve from jump start until landing, plus a horizontal boost:
// toward the held direction on a ground jump, away from the wall on a wall jump.
[DisallowMultipleComponent]
public class JumpState : MovingState
{
    [Tooltip("Upward acceleration (m/s²) by time since jump start (s), added on top of gravity until landing. " +
             "Holds its last key's value after the curve ends.")]
    [SerializeField] private AnimationCurve jumpAcceleration = new(
        new Keyframe(0f, 120f, -573.36f, -573.36f),
        new Keyframe(0.032f, 76.49f, -1672.31f, -1672.31f),
        new Keyframe(0.15f, 0f));

    private WallGrabState _wallGrab;
    private float _jumpTime;
    private int _pendingBoostDirection;

    public override PlayerStateId Id => PlayerStateId.Jump;
    public override int AnimationValue => DirectionOf(Rigidbody.linearVelocity.y);

    // The jump starts grounded and at rest, so landing is only checked from the second step on.
    private bool HasLanded => _jumpTime > 0f && Controller.IsGrounded && Rigidbody.linearVelocity.y <= 0f;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
        _wallGrab = GetComponent<WallGrabState>();
    }

    public override bool CanBeActivated(PlayerStateId? currentState)
    {
        if (currentState == Id) return !HasLanded;
        if (!Input.JumpBuffered) return false;

        if (currentState == PlayerStateId.WallGrab)
        {
            _pendingBoostDirection = -_wallGrab.Side;
            return true;
        }

        // A crouch under a low ceiling can't stand up, so it can't jump either.
        if (!Controller.IsGrounded || (currentState == PlayerStateId.Crouch && Controller.IsTouchingCeiling))
            return false;

        _pendingBoostDirection = DirectionOf(Input.MoveAxis);
        return true;
    }

    public override void EnterState()
    {
        base.EnterState();
        Input.ConsumeJump();
        _jumpTime = 0f;
        if (_pendingBoostDirection != 0) StartBoost(_pendingBoostDirection);
    }

    public override void EndState()
    {
        base.EndState();
        if (HasLanded && Controller.Effects != null) Controller.Effects.PlayLanding();
    }

    protected override void UpdateMovement()
    {
        Rigidbody.AddForce(Vector2.up * (jumpAcceleration.Evaluate(_jumpTime) * Rigidbody.mass));
        _jumpTime += Time.fixedDeltaTime;
    }
}
