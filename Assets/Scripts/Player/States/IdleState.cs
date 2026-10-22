using UnityEngine;

// Grounded with no horizontal input: brakes to a stop.
[DisallowMultipleComponent]
public class IdleState : StationaryState
{
    public override PlayerStateId Id => PlayerStateId.Idle;

    public override bool CanBeActivated(PlayerStateId? currentState) =>
        Controller.IsGrounded && Input.MoveAxis == 0f;

    protected override void HoldPosition() => BrakeHorizontal(Controller.GroundDeceleration);
}
