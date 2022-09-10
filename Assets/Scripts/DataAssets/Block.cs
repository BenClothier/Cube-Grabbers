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
            [SerializeField] private NeighbourState NE;
            [SerializeField] private NeighbourState E;
            [SerializeField] private NeighbourState SE;
            [SerializeField] private NeighbourState S;
            [SerializeField] private NeighbourState SW;
            [SerializeField] private NeighbourState W;
            [SerializeField] private NeighbourState NW;
            [Space]
            [SerializeField] MeshFace[] meshes;

            public NeighbourState[] Configuration => new NeighbourState[] { N, NE, E, SE, S, SW, W, NW };

            public MeshFace[] Meshes => meshes;
        }

        [Serializable]
        public struct MeshFace
        {
            public Mesh Mesh;
            public Direction Direction;
            public bool Flip;
        }

    }
}
