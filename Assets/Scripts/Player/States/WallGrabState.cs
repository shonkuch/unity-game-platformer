using UnityEngine;

// Airborne, falling and touching a wall: sticks to it with gravity off.
// Ends on landing, losing the wall, pressing away (push-off, see FallState) or jumping (wall jump, see JumpState).
[DisallowMultipleComponent]
public class WallGrabState : StationaryState
{
    // Wall side released while airborne; it can't be grabbed again until its sensor leaves the wall.
    private int _regrabBlockedSide;

    public override PlayerStateId Id => PlayerStateId.WallGrab;
    // -1 left, 1 right; the last grabbed side.
    public int Side { get; private set; }

    public override bool CanBeActivated(PlayerStateId? currentState)
    {
        if (_regrabBlockedSide != 0 && !Controller.IsTouchingWall(_regrabBlockedSide))
            _regrabBlockedSide = 0;

        if (Controller.IsGrounded) return false;

        if (currentState == Id)
            return Controller.IsTouchingWall(Side) && !Input.JumpBuffered && Input.MoveAxis * Side >= 0f;

        // Auto-grab only on the way down, so jumps next to a wall still rise. Not while bumping a ceiling either:
        // the head stops there with no vertical speed, which would otherwise latch a grab in mid-air.
        if (Rigidbody.linearVelocity.y > 0f || Controller.IsTouchingCeiling) return false;

        if (CanGrab(-1)) Side = -1;
        else if (CanGrab(1)) Side = 1;
        else return false;

        return true;
    }

    public override void EnterState()
    {
        base.EnterState();
        Rigidbody.gravityScale = 0f;
        Rigidbody.linearVelocity = Vector2.zero;
        if (Controller.Effects != null) Controller.Effects.PlayWallGrab(Side);
    }

    public override void EndState()
    {
        base.EndState();
        if (!Controller.IsGrounded) _regrabBlockedSide = Side;
    }

    public override void ResetState()
    {
        _regrabBlockedSide = 0;
        Side = 0;
    }

    protected override void HoldPosition() => Rigidbody.linearVelocity = Vector2.zero;

    private bool CanGrab(int side) => side != _regrabBlockedSide && Controller.IsTouchingWall(side);
}
