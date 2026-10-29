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
        }

        public const float DespairDiscount = 0.2f;

        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private List<Entry> entries = new List<Entry>();

        public string Title => title;
        public IReadOnlyList<Entry> Entries => entries;

        public static bool TradersAfraid => ShardManager.BonusLevel(ShardType.Despair) >= 1;

        public int PriceOf(Entry e) =>
            TradersAfraid ? Mathf.Max(1, Mathf.RoundToInt(e.price * (1f - DespairDiscount))) : e.price;

        public int Left(Entry e) => e.stock < 0 ? int.MaxValue : Mathf.Max(0, e.stock - StoryState.GetInt(BoughtKey(e)));

        public void RecordPurchase(Entry e) => StoryState.AddInt(BoughtKey(e), 1);

        private string BoughtKey(Entry e) => $"shop_{id}_{(e.item != null ? e.item.Id : "none")}";

        public void Configure(string shopId, string shopTitle, List<Entry> goods)
        {
            id = shopId;
            title = shopTitle;
            entries = goods;
        }
    }
}
