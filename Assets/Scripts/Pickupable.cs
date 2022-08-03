using UnityEngine;
using Unity.Netcode;

[RequireComponent(typeof(Collider))]
public class Pickupable : NetworkBehaviour
{
    [SerializeField] private Holdable holdablePrefab;

    public GameObject HoldablePrefab => holdablePrefab.gameObject;
}
