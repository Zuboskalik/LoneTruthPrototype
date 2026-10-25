using UnityEngine;

namespace LoneTruth.Inventory
{
    public enum ItemType
    {
        Key,
        Weapon,
        Ammo,
        Consumable,
        Document
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "LoneTruth/Inventory/Item")]
    public class ItemSO : ScriptableObject
    {
        [SerializeField] private string id;
        [SerializeField] private string itemName;
        [SerializeField] private Sprite icon;
        [SerializeField] private ItemType itemType;
        [SerializeField] private int maxStack = 1;

        [Header("Use")]
        [TextArea(2, 4)] [SerializeField] private string description;
        [Tooltip("Text shown full-screen when a Document is used (G).")]
        [TextArea(3, 10)] [SerializeField] private string documentText;
        [Tooltip("Health restored by a Consumable.")]
        [SerializeField] private int healAmount;

        public string Id => id;
        public string ItemName => itemName;
        public Sprite Icon => icon;
        public ItemType ItemType => itemType;
        public int MaxStack => maxStack;
        public string Description => description;
        public string DocumentText => documentText;
        public int HealAmount => healAmount;
    }
}
