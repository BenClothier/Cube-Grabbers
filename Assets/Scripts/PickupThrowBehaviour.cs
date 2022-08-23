namespace Character
{
    using UnityEngine;
    using Unity.Netcode;
    using Game.Utility.Networking;
    using Game.Utility.Math;
    using UnityEngine.InputSystem;
    using System.Collections.Generic;
    using System;

    public class PickupThrowBehaviour : NetworkBehaviour
    {
        [SerializeField] private Transform objectHoldingPos;

        public Vector3 HoldingPosition => objectHoldingPos.position;

        private GameObject heldObject;

        #region Pickup Behaviour

        [ServerRpc]
        public void RequestPickupServerRpc(ulong pickupNetObjID)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects[pickupNetObjID].gameObject.TryGetComponent(out Pickupable pickupable))
            {
                if (pickupable.IsPickupable)
                {
                    SpawnHoldable(pickupable);

                    GrantPickupClientRpc(pickupNetObjID);
                    NetworkingTools.DespawnAfterSeconds(pickupable.NetworkObject, 2);

                    if (IsOwner)
                    {
                        MoveState(Command.Pickup);
                    }
                }
            }
            else
            {
                Debug.LogError("Something went wrong when trying to find the pickupable's network object.");
            }
        }

        [ClientRpc]
        public void GrantPickupClientRpc(ulong pickupNetObjID)
        {
            if (!IsServer)
            {
                if (NetworkManager.SpawnManager.SpawnedObjects[pickupNetObjID].gameObject.TryGetComponent(out Pickupable pickupable))
                {
                    SpawnHoldable(pickupable);

                    if (IsOwner)
                    {
                        MoveState(Command.Pickup);
                    }
                }
                else
                {
                    Debug.LogError("Something went wrong when trying to find the pickupable's network object.");
                }
            }
        }

        private void SpawnHoldable(Pickupable pickupable)
        {
            heldObject = Instantiate(pickupable.HoldablePrefab, objectHoldingPos);
            pickupable.gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsClient && IsOwner && other.gameObject.CompareTag("Pickupable"))
            {
                Pickupable pickupable = other.gameObject.GetComponent<Pickupable>();

                if (pickupable.IsPickupable)
                {
                    RequestPickupServerRpc(pickupable.NetworkObjectId);
                }
            }
        }

        #endregion

        #region Throw Behaviour

        [SerializeField] Transform projectilePrefab;

        private Controls controls;
        private Ballistics.LaunchPathInfo? launchPathInfo = null;

        public float ThrowSpeed { get; set; } = 12;

        private void LateUpdate()
        {
            if (IsInState(State.Aiming) && CalculateMouseWorldIntersect(Mouse.current.position.ReadValue(), out RaycastHit mouseWorldHitInfo))
            {
                launchPathInfo = Ballistics.GenerateComplexTrajectoryPath(HoldingPosition, mouseWorldHitInfo.point, ThrowSpeed, 0.002f, 8);

                Debug.DrawRay(launchPathInfo.Value.highestPoint, Vector3.down, Color.red);

                if (launchPathInfo.Value.launchPath.Length > 0)
                {
                    foreach (var point in launchPathInfo.Value.launchPath)
                    {
                        Debug.DrawRay(point, Vector3.down * .25f, Color.blue);
                    }
                }

                if (launchPathInfo.Value.hit.HasValue)
                {
                    Debug.DrawRay(launchPathInfo.Value.hit.Value.point, launchPathInfo.Value.hit.Value.normal, Color.red);
                }
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                controls = new Controls();

                controls.Default.AimThrow.performed += Aim;
                controls.Default.AimThrow.canceled += Throw;
                controls.Default.CancelThrow.performed += CancelAim;

                controls.Default.AimThrow.Enable();
                controls.Default.CancelThrow.Enable();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner && controls is not null)
            {
                controls.Default.AimThrow.Disable();
                controls.Default.CancelThrow.Disable();
            }
        }

        [ServerRpc]
        private void RequestThrowServerRpc(Quaternion launchDir)
        {
            Transform projectile = Instantiate(projectilePrefab, HoldingPosition, launchDir);
            projectile.GetComponent<NetworkObject>().Spawn();
            projectile.GetComponent<Rigidbody>().velocity = projectile.transform.forward * ThrowSpeed;
            GrantThrowClientRpc();
        }

        [ClientRpc]
        private void GrantThrowClientRpc()
        {
            Destroy(heldObject);
            heldObject = null;

            if (IsOwner)
            {
                MoveState(Command.Throw);
            }
        }

        private void Aim(InputAction.CallbackContext context)
        {
            if (IsInState(State.Holding))
            {
                MoveState(Command.Aim);
            }
        }

        private void Throw(InputAction.CallbackContext context)
        {
            if (IsInState(State.Aiming))
            {
                if (launchPathInfo.HasValue)
                {
                    RequestThrowServerRpc(launchPathInfo.Value.launchDir);
                    launchPathInfo = null;
                }
                else
                {
                    MoveState(Command.CancelAim);
                    launchPathInfo = null;
                }
            }
        }

        private void CancelAim(InputAction.CallbackContext context)
        {
            if (IsInState(State.Aiming))
            {
                MoveState(Command.CancelAim);
                launchPathInfo = null;
            }
        }

        private static bool CalculateMouseWorldIntersect(Vector2 mousePos, out RaycastHit hitInfo)
        {
            Ray pointerRay = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(pointerRay, out hitInfo, 200, LayerMask.GetMask("Default")))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region State Machine

        public State CurrentState { get; private set; }

        private static readonly Dictionary<StateTransition, State> transitions = new Dictionary<StateTransition, State>()
        {
            { new StateTransition(State.Normal, Command.Pickup), State.Holding },
            { new StateTransition(State.Holding, Command.Aim), State.Aiming },
            { new StateTransition(State.Aiming, Command.CancelAim), State.Holding },
            { new StateTransition(State.Aiming, Command.Throw), State.Normal },
        };

        private State GetState(Command command)
        {
            StateTransition transition = new StateTransition(CurrentState, command);

            if (!transitions.TryGetValue(transition, out State nextState))
            {
                throw new Exception("Invalid transition: " + CurrentState + " -> " + command);
            }

            return nextState;
        }

        public State MoveState(Command command)
        {
            CurrentState = GetState(command);
            Debug.Log($"New State: {CurrentState}");
            return CurrentState;
        }

        public bool IsInState(State state) => CurrentState.Equals(state);

        public enum State
        {
            Normal,
            Holding,
            Aiming,
        }

        public enum Command
        {
            Pickup,
            Aim,
            CancelAim,
            Throw,
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

        #endregion
    }
}
