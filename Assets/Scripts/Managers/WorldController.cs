namespace Game.Managers
{
    using Game.DataAssets;
    using Game.Behaviours;
    using Game.Utility.Networking;

    using System.Collections.Generic;
    using UnityEngine;
    using Unity.Netcode;

    public class WorldController : NetworkSingleton<WorldController>
    {
        [SerializeField] private Grid[] grids;

        [SerializeField] private Vector2Int gridFillMin;
        [SerializeField] private Vector2Int gridFillMax;
        [SerializeField] private Vector2Int backGridFillMin;
        [SerializeField] private Vector2Int backGridFillMax;

        private Dictionary<Vector2Int, Mineable> worldCells = new ();

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            GenerateWorld();
        }

        public Vector2Int GetCellPosFromWorldPos(Vector2 worldPos)
        {
            return (Vector2Int)grids[0].WorldToCell(worldPos);
        }

        public Vector2 GetWorldPosFromCellPos(Vector2Int cellPos)
        {
            return (Vector2)grids[0].CellToWorld((Vector3Int)cellPos);
        }

        public void MineCell(Vector2Int cellPos)
        {
            if (IsServer)
            {
                if (worldCells.TryGetValue(cellPos, out Mineable cellToMine))
                {

                    if (CellDatabase.Instance.GetCellByID(cellToMine.ID, out Cell cell))
                    {
                        int[] itemsToSpawn = cell.GenerateDrops();

                        foreach (int itemID in itemsToSpawn)
                        {
                            if (ItemDatabase.Instance.GetItemByID(itemID, out Item item))
                            {
                                Transform projectile = Instantiate(item.PickupPrefab, GetWorldPosFromCellPos(cellPos), Quaternion.identity).transform;
                                projectile.GetComponent<NetworkObject>().Spawn();
                            }
                        }

                        Destroy(cellToMine.gameObject);
                        MineCellClientRPC(cellPos);
                    }
                    else
                    {
                        Debug.LogError($"Could not find cell information with ID [{cellToMine.ID}]");
                    }
                }
                else
                {
                    Debug.LogError($"No cell was found at world position [{cellPos}]");
                }
            }
            else
            {
                Debug.LogError("MineCell should not be called by clients - it is server only.");
            }
        }

        [ClientRpc]
        private void MineCellClientRPC(Vector2Int cellPos)
        {
            if (!IsServer)
            {
                Mineable cellToMine = worldCells[cellPos];
                Destroy(cellToMine.gameObject);
            }
        }

        private void GenerateWorld()
        {
            if (!CellDatabase.Instance.GetCellByID(0, out Cell cell))
            {
                return;
            }

            for (int x = gridFillMin.x; x <= gridFillMax.x; x++)
            {
                for (int y = gridFillMin.y; y <= gridFillMax.y; y++)
                {
                    Mineable newCell = Instantiate(cell.MineableCellPrefab, grids[0].CellToWorld(new Vector3Int(x, y, 0)), Quaternion.identity, grids[0].transform).GetComponent<Mineable>();
                    worldCells.Add(new Vector2Int(x, y), newCell);
                }
            }

            for (int x = backGridFillMin.x; x <= backGridFillMax.x; x++)
            {
                for (int y = backGridFillMin.y; y <= backGridFillMax.y; y++)
                {
                    Instantiate(cell.CellPrefab, grids[1].CellToWorld(new Vector3Int(x, y, 0)), Quaternion.identity, grids[1].transform);
                }
            }
        }
    }
}
