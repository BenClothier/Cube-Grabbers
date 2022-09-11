namespace Game.Components
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using UnityEngine;
    using Unity.VisualScripting;
    using Game.Managers;
    using Game.DataAssets;

    [RequireComponent(typeof(Grid))]
    public class WorldGrid : MonoBehaviour
    {
        private static readonly Dictionary<NeighbourDir, Vector2Int> DIRECTION_VECTORS = new Dictionary<NeighbourDir, Vector2Int>
        {
            { NeighbourDir.N, Vector2Int.up },
            { NeighbourDir.NE, Vector2Int.up + Vector2Int.right },
            { NeighbourDir.E, Vector2Int.right },
            { NeighbourDir.SE, Vector2Int.down + Vector2Int.right},
            { NeighbourDir.S, Vector2Int.down },
            { NeighbourDir.SW, Vector2Int.down + Vector2Int.left},
            { NeighbourDir.W, Vector2Int.left },
            { NeighbourDir.NW, Vector2Int.up + Vector2Int.left},
        };

        private static readonly int DIRECTION_COUNT = Enum.GetValues(typeof(NeighbourDir)).Length;

        [SerializeField] private Vector2Int gridFillMin;
        [SerializeField] private Vector2Int gridFillMax;

        private Dictionary<Vector2Int, WorldCell> worldCells = new ();
        private HashSet<Vector2Int> dirtyCells = new ();

        [Flags]
        public enum NeighbourDir
        {
            None = 0b00000000,

            N   = 0b00000001,
            NE  = 0b00000010,
            E   = 0b00000100,
            SE  = 0b00001000,
            S   = 0b00010000,
            SW  = 0b00100000,
            W   = 0b01000000,
            NW  = 0b10000000,

            All = 0b11111111,
        }

        public Grid Grid { get; private set; }

        /// <summary>
        /// Convert from world position to grid location.
        /// </summary>
        /// <param name="worldPos">The world position.</param>
        /// <returns>The grid location closest to the world position.</returns>
        public Vector2Int GetGridLocFromWorldPos(Vector2 worldPos)
        {
            return (Vector2Int)Grid.WorldToCell(worldPos);
        }

        /// <summary>
        /// Convert from cell location to world position.
        /// </summary>
        /// <param name="gridLoc">The location of the grid cell.</param>
        /// <returns>The world position of the grid location.</returns>
        public Vector2 GetWorldPosFromGridLoc(Vector2Int gridLoc)
        {
            return (Vector2)Grid.CellToWorld((Vector3Int)gridLoc);
        }

        /// <summary>
        /// Adds a cell to the grid without updating any meshes.
        /// </summary>
        /// <param name="loc">Location.</param>
        /// <param name="id">Block ID.</param>
        public void AddCell(Vector2Int loc, int id)
        {
            worldCells.Add(loc, new WorldCell(id));

            // Set this and all neighbouring cells as 'dirty'
            dirtyCells.AddRange(DIRECTION_VECTORS.Values.Select(dir => dir + loc).Append(loc));
        }

        /// <summary>
        /// Adds a cell to the grid then updates the meshes of all dirty cells (including this and neighbouring cells).
        /// </summary>
        /// <param name="loc">Location.</param>
        /// <param name="id">Block ID.</param>
        public void AddCellAndUpdate(Vector2Int loc, int id)
        {
            AddCell(loc, id);
            UpdateDirtyCells();
        }

        /// <summary>
        /// Removes a cell from the grid without updating any meshes.
        /// </summary>
        /// <param name="loc">Location.</param>
        public void RemoveCell(Vector2Int loc)
        {
            worldCells.Remove(loc);

            // Set all neighbouring cells as 'dirty'
            dirtyCells.AddRange(DIRECTION_VECTORS.Values.Select(dir => dir + loc));
        }

        /// <summary>
        /// Removes a cell from the grid then updates the meshes of all dirty cells (including neighbouring cells).
        /// </summary>
        /// <param name="loc">Location.</param>
        public void RemoveCellAndUpdate(Vector2Int loc)
        {
            RemoveCell(loc);
            UpdateDirtyCells();
        }

        /// <summary>
        /// Gets the cell at the given grid location, if one is present.
        /// </summary>
        /// <param name="loc">Location.</param>
        /// <returns>The cell if one exists, otherwise null.</returns>
        public WorldCell? GetCell(Vector2Int loc)
        {
            return worldCells.TryGetValue(loc, out WorldCell cell) ? cell : null;
        }

        /// <summary>
        /// Tries to get the cell at the given grid location, if one is present.
        /// </summary>
        /// <param name="loc">Location.</param>
        /// <returns>True if a cell was found.</returns>
        public bool TryGetCell(Vector2Int loc, out WorldCell cell)
        {
            return worldCells.TryGetValue(loc, out cell);
        }

        /// <summary>
        /// Gets a value indicating whether there is a cell at this grid location.
        /// </summary>
        /// <param name="loc">Location.</param>
        /// <returns>True if there is a cell at the given grid location.</returns>
        public bool CellIsPresent(Vector2Int loc)
        {
            return worldCells.TryGetValue(loc, out _);
        }

        /// <summary>
        /// Update any cells in the grid marked as 'dirty'.
        /// </summary>
        public void UpdateDirtyCells()
        {
            foreach (Vector2Int loc in dirtyCells)
            {
                UpdateCell(loc);
            }

            dirtyCells.Clear();
        }

        private void Awake()
        {
            Grid = GetComponent<Grid>();
        }

        private void UpdateCell(Vector2Int loc)
        {
            if (TryGetCell(loc, out WorldCell cell))
            {
                if (BlockDatabase.Instance.TryGetBlockByID(cell.BlockID, out Block block))
                {
                    if (block.TryGetMeshes(GetNeighbourPattern(loc), out Mesh[] meshes))
                    {
                        GameObject cellGO = Instantiate(new GameObject($"Cell[{loc}]"), GetWorldPosFromGridLoc(loc), Quaternion.identity, transform);

                        for (int i = 0; i < meshes.Length; i++)
                        {
                            Mesh mesh = meshes[i];
                            if (mesh != null)
                            {
                                MeshFilter mf = Instantiate(new GameObject($"face"), cellGO.transform).AddComponent<MeshFilter>();

                                mf.mesh = mesh;
                                mf.transform.RotateAround(mf.transform.position, Vector3.forward, Block.DIRECTION_ANGLES[i]);

                                MeshRenderer mr = mf.AddComponent<MeshRenderer>();

                                mr.material = block.Material;
                            }
                        }
                    }
                }
                else
                {
                    Debug.LogError($"Now block was found with ID [{cell.BlockID}]");
                }
            }
        }

        private byte GetNeighbourPattern(Vector2Int loc)
        {
            byte pattern = 0;

            for (NeighbourDir dir = NeighbourDir.N; dir <= NeighbourDir.NW; dir = (NeighbourDir)((byte)dir << 1))
            {
                if (CellIsPresent(loc + DIRECTION_VECTORS[dir]))
                {
                    pattern |= (byte)dir;
                }
            }

            Debug.Log($"loc[{loc}] has neighbours: {pattern}");
            return pattern;
        }

        private WorldCell?[] GetNeighbours(Vector2Int loc)
        {
            return DIRECTION_VECTORS.Values
                .Select(dir => GetCell(dir + loc))
                .ToArray();
        }

        public void Initialise()
        {
            for (int x = gridFillMin.x; x <= gridFillMax.x; x++)
            {
                for (int y = gridFillMin.y; y <= gridFillMax.y; y++)
                {
                    AddCell(new Vector2Int(x, y), 0);
                }
            }

            UpdateDirtyCells();
        }

        public struct WorldCell
        {
            public int BlockID;
            public Mesh[] Meshes;

            public WorldCell(int id)
            {
                BlockID = id;
                Meshes = null;
            }
        }
    }
}
