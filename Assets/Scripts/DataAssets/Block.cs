namespace Game.DataAssets
{
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

        [Header("General")]
        [SerializeField] private int id;
        [Range(0, 60)][SerializeField] private float secondsToMine = 3;
        [Space]
        [SerializeField] private ItemDropChance[] itemDropChances;

        [Header("Visual")]
        [SerializeField] private Material material;
        [Space]
        [SerializeField] private ConfigurationPattern[] meshConfigurations;

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

        public float SecondsToMine => secondsToMine;

        public Material Material => material;

        public bool TryMatchConfiguration(byte neighbourPattern, out MeshConfig meshConfig)
        {
            // Check all mesh configurations
            foreach (ConfigurationPattern config in meshConfigurations)
            {
                // If can rotate config, check all 4 rotations
                for (int rotOffset = 0; rotOffset <= (config.CanRotate ? 4 : 0); rotOffset++)
                {
                    byte shiftedPattern = (byte)(neighbourPattern << rotOffset * 2 | neighbourPattern >> 8 - rotOffset * 2);

                    // If our assumption that it matches hasn't been disproven, we have found a match, so return list of meshes accounting for the rotation offset.
                    if (config.DoesMatch(shiftedPattern))
                    {
                        meshConfig = config.ConvertToMeshConfig(rotOffset);
                        return true;
                    }
                }
            }

            // If no matches found in any config, return null and false - not an error, will just have no mesh.
            meshConfig = default;
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
        public class ConfigurationPattern
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
            [SerializeField] private Mesh Face_Z = null;

            private NeighbourState[] configuration => new NeighbourState[] { N, NE, E, SE, S, SW, W, NW };

            private Mesh[] meshes => new Mesh[] { Face_N, Face_E, Face_S, Face_W };

            public bool CanRotate => canRotate;

            public bool DoesMatch(byte neighbourPattern)
            {
                byte irrelevantMask = configuration
                    .Select((item, index) => new {item, index})
                    .Aggregate((byte)0, (acc, next) => (byte)(next.item == NeighbourState.Irrelevant ? acc : acc | (1 << next.index)));

                byte thisPattern = configuration
                    .Select((item, index) => new { item, index })
                    .Aggregate((byte)0, (acc, next) => (byte)(next.item == NeighbourState.Present ? acc | (1 << next.index) : acc));

                neighbourPattern &= irrelevantMask;

                return neighbourPattern == thisPattern;
            }

            public MeshConfig ConvertToMeshConfig(int rotOffset)
            {
                Mesh[] meshList = new Mesh[meshes.Length];
                for (int i = 0; i < meshes.Length; i++)
                {
                    meshList[i] = meshes[(i + rotOffset) % meshes.Length];
                }

                return new MeshConfig(meshList, Face_Z);
            }
        }

        public struct MeshConfig
        {
            public Mesh[] MainMeshes;
            public Mesh FrontMesh;

            public MeshConfig(Mesh[] mainMeshes, Mesh zMesh)
            {
                MainMeshes = mainMeshes;
                FrontMesh = zMesh;
            }
        }

    }
}
