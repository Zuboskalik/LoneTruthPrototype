using LoneTruth.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    public class InventoryUIController : MonoBehaviour
    {
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private Image[] slotImages;
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color filledColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f, 0.9f);

        private void Start()
        {
            if (inventoryManager == null)
            {
                inventoryManager = FindAnyObjectByType<InventoryManager>();
            }

            if (inventoryManager == null)
            {
                return;
            }

            inventoryManager.OnInventoryUpdated += Refresh;
            inventoryManager.OnSlotSelected += HandleSlotSelected;
            Refresh();
        }

        private void OnDestroy()
        {
            if (inventoryManager == null)
            {
                return;
            }

            inventoryManager.OnInventoryUpdated -= Refresh;
            inventoryManager.OnSlotSelected -= HandleSlotSelected;
        }

        private void HandleSlotSelected(int index)
        {
            Refresh();
        }

        private void Refresh()
        {
            for (int i = 0; i < slotImages.Length; i++)
            {
                Image img = slotImages[i];
                if (img == null)
                {
                    continue;
                }

                InventorySlot slot = inventoryManager.GetSlot(i);
                bool isSelected = i == inventoryManager.SelectedSlotIndex;

                if (slot == null || slot.IsEmpty)
                {
                    img.sprite = null;
                    img.color = isSelected ? selectedColor : emptyColor;
                }
                else
                {
                    img.sprite = slot.item.Icon;
                    img.color = isSelected ? selectedColor : filledColor;
                }
            }
        }
    }
}
