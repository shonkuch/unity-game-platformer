using UnityEngine;

// A state where the player holds still instead of following horizontal input. Standing still starts stealth.
public abstract class StationaryState : PlayerStateBase
{
    public override void EnterState()
    {
        base.EnterState();
        if (Controller.Stealth != null) Controller.Stealth.Activate();
    }

    public override void UpdateState() => HoldPosition();

    protected abstract void HoldPosition();
}
