using System;
using LoneTruth.Core;

namespace LoneTruth.Investigation
{
    public enum ShardType
    {
        Hope,    // amber: help, spare, give up your own resource
        Despair, // violet-black: profit, threat, reprisal, indifference
        Truth    // cold white: present a clue, ask the awkward question
    }

    public enum Ending
    {
        Hope,    // «Рассвет»
        Despair, // «Новый хозяин» — always open
        Truth    // «Одинокая истина»
    }

    /// <summary>
    /// Shards of the Heart (design doc, section 4). Counts, the order they were gained in (for
    /// the mosaic in the father's watch) and key truths live in StoryState, so they travel with
    /// the other story flags. The HUD watch listens to ShardGained; numbers are shown only in
    /// the notebook.
    /// </summary>
    public static class ShardManager
    {
        public const int KeystonesRequired = 3;  // one key truth per chapter
        public const int TruthThreshold = 7;     // Truth shards needed for the true ending
        public const int ShardsPerBonusLevel = 3;
        public const int MaxBonusLevel = 3;

        private const string CountPrefix = "shard_";
        private const string SequenceKey = "shard_total";
        private const string SequencePrefix = "shard_seq_";
        private const string KeystonePrefix = "keystone_";
        private const string KeystoneCountKey = "keystones";

        /// <summary>(type, new total of that type) — after the shard is recorded.</summary>
        public static event Action<ShardType, int> ShardGained;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            ShardGained = null;
        }

        public static int Get(ShardType type) => StoryState.GetInt(CountPrefix + type);

        /// <summary>Total shards gained, of any type.</summary>
        public static int Total => StoryState.GetInt(SequenceKey);

        /// <summary>The type of the n-th shard gained (0-based), for the mosaic.</summary>
        public static ShardType GetInOrder(int index) => (ShardType)StoryState.GetInt(SequencePrefix + index);

        public static void AddShard(ShardType type, int amount = 1)
        {
            for (int i = 0; i < amount; i++)
            {
                StoryState.SetInt(SequencePrefix + Total, (int)type);
                StoryState.AddInt(SequenceKey, 1);
                StoryState.AddInt(CountPrefix + type, 1);
                ShardGained?.Invoke(type, Get(type));
            }
        }

        /// <summary>A key truth (one per chapter): +1 Truth and a flag, once.</summary>
        public static bool AddKeystone(string id)
        {
            if (string.IsNullOrEmpty(id) || HasKeystone(id))
            {
                return false;
            }

            StoryState.Set(KeystonePrefix + id);
            StoryState.AddInt(KeystoneCountKey, 1);
            AddShard(ShardType.Truth);
            return true;
        }

        public static bool HasKeystone(string id) => StoryState.Has(KeystonePrefix + id);

        public static int KeystoneCount => StoryState.GetInt(KeystoneCountKey);

        /// <summary>Soft bonus level 0–3: one level per three shards of a type.</summary>
        public static int BonusLevel(ShardType type) =>
            Math.Min(MaxBonusLevel, Get(type) / ShardsPerBonusLevel);

        public static bool IsEndingAvailable(Ending ending)
        {
            switch (ending)
            {
                case Ending.Truth:
                    return KeystoneCount >= KeystonesRequired && Get(ShardType.Truth) >= TruthThreshold;
                case Ending.Hope:
                    return Get(ShardType.Hope) >= Get(ShardType.Despair);
                default:
                    return true;
            }
        }
    }
}
