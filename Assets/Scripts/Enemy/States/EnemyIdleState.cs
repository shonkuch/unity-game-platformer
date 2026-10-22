using UnityEngine;

// Fallback state: brakes to a stop and stands. Patrol hands over to it while waiting at a waypoint.
[DisallowMultipleComponent]
public class EnemyIdleState : EnemyStateBase
{
    public override EnemyStateId Id => EnemyStateId.Idle;

    public override bool CanBeActivated(EnemyStateId? currentState) => true;

    public override void UpdateState() => BrakeHorizontal(Controller.Deceleration);
}
