using System;
using UnityEngine;

// Plays animations for a character's current state. Optional: the StateMachine calls it every physics step if present.
// value is state-specific, e.g. input direction while moving, vertical direction while airborne.
public abstract class CharacterAnimationController<TId> : MonoBehaviour where TId : struct, Enum
{
    public abstract void PlayAnimation(TId state, int value);
}
