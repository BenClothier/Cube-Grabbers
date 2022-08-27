using UnityEngine;
using Game.Utility.Networking;

public class WorldGenerator : NetworkSingleton<WorldGenerator>
{
    [SerializeField] private Grid[] grids;
    [SerializeField] private GameObject cell;

    [SerializeField] private Vector2Int gridFillMin;
    [SerializeField] private Vector2Int gridFillMax;
    [SerializeField] private Vector2Int backGridFillMin;
    [SerializeField] private Vector2Int backGridFillMax;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            for (int x = gridFillMin.x; x <= gridFillMax.x; x++)
            {
                for (int y = gridFillMin.y; y <= gridFillMax.y; y++)
                {
                    Instantiate(cell, grids[0].CellToWorld(new Vector3Int(x, y, 0)), Quaternion.identity, grids[0].transform);
                }
            }

            for (int x = backGridFillMin.x; x <= backGridFillMax.x; x++)
            {
                for (int y = backGridFillMin.y; y <= backGridFillMax.y; y++)
                {
                    Instantiate(cell, grids[1].CellToWorld(new Vector3Int(x, y, 0)), Quaternion.identity, grids[1].transform);
                }
            }
        }
    }
}
