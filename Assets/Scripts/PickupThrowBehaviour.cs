namespace Game.Behaviours.Player
{
    using Game.Utility.Networking;
    using Game.Utility;
    using Game.Managers;

    using UnityEngine;
    using Unity.Netcode;
    using UnityEngine.InputSystem;
    using System.Collections.Generic;
    using System.Collections;
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

        private const float ARC_SEGMENT_INTERVAL = 0.005f;
        private const float ARC_MAX_SIMULATION_TIME = 8f;

        [SerializeField] private Transform projectilePrefab;
        [SerializeField] private Transform arcTarget;

        private LineRenderer arcLineRenderer;
        private Ballistics.LaunchPathInfo? launchPathInfo = null;

        public float ThrowSpeed { get; set; } = 12;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                arcLineRenderer = GetComponent<LineRenderer>();

                UserInputManager.Instance.OnPrimaryMouseDown += Aim;
                UserInputManager.Instance.OnPrimaryMouseUp += Throw;
                UserInputManager.Instance.OnSecondaryMouseDown += CancelAim;

                SetArcActive(false);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                StopAllCoroutines();
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
                StartCoroutine(CalculateAndRenderThrowPathRoutine());
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

        private IEnumerator CalculateAndRenderThrowPathRoutine()
        {
            while (IsInState(State.Aiming))
            {
                if (Raycasting.CalculateMouseWorldIntersect(Mouse.current.position.ReadValue(), out RaycastHit mouseWorldHitInfo, layermask: LayerMask.GetMask("Default")))
                {
                    launchPathInfo = Ballistics.GenerateComplexTrajectoryPath(HoldingPosition, (Vector2)mouseWorldHitInfo.point, ThrowSpeed, ARC_SEGMENT_INTERVAL, ARC_MAX_SIMULATION_TIME);

                    if (launchPathInfo.HasValue)
                    {
                        SetArcActive(true);

                        arcLineRenderer.positionCount = launchPathInfo.Value.launchPath.Length;
                        arcLineRenderer.SetPositions(launchPathInfo.Value.launchPath);

                        if (launchPathInfo.Value.hit.HasValue)
                        {
                            arcTarget.position = launchPathInfo.Value.hit.Value.point;
                            Debug.Log($"Normal: {launchPathInfo.Value.hit.Value.normal}");
                            arcTarget.LookAt(arcTarget.position + launchPathInfo.Value.hit.Value.normal);
                        }
                    }
                    else
                    {
                        SetArcActive(false);
                    }
                }
                else
                {
                    SetArcActive(false);
                }

                yield return new WaitForEndOfFrame();
            }

            SetArcActive(false);
        }

        private void SetArcActive(bool isActive)
        {
            arcLineRenderer.enabled = isActive;
            arcTarget.gameObject.SetActive(isActive);
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
