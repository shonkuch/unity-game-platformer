using UnityEngine;

// Grounded with horizontal input: runs.
[DisallowMultipleComponent]
public class MoveState : MovingState
{
    public override PlayerStateId Id => PlayerStateId.Move;

    public override bool CanBeActivated(PlayerStateId? currentState) =>
        Controller.IsGrounded && Input.MoveAxis != 0f;

    public override void EnterState()
    {
        base.EnterState();
        if (Controller.Effects != null) Controller.Effects.SetRunning(true);
    }

    public override void EndState()
    {
        base.EndState();
        if (Controller.Effects != null) Controller.Effects.SetRunning(false);
    }
}
