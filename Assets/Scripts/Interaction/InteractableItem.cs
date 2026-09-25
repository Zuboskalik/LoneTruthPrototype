using LoneTruth.Inventory;
using UnityEngine;

namespace LoneTruth.Interaction
{
    public class InteractableItem : MonoBehaviour, IInteractable
    {
        [Header("Item")]
        [SerializeField] private ItemSO item;
        [SerializeField] private int quantity = 1;

        public void Interact(GameObject interactor)
        {
            if (item == null || interactor == null)
            {
                return;
            }

            InventoryManager inventory = interactor.GetComponent<InventoryManager>();
            if (inventory == null)
            {
                inventory = interactor.GetComponentInParent<InventoryManager>();
            }

            if (inventory == null)
            {
                return;
            }

            if (inventory.AddItem(item, quantity))
            {
                Destroy(gameObject);
            }
        }

        public string GetInteractionPrompt()
        {
            if (item == null)
            {
                return "Pick up";
            }

            return $"Pick up {item.ItemName}";
        }
    }
}
