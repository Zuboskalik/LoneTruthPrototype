using System;
using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Investigation;
using LoneTruth.Inventory;
using UnityEngine;

namespace LoneTruth.Shop
{
    /// <summary>
    /// A trader's goods (design doc: Ruth «Магазин»). Prices are in dollars (StoryState money);
    /// a limited stock is remembered in StoryState so it survives scene loads. Despair's soft bonus
    /// makes traders afraid: 20% off from its first level (design doc, section 4).
    /// </summary>
    [CreateAssetMenu(fileName = "Shop", menuName = "Lone Truth/Shop")]
    public class ShopSO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public ItemSO item;
            [Tooltip("How many come per purchase (e.g. a box of 6 rounds).")]
            public int amount = 1;
            public int price = 1;
            [Tooltip("Purchases available; -1 = unlimited.")]
            public int stock = -1;
            public string[] requiredFlags;
            public string[] forbiddenFlags;
            public bool hideIfInInventory;
            public bool hideWhenSoldOut;
            public string setFlagOnPurchase;
        }

        public const float DespairDiscount = 0.2f;

        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public string Id => id;
        public string Title => title;
        public IReadOnlyList<Entry> Entries => entries;

        public static bool TradersAfraid => ShardManager.BonusLevel(ShardType.Despair) >= 1;

        public int PriceOf(Entry e) =>
            TradersAfraid ? Mathf.Max(1, Mathf.RoundToInt(e.price * (1f - DespairDiscount))) : e.price;

        public int Left(Entry e) => e.stock < 0 ? int.MaxValue : Mathf.Max(0, e.stock - StoryState.GetInt(BoughtKey(e)));

        public bool IsEntryVisible(Entry e, InventoryManager inventory = null)
        {
            if (e == null || e.item == null)
            {
                return false;
            }

            if (e.requiredFlags != null && e.requiredFlags.Length > 0)
            {
                foreach (string f in e.requiredFlags)
                {
                    if (!string.IsNullOrEmpty(f) && !StoryState.Has(f))
                    {
                        return false;
                    }
                }
            }

            if (e.forbiddenFlags != null && e.forbiddenFlags.Length > 0)
            {
                foreach (string f in e.forbiddenFlags)
                {
                    if (!string.IsNullOrEmpty(f) && StoryState.Has(f))
                    {
                        return false;
                    }
                }
            }

            if (e.hideIfInInventory && inventory != null && inventory.CountItem(e.item) > 0)
            {
                return false;
            }

            if (e.hideWhenSoldOut && e.stock >= 0 && Left(e) <= 0)
            {
                return false;
            }

            // Special plot logic for crowbar:
            // 1. Only after Liam discussion (q1_1_postman) adds journal entry
            // 2. Unavailable/hidden if player acquired one (Jesse's quest or bought or in inventory) or already used (grate_open)
            if (e.item.Id == "crowbar")
            {
                if (!StoryState.Has(StoryFlags.Quest11Postman))
                {
                    return false;
                }

                if (StoryState.Has(StoryFlags.CrowbarTaken) ||
                    StoryState.Has(StoryFlags.CrowbarBought) ||
                    StoryState.Has(StoryFlags.GrateOpen) ||
                    (inventory != null && inventory.CountItem(e.item) > 0))
                {
                    return false;
                }
            }

            return true;
        }

        public List<Entry> GetVisibleEntries(InventoryManager inventory = null)
        {
            var list = new List<Entry>();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (IsEntryVisible(entries[i], inventory))
                    {
                        list.Add(entries[i]);
                    }
                }
            }

            return list;
        }

        public void RecordPurchase(Entry e)
        {
            StoryState.AddInt(BoughtKey(e), 1);
            if (!string.IsNullOrEmpty(e.setFlagOnPurchase))
            {
                StoryState.Set(e.setFlagOnPurchase);
            }

            if (e.item != null && e.item.Id == "crowbar")
            {
                StoryState.Set(StoryFlags.CrowbarBought);
                StoryState.Set(StoryFlags.CrowbarTaken);
            }
        }

        private string BoughtKey(Entry e) => $"shop_{id}_{(e.item != null ? e.item.Id : "none")}";

        public void Configure(string shopId, string shopTitle, List<Entry> goods)
        {
            id = shopId;
            title = shopTitle;
            entries = goods;
        }
    }
}
