// A player state. Gravity scale is restored on enter and end, so states that change it don't leak it.
public abstract class PlayerStateBase : CharacterState<PlayerStateId>
{
    protected PlayerController Controller { get; private set; }
    protected PlayerInputBinder Input => Controller.Input;

    public virtual void Initialize(PlayerController controller) => Controller = controller;

    public override void EnterState() => Rigidbody.gravityScale = Controller.DefaultGravityScale;
    public override void EndState() => Rigidbody.gravityScale = Controller.DefaultGravityScale;
}
