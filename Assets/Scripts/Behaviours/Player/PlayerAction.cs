namespace Game.Behaviours.Player
{
    using Game.Utility.Networking;
    using Game.Utility;
    using Game.Managers;
    using Game.DataAssets;

    using UnityEngine;
    using Unity.Netcode;
    using UnityEngine.InputSystem;
    using System.Collections;
    using static PlayerStateMachine;
    using System;

    public class PlayerAction : NetworkBehaviour
    {
        private PlayerStateMachine stateMachine;

        private Coroutine miningRoutine;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsOwner)
            {
                stateMachine = GetComponent<PlayerStateMachine>();
                InitialiseMiningBehaviour();
                InitialisePickupThrowBehaviour();
                UserInputManager.Instance.OnPrimaryMouseDown += OnPrimaryMouseDown;
                UserInputManager.Instance.OnPrimaryMouseUp += OnPrimaryMouseUp;
                UserInputManager.Instance.OnSecondaryMouseDown += OnSecondaryMouseDown;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsOwner)
            {
                ShutdownMiningBehaviour();
                ShutdownPickupThrowBehaviour();
                UserInputManager.Instance.OnPrimaryMouseDown -= OnPrimaryMouseDown;
            }
        }

        private void OnPrimaryMouseDown(InputAction.CallbackContext context)
        {
            if (stateMachine.IsInStateGroup(StateGroup.CanMine))
            {
                Ray ray = Camera.main.ScreenPointToRay(UserInputManager.Instance.MousePos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    TryMine(hit.collider);
                }
            }
            else if (stateMachine.IsInStateGroup(StateGroup.Holding))
            {
                Aim();
            }
        }

        private void OnPrimaryMouseUp(InputAction.CallbackContext context)
        {
            if (stateMachine.IsInStateGroup(StateGroup.CanPickup))
            {
                Ray ray = Camera.main.ScreenPointToRay(UserInputManager.Instance.MousePos);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    TryPickup(hit.collider);
                }
            }
            else if (stateMachine.IsInStateGroup(StateGroup.Aiming))
            {
                Throw();
            }
            else if (stateMachine.IsInState(State.Mining))
            {
                TryCancelMining();
            }
        }

        private void OnSecondaryMouseDown(InputAction.CallbackContext context)
        {
            if (stateMachine.IsInStateGroup(StateGroup.Aiming))
            {
                CancelAim();
            }
        }

        #region Mining Behaviour

        [Header("Mining")]
        [SerializeField] private float maxMiningDistance;

        [SerializeField] private EventChannel_Vector2 onStartMiningEvent;
        [SerializeField] private EventChannel_Void onStopMiningEvent;

        private void InitialiseMiningBehaviour()
        {
        }

        private void ShutdownMiningBehaviour()
        {
        }

        private bool TryMine(Collider collider)
        {
            if (collider.CompareTag("Mineable") && Vector2.Distance(collider.transform.position, transform.position) <= maxMiningDistance)
            {
                Vector2Int gridLoc = WorldController.Instance.WorldGrid.GetGridLocFromWorldPos(collider.transform.parent.position);
                if (WorldController.Instance.WorldGrid.TryGetNearestEmptyNeighbour(transform.position, gridLoc, out Vector2? nearestEmptyNeighbourPos, neighbourSet: Components.WorldGrid.NeighbourSet.Normal))
                {
                    if (miningRoutine is null)
                    {
                        if (WorldController.Instance.TryGetTimeToMine(gridLoc, out float? secondsToMine))
                        {
                            Vector2 dirToCell = ((Vector2)collider.transform.parent.position - nearestEmptyNeighbourPos.Value).normalized;
                            stateMachine.TryMoveState(Command.StartMining);
                            onStartMiningEvent.InvokeEvent(nearestEmptyNeighbourPos.Value + dirToCell / 2);
                            miningRoutine = StartCoroutine(MiningRoutine(gridLoc, secondsToMine.Value));
                            return true;
                        }
                        else
                        {
                            Debug.LogError("Failed to get seconds-to-mine for the given grid location");
                        }
                    }
                    else
                    {
                        Debug.LogError("Attempted to start a second mining routine. This should not happen.");
                    }
                }
                else
                {
                    Debug.LogWarning("Couldn't find empty positions for cell.");
                }
            }

            return false;
        }

        private bool TryCancelMining()
        {
            if (miningRoutine != null)
            {
                StopCoroutine(miningRoutine);
                miningRoutine = null;
                return true;
            }

            return false;
        }

        private IEnumerator MiningRoutine(Vector2Int gridLoc, float secondsToMine)
        {
            Debug.Log("Started Mining");
            yield return new WaitForSeconds(secondsToMine);

            RequestMineCellServerRpc(gridLoc);
            miningRoutine = null;
            stateMachine.TryMoveState(Command.StartFalling);
            onStopMiningEvent.InvokeEvent();
        }

        [ServerRpc]
        private void RequestMineCellServerRpc(Vector2Int cellPosition)
        {
            WorldController.Instance.MineCell(cellPosition);
        }

        #endregion

        #region Pickup/Throw Behaviour

        private void InitialisePickupThrowBehaviour()
        {

            arcTargetInstance = Instantiate(arcTargetPrefab, transform).transform;
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

        [Header("Pickup")]
        [SerializeField] private float maxPickupDistance;
        [SerializeField] private Transform objectHoldingPos;

        private Holdable heldObject;

        public Vector3 HoldingPosition => objectHoldingPos.position;

        private bool TryPickup(Collider collider)
        {
            if (collider.CompareTag("Pickupable") && Vector2.Distance(collider.transform.position, transform.position) <= maxPickupDistance)
            {
                Pickupable pickupable = collider.gameObject.GetComponentInParent<Pickupable>();

                if (pickupable.IsPickupable)
                {
                    RequestPickupServerRpc(pickupable.NetworkObjectId);
                }

                return true;
            }

            return false;
        }

        [ServerRpc]
        private void RequestPickupServerRpc(ulong pickupNetObjID)
        {
            if (NetworkManager.SpawnManager.SpawnedObjects[pickupNetObjID].gameObject.TryGetComponent(out Pickupable pickupable))
            {
                if (pickupable.IsPickupable)
                {
                    SpawnHoldable(pickupable);

                    GrantPickupClientRpc(pickupNetObjID);
                    StartCoroutine(NetworkingTools.DespawnAfterSeconds(pickupable.NetworkObject, 2));

                    if (IsOwner)
                    {
                        stateMachine.TryMoveState(Command.Pickup);
                    }
                }
            }
            else
            {
                Debug.LogError("Something went wrong when trying to find the pickupable's network object.");
            }
        }

        [ClientRpc]
        private void GrantPickupClientRpc(ulong pickupNetObjID)
        {
            if (!IsServer)
            {
                if (NetworkManager.SpawnManager.SpawnedObjects[pickupNetObjID].gameObject.TryGetComponent(out Pickupable pickupable))
                {
                    SpawnHoldable(pickupable);

                    if (IsOwner)
                    {
                        stateMachine.TryMoveState(Command.Pickup);
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
            if (ItemDatabase.Instance.GetItemByID(pickupable.ItemID, out Item itemData))
            {
                heldObject = Instantiate(itemData.HoldablePrefab, objectHoldingPos).GetComponent<Holdable>();
                pickupable.gameObject.SetActive(false);
            }
        }

        // THROW BEHAVIOUR //

        private const float ARC_SEGMENT_INTERVAL = 0.005f;
        private const float ARC_MAX_SIMULATION_TIME = 8f;

        [Header("Throwing")]
        [SerializeField] private float throwSpeedMultiplier;
        [SerializeField] private float maxThrowSpeed;
        [SerializeField] private float minThrowSpeed;
        [Space]
        [SerializeField] private GameObject arcTargetPrefab;
        [SerializeField] private LineRenderer throwArcLineRenderer;
        [SerializeField] private LineRenderer throwTangentLineRenderer;

        private Transform arcTargetInstance;
        private Ballistics.LaunchPathInfo? launchPathInfo = null;
        private Coroutine calcAndDrawLaunchPathRoutine;

        public float MaxThrowSpeed { get; set; } = 12;

        [ServerRpc]
        private void RequestThrowServerRpc(Quaternion launchDir, float throwSpeed)
        {
            if (ItemDatabase.Instance.GetItemByID(heldObject.ItemID, out Item itemData))
            {
                Transform projectile = Instantiate(itemData.PickupPrefab, HoldingPosition, Quaternion.identity).transform;
                projectile.GetComponent<NetworkObject>().Spawn();
                projectile.GetComponent<Rigidbody>().velocity = launchDir * Vector3.forward * Mathf.Clamp(throwSpeed, 0, MaxThrowSpeed);
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
                stateMachine.TryMoveState(Command.Throw);
            }
        }

        private void Aim()
        {
            if (stateMachine.IsInStateGroup(StateGroup.Holding))
            {
                stateMachine.TryMoveState(Command.StartAiming);

                if (calcAndDrawLaunchPathRoutine is not null)
                {
                    StopCoroutine(calcAndDrawLaunchPathRoutine);
                }

                calcAndDrawLaunchPathRoutine = StartCoroutine(CalculateAndRenderThrowPathRoutine());
            }
        }

        private void Throw()
        {
            if (stateMachine.IsInStateGroup(StateGroup.Aiming))
            {
                if (launchPathInfo.HasValue)
                {
                    RequestThrowServerRpc(launchPathInfo.Value.launchDir, launchPathInfo.Value.launchSpeed);
                    launchPathInfo = null;
                }
                else
                {
                    stateMachine.TryMoveState(Command.CancelAim);
                    launchPathInfo = null;
                }
            }
        }

        private void CancelAim()
        {
            if (stateMachine.IsInStateGroup(StateGroup.Aiming))
            {
                stateMachine.TryMoveState(Command.CancelAim);
                launchPathInfo = null;
            }
        }

        private IEnumerator CalculateAndRenderThrowPathRoutine()
        {
            while (stateMachine.IsInStateGroup(StateGroup.Aiming))
            {
                Vector3 mouseWorldPoint = Raycasting.CalculateMousePlaneInstersect(Mouse.current.position.ReadValue(), Vector3.zero, Vector3.back);

                Vector3 mouseVector = mouseWorldPoint - HoldingPosition;
                Vector3 throwDir = mouseVector.normalized;
                float throwSpeed = Mathf.Clamp(mouseVector.magnitude * throwSpeedMultiplier, minThrowSpeed, maxThrowSpeed);
                Vector3 throwTangent = (throwSpeed / throwSpeedMultiplier) * throwDir;

                launchPathInfo = Ballistics.GenerateComplexTrajectoryPath(HoldingPosition, mouseVector.normalized, throwSpeed, ARC_SEGMENT_INTERVAL, ARC_MAX_SIMULATION_TIME);

                if (launchPathInfo.HasValue)
                {
                    SetArcActive(true);
                    throwArcLineRenderer.positionCount = launchPathInfo.Value.launchPath.Length;
                    throwArcLineRenderer.SetPositions(launchPathInfo.Value.launchPath);

                    throwTangentLineRenderer.positionCount = 2;
                    throwTangentLineRenderer.SetPosition(0, HoldingPosition);
                    throwTangentLineRenderer.SetPosition(1, HoldingPosition + throwTangent);

                    if (launchPathInfo.Value.hit.HasValue)
                    {
                        arcTargetInstance.position = launchPathInfo.Value.hit.Value.point + Vector3.up * 0.05f;
                        arcTargetInstance.LookAt(arcTargetInstance.position + launchPathInfo.Value.hit.Value.normal);
                    }
                    else
                    {
                        arcTargetInstance.gameObject.SetActive(false);
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
            throwArcLineRenderer.enabled = isActive;
            throwTangentLineRenderer.enabled = isActive;
            arcTargetInstance.gameObject.SetActive(isActive);
        }

        #endregion

    }
}
