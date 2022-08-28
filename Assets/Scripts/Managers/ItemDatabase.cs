namespace Game.Managers
{
    using Game.Utility;
    using Game.DataAssets;

    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.AddressableAssets;

    public class ItemDatabase : Singleton<ItemDatabase>
    {
        private Dictionary<int, Item> itemDictionary;

        private void Awake()
        {
            InitialiseDatabase();
        }

        private void InitialiseDatabase()
        {
            Item[] itemList = UnityEngine.AddressableAssets..LoadAssets()
        }
    }
}
