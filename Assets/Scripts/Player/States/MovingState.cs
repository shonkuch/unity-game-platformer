using UnityEngine;

// A state where horizontal input accelerates the player, using the controller's shared horizontal tuning.
// Can also run a horizontal boost (jump or wall push-off) that lasts until the state ends. Moving ends stealth.
public abstract class MovingState : PlayerStateBase
{
    // -1 left, 1 right, 0 no boost running.
    private int _boostDirection;
    private float _boostTime;

    public override int AnimationValue => DirectionOf(Input.MoveAxis);

    protected virtual float SpeedCap => Controller.MaxSpeed;
    protected bool IsBoosting => _boostDirection != 0;

    public override void EnterState()
    {
        base.EnterState();
        _boostDirection = 0;
        if (Controller.Stealth != null) Controller.Stealth.Deactivate();
    }

    public override void UpdateState()
    {
        float moveInput = Input.MoveAxis;
        Controller.Face(DirectionOf(moveInput));

        ApplyHorizontal(moveInput);
        ApplyBoost();
        UpdateMovement();
    }

    public override void EndState()
    {
        base.EndState();
        _boostDirection = 0;
    }

    // State-specific behaviour after horizontal control and boost.
    protected virtual void UpdateMovement() { }

    protected void StartBoost(int direction)
    {
        _boostDirection = direction;
        _boostTime = 0f;
        Controller.Face(direction);
    }

    private void ApplyHorizontal(float moveInput)
    {
        float vx = Rigidbody.linearVelocity.x;
        float speedCap = SpeedCap;

        // A boost may carry the player past the cap; don't brake it back down.
        if (IsBoosting && vx * moveInput > 0f && Mathf.Abs(vx) >= speedCap) return;

        bool speedingUp = moveInput != 0f && vx * moveInput >= 0f && Mathf.Abs(vx) < speedCap;

        float rate = Controller.IsGrounded
            ? (speedingUp ? Controller.GroundAcceleration : Controller.GroundDeceleration)
            : (speedingUp ? Controller.AirAcceleration : Controller.AirDeceleration);

        // Approach the target speed without overshooting it within one step.
        float acceleration = Mathf.Clamp((moveInput * speedCap - vx) / Time.fixedDeltaTime, -rate, rate);

        // While the boost curve runs, braking or reversing must not cancel it.
        if (IsBoosting && _boostTime < Controller.BoostCurveDuration && acceleration * _boostDirection < 0f) return;

        Rigidbody.AddForce(Vector2.right * (acceleration * Rigidbody.mass));
    }

    private void ApplyBoost()
    {
        if (!IsBoosting) return;

        float acceleration = _boostDirection * Controller.JumpHorizontalAcceleration.Evaluate(_boostTime);
        Rigidbody.AddForce(Vector2.right * (acceleration * Rigidbody.mass));
        _boostTime += Time.fixedDeltaTime;
    }
}
