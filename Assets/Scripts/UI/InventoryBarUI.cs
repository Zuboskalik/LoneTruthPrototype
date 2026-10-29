using System.Collections;
using System.Collections.Generic;
using LoneTruth.Combat;
using LoneTruth.Core;
using LoneTruth.Inventory;
using LoneTruth.Player;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    /// <summary>
    /// Silent Hill 4–style inventory: the items float in one horizontal row at the bottom of the
    /// screen, green corner brackets mark the selected one, "E" marks the equipped weapon and
    /// numbers show counts (for the revolver — rounds in the cylinder). Q / mouse wheel moves the
    /// selection, G uses the selected item (see InventoryActions).
    /// </summary>
    [RequireComponent(typeof(GameUI))]
    public class InventoryBarUI : MonoBehaviour
    {
        [SerializeField] private int maxViews = 8;
        [SerializeField] private float slotWidth = 104f;
        [SerializeField] private float iconSize = 72f;
        [SerializeField] private Color bracketColor = new Color(0.25f, 0.85f, 0.42f);
        [SerializeField] private Color countColor = new Color(0.93f, 0.88f, 0.7f);
        [SerializeField] private Color equippedColor = new Color(0.86f, 0.84f, 0.78f);

        private class SlotView
        {
            public GameObject Root;
            public Image Icon;
            public Text Count;
            public Text Equipped;
            public GameObject Brackets;
        }

        private const string Hint = "Q / колесо — выбрать   ·   G — применить   ·   R — перезарядить";
        private const string AimHint = "ПКМ / Ctrl — прицелиться   ·   E / ЛКМ — выстрел";

        private readonly List<SlotView> views = new List<SlotView>();
        private GameObject barRoot;
        private Text nameText;
        private Text moneyText;
        private Text hintText;
        private bool visible = true;
        private InventoryManager inventory;
        private WeaponController weapon;

        // A small padlock over the revolver while the weapon can't be used here (outside a CombatZone).
        private RectTransform weaponLock;
        private CanvasGroup weaponLockGroup;
        private bool lockShown = true;
        private Coroutine lockRoutine;
        private static readonly Vector2 LockOffset = new Vector2(30f, 30f); // top-right corner of the slot

        private void Start()
        {
            Build();
        }

        public void SetVisible(bool value)
        {
            visible = value;
            if (barRoot != null)
            {
                barRoot.SetActive(value);
            }
        }

        private void Bind()
        {
            if (inventory != null)
            {
                return;
            }

            GameObject player = PersistentPlayer.Current;
            if (player == null)
            {
                return;
            }

            inventory = player.GetComponent<InventoryManager>();
            weapon = player.GetComponent<WeaponController>();
        }

        private void LateUpdate()
        {
            if (barRoot == null || !visible)
            {
                return;
            }

            Bind();
            if (inventory == null)
            {
                barRoot.SetActive(false);
                return;
            }

            barRoot.SetActive(true);
            IReadOnlyList<InventorySlot> slots = inventory.Slots;
            int v = 0;
            int weaponView = -1;
            string selectedName = "";
            for (int i = 0; i < slots.Count && v < views.Count; i++)
            {
                InventorySlot slot = slots[i];
                if (slot.IsEmpty)
                {
                    continue;
                }

                SlotView view = views[v++];
                view.Root.SetActive(true);
                view.Icon.sprite = slot.item.Icon;
                view.Icon.enabled = slot.item.Icon != null;

                bool isWeapon = weapon != null && slot.item == weapon.WeaponItem;
                if (isWeapon)
                {
                    weaponView = v - 1;
                    view.Count.text = weapon.LoadedAmmo.ToString();
                }
                else
                {
                    view.Count.text = slot.quantity > 1 || slot.item.ItemType == ItemType.Ammo ? slot.quantity.ToString() : "";
                }

                view.Equipped.gameObject.SetActive(isWeapon && weapon.IsEquipped);
                bool selected = i == inventory.SelectedSlotIndex;
                view.Brackets.SetActive(selected);
                if (selected)
                {
                    selectedName = isWeapon
                        ? $"{slot.item.DisplayName}  ·  {weapon.LoadedAmmo}/{weapon.MaxLoadedAmmo}"
                        : slot.item.DisplayName;
                }
            }

            for (; v < views.Count; v++)
            {
                views[v].Root.SetActive(false);
            }

            int shown = 0;
            foreach (SlotView view in views)
            {
                if (view.Root.activeSelf) shown++;
            }

            UpdateWeaponLock(weaponView);
            nameText.text = selectedName;
            hintText.text = weapon != null && weapon.IsEquipped && CombatZone.WeaponsAllowed
                ? Loc.T(Hint) + "   ·   " + Loc.T(AimHint)
                : Loc.T(Hint);
            // Money sits right after the last item and appears once Holt has some.
            moneyText.gameObject.SetActive(StoryState.Money > 0);
            moneyText.text = "$ " + StoryState.Money;
            moneyText.rectTransform.anchoredPosition = new Vector2(shown * slotWidth + 12f, -6f);
        }

        /// <summary>
        /// Keeps the padlock on the revolver's slot. When the weapon becomes usable the lock drifts
        /// up and dissolves; when that ends it fades back in.
        /// </summary>
        private void UpdateWeaponLock(int weaponView)
        {
            if (weaponLock == null)
            {
                return;
            }

            bool hasWeapon = weaponView >= 0;
            weaponLock.gameObject.SetActive(hasWeapon);
            if (!hasWeapon)
            {
                return;
            }

            weaponLock.SetParent(views[weaponView].Root.transform, false);
            bool locked = !CombatZone.WeaponsAllowed;
            if (locked == lockShown)
            {
                return;
            }

            lockShown = locked;
            if (lockRoutine != null)
            {
                StopCoroutine(lockRoutine);
            }

            lockRoutine = StartCoroutine(locked ? LockAppear() : LockFlyAway());
        }

        private IEnumerator LockFlyAway()
        {
            float start = weaponLockGroup.alpha;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 1.1f)
            {
                float k = Mathf.SmoothStep(0f, 1f, t);
                weaponLock.anchoredPosition = LockOffset + new Vector2(6f * k, 46f * k);
                weaponLock.localScale = Vector3.one * (1f + 0.25f * k);
                weaponLockGroup.alpha = start * (1f - k);
                yield return null;
            }

            weaponLockGroup.alpha = 0f;
            lockRoutine = null;
        }

        private IEnumerator LockAppear()
        {
            weaponLock.anchoredPosition = LockOffset;
            weaponLock.localScale = Vector3.one;
            float start = weaponLockGroup.alpha;
            for (float t = 0f; t < 1f; t += Time.unscaledDeltaTime / 0.5f)
            {
                weaponLockGroup.alpha = Mathf.Lerp(start, 1f, t);
                yield return null;
            }

            weaponLockGroup.alpha = 1f;
            lockRoutine = null;
        }

        private void BuildWeaponLock()
        {
            var go = new GameObject("WeaponLock", typeof(RectTransform), typeof(CanvasGroup));
            go.transform.SetParent(barRoot.transform, false);
            weaponLock = (RectTransform)go.transform;
            weaponLockGroup = go.GetComponent<CanvasGroup>();
            weaponLockGroup.blocksRaycasts = false;
            weaponLockGroup.interactable = false;
            GameUI.Place(weaponLock, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), LockOffset, new Vector2(22f, 26f));

            // A schematic padlock: shackle (three bars) over a body with a keyhole.
            var ink = new Color(equippedColor.r, equippedColor.g, equippedColor.b, 0.9f);
            LockPart(new Vector2(-6f, 4f), new Vector2(3f, 9f), ink);
            LockPart(new Vector2(6f, 4f), new Vector2(3f, 9f), ink);
            LockPart(new Vector2(0f, 9f), new Vector2(15f, 3f), ink);
            LockPart(new Vector2(0f, -6f), new Vector2(18f, 13f), ink);
            LockPart(new Vector2(0f, -6f), new Vector2(3f, 6f), new Color(0.08f, 0.08f, 0.09f, 0.9f));
            weaponLockGroup.alpha = CombatZone.WeaponsAllowed ? 0f : 1f;
            lockShown = !CombatZone.WeaponsAllowed;
            go.SetActive(false);
        }

        private void LockPart(Vector2 pos, Vector2 size, Color color)
        {
            var part = new GameObject("LockPart", typeof(RectTransform), typeof(Image));
            part.transform.SetParent(weaponLock, false);
            var img = part.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            GameUI.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            part.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.8f);
        }

        private void Build()
        {
            GameUI ui = GetComponent<GameUI>();
            barRoot = new GameObject("InventoryBar", typeof(RectTransform));
            barRoot.transform.SetParent(transform, false);
            GameUI.Place((RectTransform)barRoot.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(48, 28), new Vector2(slotWidth * maxViews + 200, 150));

            nameText = ui.MakeText("SelectedName", barRoot.transform, 22, equippedColor, TextAnchor.LowerLeft);
            GameUI.Place(nameText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(8, 4), new Vector2(900, 30));

            hintText = ui.MakeText("Hint", barRoot.transform, 16, new Color(0.55f, 0.53f, 0.48f), TextAnchor.LowerLeft);
            GameUI.Place(hintText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(8, -22), new Vector2(900, 22));

            moneyText = ui.MakeText("Money", barRoot.transform, 24, countColor, TextAnchor.MiddleLeft);
            GameUI.Place(moneyText.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(slotWidth * maxViews + 16, -6), new Vector2(180, 40));

            for (int i = 0; i < maxViews; i++)
            {
                views.Add(MakeSlot(ui, i));
            }

            BuildWeaponLock();
        }

        private SlotView MakeSlot(GameUI ui, int index)
        {
            var view = new SlotView();
            view.Root = new GameObject($"Slot_{index}", typeof(RectTransform));
            view.Root.transform.SetParent(barRoot.transform, false);
            GameUI.Place((RectTransform)view.Root.transform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(index * slotWidth, 12), new Vector2(slotWidth, 104));

            var icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(view.Root.transform, false);
            view.Icon = icon.GetComponent<Image>();
            view.Icon.preserveAspect = true;
            view.Icon.raycastTarget = false;
            GameUI.Place(view.Icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize, iconSize));
            var shadow = icon.AddComponent<Shadow>();
            shadow.effectColor = new Color(0, 0, 0, 0.85f);
            shadow.effectDistance = new Vector2(3, -3);

            view.Count = ui.MakeText("Count", view.Root.transform, 26, countColor, TextAnchor.LowerRight);
            view.Count.fontStyle = FontStyle.Bold;
            GameUI.Place(view.Count.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-10, 4), new Vector2(60, 34));

            view.Equipped = ui.MakeText("Equipped", view.Root.transform, 24, equippedColor, TextAnchor.UpperLeft);
            view.Equipped.text = "E";
            view.Equipped.fontStyle = FontStyle.Bold;
            GameUI.Place(view.Equipped.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(-6, 14), new Vector2(30, 30));

            view.Brackets = new GameObject("Brackets", typeof(RectTransform));
            view.Brackets.transform.SetParent(view.Root.transform, false);
            GameUI.Place((RectTransform)view.Brackets.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(iconSize + 22, iconSize + 22));
            float half = (iconSize + 22) / 2f;
            foreach (Vector2 corner in new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
            {
                Bar(view.Brackets.transform, new Vector2(corner.x * (half - 9), corner.y * half), new Vector2(18, 4));
                Bar(view.Brackets.transform, new Vector2(corner.x * half, corner.y * (half - 9)), new Vector2(4, 18));
            }

            view.Root.SetActive(false);
            return view;
        }

        private void Bar(Transform parent, Vector2 pos, Vector2 size)
        {
            var go = new GameObject("Bracket", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.color = bracketColor;
            img.raycastTarget = false;
            GameUI.Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        }
    }
}
