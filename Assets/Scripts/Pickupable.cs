using UnityEngine;
using Unity.Netcode;
using System.Collections;

[RequireComponent(typeof(Collider))]
public class Pickupable : NetworkBehaviour
{
    private readonly NetworkVariable<bool> isPickupable = new(false);

    [SerializeField] private Holdable holdablePrefab;
    [SerializeField] private float timeUntilPickupable = 1f;

    public bool IsPickupable => isPickupable.Value;

    public GameObject HoldablePrefab => holdablePrefab.gameObject;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            isPickupable.Value = false;
            StartCoroutine(WaitToEnablePickup());
        }
    }

    private IEnumerator WaitToEnablePickup()
    {
        yield return new WaitForSeconds(timeUntilPickupable);
        isPickupable.Value = true;
    }
}
