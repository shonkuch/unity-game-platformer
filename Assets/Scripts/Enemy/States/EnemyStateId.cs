// Enemy states. The numeric value is the activation priority: when several states can activate, the highest wins.
// Spaced by 10 so new states can slot in between.
public enum EnemyStateId
{
    Idle = 0,
    Patrol = 10,
    Attack = 20,
    Death = 100
}
