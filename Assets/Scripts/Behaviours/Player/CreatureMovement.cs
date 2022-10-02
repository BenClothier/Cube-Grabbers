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
    using static CreatureStateMachine;

    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkObject))]
    public class CreatureMovement : NetworkBehaviour
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

        [Header("In Air Movement")]
        [SerializeField] private float inAirMaxSpeed;
        [SerializeField] private float inAirAccelerationSpeed;
        [SerializeField] private float inAirDecelerationSpeed;

        [SerializeField] private float flyUpSpeed;
        [SerializeField] private AnimationCurve gravityMultiplierByVerticalVelocity;

        [Header("LookRotation")]
        [SerializeField] private float lookSpeed;

        private CreatureStateMachine stateMachine;
        private CinemachineVirtualCamera mainVirtualCam;

        private Quaternion targetLookRotation;

        private float targetHorizontalVelocity;
        private float horizontalVelocity;
        private float verticalVelocity;

        private List<GameObject> currentHorizontalColliders = new List<GameObject>();
        private List<GameObject> currentVerticalColliders = new List<GameObject>();

        private bool canGrabSurface = true;

        private bool IsOnHorizontalSurface => CountHorizontalColliders() > 0;

        private bool IsOnVerticalSurface => CountVerticalColliders() > 0;

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
                stateMachine = GetComponent<CreatureStateMachine>();
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsClient)
            {
                mainVirtualCam.Follow = null;
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
            if (stateMachine.IsInState(CreatureStateMachine.State.OnSurface))
            {
                CalcAndSetTargetLookRotation();
                CalcAndSetTargetHorizontalVelocity();
            }
            else if (stateMachine.IsInState(CreatureStateMachine.State.InAir))
            {
                if (!IsOnHorizontalSurface && !IsOnVerticalSurface)
                {
                    canGrabSurface = true;
                }
                if (IsOnHorizontalSurface || IsOnVerticalSurface)
                {
                    stateMachine.TryMoveState(Command.GrabSurface);
                    canGrabSurface = false;
                }

                CalcAndSetTargetLookRotation();
                CalcAndSetTargetHorizontalVelocity();
            }

            // Tell the server what velocity we want and what the current rotation of the character is
            UpdatePositionRotationServerRpc(targetHorizontalVelocity, verticalVelocity, model.transform.rotation);
        }

        /// <summary>
        /// Updates the position of the character in the world. This has server authority.
        /// </summary>
        private void UpdatePosition()
        {
            Vector3 movement = CalcAndSetVelocity();

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
        private Vector2 CalcAndSetVelocity()
        {
            bool isAccelerating = Mathf.Abs(targetHorizontalVelocity) >= Mathf.Abs(horizontalVelocity);

            if (stateMachine.IsInState(CreatureStateMachine.State.InAir))
            {
                horizontalVelocity = Mathf.Lerp(horizontalVelocity, targetHorizontalVelocity, (isAccelerating ? inAirAccelerationSpeed : inAirDecelerationSpeed) * Time.deltaTime);
                verticalVelocity += (UserInputManager.Instance.IsJumpPressed ? flyUpSpeed : (Physics.gravity.y * gravityMultiplierByVerticalVelocity.Evaluate(verticalVelocity))) * Time.deltaTime;
            }
            else if (stateMachine.IsInState(CreatureStateMachine.State.OnSurface))
            {
                horizontalVelocity = 0;
                verticalVelocity = 0;
                return Vector2.zero;
            }

            return new Vector2(horizontalVelocity, verticalVelocity);
        }

        /// <summary>
        /// Reads the user input to calculate the desired movement velocity.
        /// </summary>
        /// <returns>desired movement velocity of the character.</returns>
        private float CalcAndSetTargetHorizontalVelocity()
        {
            if (stateMachine.IsInState(CreatureStateMachine.State.InAir))
            {
                targetHorizontalVelocity = UserInputManager.Instance.PlayerMovementVector * inAirMaxSpeed;
            }
            else
            {
                targetHorizontalVelocity = 0;
            }


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

        private int CountVerticalColliders()
        {
            currentVerticalColliders = currentVerticalColliders.Where(go => go is not null && !go.IsDestroyed()).ToList();
            return currentVerticalColliders.Count();
        }

        private int CountHorizontalColliders()
        {
            currentHorizontalColliders = currentHorizontalColliders.Where(go => go is not null && !go.IsDestroyed()).ToList();
            return currentHorizontalColliders.Count();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.transform.CompareTag("Mineable"))
            {
                if (collision.contacts.Any(contact => contact.normal == Vector3.up || contact.normal == Vector3.down))
                {
                    currentHorizontalColliders.Add(collision.gameObject);
                }
                else if (collision.contacts.Any(contact => contact.normal == Vector3.left || contact.normal == Vector3.right))
                {
                    currentVerticalColliders.Add(collision.gameObject);
                }
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            currentHorizontalColliders.Remove(collision.gameObject);
            currentVerticalColliders.Remove(collision.gameObject);
        }
    }
}
