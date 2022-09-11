namespace Game.Components
{
    using Game.Managers;
    using Game.DataAssets;

    using System;
    using System.Linq;
    using UnityEngine;
    using Unity.VisualScripting;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

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

        [SerializeField] private Vector2Int gridFillMin;
        [SerializeField] private Vector2Int gridFillMax;

        private Dictionary<Vector2Int, int?> worldCells = new ();

        public delegate void OnPushGridUpdateAction(Vector2Int[] modifiedCells);

        public event OnPushGridUpdateAction OnPushGridUpdate;

        /// <summary>
        /// Cell positions that have been changed in the 'worldCells' dictionary but have not had their meshes updated in the 'worldBlocks' dictionary.
        /// </summary>
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
            worldCells.Add(loc, id);

            // Set this and all neighbouring cells as 'dirty'
            dirtyCells.AddRange(GetNeighbourLocations(loc, true));
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
            if (worldCells.TryGetValue(loc, out int? id))
            {
                worldCells.Remove(loc);

                // Set this and all neighbouring cells as 'dirty'
                dirtyCells.AddRange(GetNeighbourLocations(loc, true));
            }
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
        /// Tries to get the cell at the given grid location, if one is present.
        /// </summary>
        /// <param name="loc">Location.</param>
        /// <returns>True if a cell was found.</returns>
        public bool TryGetCell(Vector2Int loc, out int? cell)
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
            OnPushGridUpdate?.Invoke(dirtyCells.ToArray());
            dirtyCells.Clear();
        }

        
        public Vector2Int[] GetNeighbourLocations(Vector2Int loc, bool includeThisLocation = false, bool clampToWorldBounds = true)
        {
            IEnumerable<Vector2Int> neighbourLcoations;

            if (includeThisLocation)
            {
                neighbourLcoations = DIRECTION_VECTORS.Values.Select(dir => dir + loc).Append(loc);
            }
            else
            {
                neighbourLcoations = DIRECTION_VECTORS.Values.Select(dir => dir + loc);
            }

            if (!clampToWorldBounds)
            {
                return neighbourLcoations.ToArray();
            }

            return neighbourLcoations.Where(loc => loc.x >= gridFillMin.x && loc.y >= gridFillMin.y && loc.x <= gridFillMax.x && loc.y <= gridFillMax.y).ToArray();
        }

        /// <summary>
        /// Gets the neighbour pattern around the provided grid location (e.g. "11001000" if has neighbours N, NE, and S).
        /// </summary>
        /// <param name="loc">The grid location.</param>
        /// <returns>What neighbours this grid location has in binary form.</returns>
        public byte GetNeighbourPattern(Vector2Int loc)
        {
            byte pattern = 0;

            for (NeighbourDir dir = NeighbourDir.N; dir <= NeighbourDir.NW; dir = (NeighbourDir)((byte)dir << 1))
            {
                if (CellIsPresent(loc + DIRECTION_VECTORS[dir]))
                {
                    pattern |= (byte)dir;
                }
            }

            return pattern;
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

        private void Awake()
        {
            Grid = GetComponent<Grid>();
            OnPushGridUpdate += RenderMainGrid;
            OnPushGridUpdate += RenderBackground;
        }

        #region Render Main Grid

        private Dictionary<Vector2Int, GameObject> worldBlocks = new();

        private void RenderMainGrid(Vector2Int[] cellsToRerender)
        {
            foreach (Vector2Int loc in cellsToRerender)
            {
                RerenderBlock(loc);
            }
        }

        /// <summary>
        /// Add or remove meshes for the cell at the given position.
        /// </summary>
        /// <param name="loc">The grid location of the cell.</param>
        private void RerenderBlock(Vector2Int loc)
        {
            // Remove existing block at the location if one exists
            RemoveBlock(loc);

            // If their exists a cell entry for this location
            if (TryGetCell(loc, out int? cell))
            {
                // Get the block-information associated with the id of the entry
                if (BlockDatabase.Instance.TryGetBlockByID(cell.Value, out Block block))
                {
                    // Try to match the cell's neighbour pattern with one of the defined patterns to get the required meshes
                    if (block.TryGetMeshes(GetNeighbourPattern(loc), out Mesh[] meshes))
                    {
                        // Create a parent object for the block
                        Transform blockParent = new GameObject($"Cell[{loc}]").transform;
                        blockParent.position = GetWorldPosFromGridLoc(loc);
                        blockParent.parent = transform;

                        // For each mesh given by the pattern, create a face object to render the mesh
                        for (int i = 0; i < meshes.Length; i++)
                        {
                            Mesh mesh = meshes[i];

                            if (mesh != null)
                            {
                                Transform face = new GameObject($"Face", typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider)).transform;

                                face.parent = blockParent;
                                face.localPosition = Vector3.up;
                                face.RotateAround(blockParent.position, Vector3.forward, Block.DIRECTION_ANGLES[i]);
                                face.GetComponent<MeshFilter>().mesh = mesh;
                                face.GetComponent<MeshRenderer>().material = block.Material;
                                face.GetComponent<MeshCollider>().sharedMesh = mesh;
                            }
                        }

                        // Update the 'worldBlocks' dictionary
                        worldBlocks.Add(loc, blockParent.gameObject);
                    }
                }
                else
                {
                    Debug.LogError($"Now block was found with ID [{cell.Value}]");
                }
            }
        }

        private void RemoveBlock(Vector2Int loc)
        {
            if (worldBlocks.TryGetValue(loc, out GameObject block))
            {
                worldBlocks.Remove(loc);
                Destroy(block);
            }
        }

        #endregion

        #region Render Background

        [SerializeField] private Mesh backgroundPanelMesh;
        private Dictionary<Vector2Int, GameObject> backgroundPanels = new ();

        private void RenderBackground(Vector2Int[] cellsToRerender)
        {
            foreach (Vector2Int loc in cellsToRerender)
            {
                RerenderPanel(loc);
            }
        }

        private void RerenderPanel(Vector2Int loc)
        {
            RemovePanel(loc);

            if (!CellIsPresent(loc))
            {
                if (BlockDatabase.Instance.TryGetBlockByID(0, out Block block))
                {
                    Transform face = new GameObject($"BackPanel", typeof(MeshFilter), typeof(MeshRenderer)).transform;

                    face.parent = transform;
                    face.position = (Vector3)GetWorldPosFromGridLoc(loc) - Vector3.back;
                    face.RotateAround(face.transform.position, Vector3.right, -90);
                    face.GetComponent<MeshFilter>().mesh = backgroundPanelMesh;
                    face.GetComponent<MeshRenderer>().material = block.Material;

                    backgroundPanels.Add(loc, face.gameObject);
                }
                else
                {
                    Debug.LogError($"Now block was found with ID [{0}]");
                }
            }
        }

        private void RemovePanel(Vector2Int loc)
        {
            if (backgroundPanels.TryGetValue(loc, out GameObject panel))
            {
                backgroundPanels.Remove(loc);
                Destroy(panel);
            }
        }

        #endregion
    }
}
