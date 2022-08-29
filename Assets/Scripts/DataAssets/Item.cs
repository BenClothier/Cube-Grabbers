namespace Game.DataAssets
{
    using Game.Behaviours;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Item", menuName = "Game/Item")]
    public class Item : ScriptableObject
    {
        [SerializeField] private int id;
        [SerializeField] private Pickupable pickupPrefab;
        [SerializeField] private Holdable holdablePrefab;

        public int ID => id;

        public GameObject PickupPrefab => pickupPrefab.gameObject;

        public GameObject HoldablePrefab => holdablePrefab.gameObject;

        private void OnValidate()
        {
            pickupPrefab.SetItemID(id);
            holdablePrefab.SetItemID(id);
        }
    }
}
