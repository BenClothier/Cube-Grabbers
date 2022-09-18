namespace Game.Behaviours
{
    using UnityEngine;
    using Unity.Netcode;

    public class Pickupable : NetworkBehaviour
    {
        private readonly NetworkVariable<bool> isPickupable = new (true);

        [SerializeField] private int itemID;

        [Header("Outline Shader Modification")]
        [SerializeField] private MultiMeshRenderer meshRenderer;
        [SerializeField] private int materialIndex;
        [SerializeField] private MaterialColourModification materialDefaultModification;
        [SerializeField] private MaterialColourModification materialHighlightModification;

        public int ItemID => itemID;

        public bool IsPickupable => isPickupable.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            meshRenderer.ModifyMaterial(materialIndex, materialDefaultModification.ModificationAction);
        }

        public void SetItemID(int id)
        {
            itemID = id;
        }

        private void OnMouseEnter()
        {
            Debug.Log("HELLO");
            meshRenderer.ModifyMaterial(materialIndex, materialHighlightModification.ModificationAction);
        }

        private void OnMouseExit()
        {
            meshRenderer.ModifyMaterial(materialIndex, materialDefaultModification.ModificationAction);
        }
    }
}
