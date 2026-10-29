using System;
using System.Collections.Generic;
using LoneTruth.Core;
using UnityEngine;

namespace LoneTruth.Investigation
{
    public enum QuestStatus
    {
        Hidden,   // no stage reached yet
        Active,
        Done,     // an outcome was chosen
        Failed
    }

    /// <summary>
    /// A quest (main story or side quest, design doc 5.2). Stages unlock by story flags; the
    /// journal shows every reached stage. Each outcome sets its own flag and grants shards
    /// (1 for a side-quest choice, 2 for a story choice) — see Notebook.CompleteQuest.
    /// </summary>
    [CreateAssetMenu(fileName = "Quest", menuName = "Lone Truth/Notebook/Quest")]
    public class QuestSO : ScriptableObject
    {
        [Serializable]
        public class Stage
        {
            [Tooltip("Flag that reaches this stage; empty = reached as soon as the quest is visible.")]
            public string flag;
            [TextArea(2, 6)] public string text;
        }

        [Serializable]
        public class Outcome
        {
            public string id;
            public ShardType shard;
            public int shards = 1;
            [Tooltip("Optional: flag required for this choice to be offered (e.g. a clue for Truth).")]
            public string requiredFlag;
            [TextArea(2, 6)] public string text;
        }

        [SerializeField] private string id;
        [SerializeField] private string title;
        [SerializeField] private bool isMainQuest;
        [Tooltip("Flag that fails the quest (e.g. Agnes stops opening her door).")]
        [SerializeField] private string failFlag;
        [SerializeField] private List<Stage> stages = new List<Stage>();
        [SerializeField] private List<Outcome> outcomes = new List<Outcome>();

        public string Id => id;
        public string Title => title;
        public bool IsMainQuest => isMainQuest;
        public IReadOnlyList<Stage> Stages => stages;
        public IReadOnlyList<Outcome> Outcomes => outcomes;

        public string OutcomeFlag(string outcomeId) => $"quest_{id}_{outcomeId}";

        public Outcome ChosenOutcome
        {
            get
            {
                foreach (Outcome o in outcomes)
                {
                    if (StoryState.Has(OutcomeFlag(o.id)))
                    {
                        return o;
                    }
                }

                return null;
            }
        }

        public QuestStatus Status
        {
            get
            {
                if (ChosenOutcome != null) return QuestStatus.Done;
                if (!string.IsNullOrEmpty(failFlag) && StoryState.Has(failFlag)) return QuestStatus.Failed;
                foreach (Stage s in stages)
                {
                    if (!string.IsNullOrEmpty(s.flag) && StoryState.Has(s.flag)) return QuestStatus.Active;
                }

                return stages.Count > 0 && string.IsNullOrEmpty(stages[0].flag) ? QuestStatus.Active : QuestStatus.Hidden;
            }
        }

        /// <summary>Stages reached so far, in order.</summary>
        public IEnumerable<Stage> ReachedStages()
        {
            foreach (Stage s in stages)
            {
                if (string.IsNullOrEmpty(s.flag) || StoryState.Has(s.flag))
                {
                    yield return s;
                }
            }
        }

        public void Configure(string questId, string questTitle, bool main, string fail)
        {
            id = questId;
            title = questTitle;
            isMainQuest = main;
            failFlag = fail;
            stages.Clear();
            outcomes.Clear();
        }

        public void AddStage(string flag, string text) => stages.Add(new Stage { flag = flag, text = text });

        public void AddOutcome(string outcomeId, ShardType shard, int shardCount, string required, string text) =>
            outcomes.Add(new Outcome { id = outcomeId, shard = shard, shards = shardCount, requiredFlag = required, text = text });
    }
}
