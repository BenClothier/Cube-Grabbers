using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Cinemachine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class PlayerController : NetworkBehaviour
{
    /// <summary>
    /// <para>Speed at which the character's local position will be corrected to the server position.</para>
    /// <br>Lower: Less stutter, slower sync</br>
    /// <br>Higher: More stutter, faster sync</br>
    /// </summary>
    private static readonly float MOVE_SYNC_SPEED = 0.15f;

    // network variables
    private readonly NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>();

    [Header("Model")]
    [SerializeField] private GameObject model;

    [Header("Movement")]
    [SerializeField] private float speed;
    [SerializeField] private float accelerationSpeed;
    [SerializeField] private float decelerationSpeed;

    private Rigidbody rb;
    private Vector3 velocity;
    private Vector3 targetVelocity;

    // client input
    private Controls controls;
    private InputAction movement;

    private void Awake()
    {
        controls = new Controls();
    }

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        movement = controls.Default.Move;
        movement.Enable();
    }

    private void OnDisable()
    {
        movement.Disable();
    }

    private void FixedUpdate()
    {
        if (IsClient && IsOwner)
        {
            ClientInput();
        }

        UpdatePosition();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsClient)
        {
            FindObjectOfType<CinemachineTargetGroup>().AddMember(transform, 1, 2);
        }
    }

    /// <summary>
    /// Get the user input from the owner and send RPC to update server information.
    /// </summary>
    private void ClientInput()
    {
        targetVelocity = GetNewTargetVelocity();

        // Tell the server what velocity we want and what the current rotation of the character is
        UpdateClientPositionServerRpc(targetVelocity);
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
    /// Send desired velocity and look-rotation information to the server.
    /// </summary>
    /// <param name="targetVelocity">desired velocity of the character (server-authorised).</param>
    [ServerRpc]
    private void UpdateClientPositionServerRpc(Vector3 targetVelocity)
    {
        this.targetVelocity = targetVelocity;
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
    /// Reads the user input to calculate the desired movement velocity.
    /// </summary>
    /// <returns>desired movement velocity of the character.</returns>
    private Vector3 GetNewTargetVelocity()
    {
        Vector2 moveVector = movement.ReadValue<Vector2>();
        return new Vector3(moveVector.x, 0, moveVector.y) * speed;
    }
}
