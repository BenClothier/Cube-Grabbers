namespace Game.Behaviours
{
    using UnityEngine;

    public class Mineable : MonoBehaviour
    {
        [SerializeField] private int id;

        public int ID => id;

        public void SetCellID(int id)
        {
            this.id = id;
        }
    }
}
