using UnityEngine;
using Game.Utility.Networking;

public class WorldGenerator : NetworkSingleton<WorldGenerator>
{
    [SerializeField] private Grid grid;
    [SerializeField] private GameObject cell;
    [SerializeField] private Vector2Int gridFillMin;
    [SerializeField] private Vector2Int gridFillMax;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            for (int x = gridFillMin.x; x <= gridFillMax.x; x++)
            {
                for (int y = gridFillMin.y; y <= gridFillMax.y; y++)
                {
                    Instantiate(cell, grid.CellToWorld(new Vector3Int(x, y, 0)), Quaternion.identity, grid.transform);
                }
            }
        }
    }
}
