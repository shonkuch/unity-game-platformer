using UnityEngine;

// Hit points ran out. Terminal: once active it never hands over and ignores input.
// The body falls under gravity and brakes to a stop; stealth drops to 0 and stays off.
[DisallowMultipleComponent]
[RequireComponent(typeof(Damageable))]
public class DeathState : PlayerStateBase
{
    private Damageable _damageable;

    public override PlayerStateId Id => PlayerStateId.Death;

    public override void Initialize(PlayerController controller)
    {
        base.Initialize(controller);
        _damageable = GetComponent<Damageable>();
    }

    public override bool CanBeActivated(PlayerStateId? currentState) => currentState == Id || _damageable.IsDead;

    public override void EnterState()
    {
        base.EnterState();

        if (Controller.Stealth != null)
        {
            // Deactivate stops it re-cloaking; DropInstantly skips the fade.
            Controller.Stealth.Deactivate();
            Controller.Stealth.DropInstantly();
        }
    }

    public override void UpdateState() => BrakeHorizontal(Controller.GroundDeceleration);
}
