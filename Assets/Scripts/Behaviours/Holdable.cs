namespace Game.Behaviours
{
    using UnityEngine;

    public class Holdable : MonoBehaviour
    {
        [SerializeField] private Pickupable pickupPrefab;
        public GameObject PickupPrefab => pickupPrefab.gameObject;
    }
}
