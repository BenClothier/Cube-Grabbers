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
        [SerializeField] private float feetOffset = 1;
        [SerializeField] private float feetWidth = 1;

        [Header("Horizontal Movement")]
        [SerializeField] private float maxSpeed;
        [SerializeField] private float accelerationSpeed;
        [SerializeField] private float decelerationSpeed;
        [Space]
        [SerializeField] private float inAirMaxSpeed;
        [SerializeField] private float inAirAccelerationSpeed;
        [SerializeField] private float inAirDecelerationSpeed;

        [Header("Vertical Movement")]
        [SerializeField] private float minJumpSpeed;
        [SerializeField] private float maxJumpSpeed;
        [SerializeField] private float risingGravityMultiplier = 1;
        [SerializeField] private float fallingGravityMultiplier = 1.5f;

        [Header("LookRotation")]
        [SerializeField] private float lookSpeed;

        private List<GameObject> currentGroundCollisions = new List<GameObject>();

        private Rigidbody rb;

        private Quaternion targetLookRotation;
        private float targetHorizontalVelocity;

        private float horizontalVelocity;
        private float verticalVelocity;

        private bool IsOnGround => CountGroundColliders() > 0;

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
            UserInputManager.Instance.OnJumpPressed += OnJumpPressed;
            UserInputManager.Instance.OnJumpReleased += OnJumpReleased;
        }

        private void OnDisable()
        {
            rb = GetComponent<Rigidbody>();
            UserInputManager.Instance.OnJumpPressed -= OnJumpPressed;
            UserInputManager.Instance.OnJumpReleased -= OnJumpReleased;
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
            switch (CurrentState)
            {
                case State.OnGround:
                    if (!IsOnGround)
                    {
                        MoveState(Command.StartFalling);
                    }
                    break;

                case State.ChargingJump:
                    break;

                case State.Rising:
                    if (verticalVelocity <= 0)
                    {
                        MoveState(Command.StartFalling);
                    }
                    break;

                case State.Falling:
                    if (IsOnGround)
                    {
                        MoveState(Command.HitGround);
                    }
                    break;
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
                rb.MovePosition(rb.position + movement);
                networkPosition.Value = rb.position;
            }
            else
            {
                // Calculate new position based on velocity
                Vector3 newLocalPosition = rb.position + movement;
                Vector3 newServerPosition = networkPosition.Value + movement;

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
        /// <returns>New velocity, adjusted for frame length.</returns>
        private float CalcAndSetVerticalVelocity()
        {
            verticalVelocity = CurrentState switch
            {
                State.Rising => verticalVelocity + Physics.gravity.y * risingGravityMultiplier * Time.deltaTime,
                State.Falling => verticalVelocity + Physics.gravity.y * fallingGravityMultiplier * Time.deltaTime,
                _ => 0,
            };

            return verticalVelocity;
        }

        /// <summary>
        /// Accelerates/decelerates the horizontal velocity to the target velocity.
        /// </summary>
        /// <returns>New velocity, adjusted for frame length.</returns>
        private float CalcAndSetHorizontalVelocity()
        {
            bool isAccelerating = Mathf.Abs(targetHorizontalVelocity) >= Mathf.Abs(horizontalVelocity);

            horizontalVelocity = CurrentState switch
            {
                State.OnGround => Mathf.Lerp(horizontalVelocity, targetHorizontalVelocity, (isAccelerating ? accelerationSpeed : decelerationSpeed) * Time.deltaTime),
                State.Rising or State.Falling => Mathf.Lerp(horizontalVelocity, targetHorizontalVelocity, (isAccelerating ? inAirAccelerationSpeed : inAirDecelerationSpeed) * Time.deltaTime),
                _ => 0,
            };

            return horizontalVelocity;
        }

        /// <summary>
        /// Reads the user input to calculate the desired movement velocity.
        /// </summary>
        /// <returns>desired movement velocity of the character.</returns>
        private float CalcAndSetTargetHorizontalVelocity()
        {
            targetHorizontalVelocity = UserInputManager.Instance.PlayerMovementVector * maxSpeed;
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
            if (IsInState(State.OnGround))
            {
                MoveState(Command.StartChargingJump);

                // start doing charging effects
            }
        }

        private void OnJumpReleased(InputAction.CallbackContext cxt)
        {
            if (IsInState(State.ChargingJump))
            {
                MoveState(Command.StartRising);


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

        #region State Machine

#if UNITY_EDITOR
        [Header("State Machine")]
        [SerializeField] private bool debugMode;
#endif

        public State CurrentState { get; private set; }

        private static readonly Dictionary<StateTransition, State> transitions = new Dictionary<StateTransition, State>()
        {
            // Pickup/Throw Behaviour
            { new StateTransition(State.OnGround, Command.StartChargingJump), State.ChargingJump },
            { new StateTransition(State.OnGround, Command.StartFalling), State.Falling },
            { new StateTransition(State.ChargingJump, Command.StartRising), State.Rising },
            { new StateTransition(State.Rising, Command.StartFalling), State.Falling },
            { new StateTransition(State.Falling, Command.HitGround), State.OnGround },
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

#if UNITY_EDITOR
            if (debugMode)
            {
                Debug.Log($"New State: {CurrentState}");
            }
#endif

            return CurrentState;
        }

        public bool IsInState(State state) => CurrentState.Equals(state);

        public enum State
        {
            OnGround,
            ChargingJump,
            Rising,
            Falling,
        }

        public enum Command
        {
            StartChargingJump,
            StartRising,
            StartFalling,
            HitGround,
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

//#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(transform.position.x - feetWidth / 2, transform.position.y - feetOffset, 0), new Vector3(transform.position.x + feetWidth / 2, transform.position.y - feetOffset, 0));
        }
//#endif
    }
}
