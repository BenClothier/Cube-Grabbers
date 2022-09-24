namespace Game.Behaviours.Player
{
    using Game.Managers;

    using UnityEngine;
    using Unity.Netcode;
    using Cinemachine;
    using System.Collections.Generic;
    using System;
    using UnityEngine.InputSystem;
    using System.Linq;
    using Unity.VisualScripting;
    using static PlayerStateMachine;

    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkObject))]
    public class PlayerMovement : NetworkBehaviour
    {
        /// <summary>
        /// <para>Speed at which the character's local position will be corrected to the server position.</para>
        /// <br>Lower: Less stutter, slower sync</br>
        /// <br>Higher: More stutter, faster sync</br>
        /// </summary>
        private static readonly float MOVE_SYNC_SPEED = 0.15f;

        /// <summary>
        /// <para>Speed at which the character's local rotation will be corrected to the owner-client position.</para>
        /// <br>Lower: Less stutter, slower sync</br>
        /// <br>Higher: More stutter, faster sync</br>
        /// </summary>
        private static readonly float LOOK_SYNC_SPEED = 0.3f;

        // network variables
        private readonly NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();
        private readonly NetworkVariable<Quaternion> networkLookRotation = new NetworkVariable<Quaternion>();

        [Header("Model")]
        [SerializeField] private GameObject model;

        [Header("Horizontal Movement")]
        [SerializeField] private float maxSpeed;
        [SerializeField] private float accelerationSpeed;
        [SerializeField] private float decelerationSpeed;
        [Space]
        [SerializeField] private float inAirMaxSpeed;
        [SerializeField] private float inAirAccelerationSpeed;
        [SerializeField] private float inAirDecelerationSpeed;

        [Header("Vertical Movement")]
        [SerializeField] private AnimationCurve jumpSpeedByChargeTime;
        [SerializeField] private AnimationCurve gravityMultiplierByVerticalVelocity;
        [SerializeField] private EventChannel_Void OnStartChargingJumpChannel;
        [SerializeField] private EventChannel_Void OnJumpChannel;

        [Header("LookRotation")]
        [SerializeField] private float lookSpeed;

        private PlayerStateMachine stateMachine;
        private CinemachineVirtualCamera mainVirtualCam;

        private Quaternion targetLookRotation;
        private float targetHorizontalVelocity;
        private float horizontalVelocity;
        private float verticalVelocity;

        private float jumpChargeTimeStarted;

        private List<GameObject> currentGroundCollisions = new List<GameObject>();

        private bool IsOnGround => CountGroundColliders() > 0;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsClient)
            {
                mainVirtualCam = FindObjectOfType<CinemachineVirtualCamera>();
                mainVirtualCam.Follow = transform;
            }

            if (IsClient && IsOwner)
            {
                stateMachine = GetComponent<PlayerStateMachine>();
                UserInputManager.Instance.OnJumpPressed += OnJumpPressed;
                UserInputManager.Instance.OnJumpReleased += OnJumpReleased;
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsClient)
            {
                mainVirtualCam.Follow = null;
            }

            if (IsClient && IsOwner)
            {
                UserInputManager.Instance.OnJumpPressed -= OnJumpPressed;
                UserInputManager.Instance.OnJumpReleased -= OnJumpReleased;
            }
        }

        private void FixedUpdate()
        {
            if (IsClient && IsOwner)
            {
                ClientInput();
            }

            UpdatePosition();
            UpdateLookRotation();
        }

        /// <summary>
        /// Send desired velocity and look-rotation information to the server.
        /// </summary>
        /// <param name="targetHorizontalVelocity">desired horizontal velocity of the character (server-authorised).</param>
        /// <param name="verticalVelocity">vertical velocity of the character (server-authorised).</param>
        /// <param name="lookRotation">look-rotation of the character (client-authorised).</param>
        [ServerRpc]
        private void UpdatePositionRotationServerRpc(float targetHorizontalVelocity, float verticalVelocity, Quaternion lookRotation)
        {
            this.targetHorizontalVelocity = targetHorizontalVelocity;
            this.verticalVelocity = verticalVelocity;
            networkLookRotation.Value = lookRotation;
        }

        /// <summary>
        /// Get the user input from the owner and send RPC to update server information.
        /// </summary>
        private void ClientInput()
        {
            if (stateMachine.IsInStateGroup(StateGroup.OnGround))
            {
                if (!IsOnGround)
                {
                    stateMachine.TryMoveState(Command.StartFalling);
                }
            }
            else if (stateMachine.IsInStateGroup(StateGroup.Rising))
            {
                if (verticalVelocity <= 0)
                {
                    stateMachine.TryMoveState(Command.StartFalling);
                }
            }
            else if (stateMachine.IsInStateGroup(StateGroup.Falling))
            {
                if (IsOnGround)
                {
                    stateMachine.TryMoveState(Command.HitGround);
                }
            }

            CalcAndSetTargetLookRotation();
            CalcAndSetTargetHorizontalVelocity();
            CalcAndSetVerticalVelocity();

            // Tell the server what velocity we want and what the current rotation of the character is
            UpdatePositionRotationServerRpc(targetHorizontalVelocity, verticalVelocity, model.transform.rotation);
        }

        /// <summary>
        /// Updates the position of the character in the world. This has server authority.
        /// </summary>
        private void UpdatePosition()
        {
            Vector3 movement = new Vector3(CalcAndSetHorizontalVelocity(), verticalVelocity, 0) * Time.deltaTime;

            if (IsServer)
            {
                // Simply move the character and update the networked value for other clients to see
                transform.position = transform.position + movement;
                networkPosition.Value = transform.position;
            }
            else
            {
                // Calculate new position based on velocity
                Vector3 newLocalPosition = transform.position + movement;
                Vector3 newServerPosition = networkPosition.Value + movement;

                // Gradually corrects to the server's position. If this is done too quickly, the user feels stutter
                transform.position = Vector3.Lerp(newLocalPosition, newServerPosition, MOVE_SYNC_SPEED);
            }
        }

        /// <summary>
        /// Updates the look-rotation of the character in the world. This has Client-authority.
        /// </summary>
        private void UpdateLookRotation()
        {
            if (IsOwner)
            {
                // If we own this, just ignore networking and rotate the character
                model.transform.rotation = LerpLookVector();
            }
            else
            {
                // If we don't own this, set the current rotation to that of the networked value (with smoothing)
                model.transform.rotation = Quaternion.Lerp(model.transform.rotation, networkLookRotation.Value, LOOK_SYNC_SPEED);
            }
        }

        /// <summary>
        /// Smoothly animates the look-direction of the character to the desired direction.
        /// </summary>
        /// <returns>look-rotation at this time through the animation.</returns>
        private Quaternion LerpLookVector()
        {
            return Quaternion.Lerp(model.transform.rotation, targetLookRotation, lookSpeed * Time.deltaTime);
        }

        /// <summary>
        /// Accelerates/decelerates the vertical velocity depending on the character state.
        /// </summary>
        /// <returns>The new vertical velocity.</returns>
        private float CalcAndSetVerticalVelocity()
        {
            if (stateMachine.IsInStateGroup(StateGroup.InAir))
            {
                verticalVelocity += Physics.gravity.y * gravityMultiplierByVerticalVelocity.Evaluate(verticalVelocity) * Time.deltaTime;
            }
            else
            {
                verticalVelocity = 0;
            }

            return verticalVelocity;
        }

        /// <summary>
        /// Accelerates/decelerates the horizontal velocity to the target velocity.
        /// </summary>
        /// <returns>The new horizontal velocity.</returns>
        private float CalcAndSetHorizontalVelocity()
        {
            bool isAccelerating = Mathf.Abs(targetHorizontalVelocity) >= Mathf.Abs(horizontalVelocity);

            if (stateMachine.IsInStateGroup(StateGroup.OnGround))
            {
                horizontalVelocity = Mathf.Lerp(horizontalVelocity, targetHorizontalVelocity, (isAccelerating ? accelerationSpeed : decelerationSpeed) * Time.deltaTime);
            }
            else if (stateMachine.IsInStateGroup(StateGroup.InAir))
            {
                horizontalVelocity = Mathf.Lerp(horizontalVelocity, targetHorizontalVelocity, (isAccelerating ? inAirAccelerationSpeed : inAirDecelerationSpeed) * Time.deltaTime);
            }
            else
            {
                horizontalVelocity = 0;
            }

            return horizontalVelocity;
        }

        /// <summary>
        /// Reads the user input to calculate the desired movement velocity.
        /// </summary>
        /// <returns>desired movement velocity of the character.</returns>
        private float CalcAndSetTargetHorizontalVelocity()
        {
            targetHorizontalVelocity = UserInputManager.Instance.PlayerMovementVector * (stateMachine.IsInStateGroup(StateGroup.InAir) ? inAirMaxSpeed : maxSpeed);
            return targetHorizontalVelocity;
        }

        /// <summary>
        /// Reads the user input of the mouse to calculate the desired look-rotation.
        /// </summary>
        /// <returns>desired look-rotation of the character.</returns>
        private Quaternion CalcAndSetTargetLookRotation()
        {
            targetLookRotation = Quaternion.LookRotation(UserInputManager.Instance.MousePos.x < Screen.width / 2 ? Vector3.left : Vector3.right, Vector3.up);
            return targetLookRotation;
        }

        private void OnJumpPressed(InputAction.CallbackContext cxt)
        {
            if (stateMachine.IsInStateGroup(StateGroup.OnGround))
            {
                stateMachine.TryMoveState(Command.StartChargingJump);
                jumpChargeTimeStarted = Time.time;
                OnStartChargingJumpChannel.RaiseEvent();
            }
        }

        private void OnJumpReleased(InputAction.CallbackContext cxt)
        {
            if (stateMachine.IsInStateGroup(StateGroup.ChargingJump))
            {
                stateMachine.TryMoveState(Command.StartRising);
                OnJumpChannel.RaiseEvent();
                verticalVelocity = jumpSpeedByChargeTime.Evaluate(Time.time - jumpChargeTimeStarted);
            }
        }

        private int CountGroundColliders()
        {
            currentGroundCollisions = currentGroundCollisions.Where(go => go is not null && !go.IsDestroyed()).ToList();
            return currentGroundCollisions.Count();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.contacts.Any(contact => contact.normal == Vector3.up))
            {
                currentGroundCollisions.Add(collision.gameObject);
            }
        }


        private void OnCollisionExit(Collision collision)
        {
            currentGroundCollisions.Remove(collision.gameObject);
        }
    }
}
