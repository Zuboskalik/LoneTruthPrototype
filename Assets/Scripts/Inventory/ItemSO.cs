using UnityEngine;

namespace LoneTruth.Inventory
{
    public enum ItemType
    {
        Key,
        Weapon,
        Ammo,
        Consumable
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "LoneTruth/Inventory/Item")]
    public class ItemSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string itemName;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemType itemType;
        [SerializeField] private int maxStack = 1;

        public string Id => id;
        public string ItemName => itemName;
        public Sprite Icon => icon;
        public ItemType ItemType => itemType;
        public int MaxStack => maxStack;
    }
}
