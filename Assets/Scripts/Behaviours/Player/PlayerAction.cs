namespace Game.Behaviours.Player
{
    using Game.Utility.Networking;
    using Game.Utility;
    using Game.Managers;
    using Game.DataAssets;

    using UnityEngine;
    using Unity.Netcode;
    using UnityEngine.InputSystem;
    using System.Collections.Generic;
    using System.Collections;
    using System;

    public class PlayerAction : NetworkBehaviour
    {
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                InitialiseMiningBehaviour();
                InitialisePickupThrowBehaviour();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                ShutdownMiningBehaviour();
                ShutdownPickupThrowBehaviour();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (IsOwner)
            {
                CheckTriggerWithPickupBehaviour(other);
            }
        }

        #region State Machine

        public State CurrentState { get; private set; }

        private static readonly Dictionary<StateTransition, State> transitions = new Dictionary<StateTransition, State>()
        {
            // Pickup/Throw Behaviour
            { new StateTransition(State.Idle, Command.Pickup), State.HoldingObject },
            { new StateTransition(State.HoldingObject, Command.Aim), State.Aiming },
            { new StateTransition(State.Aiming, Command.CancelAim), State.HoldingObject },
            { new StateTransition(State.Aiming, Command.Throw), State.Idle },
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
            Idle,
            HoldingObject,
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

        #region Mining Behaviour

        private void InitialiseMiningBehaviour()
        {
            UserInputManager.Instance.OnPrimaryMouseDown += OnPrimaryMouseDown;
        }

        private void ShutdownMiningBehaviour()
        {
            UserInputManager.Instance.OnPrimaryMouseDown -= OnPrimaryMouseDown;
        }

        private void OnPrimaryMouseDown(InputAction.CallbackContext context)
        {
            if (IsInState(State.Idle) && Raycasting.CalculateMouseWorldIntersect(UserInputManager.Instance.MousePos, out RaycastHit hitInfo, new string[] { "Mineable" }))
            {
                Destroy(hitInfo.collider.gameObject);
            }
        }

        #endregion

        #region Pickup/Throw Behaviour

        private void InitialisePickupThrowBehaviour()
        {
            arcLineRenderer = GetComponent<LineRenderer>();

            UserInputManager.Instance.OnPrimaryMouseDown += Aim;
            UserInputManager.Instance.OnPrimaryMouseUp += Throw;
            UserInputManager.Instance.OnSecondaryMouseDown += CancelAim;

            SetArcActive(false);
        }

        private void ShutdownPickupThrowBehaviour()
        {
            if (calcAndDrawLaunchPathRoutine is not null)
            {
                StopCoroutine(calcAndDrawLaunchPathRoutine);
            }
        }

        // PICKUP BEHAVIOUR //

        [SerializeField] private Transform objectHoldingPos;

        private Holdable heldObject;

        public Vector3 HoldingPosition => objectHoldingPos.position;

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

        private void CheckTriggerWithPickupBehaviour(Collider other)
        {
            if (IsInState(State.Idle) && IsClient && IsOwner && other.gameObject.CompareTag("Pickupable"))
            {
                Pickupable pickupable = other.gameObject.GetComponent<Pickupable>();

                if (pickupable.IsPickupable)
                {
                    RequestPickupServerRpc(pickupable.NetworkObjectId);
                }
            }
        }

        private void SpawnHoldable(Pickupable pickupable)
        {
            if (ItemDatabase.Instance.GetItemByID(pickupable.ItemID, out Item itemData))
            {
                heldObject = Instantiate(itemData.HoldablePrefab, objectHoldingPos).GetComponent<Holdable>();
                pickupable.gameObject.SetActive(false);
            }
        }

        // THROW BEHAVIOUR //

        private const float ARC_SEGMENT_INTERVAL = 0.005f;
        private const float ARC_MAX_SIMULATION_TIME = 8f;

        [SerializeField] private Transform arcTarget;

        private LineRenderer arcLineRenderer;
        private Ballistics.LaunchPathInfo? launchPathInfo = null;
        private Coroutine calcAndDrawLaunchPathRoutine;

        public float ThrowSpeed { get; set; } = 12;

        [ServerRpc]
        private void RequestThrowServerRpc(Quaternion launchDir)
        {
            if (ItemDatabase.Instance.GetItemByID(heldObject.ItemID, out Item itemData))
            {
                Transform projectile = Instantiate(itemData.PickupPrefab, HoldingPosition, Quaternion.identity).transform;
                projectile.GetComponent<NetworkObject>().Spawn();
                projectile.GetComponent<Rigidbody>().velocity = launchDir * Vector3.forward * ThrowSpeed;
                GrantThrowClientRpc();
            }
        }

        [ClientRpc]
        private void GrantThrowClientRpc()
        {
            Destroy(heldObject.gameObject);
            heldObject = null;

            if (IsOwner)
            {
                MoveState(Command.Throw);
            }
        }

        private void Aim(InputAction.CallbackContext context)
        {
            if (IsInState(State.HoldingObject))
            {
                MoveState(Command.Aim);

                if (calcAndDrawLaunchPathRoutine is not null)
                {
                    StopCoroutine(calcAndDrawLaunchPathRoutine);
                }

                calcAndDrawLaunchPathRoutine = StartCoroutine(CalculateAndRenderThrowPathRoutine());
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
                Vector3 mouseWorldPoint = Raycasting.CalculateMousePlaneInstersect(Mouse.current.position.ReadValue(), Vector3.zero, Vector3.back);
                launchPathInfo = Ballistics.GenerateComplexTrajectoryPath(HoldingPosition, mouseWorldPoint, ThrowSpeed, ARC_SEGMENT_INTERVAL, ARC_MAX_SIMULATION_TIME);

                if (launchPathInfo.HasValue)
                {
                    SetArcActive(true);

                    arcLineRenderer.positionCount = launchPathInfo.Value.launchPath.Length;
                    arcLineRenderer.SetPositions(launchPathInfo.Value.launchPath);

                    if (launchPathInfo.Value.hit.HasValue)
                    {
                        arcTarget.position = launchPathInfo.Value.hit.Value.point;
                        arcTarget.LookAt(arcTarget.position + launchPathInfo.Value.hit.Value.normal);
                    }
                }
                else
                {
                    SetArcActive(false);
                }

                yield return new WaitForEndOfFrame();
            }

            calcAndDrawLaunchPathRoutine = null;
            SetArcActive(false);
        }

        private void SetArcActive(bool isActive)
        {
            arcLineRenderer.enabled = isActive;
            arcTarget.gameObject.SetActive(isActive);
        }

        #endregion

    }
}
