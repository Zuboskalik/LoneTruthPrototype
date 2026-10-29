using System.Collections;
using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Investigation;
using LoneTruth.Inventory;
using LoneTruth.Player;
using LoneTruth.UI;
using UnityEngine;

namespace LoneTruth.Dialogue
{
    /// <summary>How one answer is offered right now.</summary>
    public struct ChoiceView
    {
        public string Text;          // untranslated answer text
        public string ClueTitle;     // set for «present a clue» answers
        public bool Locked;          // shown greyed, cannot be picked
        public string LockedHint;    // e.g. «нужен бинт»
        public bool HasShard;
        public ShardType Shard;
    }

    /// <summary>
    /// Plays a DialogueSO: shows lines, offers the answers whose conditions hold, applies the
    /// chosen answer's effects and follows it to the next node.
    /// </summary>
    public static class DialogueRunner
    {
        public const int MaxChoices = 4;

        public static bool IsRunning { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            IsRunning = false;
        }

        public static IEnumerator Run(DialogueSO dialogue)
        {
            DialogueUI ui = DialogueUI.Instance;
            if (dialogue == null || ui == null)
            {
                yield break;
            }

            // Outside a cutscene the conversation owns the input (GameState.Dialogue).
            GameMaster gm = GameMaster.Instance;
            bool ownState = gm != null && gm.IsInState(GameState.Exploration);
            if (ownState)
            {
                gm.SetState(GameState.Dialogue);
                GameUI.Instance.SetHudVisible(false);
            }

            IsRunning = true;
            DialogueSO.Node node = dialogue.Find(dialogue.StartNode);
            while (node != null)
            {
                SetFlags(node.setFlags, true);
                var offered = new List<DialogueSO.Choice>();
                var views = new List<ChoiceView>();
                foreach (DialogueSO.Choice choice in node.choices)
                {
                    if (Describe(choice, out ChoiceView view))
                    {
                        offered.Add(choice);
                        views.Add(view);
                    }
                }

                // The panel holds 2–4 answers (design doc 3.2).
                if (offered.Count > MaxChoices)
                {
                    Debug.LogWarning($"[{dialogue.name}] Node '{node.id}' offers {offered.Count} answers; only {MaxChoices} are shown.");
                    offered.RemoveRange(MaxChoices, offered.Count - MaxChoices);
                    views.RemoveRange(MaxChoices, views.Count - MaxChoices);
                }

                LoneTruth.Story.CutsceneActor.SetTalking(node.speaker, true);
                if (offered.Count == 0)
                {
                    yield return ui.ShowLine(node.speaker, node.text);
                    LoneTruth.Story.CutsceneActor.SetTalking(node.speaker, false);
                    node = dialogue.Find(node.next);
                    continue;
                }

                int picked = -1;
                yield return ui.ShowChoices(node.speaker, node.text, views, i => picked = i);
                LoneTruth.Story.CutsceneActor.SetTalking(node.speaker, false);
                DialogueSO.Choice chosen = offered[Mathf.Clamp(picked, 0, offered.Count - 1)];
                Apply(chosen);
                if (chosen.openShop != null && ShopUI.Instance != null)
                {
                    ui.Hide();
                    yield return ShopUI.Instance.Open(chosen.openShop);
                }

                node = dialogue.Find(chosen.next);
            }

            ui.Hide();
            IsRunning = false;
            if (ownState && gm != null && gm.IsInState(GameState.Dialogue))
            {
                gm.SetState(GameState.Exploration);
                GameUI.Instance.SetHudVisible(true);
            }
        }

        /// <summary>False when the answer is hidden; otherwise how to show it.</summary>
        public static bool Describe(DialogueSO.Choice choice, out ChoiceView view)
        {
            view = new ChoiceView { Text = choice.text, LockedHint = choice.lockedHint };

            // A clue answer exists only once the clue is in the notebook.
            if (!string.IsNullOrEmpty(choice.presentClue))
            {
                if (!Notebook.HasClue(choice.presentClue))
                {
                    return false;
                }

                view.ClueTitle = ClueTitle(choice.presentClue);
            }

            bool met = StoryState.Check(choice.requiredFlags, choice.forbiddenFlags) && HasItem(choice.requiredItem, choice.requiredAmount);
            if (!met && choice.hideWhenLocked)
            {
                return false;
            }

            view.Locked = !met;
            view.HasShard = ShardOf(choice, out view.Shard);
            return true;
        }

        private static void Apply(DialogueSO.Choice c)
        {
            InventoryManager inventory = PersistentPlayer.Current != null ? PersistentPlayer.Current.GetComponent<InventoryManager>() : null;
            if (c.takeItem != null && inventory != null)
            {
                inventory.RemoveItem(c.takeItem, Mathf.Max(1, c.takeAmount));
            }

            if (c.giveItem != null && inventory != null)
            {
                inventory.AddItem(c.giveItem, Mathf.Max(1, c.giveAmount));
                if (GameUI.Instance != null)
                {
                    GameUI.Instance.Toast(Loc.Format("Получено: {0}", c.giveItem.DisplayName));
                }
            }

            if (c.money != 0)
            {
                StoryState.AddInt(StoryState.MoneyKey, c.money);
            }

            SetFlags(c.clearFlags, false);
            SetFlags(c.setFlags, true);

            if (!string.IsNullOrEmpty(c.questId))
            {
                Notebook.CompleteQuest(c.questId, c.outcomeId);
            }
            else if (c.givesShard)
            {
                ShardManager.AddShard(c.shard, Mathf.Max(1, c.shards));
            }
        }

        private static bool ShardOf(DialogueSO.Choice c, out ShardType shard)
        {
            shard = c.shard;
            if (!string.IsNullOrEmpty(c.questId))
            {
                QuestSO quest = Notebook.FindQuest(c.questId);
                if (quest != null)
                {
                    foreach (QuestSO.Outcome o in quest.Outcomes)
                    {
                        if (o.id == c.outcomeId)
                        {
                            shard = o.shard;
                            return o.shards > 0;
                        }
                    }
                }

                return false;
            }

            return c.givesShard;
        }

        private static bool HasItem(ItemSO item, int amount)
        {
            if (item == null)
            {
                return true;
            }

            InventoryManager inventory = PersistentPlayer.Current != null ? PersistentPlayer.Current.GetComponent<InventoryManager>() : null;
            return inventory != null && inventory.CountItem(item) >= Mathf.Max(1, amount);
        }

        private static string ClueTitle(string clueFlag)
        {
            if (Notebook.Chapter != null)
            {
                foreach (ClueSO clue in Notebook.Chapter.Clues)
                {
                    if (clue != null && clue.Flag == clueFlag)
                    {
                        return clue.Title;
                    }
                }
            }

            return clueFlag;
        }

        private static void SetFlags(string[] flags, bool value)
        {
            if (flags == null)
            {
                return;
            }

            foreach (string f in flags)
            {
                StoryState.Set(f, value);
            }
        }
    }
}
