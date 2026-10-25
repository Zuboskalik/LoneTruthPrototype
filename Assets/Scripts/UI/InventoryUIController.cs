using LoneTruth.Combat;
using LoneTruth.Inventory;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    public class InventoryUIController : MonoBehaviour
    {
        [SerializeField] private InventoryManager inventoryManager;
        [SerializeField] private WeaponController weaponController;
        [SerializeField] private Image[] slotImages;
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color filledColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color selectedColor = new Color(1f, 0.85f, 0.3f, 0.9f);

        private Text[] countTexts;
        private Text[] badgeTexts;

        private void Start()
        {
            if (inventoryManager == null)
            {
                inventoryManager = FindAnyObjectByType<InventoryManager>();
            }

            if (weaponController == null)
            {
                weaponController = FindAnyObjectByType<WeaponController>();
            }

            if (inventoryManager == null)
            {
                return;
            }

            BuildOverlays();
        }

        private void Update()
        {
            Refresh();
        }

        private void BuildOverlays()
        {
            countTexts = new Text[slotImages.Length];
            badgeTexts = new Text[slotImages.Length];

            for (int i = 0; i < slotImages.Length; i++)
            {
                if (slotImages[i] == null)
                {
                    continue;
                }

                countTexts[i] = CreateOverlayText(slotImages[i].transform, "CountText", TextAnchor.LowerRight, new Vector2(1f, 0f));
                badgeTexts[i] = CreateOverlayText(slotImages[i].transform, "BadgeText", TextAnchor.UpperLeft, new Vector2(0f, 1f));
            }
        }

        private Text CreateOverlayText(Transform parent, string name, TextAnchor anchor, Vector2 anchorPoint)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            Text text = go.AddComponent<Text>();
            text.font = UIFonts.Default;
            text.fontSize = 26;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = Color.white;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Outline outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            RectTransform rect = text.GetComponent<RectTransform>();
            rect.anchorMin = anchorPoint;
            rect.anchorMax = anchorPoint;
            rect.pivot = anchorPoint;
            rect.sizeDelta = new Vector2(64f, 28f);
            rect.anchoredPosition = Vector2.zero;

            return text;
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
                string badge = string.Empty;
                string count = string.Empty;

                if (slot == null || slot.IsEmpty)
                {
                    img.sprite = null;
                    img.color = isSelected ? selectedColor : emptyColor;
                }
                else
                {
                    img.sprite = slot.item.Icon;
                    img.color = isSelected ? selectedColor : filledColor;

                    bool isWeaponSlot = weaponController != null && slot.item == weaponController.WeaponItem;
                    if (isWeaponSlot)
                    {
                        badge = weaponController.IsEquipped ? "E" : string.Empty;
                        count = weaponController.LoadedAmmo + "/" + weaponController.MaxLoadedAmmo;
                    }
                    else if (slot.quantity > 1)
                    {
                        count = slot.quantity.ToString();
                    }
                }

                if (badgeTexts != null && badgeTexts[i] != null)
                {
                    badgeTexts[i].text = badge;
                }

                if (countTexts != null && countTexts[i] != null)
                {
                    countTexts[i].text = count;
                }
            }
        }
    }
}
