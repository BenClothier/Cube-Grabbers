namespace Game.DataAssets
{
    using Game.Behaviours;

    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Cell", menuName = "Game/Cell")]
    public class Cell : ScriptableObject
    {
        [SerializeField] private int id;
        [SerializeField] private ItemDropChance[] itemDropChances;

        [Header("Prefabs")]
        [SerializeField] private GameObject cellPrefab;
        [SerializeField] private Mineable mineableCellPrefab;

        public int ID => id;

        public GameObject CellPrefab => cellPrefab;

        public GameObject MineableCellPrefab => mineableCellPrefab.gameObject;

        public int[] GenerateDrops()
        {
            List<int> itemDropList = new ();

            foreach (ItemDropChance itemDropChance in itemDropChances)
            {
                if (UnityEngine.Random.value <= itemDropChance.dropChance)
                {
                    itemDropList.Add(itemDropChance.ItemID);
                }
            }

            return itemDropList.ToArray();
        }

        [Serializable]
        public class ItemDropChance
        {
            [SerializeField] public int ItemID;
            [Range(0, 1)][SerializeField] public float dropChance;
        }
    }
}
