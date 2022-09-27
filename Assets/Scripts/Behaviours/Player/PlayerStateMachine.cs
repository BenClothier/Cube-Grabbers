using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    private static readonly Dictionary<StateTransition, State> transitions = new ()
    {
        { new StateTransition(State.OnSurface, Command.EnterAir), State.InAir },
        { new StateTransition(State.OnSurface, Command.StartChargingJump), State.ChargingJump },
        { new StateTransition(State.OnSurface, Command.Pickup), State.OnSurfaceHolding },
        { new StateTransition(State.OnSurface, Command.StartMining), State.OnSurfaceMining },

        { new StateTransition(State.OnSurfaceHolding, Command.EnterAir), State.InAirHolding },
        { new StateTransition(State.OnSurfaceHolding, Command.StartChargingJump), State.ChargingJumpHolding },
        { new StateTransition(State.OnSurfaceHolding, Command.StartAiming), State.OnSurfaceAiming },

        { new StateTransition(State.OnSurfaceAiming, Command.EnterAir), State.InAirAiming },
        { new StateTransition(State.OnSurfaceAiming, Command.StartChargingJump), State.ChargingJumpAiming },
        { new StateTransition(State.OnSurfaceAiming, Command.CancelAim), State.OnSurfaceHolding },
        { new StateTransition(State.OnSurfaceAiming, Command.Throw), State.OnSurface },

        { new StateTransition(State.OnSurfaceMining, Command.EnterAir), State.InAir },
        { new StateTransition(State.OnSurfaceMining, Command.StartChargingJump), State.ChargingJump },


        { new StateTransition(State.ChargingJump, Command.EnterAir), State.InAir },
        { new StateTransition(State.ChargingJump, Command.Pickup), State.ChargingJumpHolding },

        { new StateTransition(State.ChargingJumpHolding, Command.EnterAir), State.InAirHolding },
        { new StateTransition(State.ChargingJumpHolding, Command.StartAiming), State.ChargingJumpAiming },

        { new StateTransition(State.ChargingJumpAiming, Command.EnterAir), State.InAirAiming },
        { new StateTransition(State.ChargingJumpAiming, Command.CancelAim), State.ChargingJumpHolding },
        { new StateTransition(State.ChargingJumpAiming, Command.Throw), State.ChargingJump },


        { new StateTransition(State.InAir, Command.GrabSurface), State.OnSurface },
        { new StateTransition(State.InAir, Command.Pickup), State.InAirHolding },

        { new StateTransition(State.InAirHolding, Command.GrabSurface), State.OnSurfaceHolding },
        { new StateTransition(State.InAirHolding, Command.StartAiming), State.InAirAiming },

        { new StateTransition(State.InAirAiming, Command.GrabSurface), State.OnSurfaceAiming },
        { new StateTransition(State.InAirAiming, Command.CancelAim), State.InAirHolding },
        { new StateTransition(State.InAirAiming, Command.Throw), State.InAir },
    };

    private static readonly Dictionary<StateGroup, State[]> stateGroups = new ()
    {
        { StateGroup.CanPickup, new State[]{ State.OnSurface, State.ChargingJump, State.InAir } },
        { StateGroup.Holding, new State[]{ State.OnSurfaceHolding, State.ChargingJumpHolding, State.InAirHolding } },
        { StateGroup.Aiming, new State[]{ State.OnSurfaceAiming, State.ChargingJumpAiming, State.InAirAiming } },

        { StateGroup.OnSurface, new State[]{ State.OnSurface, State.OnSurfaceHolding, State.OnSurfaceAiming, State.OnSurfaceMining } },
        { StateGroup.InAir, new State[]{ State.InAir, State.InAirAiming, State.InAirHolding } },
        { StateGroup.ChargingJump, new State[]{ State.ChargingJump, State.ChargingJumpAiming, State.ChargingJumpHolding } },

        { StateGroup.CanMine, new State[]{ State.OnSurface } },
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
        StateTransition transition = new (CurrentState, command);
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

    public enum State
    {
        OnSurface,
        OnSurfaceHolding,
        OnSurfaceAiming,
        OnSurfaceMining,

        ChargingJump,
        ChargingJumpHolding,
        ChargingJumpAiming,

        InAir,
        InAirHolding,
        InAirAiming,
    }

    public enum Command
    {
        EnterAir,
        GrabSurface,
        StartChargingJump,

        Pickup,
        StartAiming,
        CancelAim,
        Throw,

        StartMining,
        StopMining,
    }

    public enum StateGroup
    {
        CanPickup,
        Holding,
        Aiming,

        OnSurface,
        InAir,
        ChargingJump,

        CanMine,
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
