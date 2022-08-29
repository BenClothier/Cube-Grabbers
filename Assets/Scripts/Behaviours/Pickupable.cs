namespace Game.Behaviours
{
    using UnityEngine;
    using Unity.Netcode;
    using System.Collections;

    [RequireComponent(typeof(Collider))]
    public class Pickupable : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> isPickupable = new(false);

        [SerializeField] private int itemID;
        [Space]
        [SerializeField] private float timeUntilPickupable = 1f;

        public int ItemID => itemID;

        public bool IsPickupable => isPickupable.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (IsServer)
            {
                isPickupable.Value = false;
                StartCoroutine(WaitToEnablePickup());
            }
        }

        public void SetItemID(int id)
        {
            itemID = id;
        }

        private IEnumerator WaitToEnablePickup()
        {
            yield return new WaitForSeconds(timeUntilPickupable);
            isPickupable.Value = true;
        }
    }
}
