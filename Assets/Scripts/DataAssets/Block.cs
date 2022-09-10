namespace Game.DataAssets
{
    using Game.Behaviours;
    using Game.Components;
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    [CreateAssetMenu(fileName = "Cell", menuName = "Game/Cell")]
    public class Block : ScriptableObject
    {
        [SerializeField] private int id;
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

        public bool TryGetMeshes(bool[] neighbourPresence, out Mesh[] meshes)
        {
            // Check all mesh configurations
            foreach (MeshConfiguration config in meshConfigurations)
            {
                // If can rotate config, check all 4 rotations
                for (int rotOffset = 0; rotOffset <= (config.CanRotate ? 6 : 0); rotOffset += 2)
                {
                    // Assume the pattern matches
                    bool allMatch = true;

                    // For each neighbour (8 total)
                    for (int i = 0; i < neighbourPresence.Length; i++)
                    {
                        // Get presence of neighbour
                        bool isCurrentNeighbourPresent = neighbourPresence[(rotOffset + i) % neighbourPresence.Length];

                        // Check if it matches the config requirement
                        bool matches = config.Configuration[i] switch
                        {
                            NeighbourState.Present => isCurrentNeighbourPresent,
                            NeighbourState.Absent => !isCurrentNeighbourPresent,
                            _ => true,
                        };

                        // If it doesn't match, we know the pattern (in this rotation) doesn't match, so don't check this rotation any further.
                        if (!matches)
                        {
                            allMatch = false;
                            break;
                        }
                    }

                    // If our assumption that it matches hasn't been disproven, we have found a match, so return list of meshes accounting for the rotation offset.
                    if (allMatch)
                    {
                        meshes = new Mesh[config.Meshes.Length];
                        for (int i = 0; i < meshes.Length; i++)
                        {
                            meshes[(i + rotOffset) % meshes.Length] = config.Meshes[i];
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
        }

    }
}
