using UnityEngine;
using Unity.Netcode;
using Game.Utility.Networking;
using Game.Utility.Math;
using UnityEngine.InputSystem;

public class PickupThrowBehaviour : NetworkBehaviour
{
    [SerializeField] private Transform objectHoldingPos;

    public bool IsHoldingSomething { get; private set; }

    public Vector3 HoldingPosition => objectHoldingPos.position;

    #region Pickup Behaviour

    [ServerRpc]
    public void RequestPickupServerRpc(ulong pickupNetObjID)
    {
        if (NetworkManager.SpawnManager.SpawnedObjects[pickupNetObjID].gameObject.TryGetComponent(out Pickupable pickupable))
        {
            Instantiate(pickupable.HoldablePrefab, objectHoldingPos);
            GrantPickupClientRpc(pickupNetObjID);
            pickupable.gameObject.SetActive(false);
            NetworkingTools.DespawnAfterSeconds(pickupable.NetworkObject, 2);
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
                Instantiate(pickupable.HoldablePrefab, objectHoldingPos);
                pickupable.gameObject.SetActive(false);
                IsHoldingSomething = true;
            }
            else
            {
                Debug.LogError("Something went wrong when trying to find the pickupable's network object.");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsClient && IsOwner && other.gameObject.CompareTag("Pickupable"))
        {
            Pickupable pickupable = other.gameObject.GetComponent<Pickupable>();
            RequestPickupServerRpc(pickupable.NetworkObjectId);
        }
    }

    #endregion

    #region Throw Behaviour

    [SerializeField] Transform projectilePrefab;
    Vector3 throwOrigin = new Vector3(0, 0, -10);
    Vector3 throwTarget = new Vector3(0, 0, 0);
    private bool thrown = false;

    private float throwSpeed;
    private float maxRange;

    public float ThrowSpeed
    {
        get
        {
            return throwSpeed;
        }

        set
        {
            throwSpeed = value;
            maxRange = Ballistics.CalculateMaxRange(value);
        }
    }

    private void Start()
    {
        ThrowSpeed = 12;
    }

    private void FixedUpdate()
    {
        if (CalculateMouseWorldIntersect(Mouse.current.position.ReadValue(), out RaycastHit mouseWorldHitInfo))
        {
            throwOrigin = transform.position;
            throwTarget = mouseWorldHitInfo.point;
            GenerateTrajectoryPath(throwOrigin, throwTarget, out Quaternion launchDir);

            if (!thrown && Mouse.current.leftButton.isPressed)
            {
                Transform projectile = Instantiate(projectilePrefab, throwOrigin, launchDir);
                Debug.Log(launchDir);
                projectile.GetComponent<Rigidbody>().velocity = projectile.transform.forward * throwSpeed;
                thrown = true;
            }
            else if (thrown && Mouse.current.rightButton.isPressed)
            {
                thrown = false;
            }
        }
    }

    private void GenerateTrajectoryPath(Vector3 launchOrigin, Vector3 launchTarget, out Quaternion launchDir)
    {
        Ballistics.CalculateTrajectory(launchOrigin, launchTarget, throwSpeed, out float angle);
        launchDir = TrajectoryToLookDir(launchOrigin, launchTarget, angle);

        Transform GO = Instantiate(new GameObject("Name"), Vector3.zero, launchDir).transform;

        Ballistics.LaunchPathInfo pathInfo = Ballistics.GenerateLaunchPathInfo(launchOrigin, GO.forward, throwSpeed);

        Debug.DrawRay(pathInfo.highestPoint, Vector3.down, Color.red);

        if (pathInfo.launchPath.Length > 0)
        {
            foreach (var point in pathInfo.launchPath)
            {
                Debug.DrawRay(point, Vector3.down * .25f, Color.blue);
            }
        }

        if (pathInfo.hit.HasValue)
        {
            Debug.DrawRay(pathInfo.hit.Value.point, pathInfo.hit.Value.normal, Color.red);
        }
    }

    /// <summary>
    /// Calculates the position in the world where the mouse is pointing at.
    /// </summary>
    /// <param name="mousePos">the screen position of the mouse.</param>
    /// <returns>The point of intersection in the world by a ray from the camera through the mouse-screen position.</returns>
    private bool CalculateMouseWorldIntersect(Vector2 mousePos, out RaycastHit hitInfo)
    {
        Ray pointerRay = Camera.main.ScreenPointToRay(mousePos);
        if (Physics.Raycast(pointerRay, out hitInfo, 200, LayerMask.GetMask("Default")))
        {
            return true;
        }

        return false;
    }

    public Quaternion TrajectoryToLookDir(Vector3 start, Vector3 end, float angle)
    {
        Vector3 wantedRotationVector = Quaternion.LookRotation(end - start).eulerAngles;
        wantedRotationVector.x = angle;
        return Quaternion.Euler(wantedRotationVector);
    }

    #endregion
}
