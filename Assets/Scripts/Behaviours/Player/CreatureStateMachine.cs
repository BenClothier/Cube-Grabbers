using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CreatureStateMachine : MonoBehaviour
{
    public enum State
    {
        OnSurface,
        InAir,
    }

    public enum Command
    {
        EnterAir,
        GrabSurface,
    }

    public enum StateGroup
    {
    }

    private static readonly Dictionary<StateTransition, State> transitions = new()
    {
        { new StateTransition(State.OnSurface, Command.EnterAir), State.InAir },
        { new StateTransition(State.InAir, Command.GrabSurface), State.OnSurface },
    };

    private static readonly Dictionary<StateGroup, State[]> stateGroups = new()
    {
    };

#if UNITY_EDITOR
    [Header("State Machine")]
    [SerializeField] private bool debugMode;
#endif

    public State CurrentState { get; private set; }

    public bool IsInState(State state) => CurrentState.Equals(state);

    public bool IsInStateGroup(StateGroup stateGroup) => stateGroups[stateGroup].Any(state => IsInState(state));

    public bool TryGetState(Command command, out State newState)
    {
        StateTransition transition = new(CurrentState, command);
        return transitions.TryGetValue(transition, out newState);
    }

    public bool TryMoveState(Command command, out State newState, bool erroneousIfCantDoTransition = true)
    {
        if (TryGetState(command, out newState))
        {
            CurrentState = newState;

#if UNITY_EDITOR
            if (debugMode)
            {
                Debug.Log($"New State: {CurrentState}");
            }
#endif
            return true;
        }
        else
        {
            if (erroneousIfCantDoTransition)
            {
                throw new Exception("Invalid transition: " + CurrentState + " -> " + command);
            }

            return false;
        }
    }

    public bool TryMoveState(Command command, bool erroneousIfCantDoTransition = true)
    {
        return TryMoveState(command, out _, erroneousIfCantDoTransition);
    }

    private struct StateTransition
    {
        public State currentState;
        public Command transition;

        public StateTransition(State currentState, Command transition)
        {
            this.currentState = currentState;
            this.transition = transition;
        }
    }
}

