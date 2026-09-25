using System;

namespace LoneTruth.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        public ItemSO item;
        public int quantity;

        public InventorySlot(ItemSO item, int quantity)
        {
            this.item = item;
            this.quantity = quantity;
        }

        public bool IsEmpty => item == null || quantity <= 0;
    }
}
