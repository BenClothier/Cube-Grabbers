namespace Game.Managers
{
    using Game.Utility;
    using Game.DataAssets;

    using System.Collections.Generic;
    using UnityEngine;

    public class BlockDatabase : Singleton<BlockDatabase>
    {
        private Dictionary<int, Block> cellDictionary = new ();

        public bool GetCellByID(int id, out Block cell)
        {
            if (cellDictionary.TryGetValue(id, out cell))
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
                cellDictionary.Add(cell.ID, cell);
            }
        }
    }
}
