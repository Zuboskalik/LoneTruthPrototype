using LoneTruth.Core;
using LoneTruth.Inventory;
using LoneTruth.UI;
using UnityEngine;

namespace LoneTruth.Interaction
{
    /// <summary>
    /// An item lying in a location that the story needs: it stays there until there is room for it
    /// in the inventory (nothing is lost to a full bar) and does not come back once taken.
    /// </summary>
    public class StoryPickup : MonoBehaviour, IInteractable
    {
        [SerializeField] private ItemSO item;
        [SerializeField] private int amount = 1;
        [SerializeField] private string takenFlag;

        public void Configure(ItemSO pickupItem, int count, string flagWhenTaken)
        {
            item = pickupItem;
            amount = count;
            takenFlag = flagWhenTaken;
        }

        private void OnEnable()
        {
            if (StoryState.Has(takenFlag))
            {
                gameObject.SetActive(false);
            }
        }

        public void Interact(GameObject interactor)
        {
            InventoryManager inventory = interactor.GetComponent<InventoryManager>();
            if (inventory == null || item == null)
            {
                return;
            }

            if (!inventory.AddItem(item, amount))
            {
                if (GameUI.Instance != null)
                {
                    GameUI.Instance.Toast("Некуда положить.", 2.2f);
                }

                return;
            }

            if (GameUI.Instance != null)
            {
                GameUI.Instance.Toast(Loc.Format("Получено: {0}", item.DisplayName));
            }

            StoryState.Set(takenFlag);
            gameObject.SetActive(false);
        }

        public string GetInteractionPrompt()
        {
            return item != null ? Loc.Format("Подобрать: {0}", item.DisplayName) : "";
        }
    }
}
