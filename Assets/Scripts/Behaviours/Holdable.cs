namespace Game.Behaviours
{
    using UnityEngine;

    public class Holdable : MonoBehaviour
    {
        [SerializeField] private int itemID;

        public int ItemID => itemID;

        public void SetItemID(int id)
        {
            itemID = id;
        }
    }
}
