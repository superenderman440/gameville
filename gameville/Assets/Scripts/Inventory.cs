using UnityEngine;
using System.Collections.Generic;

namespace Gameville
{
    [System.Serializable]
    public class ItemDefinition
    {
        public int id;
        public string name;
        public string type; // weapon, armor, consumable, tool, misc
        public int damage;
        public int defense;
        public int healAmount;
        public string description;
    }

    [System.Serializable]
    public class ItemSet
    {
        public List<ItemDefinition> items = new List<ItemDefinition>();
    }

    public class Item
    {
        public ItemDefinition definition;
        public int quantity = 1;

        public Item(ItemDefinition def, int qty = 1)
        {
            definition = def;
            quantity = qty;
        }
    }

    public class Inventory : MonoBehaviour
    {
        public int maxSlots = 20;
        private List<Item> items = new List<Item>();
        private static ItemSet itemDatabase;
        public static ItemSet ItemDatabase { get { return itemDatabase; } }

        private void Start()
        {
            LoadItemDatabase();
        }

        public static void LoadItemDatabase()
        {
            if (itemDatabase != null)
                return;

            string itemPath = System.IO.Path.Combine(Application.streamingAssetsPath, "Items/items.json");
            if (System.IO.File.Exists(itemPath))
            {
                string json = System.IO.File.ReadAllText(itemPath);
                itemDatabase = JsonUtility.FromJson<ItemSet>(json);
                Debug.Log($"Loaded {itemDatabase.items.Count} items.");
            }
            else
            {
                itemDatabase = new ItemSet();
                CreateDefaultItems();
                Debug.Log("Created default item database.");
            }
        }

        private static void CreateDefaultItems()
        {
            itemDatabase.items.Add(new ItemDefinition { id = 1, name = "Wooden Sword", type = "weapon", damage = 5, description = "A basic wooden sword" });
            itemDatabase.items.Add(new ItemDefinition { id = 2, name = "Iron Sword", type = "weapon", damage = 15, description = "A sturdy iron blade" });
            itemDatabase.items.Add(new ItemDefinition { id = 3, name = "Health Potion", type = "consumable", healAmount = 25, description = "Restores health" });
            itemDatabase.items.Add(new ItemDefinition { id = 4, name = "Shield", type = "armor", defense = 10, description = "Wooden shield" });
        }

        public bool AddItem(int itemId, int quantity = 1)
        {
            if (items.Count >= maxSlots && !ItemStackable(itemId))
                return false;

            ItemDefinition def = GetItemDefinition(itemId);
            if (def == null)
                return false;

            Item existing = items.Find(i => i.definition.id == itemId);
            if (existing != null && IsStackable(def))
            {
                existing.quantity += quantity;
            }
            else
            {
                items.Add(new Item(def, quantity));
            }
            return true;
        }

        public bool RemoveItem(int itemId, int quantity = 1)
        {
            Item item = items.Find(i => i.definition.id == itemId);
            if (item == null)
                return false;

            item.quantity -= quantity;
            if (item.quantity <= 0)
            {
                items.Remove(item);
            }
            return true;
        }

        public Item GetItem(int index)
        {
            return index >= 0 && index < items.Count ? items[index] : null;
        }

        public ItemDefinition GetItemDefinition(int itemId)
        {
            return itemDatabase?.items.Find(i => i.id == itemId);
        }

        private bool IsStackable(ItemDefinition def)
        {
            return def.type == "consumable" || def.type == "misc";
        }

        private bool ItemStackable(int itemId)
        {
            ItemDefinition def = GetItemDefinition(itemId);
            return def != null && IsStackable(def);
        }

        public List<Item> GetAllItems()
        {
            return new List<Item>(items);
        }

        public int GetItemCount()
        {
            return items.Count;
        }
    }
}
