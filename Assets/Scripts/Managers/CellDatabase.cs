namespace Game.Managers
{
    using Game.Utility;
    using Game.DataAssets;

    using System.Collections.Generic;
    using UnityEngine;

    public class CellDatabase : Singleton<CellDatabase>
    {
        private Dictionary<int, Cell> cellDictionary = new ();

        public bool GetCellByID(int id, out Cell cell)
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
            Cell[] cellList = Resources.LoadAll<Cell>("Cells");

            foreach (Cell cell in cellList)
            {
                cellDictionary.Add(cell.ID, cell);
            }
        }
    }
}
