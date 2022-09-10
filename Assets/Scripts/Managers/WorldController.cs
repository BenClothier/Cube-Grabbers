namespace Game.Managers
{
    using Game.DataAssets;
    using Game.Behaviours;
    using Game.Components;
    using Game.Utility.Networking;

    using UnityEngine;
    using Unity.Netcode;

    public class WorldController : NetworkSingleton<WorldController>
    {
        [SerializeField] private WorldGrid worldGrid;

        public WorldGrid WorldGrid => worldGrid;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            worldGrid.Initialise();
        }

        public void MineCell(Vector2Int gridLoc)
        {
            if (IsServer)
            {
                if (worldGrid.TryGetCell(gridLoc, out WorldGrid.WorldCell cell))
                {
                    if (BlockDatabase.Instance.GetCellByID(cell.BlockID, out Block block))
                    {
                        int[] itemsToSpawn = block.GenerateDrops();

                        foreach (int itemID in itemsToSpawn)
                        {
                            if (ItemDatabase.Instance.GetItemByID(itemID, out Item item))
                            {
                                Transform projectile = Instantiate(item.PickupPrefab, worldGrid.GetWorldPosFromGridLoc(gridLoc), Quaternion.identity).transform;
                                projectile.GetComponent<NetworkObject>().Spawn();
                            }
                        }

                        worldGrid.RemoveCellAndUpdate(gridLoc);
                        MineCellClientRPC(gridLoc);
                    }
                    else
                    {
                        Debug.LogError($"Could not find block information with ID [{cell.BlockID}]");
                    }
                }
                else
                {
                    Debug.LogError($"No cell was found at grid location [{gridLoc}]");
                }
            }
            else
            {
                Debug.LogError("MineCell should not be called by clients - it is server only.");
            }
        }

        [ClientRpc]
        private void MineCellClientRPC(Vector2Int gridLoc)
        {
            if (!IsServer)
            {
                if (worldGrid.CellIsPresent(gridLoc))
                {
                    worldGrid.RemoveCellAndUpdate(gridLoc);
                }
                else
                {
                    Debug.LogError($"No cell was found at grid location [{gridLoc}]");
                    worldGrid.UpdateDirtyCells();
                }
            }
        }
    }
}
