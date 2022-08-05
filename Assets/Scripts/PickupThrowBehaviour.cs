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

    Vector3? position;

    private void Update()
    { 
        if (CalculateMouseWorldIntersect(Mouse.current.position.ReadValue(), out RaycastHit mouseWorldHitInfo))
        {
            position = mouseWorldHitInfo.point;
            Vector3 displacementVector = mouseWorldHitInfo.point - transform.position;
            Debug.DrawLine(transform.position, mouseWorldHitInfo.point, Color.red);
            Debug.DrawRay(transform.position, CalculateThrowDir(displacementVector, 6), Color.blue);
        }
        else
        {
            position = null;
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

    private Vector3 CalculateThrowDir2(Vector3 displacementVector, float throwSpeed)
    {
        Vector3 hrzDisp = new Vector3(displacementVector.x, 0, displacementVector.z);
        Vector3 vrtDisp = new Vector3(0, -displacementVector.y, 0);
        float angleOfThrow = Mathf.Rad2Deg * Mathf.Asin((displacementVector.magnitude * -Physics.gravity.y) / Mathf.Pow(throwSpeed, 2)) / 2;
        return Vector3.Lerp(Vector3.up, displacementVector.normalized, angleOfThrow / 90);
    }

    private Vector3 CalculateThrowDir(Vector3 displacementVector, float throwSpeed)
    {
        float hrzDisp = new Vector3(displacementVector.x, 0, displacementVector.z).magnitude;
        float vrtDisp = -displacementVector.y;

        float a = (-Physics.gravity.y * Mathf.Pow(hrzDisp, 2)) / Mathf.Pow(throwSpeed, 2);
        float b = (a - vrtDisp) / Mathf.Sqrt(Mathf.Pow(hrzDisp, 2) + Mathf.Pow(vrtDisp, 2));
        float angleOfThrow = Mathf.Rad2Deg * 0.5f * (Mathf.Acos(b) + Mathf.Atan(hrzDisp / vrtDisp));

        Debug.Log($"H: {vrtDisp}, R: {hrzDisp}, A: {angleOfThrow}");

        if (angleOfThrow >= 0)
        {
            return Vector3.Lerp(Vector3.up, displacementVector.normalized, (90 - angleOfThrow) / 90);
        }
        else
        {
            return Vector3.Lerp(Vector3.up, displacementVector.normalized, (-angleOfThrow) / 90);
        }
    }

    private void OnDrawGizmos()
    {
        if (position.HasValue)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(position.Value, 0.1f);
        }
    }

    #endregion
}
