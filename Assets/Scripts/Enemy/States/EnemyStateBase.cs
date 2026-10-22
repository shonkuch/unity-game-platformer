// An enemy state.
public abstract class EnemyStateBase : CharacterState<EnemyStateId>
{
    protected EnemyController Controller { get; private set; }

    public virtual void Initialize(EnemyController controller) => Controller = controller;
}
