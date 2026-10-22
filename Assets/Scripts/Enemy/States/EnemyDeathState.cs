using UnityEngine;

// Hit points ran out. Terminal: once active it never hands over. The body falls and brakes to a stop and stays
// as a corpse; Damageable has already disabled its hitboxes, and the controller stops seeing targets.
// An EnemyAnimationController, if present, receives Death every step to hold the death animation.
[DisallowMultipleComponent]
public class EnemyDeathState : EnemyStateBase
{
    public override EnemyStateId Id => EnemyStateId.Death;

    public override bool CanBeActivated(EnemyStateId? currentState) =>
        currentState == Id || Controller.Damageable.IsDead;

    public override void UpdateState() => BrakeHorizontal(Controller.Deceleration);
}
