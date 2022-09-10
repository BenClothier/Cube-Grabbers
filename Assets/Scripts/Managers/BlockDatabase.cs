namespace Game.Managers
{
    using Game.Utility;
    using Game.DataAssets;

    using System.Collections.Generic;
    using UnityEngine;

    public class BlockDatabase : Singleton<BlockDatabase>
    {
        private Dictionary<int, Block> blockDictionary = new ();

        public bool TryGetBlockByID(int id, out Block block)
        {
            if (blockDictionary.TryGetValue(id, out block))
            {
                return true;
            }
            else
            {
                Debug.LogWarning($"Could not find cell with ID [{id}]");
                return false;
            }
        }

        private void Awake()
        {
            InitialiseDatabase();
        }

        private void InitialiseDatabase()
        {
            Block[] cellList = Resources.LoadAll<Block>("Cells");

            foreach (Block cell in cellList)
            {
                blockDictionary.Add(cell.ID, cell);
            }
        }
    }
}
