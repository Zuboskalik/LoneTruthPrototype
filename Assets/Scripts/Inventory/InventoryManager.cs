using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoneTruth.Inventory
{
    public class InventoryManager : MonoBehaviour
    {
        [Header("Inventory")]
        [SerializeField] private int inventorySize = 8;
        [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

        [Header("Slot Selection")]
        [SerializeField] private float scrollThreshold = 0.05f;

        public int SelectedSlotIndex { get; private set; }

        public event Action OnInventoryUpdated;
        public event Action<int> OnSlotSelected;

        private void Awake()
        {
            while (slots.Count < inventorySize)
            {
                slots.Add(new InventorySlot(null, 0));
            }
        }

        private void Update()
        {
            HandleSlotSelectionInput();
        }

        private void HandleSlotSelectionInput()
        {
            float scroll = Input.mouseScrollDelta.y;

            if (scroll > scrollThreshold)
            {
                SelectPreviousSlot();
            }
            else if (scroll < -scrollThreshold)
            {
                SelectNextSlot();
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                SelectPreviousSlot();
            }
            else if (Input.GetKeyDown(KeyCode.E))
            {
                SelectNextSlot();
            }
        }

        public void SelectNextSlot()
        {
            SelectSlot((SelectedSlotIndex + 1) % slots.Count);
        }

        public void SelectPreviousSlot()
        {
            SelectSlot((SelectedSlotIndex - 1 + slots.Count) % slots.Count);
        }

        public void SelectSlot(int index)
        {
            if (index < 0 || index >= slots.Count)
            {
                return;
            }

            SelectedSlotIndex = index;
            OnSlotSelected?.Invoke(SelectedSlotIndex);
        }

        public bool AddItem(ItemSO item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                return false;
            }

            for (int i = 0; i < slots.Count && quantity > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.item == item && slot.quantity < item.MaxStack)
                {
                    int spaceLeft = item.MaxStack - slot.quantity;
                    int amountToAdd = Mathf.Min(spaceLeft, quantity);
                    slot.quantity += amountToAdd;
                    quantity -= amountToAdd;
                }
            }

            for (int i = 0; i < slots.Count && quantity > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    int amountToAdd = Mathf.Min(item.MaxStack, quantity);
                    slots[i] = new InventorySlot(item, amountToAdd);
                    quantity -= amountToAdd;
                }
            }

            OnInventoryUpdated?.Invoke();

            return quantity <= 0;
        }

        public bool RemoveItem(ItemSO item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                return false;
            }

            int remaining = quantity;

            for (int i = 0; i < slots.Count && remaining > 0; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.item != item)
                {
                    continue;
                }

                int amountToRemove = Mathf.Min(slot.quantity, remaining);
                slot.quantity -= amountToRemove;
                remaining -= amountToRemove;

                if (slot.quantity <= 0)
                {
                    slots[i] = new InventorySlot(null, 0);
                }
            }

            OnInventoryUpdated?.Invoke();

            return remaining <= 0;
        }

        public bool HasItem(ItemSO item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
            {
                return false;
            }

            int total = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].item == item)
                {
                    total += slots[i].quantity;
                    if (total >= quantity)
                    {
                        return true;
                    }
                }
            }

            return false;
        }


        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= slots.Count)
            {
                return null;
            }

            return slots[index];
        }

        public InventorySlot GetSelectedSlot()
        {
            return GetSlot(SelectedSlotIndex);
        }

        public IReadOnlyList<InventorySlot> Slots => slots;
    }
}
