namespace Game.Behaviours
{
    using UnityEngine;
    using Unity.Netcode;

    public class Pickupable : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> isPickupable = new (true);

        [SerializeField] private int itemID;

        public int ItemID => itemID;

        public bool IsPickupable => isPickupable.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
        }

        public void SetItemID(int id)
        {
            itemID = id;
        }
    }
}
