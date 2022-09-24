using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    private static readonly Dictionary<StateTransition, State> transitions = new ()
    {
        { new StateTransition(State.OnGround, Command.StartChargingJump), State.ChargingJump },
        { new StateTransition(State.OnGround, Command.StartFalling), State.Falling },
        { new StateTransition(State.OnGround, Command.StartMining), State.Mining },
        { new StateTransition(State.OnGround, Command.Pickup), State.OnGroundHolding },

        { new StateTransition(State.OnGroundHolding, Command.StartChargingJump), State.ChargingJumpHolding },
        { new StateTransition(State.OnGroundHolding, Command.StartFalling), State.FallingHolding },
        { new StateTransition(State.OnGroundHolding, Command.StartAiming), State.OnGroundAiming },

        { new StateTransition(State.OnGroundAiming, Command.StartChargingJump), State.ChargingJumpAiming },
        { new StateTransition(State.OnGroundAiming, Command.StartFalling), State.FallingAiming },
        { new StateTransition(State.OnGroundAiming, Command.CancelAim), State.OnGroundHolding },
        { new StateTransition(State.OnGroundAiming, Command.Throw), State.OnGround },


        { new StateTransition(State.ChargingJump, Command.StartRising), State.Rising },
        { new StateTransition(State.ChargingJump, Command.Pickup), State.ChargingJumpHolding },

        { new StateTransition(State.ChargingJumpHolding, Command.StartRising), State.RisingHolding },
        { new StateTransition(State.ChargingJumpHolding, Command.StartAiming), State.ChargingJumpAiming },

        { new StateTransition(State.ChargingJumpAiming, Command.StartRising), State.RisingAiming },
        { new StateTransition(State.ChargingJumpAiming, Command.CancelAim), State.ChargingJumpHolding },
        { new StateTransition(State.ChargingJumpAiming, Command.Throw), State.ChargingJump },


        { new StateTransition(State.Rising, Command.StartFalling), State.Falling },
        { new StateTransition(State.Rising, Command.StartMining), State.Mining },
        { new StateTransition(State.Rising, Command.Pickup), State.RisingHolding },

        { new StateTransition(State.RisingHolding, Command.StartFalling), State.FallingHolding },
        { new StateTransition(State.RisingHolding, Command.StartAiming), State.RisingAiming },

        { new StateTransition(State.RisingAiming, Command.StartFalling), State.FallingAiming },
        { new StateTransition(State.RisingAiming, Command.CancelAim), State.RisingHolding },
        { new StateTransition(State.RisingAiming, Command.Throw), State.Rising },


        { new StateTransition(State.Falling, Command.HitGround), State.OnGround },
        { new StateTransition(State.Falling, Command.StartMining), State.Mining },
        { new StateTransition(State.Falling, Command.Pickup), State.FallingHolding },

        { new StateTransition(State.FallingHolding, Command.HitGround), State.OnGroundHolding },
        { new StateTransition(State.FallingHolding, Command.StartAiming), State.FallingAiming },

        { new StateTransition(State.FallingAiming, Command.HitGround), State.OnGroundAiming },
        { new StateTransition(State.FallingAiming, Command.CancelAim), State.FallingHolding },
        { new StateTransition(State.FallingAiming, Command.Throw), State.Falling },


        { new StateTransition(State.Mining, Command.StartFalling), State.Falling },
    };

    private static readonly Dictionary<StateGroup, State[]> stateGroups = new ()
    {
        { StateGroup.CanPickup, new State[]{ State.OnGround, State.ChargingJump, State.Rising, State.Falling } },
        { StateGroup.Holding, new State[]{ State.OnGroundHolding, State.ChargingJumpHolding, State.RisingHolding, State.FallingHolding } },
        { StateGroup.Aiming, new State[]{ State.OnGroundAiming, State.ChargingJumpAiming, State.RisingAiming, State.FallingAiming } },

        { StateGroup.OnGround, new State[]{ State.OnGround, State.OnGroundHolding, State.OnGroundAiming } },
        { StateGroup.InAir, new State[]{ State.Rising, State.RisingAiming, State.RisingHolding, State.Falling, State.FallingAiming, State.FallingHolding } },
        { StateGroup.ChargingJump, new State[]{ State.ChargingJump, State.ChargingJumpAiming, State.ChargingJumpHolding } },
        { StateGroup.Rising, new State[]{ State.Rising, State.RisingAiming, State.RisingHolding } },
        { StateGroup.Falling, new State[]{ State.Falling, State.FallingAiming, State.FallingHolding } },

        { StateGroup.CanMine, new State[]{ State.OnGround, State.Rising, State.Falling } },
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
        OnGround,
        OnGroundHolding,
        OnGroundAiming,

        ChargingJump,
        ChargingJumpHolding,
        ChargingJumpAiming,

        Rising,
        RisingHolding,
        RisingAiming,

        Falling,
        FallingHolding,
        FallingAiming,

        Mining,
    }

    public enum Command
    {
        StartChargingJump,
        StartRising,
        StartFalling,
        HitGround,

        Pickup,
        StartAiming,
        CancelAim,
        Throw,

        StartMining,
    }

    public enum StateGroup
    {
        CanPickup,
        Holding,
        Aiming,

        OnGround,
        InAir,
        Rising,
        Falling,
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
