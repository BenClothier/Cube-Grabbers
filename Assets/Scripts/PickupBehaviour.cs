using UnityEngine;
using Unity.Netcode;
using GameCore.Utility.Networking;

public class PickupBehaviour : NetworkBehaviour
{
    [SerializeField] private Transform objectHoldingPos;

    public Vector3 HoldingPosition => objectHoldingPos.position;

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
}
