using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoneTruth.Core
{
    /// <summary>
    /// Story flags and counters for the whole session (the StoryFlags system from the design doc).
    /// Static, so it survives scene loads; reset whenever Play mode starts.
    /// </summary>
    public static class StoryState
    {
        private static readonly HashSet<string> flags = new HashSet<string>();
        private static readonly Dictionary<string, int> ints = new Dictionary<string, int>();

        public static event Action Changed;

        public const string MoneyKey = "money";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            flags.Clear();
            ints.Clear();
            Changed = null;
        }

        public static bool Has(string flag)
        {
            return !string.IsNullOrEmpty(flag) && flags.Contains(flag);
        }

        public static void Set(string flag, bool value = true)
        {
            if (string.IsNullOrEmpty(flag))
            {
                return;
            }

            bool changed = value ? flags.Add(flag) : flags.Remove(flag);
            if (changed)
            {
                Changed?.Invoke();
            }
        }

        public static int GetInt(string key)
        {
            return ints.TryGetValue(key, out int v) ? v : 0;
        }

        public static void AddInt(string key, int amount)
        {
            ints[key] = GetInt(key) + amount;
            Changed?.Invoke();
        }

        public static int Money => GetInt(MoneyKey);

        /// <summary>True when every required flag is set and none of the forbidden ones.</summary>
        public static bool Check(string[] required, string[] forbidden)
        {
            if (required != null)
            {
                foreach (string f in required)
                {
                    if (!string.IsNullOrEmpty(f) && !Has(f))
                    {
                        return false;
                    }
                }
            }

            if (forbidden != null)
            {
                foreach (string f in forbidden)
                {
                    if (Has(f))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    /// <summary>Flag names used by chapter 1, so typos fail at compile time.</summary>
    public static class StoryFlags
    {
        public const string PrologueDone = "prologue_done";
        public const string MetCallahan = "met_callahan";
        public const string EscortToSaloon = "escort_to_saloon";
        public const string EscortDone = "escort_done";
        public const string SaloonCheckIn = "saloon_checkin";
        public const string TimeDay = "time_day";
        public const string Day1Morning = "day1_morning";
        public const string RuthMorning = "ruth_morning";
        public const string OfficeMeeting = "office_meeting";
        public const string FreeInvestigationDay1 = "free_investigation_day1";
        public const string RuthRumorBarn = "ruth_rumor_barn";
        public const string ClueTelegrams = "clue_unsent_telegrams";
        public const string CluePortrait = "clue_portrait";
    }
}
