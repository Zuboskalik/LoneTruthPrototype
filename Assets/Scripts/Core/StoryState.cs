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

        /// <summary>Copies all story state out for a save (ints as "key=value").</summary>
        public static void Export(List<string> flagsOut, List<string> intsOut)
        {
            flagsOut.AddRange(flags);
            foreach (KeyValuePair<string, int> pair in ints)
            {
                intsOut.Add(pair.Key + "=" + pair.Value);
            }
        }

        /// <summary>Replaces all story state with a saved one.</summary>
        public static void Import(string[] savedFlags, string[] savedInts)
        {
            flags.Clear();
            ints.Clear();
            foreach (string f in savedFlags ?? new string[0])
            {
                flags.Add(f);
            }

            foreach (string entry in savedInts ?? new string[0])
            {
                int eq = entry.LastIndexOf('=');
                ints[entry.Substring(0, eq)] = int.Parse(entry.Substring(eq + 1));
            }

            Changed?.Invoke();
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

        public static void SetInt(string key, int value)
        {
            if (GetInt(key) == value && ints.ContainsKey(key))
            {
                return;
            }

            ints[key] = value;
            Changed?.Invoke();
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

        // Day 1, free investigation.
        public const string SheriffReport = "sheriff_report";          // Dolan handed over the report (the day's one required beat)
        public const string ClueCollapseReport = "clue_collapse_report";
        public const string ClueNamePlaques = "clue_name_plaques";
        public const string ClueThomasLetters = "clue_thomas_letters";
        public const string ClueThomasOldLetter = "clue_thomas_old_letter";
        public const string Quest11Started = "q1_1_started";             // Agnes showed the letters
        public const string Quest12FoundJesse = "q1_2_found_jesse";      // first contact in the shed
        public const string Quest13Started = "q1_3_started";             // Father Matthew asked about the bell

        // Night 1.
        public const string Night1 = "night1";                           // the bell has rung; the night after day 1
        public const string BedRest = "bed_rest";                        // the bed's menu: rest was chosen
        public const string BedSave = "bed_save";                        // the bed's menu: save was chosen
        public const string FlareGiven = "flare_given";                  // Ruth handed over the first flare
        public const string FirstHollowDead = "first_hollow_dead";       // the night's one required beat
        public const string Counterweight1 = "counterweight_1";
        public const string Counterweight2 = "counterweight_2";
        public const string Counterweight3 = "counterweight_3";
        public const string AmosNote = "clue_amos_note";
        public const string MatthewGone = "matthew_gone";                // quest 1.3, Despair: he and the bell-ringer left
        public const string EdgarGraveKnown = "edgar_grave_known";       // quest 1.3, Truth: the page of 1906
        public const string SpadeTaken = "spade_taken";
        public const string ClueHarmonica = "clue_harmonica";
        public const string AlleyHidden = "alley_hidden";                // Holt is in the shadow by Agnes's window
        public const string Quest11Postman = "q1_1_postman";             // Liam caught with a letter
        public const string GrateOpen = "grate_open";
        public const string ClueFreshLetter = "clue_fresh_letter";
        public const string Quest11Knock = "q1_1_knock";                 // the knocking from below
        public const string ClueThomasLamp = "clue_thomas_lamp";
        public const string VentSealed = "VENT_SEALED";                  // quest 1.1, Despair (chapter 2 reads it)
        public const string JesseMoved = "jesse_moved";                  // Father Matthew took Jesse into the vestry
        public const string ArchiveKeyTaken = "archive_key_taken";
        public const string CrowbarTaken = "crowbar_taken";
        public const string CrowbarBought = "crowbar_bought";

        /// <summary>Runtime state, not story: the revolver is drawn (people react to it).</summary>
        public const string WeaponDrawn = "weapon_drawn";
        public const string JesseCalm = "jesse_calm";                    // Jesse lowered the rifle: Holt named Agnes
    }
}
