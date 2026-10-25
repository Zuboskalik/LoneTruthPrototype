using System.Collections;
using LoneTruth.Combat;
using LoneTruth.Core;
using LoneTruth.Interaction;
using LoneTruth.Player;
using LoneTruth.UI;
using UnityEngine;

namespace LoneTruth.Inventory
{
    /// <summary>
    /// G uses the item selected in the inventory bar, by type:
    /// weapon — draw / holster (marked "E"), ammo — load the cylinder, document — read it,
    /// consumable — use it, key — use it on the door in front of the player.
    /// </summary>
    [RequireComponent(typeof(InventoryManager))]
    public class InventoryActions : MonoBehaviour
    {
        [SerializeField] private KeyCode useKey = KeyCode.G;

        private InventoryManager inventory;
        private WeaponController weapon;
        private InteractionDetector detector;
        private PlayerController player;
        private Health health;

        private void Awake()
        {
            inventory = GetComponent<InventoryManager>();
            weapon = GetComponent<WeaponController>();
            detector = GetComponent<InteractionDetector>();
            player = GetComponent<PlayerController>();
            health = GetComponent<Health>();
        }

        private void Update()
        {
            if (GameMaster.Instance != null && !GameMaster.Instance.IsInState(GameState.Exploration))
            {
                return;
            }

            if (Input.GetKeyDown(useKey))
            {
                UseSelected();
            }
        }

        public void UseSelected()
        {
            InventorySlot slot = inventory.GetSelectedSlot();
            if (slot == null || slot.IsEmpty)
            {
                return;
            }

            ItemSO item = slot.item;
            switch (item.ItemType)
            {
                case ItemType.Weapon:
                    if (weapon != null && item == weapon.WeaponItem)
                    {
                        weapon.ToggleEquip();
                        Say(Loc.Format(weapon.IsEquipped ? "{0} — в руке" : "{0} — в кобуре", item.DisplayName));
                    }

                    break;

                case ItemType.Ammo:
                    LoadAmmo();
                    break;

                case ItemType.Document:
                    StartCoroutine(ReadDocument(item));
                    break;

                case ItemType.Consumable:
                    if (health != null && health.CurrentHealth >= health.MaxHealth)
                    {
                        Say("Сейчас это не нужно.");
                        break;
                    }

                    if (health != null)
                    {
                        health.Heal(item.HealAmount);
                    }

                    inventory.RemoveItem(item, 1);
                    Say(Loc.Format("{0} — использовано", item.DisplayName));
                    break;

                case ItemType.Key:
                    var receiver = detector != null ? detector.CurrentTarget as IItemReceiver : null;
                    if (receiver == null || !receiver.TryUseItem(item, gameObject))
                    {
                        Say("Здесь это не пригодится.");
                    }

                    break;
            }
        }

        private void LoadAmmo()
        {
            if (weapon == null)
            {
                return;
            }

            if (weapon.LoadedAmmo >= weapon.MaxLoadedAmmo)
            {
                Say("Барабан полон.");
                return;
            }

            int before = weapon.LoadedAmmo;
            if (weapon.Reload())
            {
                Say(Loc.Format("Заряжено: {0}", weapon.LoadedAmmo - before));
            }
        }

        private IEnumerator ReadDocument(ItemSO item)
        {
            if (GameUI.Instance == null || string.IsNullOrEmpty(item.DocumentText))
            {
                Say(item.Description);
                yield break;
            }

            GameMaster gm = GameMaster.Instance;
            if (gm != null)
            {
                gm.SetState(GameState.Cutscene);
            }

            if (player != null)
            {
                player.PlayAnimationTrigger("ReadLetter");
            }

            GameUI.Instance.SetHudVisible(false);
            yield return new WaitForSeconds(0.6f);
            yield return GameUI.Instance.ShowCard(item.DocumentText, 0f);
            GameUI.Instance.SetHudVisible(true);

            if (gm != null)
            {
                gm.SetState(GameState.Exploration);
            }
        }

        private static void Say(string text)
        {
            if (GameUI.Instance != null && !string.IsNullOrEmpty(text))
            {
                GameUI.Instance.Toast(text, 2.2f);
            }
        }
    }
}
