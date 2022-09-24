namespace Game.Managers
{
    using Game.DataAssets;
    using Game.Components;
    using Game.Utility.Networking;

    using UnityEngine;
    using Unity.Netcode;
    using System.Linq;

    public class WorldController : NetworkSingleton<WorldController>
    {
        [SerializeField] private WorldGrid worldGrid;

        public WorldGrid WorldGrid => worldGrid;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            worldGrid.Initialise();
        }

        public bool TryGetTimeToMine(Vector2Int cellPos, out float? secondsToMine)
        {
            if (worldGrid.TryGetCell(cellPos, out int? blockID) && BlockDatabase.Instance.TryGetBlockByID(blockID.Value, out Block block))
            {
                secondsToMine = block.SecondsToMine;
                return true;
            }

            secondsToMine = null;
            return false;
        }

        public void MineCell(Vector2Int gridLoc)
        {
            if (IsServer)
            {
                if (worldGrid.TryGetCell(gridLoc, out int? cell))
                {
                    if (BlockDatabase.Instance.TryGetBlockByID(cell.Value, out Block block))
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
                        Debug.LogError($"Could not find block information with ID [{cell.Value}]");
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
