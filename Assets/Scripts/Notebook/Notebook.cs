using System;
using System.Collections.Generic;
using System.Linq;
using LoneTruth.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LoneTruth.Investigation
{
    /// <summary>
    /// The detective's notebook as game logic: which chapter it shows, visited places, quest
    /// outcomes and comparing clues. Everything is derived from StoryState flags, so cutscenes
    /// and dialogue only ever set flags (or call CompleteQuest / AddShard).
    /// </summary>
    public static class Notebook
    {
        private const string VisitedPrefix = "visited_";

        /// <summary>Raised when a new clue, person, quest stage, deduction or place becomes known.</summary>
        public static event Action Updated;

        public static ChapterNotebookSO Chapter { get; private set; }

        private static int knownEntries = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Updated = null;
            Chapter = null;
            knownEntries = -1;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Hook()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            StoryState.Changed -= OnStoryChanged;
            StoryState.Changed += OnStoryChanged;
            MarkVisited(SceneManager.GetActiveScene().name);
        }

        public static void SetChapter(ChapterNotebookSO chapter)
        {
            Chapter = chapter;
            knownEntries = CountKnown();
        }

        // ---------------------------------------------------------------- queries

        public static bool HasClue(string clueFlag) => StoryState.Has(clueFlag);

        public static bool IsVisited(string scene) => !string.IsNullOrEmpty(scene) && StoryState.Has(VisitedPrefix + scene);

        public static string CurrentScene => SceneManager.GetActiveScene().name;

        public static IEnumerable<ClueSO> KnownClues() =>
            Chapter == null ? Enumerable.Empty<ClueSO>() : Chapter.Clues.Where(c => c != null && c.IsKnown);

        public static IEnumerable<PersonSO> KnownPeople() =>
            Chapter == null ? Enumerable.Empty<PersonSO>() : Chapter.People.Where(p => p != null && p.IsKnown);

        /// <summary>Visible quests: main quest first, then active, then finished.</summary>
        public static IEnumerable<QuestSO> VisibleQuests() =>
            Chapter == null
                ? Enumerable.Empty<QuestSO>()
                : Chapter.Quests.Where(q => q != null && q.Status != QuestStatus.Hidden)
                    .OrderBy(q => q.IsMainQuest ? 0 : 1)
                    .ThenBy(q => q.Status == QuestStatus.Active ? 0 : 1);

        public static IEnumerable<DeductionSO> ConcludedDeductions() =>
            Chapter == null ? Enumerable.Empty<DeductionSO>() : Chapter.Deductions.Where(d => d != null && d.IsConcluded);

        public static QuestSO FindQuest(string questId) =>
            Chapter == null ? null : Chapter.Quests.FirstOrDefault(q => q != null && q.Id == questId);

        // ---------------------------------------------------------------- actions

        /// <summary>
        /// Resolves a quest with one of its outcomes: sets the outcome flag and grants its shards.
        /// Does nothing if the quest is already resolved.
        /// </summary>
        public static bool CompleteQuest(QuestSO quest, string outcomeId)
        {
            if (quest == null || quest.ChosenOutcome != null)
            {
                return false;
            }

            QuestSO.Outcome outcome = quest.Outcomes.FirstOrDefault(o => o.id == outcomeId);
            if (outcome == null)
            {
                Debug.LogWarning($"[Notebook] Quest {quest.Id} has no outcome '{outcomeId}'.");
                return false;
            }

            StoryState.Set(quest.OutcomeFlag(outcome.id));
            ShardManager.AddShard(outcome.shard, Mathf.Max(0, outcome.shards));
            return true;
        }

        public static bool CompleteQuest(string questId, string outcomeId)
        {
            QuestSO quest = FindQuest(questId);
            if (quest == null)
            {
                Debug.LogWarning($"[Notebook] No quest '{questId}' in the current chapter.");
                return false;
            }

            return CompleteQuest(quest, outcomeId);
        }

        /// <summary>
        /// Compares the marked clues. Returns the deduction they make (already concluded or new),
        /// or null if they do not fit together. A new deduction sets its flag and key truth.
        /// </summary>
        public static DeductionSO Compare(ICollection<ClueSO> marked, out bool isNew)
        {
            isNew = false;
            if (Chapter == null || marked == null || marked.Count < 2)
            {
                return null;
            }

            foreach (DeductionSO d in Chapter.Deductions)
            {
                if (d == null || d.Clues.Count != marked.Count || !d.Clues.All(marked.Contains))
                {
                    continue;
                }

                if (!d.IsConcluded)
                {
                    isNew = true;
                    StoryState.Set(d.ResultFlag);
                    ShardManager.AddKeystone(d.KeystoneId);
                }

                return d;
            }

            return null;
        }

        // ---------------------------------------------------------------- bookkeeping

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => MarkVisited(scene.name);

        private static void MarkVisited(string scene)
        {
            if (!string.IsNullOrEmpty(scene))
            {
                StoryState.Set(VisitedPrefix + scene);
            }
        }

        private static void OnStoryChanged()
        {
            int count = CountKnown();
            if (knownEntries >= 0 && count > knownEntries)
            {
                Updated?.Invoke();
            }

            knownEntries = count;
        }

        private static int CountKnown()
        {
            if (Chapter == null)
            {
                return -1;
            }

            int count = KnownClues().Count() + ConcludedDeductions().Count();
            foreach (PersonSO p in KnownPeople())
            {
                count += 1 + p.Notes.Count(n => StoryState.Has(n.flag));
            }

            foreach (QuestSO q in Chapter.Quests)
            {
                if (q != null && q.Status != QuestStatus.Hidden)
                {
                    count += q.ReachedStages().Count() + (q.Status == QuestStatus.Active ? 0 : 1);
                }
            }

            return count;
        }
    }
}
