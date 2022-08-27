namespace Game.Behaviours.Player
{
    using Game.Managers;

    using UnityEngine;
    using Unity.Netcode;
    using Cinemachine;

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

        [Header("Movement")]
        [SerializeField] private float speed;
        [SerializeField] private float accelerationSpeed;
        [SerializeField] private float decelerationSpeed;

        [Header("LookRotation")]
        [SerializeField] private float lookSpeed;

        private Rigidbody rb;
        private Vector3 velocity;
        private Vector3 targetVelocity;
        private Quaternion targetLookRotation;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsClient)
            {
                FindObjectOfType<CinemachineTargetGroup>().AddMember(transform, 1, 2);
            }
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            if (IsClient)
            {
                FindObjectOfType<CinemachineTargetGroup>().RemoveMember(transform);
            }
        }

        private void OnEnable()
        {
            rb = GetComponent<Rigidbody>();
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
        /// Get the user input from the owner and send RPC to update server information.
        /// </summary>
        private void ClientInput()
        {
            targetVelocity = GetNewTargetVelocity();
            targetLookRotation = MouseLook();

            // Tell the server what velocity we want and what the current rotation of the character is
            UpdateClientPositionRotationServerRpc(targetVelocity, model.transform.rotation);
        }

        /// <summary>
        /// Updates the position of the character in the world. This has server authority.
        /// </summary>
        private void UpdatePosition()
        {
            if (IsServer)
            {
                // Simply move the character and update the networked value for other clients to see
                rb.MovePosition(rb.position + GetLerpedVelocity());
                networkPosition.Value = rb.position;
            }
            else
            {
                Vector3 moveVector = GetLerpedVelocity();

                // Calculate new position based on velocity
                Vector3 newLocalPosition = rb.position + moveVector;
                Vector3 newServerPosition = networkPosition.Value + moveVector;

                // Gradually corrects to the server's position. If this is done too quickly, the user feels stutter
                rb.MovePosition(Vector3.Lerp(newLocalPosition, newServerPosition, MOVE_SYNC_SPEED));
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
        /// Send desired velocity and look-rotation information to the server.
        /// </summary>
        /// <param name="targetVelocity">desired velocity of the character (server-authorised).</param>
        /// <param name="lookRotation">look-rotation of the character (client-authorised).</param>
        [ServerRpc]
        private void UpdateClientPositionRotationServerRpc(Vector3 targetVelocity, Quaternion lookRotation)
        {
            this.targetVelocity = targetVelocity;
            networkLookRotation.Value = lookRotation;
        }

        /// <summary>
        /// Accelerates/decelerates the velocity to the target velocity.
        /// </summary>
        /// <returns>new velocity, adjusted for frame length.</returns>
        private Vector3 GetLerpedVelocity()
        {
            if (targetVelocity.magnitude >= velocity.magnitude)
            {
                velocity = Vector3.Lerp(velocity, targetVelocity, accelerationSpeed * Time.deltaTime);
            }
            else
            {
                velocity = Vector3.Lerp(velocity, targetVelocity, decelerationSpeed * Time.deltaTime);
            }

            return velocity * Time.deltaTime;
        }

        /// <summary>
        /// Smoothly animates the look-direction of the character to the desired direction.
        /// </summary>
        /// <returns>look-rotation at this time through the animation.</returns>
        private Quaternion LerpLookVector()
        {
            return Quaternion.Lerp(model.transform.rotation, targetLookRotation, lookSpeed * Time.deltaTime);
        }

        #region User Input

        /// <summary>
        /// Reads the user input to calculate the desired movement velocity.
        /// </summary>
        /// <returns>desired movement velocity of the character.</returns>
        private Vector3 GetNewTargetVelocity()
        {
            return UserInputManager.Instance.PlayerMovementVector * speed;
        }

        /// <summary>
        /// Reads the user input of the mouse to calculate the desired look-rotation.
        /// </summary>
        /// <returns>desired look-rotation of the character.</returns>
        private Quaternion MouseLook()
        {
            return Quaternion.LookRotation(UserInputManager.Instance.MousePos.x < Screen.width / 2 ? Vector3.left : Vector3.right, Vector3.up);
        }

        #endregion
    }
}
