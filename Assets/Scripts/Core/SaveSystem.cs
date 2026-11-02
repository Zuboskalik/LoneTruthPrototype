using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace LoneTruth.Core
{
    /// <summary>
    /// What a save holds, and no more: story flags and numbers (everything the story knows, killed
    /// enemies, taken items, money and shards included), the inventory, health, rounds in the
    /// cylinder, the objective. Saving happens only at the bed, so the location is fixed by Spawn.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public int v = 1;
        public string scene;
        public string spawn;
        public string goal;
        public long time;      // UTC ticks, for finding the latest save
        public int played;     // seconds of play, for the slot list
        public int hp;
        public int rounds;
        public string[] flags;
        public string[] ints;  // "key=value"
        public string[] items; // "id*quantity"; empty slots are dropped
    }

    /// <summary>
    /// Three save slots, on disk as small JSON files in persistentDataPath (in a web build, in
    /// PlayerPrefs, which the browser keeps reliably). Also remembers the state to roll back to
    /// when Holt dies: the last save, load or rest of this session.
    /// </summary>
    public static class SaveSystem
    {
        public const int SlotCount = 3;

        /// <summary>The state a death rolls back to; null until the first save, load or rest.</summary>
        public static SaveData LastCheckpoint { get; private set; }

        /// <summary>Time played this game, carried in the saves.</summary>
        public static float PlaySeconds;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            LastCheckpoint = null;
            PlaySeconds = 0f;
        }

        public static void SetCheckpoint(SaveData data)
        {
            LastCheckpoint = data;
        }

        public static void Write(int slot, SaveData data)
        {
            string json = JsonUtility.ToJson(data);
#if UNITY_WEBGL && !UNITY_EDITOR
            PlayerPrefs.SetString(Key(slot), json);
            PlayerPrefs.Save();
#else
            File.WriteAllText(FilePath(slot), json);
#endif
            LastCheckpoint = data;
        }

        public static SaveData Read(int slot)
        {
            try
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                string json = PlayerPrefs.GetString(Key(slot), "");
#else
                string path = FilePath(slot);
                string json = File.Exists(path) ? File.ReadAllText(path) : "";
#endif
                return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] Slot {slot} cannot be read: {e.Message}");
                return null;
            }
        }

        public static bool AnySaves()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (Read(i) != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>The newest of the three slots, or null.</summary>
        public static SaveData Latest()
        {
            SaveData latest = null;
            for (int i = 0; i < SlotCount; i++)
            {
                SaveData data = Read(i);
                if (data != null && (latest == null || data.time > latest.time))
                {
                    latest = data;
                }
            }

            return latest;
        }

        /// <summary>Slot-list label for a save, e.g. «0:05:05 - Глава 1 - День 1».</summary>
        public static string Describe(SaveData data)
        {
            var flags = new HashSet<string>(data.flags ?? new string[0]);
            string when = flags.Contains(StoryFlags.Night1) ? "Ночь 1"
                : flags.Contains(StoryFlags.Day1Morning) ? "День 1"
                : "Пролог";
            TimeSpan played = TimeSpan.FromSeconds(data.played);
            return $"{(int)played.TotalHours}:{played.Minutes:00}:{played.Seconds:00} - {Loc.T("Глава 1")} - {Loc.T(when)}";
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        private static string Key(int slot) => "save_slot_" + slot;
#else
        private static string FilePath(int slot) => Path.Combine(Application.persistentDataPath, $"slot{slot}.sav");
#endif
    }
}
