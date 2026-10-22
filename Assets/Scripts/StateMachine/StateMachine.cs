using System;
using System.Collections.Generic;
using UnityEngine;

// Runs a character's states: every physics step, the highest-priority state that can activate becomes current.
// Subclasses validate their bindings in Awake, then call InitializeStates.
[RequireComponent(typeof(Rigidbody2D))]
public abstract class StateMachine<TState, TId> : MonoBehaviour
    where TState : CharacterState<TId>
    where TId : struct, Enum
{
    private readonly List<TState> _states = new();
    private TState _currentState;
    private CharacterAnimationController<TId> _animationController;

    public Rigidbody2D Rigidbody { get; private set; }
    public float DefaultGravityScale { get; private set; }
    public TId? CurrentState => _currentState != null ? _currentState.Id : null;

    protected virtual void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
        DefaultGravityScale = Rigidbody.gravityScale;
        _animationController = GetComponent<CharacterAnimationController<TId>>();
    }

    // Collects the state components, checks for duplicates and initializes them. Disables this component on failure.
    protected bool InitializeStates()
    {
        GetComponents(_states);
        if (_states.Count == 0)
        {
            Fail("has no state components");
            return false;
        }

        var ids = new HashSet<TId>();
        foreach (TState state in _states)
        {
            if (!ids.Add(state.Id))
            {
                Fail($"has two {state.Id} states");
                return false;
            }

            state.Bind(Rigidbody);
            InitializeState(state);
        }

        return true;
    }

    protected abstract void InitializeState(TState state);

    protected virtual void OnDisable()
    {
        if (_currentState != null) _currentState.EndState();
        _currentState = null;

        foreach (TState state in _states)
            state.ResetState();
    }

    protected virtual void FixedUpdate()
    {
        TState next = SelectState();
        if (next == null)
        {
            Fail("has no state that can activate; add a fallback state that always can");
            return;
        }

        if (next != _currentState)
        {
            if (_currentState != null) _currentState.EndState();
            _currentState = next;
            _currentState.EnterState();
        }

        _currentState.UpdateState();

        if (_animationController != null)
            _animationController.PlayAnimation(_currentState.Id, _currentState.AnimationValue);
    }

    protected void Fail(string reason)
    {
        Debug.LogError($"{GetType().Name} on {name} {reason}.", this);
        enabled = false;
    }

    // Every state is asked each step, so states can track input or perception between activations.
    private TState SelectState()
    {
        TId? currentId = CurrentState;
        TState selected = null;

        foreach (TState state in _states)
        {
            if (state.CanBeActivated(currentId) && (selected == null || state.Priority > selected.Priority))
                selected = state;
        }

        return selected;
    }
}
