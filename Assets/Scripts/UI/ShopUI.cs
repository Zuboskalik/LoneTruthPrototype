using System.Collections;
using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Inventory;
using LoneTruth.Player;
using LoneTruth.Shop;
using UnityEngine;
using UnityEngine.UI;

namespace LoneTruth.UI
{
    /// <summary>
    /// The trade window: goods with prices, Holt's money, what he already carries. Opened from a
    /// dialogue answer (DialogueSO.Choice.openShop). W / S — goods, E — buy, Esc / Tab — leave.
    /// </summary>
    [RequireComponent(typeof(GameUI))]
    public class ShopUI : MonoBehaviour
    {
        public static ShopUI Instance { get; private set; }

        /// <summary>Tests: with GameUI.AutoAdvance on, buy this entry once and leave (-1 = just leave).</summary>
        public static int AutoBuy = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            AutoBuy = -1;
        }

        [SerializeField] private Color panelColor = new Color(0.04f, 0.045f, 0.055f, 0.94f);
        [SerializeField] private Color titleColor = new Color(0.78f, 0.57f, 0.18f);
        [SerializeField] private Color textColor = new Color(0.93f, 0.9f, 0.85f);
        [SerializeField] private Color mutedColor = new Color(0.55f, 0.53f, 0.49f);
        [SerializeField] private int maxRows = 6;

        private GameUI ui;
        private GameObject panel;
        private Text titleText;
        private Text moneyText;
        private Text discountText;
        private Text detailText;
        private Text messageText;
        private Text hintText;
        private readonly List<Text> rows = new List<Text>();

        private ShopSO shop;
        private int selected;
        private string message = "";

        private void Awake()
        {
            if (GameUI.Instance != null && GameUI.Instance.gameObject != gameObject)
            {
                return;
            }

            Instance = this;
            ui = GetComponent<GameUI>();
        }

        private void Start()
        {
            if (Instance == this)
            {
                Build();
            }
        }

        private void OnEnable()
        {
            Loc.Changed += Render;
        }

        private void OnDisable()
        {
            Loc.Changed -= Render;
        }

        public IEnumerator Open(ShopSO goods)
        {
            if (goods == null || panel == null)
            {
                yield break;
            }

            GameMaster gm = GameMaster.Instance;
            bool ownState = gm != null && gm.IsInState(GameState.Exploration);
            if (ownState)
            {
                gm.SetState(GameState.Dialogue);
                GameUI.Instance.SetHudVisible(false);
            }

            shop = goods;
            selected = 0;
            message = "";
            panel.SetActive(true);
            Render();
            yield return null;

            while (true)
            {
                if (GameUI.AutoAdvance)
                {
                    yield return new WaitForSecondsRealtime(0.4f);
                    if (AutoBuy >= 0)
                    {
                        selected = Mathf.Clamp(AutoBuy, 0, shop.Entries.Count - 1);
                        AutoBuy = -1;
                        Buy();
                        yield return new WaitForSecondsRealtime(0.4f);
                    }

                    break;
                }

                if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab))
                {
                    break;
                }

                if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow) || Input.mouseScrollDelta.y < -0.1f) Move(1);
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow) || Input.mouseScrollDelta.y > 0.1f) Move(-1);
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Buy();
                yield return null;
            }

            panel.SetActive(false);
            shop = null;
            yield return null; // don't let the closing key reach the next dialogue node
            if (ownState && gm != null && gm.IsInState(GameState.Dialogue))
            {
                gm.SetState(GameState.Exploration);
                GameUI.Instance.SetHudVisible(true);
            }
        }

        private void Move(int delta)
        {
            if (shop == null || shop.Entries.Count == 0)
            {
                return;
            }

            selected = (selected + delta + shop.Entries.Count) % shop.Entries.Count;
            message = "";
            Render();
        }

        private void Buy()
        {
            if (shop == null || selected >= shop.Entries.Count)
            {
                return;
            }

            ShopSO.Entry e = shop.Entries[selected];
            InventoryManager inventory = Inventory();
            int price = shop.PriceOf(e);
            if (shop.Left(e) <= 0)
            {
                message = Loc.T("Этого больше нет.");
            }
            else if (StoryState.Money < price)
            {
                message = Loc.T("Не хватает денег.");
            }
            else if (inventory == null || !CanFit(inventory, e.item, e.amount))
            {
                message = Loc.T("Некуда положить.");
            }
            else
            {
                StoryState.AddInt(StoryState.MoneyKey, -price);
                inventory.AddItem(e.item, e.amount);
                shop.RecordPurchase(e);
                message = Loc.Format("Куплено: {0}", e.item.DisplayName);
            }

            Render();
        }

        private static InventoryManager Inventory() =>
            PersistentPlayer.Current != null ? PersistentPlayer.Current.GetComponent<InventoryManager>() : null;

        private static bool CanFit(InventoryManager inventory, ItemSO item, int amount)
        {
            int room = 0;
            foreach (InventorySlot slot in inventory.Slots)
            {
                if (slot.IsEmpty) room += item.MaxStack;
                else if (slot.item == item) room += item.MaxStack - slot.quantity;
            }

            return room >= amount;
        }

        private void Render()
        {
            if (shop == null || panel == null)
            {
                return;
            }

            InventoryManager inventory = Inventory();
            titleText.text = Loc.T(shop.Title);
            moneyText.text = "$ " + StoryState.Money;
            discountText.text = ShopSO.TradersAfraid ? Loc.T("Вас боятся: скидка 20%") : "";
            for (int i = 0; i < rows.Count; i++)
            {
                Text row = rows[i];
                if (i >= shop.Entries.Count)
                {
                    row.text = "";
                    continue;
                }

                ShopSO.Entry e = shop.Entries[i];
                bool isSelected = i == selected;
                int left = shop.Left(e);
                string name = e.item.DisplayName + (e.amount > 1 ? $" ×{e.amount}" : "");
                string stock = left == int.MaxValue ? "" : "   " + Loc.Format("(осталось {0})", left);
                row.text = $"{(isSelected ? "›" : " ")}  {name}{stock}";
                bool affordable = StoryState.Money >= shop.PriceOf(e) && left > 0;
                row.color = !affordable ? mutedColor : isSelected ? titleColor : textColor;
                row.fontStyle = isSelected ? FontStyle.Bold : FontStyle.Normal;
                Text price = row.transform.GetChild(0).GetComponent<Text>();
                price.text = "$ " + shop.PriceOf(e);
                price.color = row.color;
            }

            if (selected < shop.Entries.Count)
            {
                ShopSO.Entry e = shop.Entries[selected];
                int have = inventory != null ? inventory.CountItem(e.item) : 0;
                detailText.text = Loc.T(e.item.Description) + "\n" + Loc.Format("У вас: {0}", have);
            }

            messageText.text = message;
            hintText.text = Loc.T("W / S — товар   ·   E — купить   ·   Esc — уйти");
        }

        private void Build()
        {
            panel = new GameObject("ShopPanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(transform, false);
            var bg = panel.GetComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = false;
            GameUI.Place((RectTransform)panel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(980, 520));

            titleText = ui.MakeText("Title", panel.transform, 30, titleColor, TextAnchor.UpperLeft);
            titleText.fontStyle = FontStyle.Bold;
            GameUI.Place(titleText.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -30), new Vector2(600, 40));
            moneyText = ui.MakeText("Money", panel.transform, 28, textColor, TextAnchor.UpperRight);
            GameUI.Place(moneyText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -30), new Vector2(300, 40));
            discountText = ui.MakeText("Discount", panel.transform, 20, mutedColor, TextAnchor.UpperRight);
            GameUI.Place(discountText.rectTransform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-40, -72), new Vector2(500, 28));

            for (int i = 0; i < maxRows; i++)
            {
                Text row = ui.MakeText($"Row_{i}", panel.transform, 26, textColor, TextAnchor.MiddleLeft);
                GameUI.Place(row.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -116 - i * 40), new Vector2(900, 38));
                Text price = ui.MakeText("Price", row.transform, 26, textColor, TextAnchor.MiddleRight);
                GameUI.Place(price.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), Vector2.zero, new Vector2(200, 38));
                rows.Add(row);
            }

            detailText = ui.MakeText("Detail", panel.transform, 22, mutedColor, TextAnchor.UpperLeft);
            GameUI.Place(detailText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 74), new Vector2(900, 64));
            messageText = ui.MakeText("Message", panel.transform, 22, titleColor, TextAnchor.LowerLeft);
            GameUI.Place(messageText.rectTransform, new Vector2(0, 0), new Vector2(0, 0), new Vector2(40, 40), new Vector2(600, 30));
            hintText = ui.MakeText("Hint", panel.transform, 18, mutedColor, TextAnchor.LowerRight);
            GameUI.Place(hintText.rectTransform, new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 16), new Vector2(700, 26));

            panel.SetActive(false);
        }
    }
}
