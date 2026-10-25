using LoneTruth.Inventory;
using UnityEngine;

namespace LoneTruth.Interaction
{
    /// <summary>An interactable that accepts an inventory item used on it (a key on a door).</summary>
    public interface IItemReceiver
    {
        /// <returns>true when the item was accepted.</returns>
        bool TryUseItem(ItemSO item, GameObject user);
    }
}
