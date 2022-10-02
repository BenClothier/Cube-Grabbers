namespace Game.Behaviours.Player
{
    using Game.Managers;

    using Unity.Netcode;
    using UnityEngine.InputSystem;

    using static CreatureStateMachine;

    public class CreatureAction : NetworkBehaviour
    {
        private CreatureStateMachine stateMachine;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                stateMachine = GetComponent<CreatureStateMachine>();
                UserInputManager.Instance.OnJumpPressed += OnJumpPressed;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                UserInputManager.Instance.OnPrimaryMouseDown -= OnJumpPressed;
            }
        }

        private void OnJumpPressed(InputAction.CallbackContext context)
        {
            if (stateMachine.IsInState(State.OnSurface))
            {
                stateMachine.TryMoveState(Command.EnterAir);
            }
        }
    }
}

