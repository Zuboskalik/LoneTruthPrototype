using LoneTruth.Core;
using LoneTruth.Inventory;
using LoneTruth.UI;
using UnityEngine;

namespace LoneTruth.Interaction
{
    /// <summary>
    /// A mechanism that needs an item (the air-shaft grate and the crowbar): E, or G with the item
    /// selected, uses it if Holt has it and sets a flag — a FlagTrigger cutscene takes it from there.
    /// </summary>
    public class ItemLock : MonoBehaviour, IInteractable, IItemReceiver
    {
        [SerializeField] private ItemSO requiredItem;
        [SerializeField] private string doneFlag;
        [SerializeField] private string prompt = "Осмотреть";
        [SerializeField] private string lockedLine;

        public void Configure(ItemSO item, string flag, string promptText, string locked)
        {
            requiredItem = item;
            doneFlag = flag;
            prompt = promptText;
            lockedLine = locked;
        }

        public void Interact(GameObject interactor)
        {
            if (StoryState.Has(doneFlag))
            {
                return;
            }

            InventoryManager inventory = interactor.GetComponent<InventoryManager>();
            if (inventory != null && inventory.CountItem(requiredItem) > 0)
            {
                Use(inventory);
            }
            else if (GameUI.Instance != null)
            {
                GameUI.Instance.Toast(lockedLine);
            }
        }

        public bool TryUseItem(ItemSO item, GameObject user)
        {
            if (item != requiredItem || StoryState.Has(doneFlag))
            {
                return false;
            }

            Use(user.GetComponent<InventoryManager>());
            return true;
        }

        public string GetInteractionPrompt()
        {
            return StoryState.Has(doneFlag) ? "" : Loc.T(prompt);
        }

        private void Use(InventoryManager inventory)
        {
            inventory.RemoveItem(requiredItem, 1);
            StoryState.Set(doneFlag);
        }
    }
}
