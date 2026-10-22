using System;
using UnityEngine;

// One state of a character (player or enemy): decides when it can run (CanBeActivated) and what it does while running.
// States are components on the character; its StateMachine polls them every physics step.
// TId's numeric value is the priority: when several states can activate, the highest wins.
public abstract class CharacterState<TId> : MonoBehaviour where TId : struct, Enum
{
    protected Rigidbody2D Rigidbody { get; private set; }

    public abstract TId Id { get; }
    public int Priority { get; private set; }
    public virtual int AnimationValue => 0;

    // Called once by the state machine before any other method.
    public void Bind(Rigidbody2D rigidbody)
    {
        Rigidbody = rigidbody;
        Priority = Convert.ToInt32(Id);
    }

    // currentState is null before the first state starts. When currentState == Id, the answer means "stay".
    public abstract bool CanBeActivated(TId? currentState);

    public virtual void EnterState() { }
    public abstract void UpdateState();
    public virtual void EndState() { }
    public virtual void ResetState() { }

    // Slows horizontal speed to zero without overshooting it within one step.
    protected void BrakeHorizontal(float deceleration) => AccelerateHorizontal(0f, deceleration);

    // Approaches the target horizontal speed at the given rate without overshooting it within one step.
    protected void AccelerateHorizontal(float targetSpeed, float rate)
    {
        float vx = Rigidbody.linearVelocity.x;
        float acceleration = Mathf.Clamp((targetSpeed - vx) / Time.fixedDeltaTime, -rate, rate);
        Rigidbody.AddForce(Vector2.right * (acceleration * Rigidbody.mass));
    }

    protected static int DirectionOf(float axis) => axis > 0f ? 1 : axis < 0f ? -1 : 0;
}
