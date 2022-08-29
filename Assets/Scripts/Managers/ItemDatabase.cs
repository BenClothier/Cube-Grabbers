namespace Game.Managers
{
    using Game.Utility;
    using Game.DataAssets;

    using System.Collections.Generic;
    using UnityEngine;

    public class ItemDatabase : Singleton<ItemDatabase>
    {
        private Dictionary<int, Item> itemDictionary = new ();

        public bool GetItemByID(int id, out Item item)
        {
            if (itemDictionary.TryGetValue(id, out item))
            {
                return true;
            }
            else
            {
                Debug.LogWarning($"Could not find item with ID [{id}]");
                return false;
            }
        }

        private void Awake()
        {
            InitialiseDatabase();
        }

        private void InitialiseDatabase()
        {
            Item[] itemList = Resources.LoadAll<Item>("Items");

            foreach (Item item in itemList)
            {
                itemDictionary.Add(item.ID, item);
            }
        }
    }
}
