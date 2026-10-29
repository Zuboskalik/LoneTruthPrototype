using System;
using System.Collections.Generic;
using LoneTruth.Investigation;
using LoneTruth.Inventory;
using UnityEngine;

namespace LoneTruth.Dialogue
{
    /// <summary>
    /// A conversation (design doc 3.2, 5.2): «реплика» nodes that follow each other, and «выбор»
    /// nodes with 2–4 answers. An answer has conditions (flags, an item, a clue to present) and
    /// effects (a shard or a quest outcome, flags, items given or taken, money). Played by
    /// DialogueRunner, usually from a cutscene Dialogue step.
    /// </summary>
    [CreateAssetMenu(fileName = "Dialogue", menuName = "Lone Truth/Dialogue")]
    public class DialogueSO : ScriptableObject
    {
        [Serializable]
        public class Node
        {
            public string id;
            [Tooltip("Who speaks; empty = Holt's inner voice.")]
            public string speaker;
            [TextArea(2, 6)] public string text;
            [Tooltip("Next node when there are no choices; empty ends the conversation.")]
            public string next;
            [Tooltip("Flags set when the node is shown.")]
            public string[] setFlags;
            public List<Choice> choices = new List<Choice>();
        }

        [Serializable]
        public class Choice
        {
            [TextArea(1, 4)] public string text;

            [Header("Conditions")]
            public string[] requiredFlags;
            public string[] forbiddenFlags;
            [Tooltip("Present a clue: the answer appears only when this clue flag is in the notebook.")]
            public string presentClue;
            public ItemSO requiredItem;
            public int requiredAmount = 1;
            [Tooltip("Hide the answer while its conditions fail (otherwise it is shown greyed with the hint).")]
            public bool hideWhenLocked;
            [Tooltip("Shown after a greyed answer, e.g. «нужен бинт».")]
            public string lockedHint;

            [Header("Effects")]
            [Tooltip("Resolves a quest (Notebook.CompleteQuest), which grants that outcome's shards.")]
            public string questId;
            public string outcomeId;
            [Tooltip("A shard given directly (when the answer is not a quest outcome).")]
            public bool givesShard;
            public ShardType shard;
            public int shards = 1;
            public string[] setFlags;
            public string[] clearFlags;
            public ItemSO giveItem;
            public int giveAmount = 1;
            public ItemSO takeItem;
            public int takeAmount = 1;
            public int money;

            [Tooltip("Node to go to; empty ends the conversation.")]
            public string next;
        }

        [SerializeField] private string startNode;
        [SerializeField] private List<Node> nodes = new List<Node>();

        public string StartNode => startNode;
        public IReadOnlyList<Node> Nodes => nodes;

        public Node Find(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return null;
            }

            foreach (Node n in nodes)
            {
                if (n.id == id)
                {
                    return n;
                }
            }

            Debug.LogWarning($"[{name}] No dialogue node '{id}'.");
            return null;
        }

        public void Configure(string start, List<Node> dialogueNodes)
        {
            startNode = start;
            nodes = dialogueNodes;
        }
    }
}
