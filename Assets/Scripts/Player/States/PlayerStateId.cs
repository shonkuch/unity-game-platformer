// Player states. The numeric value is the activation priority: when several states can activate, the highest wins.
// Spaced by 10 so new states can slot in between.
public enum PlayerStateId
{
    Fall = 0,
    Idle = 10,
    Move = 20,
    Crouch = 30,
    Jump = 40,
    WallGrab = 50,
    Attack = 60,
    Death = 100
}
