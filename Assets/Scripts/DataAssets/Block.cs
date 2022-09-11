namespace Game.DataAssets
{
    using Game.Behaviours;
    using Game.Components;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Cell", menuName = "Game/Cell")]
    public class Block : ScriptableObject
    {
        public static readonly float[] DIRECTION_ANGLES =
        {
            0,
            -90,
            180,
            90,
        };

        [SerializeField] private int id;
        [SerializeField] private Material material;
        [Space]
        [SerializeField] private ItemDropChance[] itemDropChances;
        [Space]
        [SerializeField] private MeshConfiguration[] meshConfigurations;

        public enum NeighbourState
        {
            Present,
            Absent,
            Irrelevant,
        }

        public enum Direction
        {
            N,
            E,
            S,
            W,
        }

        public int ID => id;

        public Material Material => material;

        public bool TryGetMeshes(byte neighbourPattern, out Mesh[] meshes)
        {
            // Check all mesh configurations
            foreach (MeshConfiguration config in meshConfigurations)
            {
                // If can rotate config, check all 4 rotations
                for (int rotOffset = 0; rotOffset <= (config.CanRotate ? 4 : 0); rotOffset++)
                {
                    byte shiftedPattern = (byte)(neighbourPattern << rotOffset * 2 | neighbourPattern >> 8 - rotOffset * 2);

                    // If our assumption that it matches hasn't been disproven, we have found a match, so return list of meshes accounting for the rotation offset.
                    if (config.DoesMatch(shiftedPattern))
                    {
                        meshes = new Mesh[config.Meshes.Length];
                        for (int i = 0; i < meshes.Length; i++)
                        {
                            meshes[i] = config.Meshes[(i + rotOffset) % meshes.Length];
                        }

                        return true;
                    }
                }
            }

            // If no matches found in any config, return null and false - not an error, will just have no mesh.
            meshes = null;
            return false;
        }

        public int[] GenerateDrops()
        {
            List<int> itemDropList = new ();

            foreach (ItemDropChance itemDropChance in itemDropChances)
            {
                if (UnityEngine.Random.value <= itemDropChance.dropChance)
                {
                    itemDropList.Add(itemDropChance.ItemID);
                }
            }

            return itemDropList.ToArray();
        }

        [Serializable]
        public class ItemDropChance
        {
            [SerializeField] public int ItemID;
            [Range(0, 1)][SerializeField] public float dropChance;
        }

        [Serializable]
        public class MeshConfiguration
        {
            [SerializeField] private NeighbourState N;
            [SerializeField] private NeighbourState NE = NeighbourState.Irrelevant;
            [SerializeField] private NeighbourState E;
            [SerializeField] private NeighbourState SE = NeighbourState.Irrelevant;
            [SerializeField] private NeighbourState S;
            [SerializeField] private NeighbourState SW = NeighbourState.Irrelevant;
            [SerializeField] private NeighbourState W;
            [SerializeField] private NeighbourState NW = NeighbourState.Irrelevant;
            [Space]
            [SerializeField] private bool canRotate = true;
            [Space]
            [SerializeField] private Mesh Face_N = null;
            [SerializeField] private Mesh Face_E = null;
            [SerializeField] private Mesh Face_S = null;
            [SerializeField] private Mesh Face_W = null;

            public NeighbourState[] Configuration => new NeighbourState[] { N, NE, E, SE, S, SW, W, NW };

            public Mesh[] Meshes => new Mesh[] { Face_N, Face_E, Face_S, Face_W };

            public bool CanRotate => canRotate;

            public bool DoesMatch(byte neighbourPattern)
            {
                byte irrelevantMask = Configuration
                    .Select((item, index) => new {item, index})
                    .Aggregate((byte)0, (acc, next) => (byte)(next.item == NeighbourState.Irrelevant ? acc : acc | (1 << next.index)));

                byte thisPattern = Configuration
                    .Select((item, index) => new { item, index })
                    .Aggregate((byte)0, (acc, next) => (byte)(next.item == NeighbourState.Present ? acc | (1 << next.index) : acc));

                neighbourPattern &= irrelevantMask;

                return neighbourPattern == thisPattern;
            }
        }

    }
}
