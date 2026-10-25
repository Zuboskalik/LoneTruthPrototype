using System;
using System.Collections.Generic;
using LoneTruth.Core;
using LoneTruth.Interaction;
using LoneTruth.UI;
using UnityEngine;

namespace LoneTruth.Story
{
    /// <summary>
    /// An NPC or object that plays a conversation / inspection on E. The first branch whose flags
    /// match (and whose sequence is not done) plays; otherwise the fallback line is shown.
    /// </summary>
    public class CutsceneInteractable : MonoBehaviour, IInteractable
    {
        [Serializable]
        public class Branch
        {
            public string[] requiredFlags;
            public string[] forbiddenFlags;
            public CutsceneSequence sequence;
        }

        [SerializeField] private string prompt = "Поговорить";
        [SerializeField] private List<Branch> branches = new List<Branch>();
        [SerializeField] private string fallbackSpeaker;
        [SerializeField] private string fallbackLine;

        /// <summary>Editor/builder setup.</summary>
        public void Configure(string promptText, string speaker, string line)
        {
            prompt = promptText;
            fallbackSpeaker = speaker;
            fallbackLine = line;
            branches.Clear();
        }

        public void AddBranch(CutsceneSequence sequence, string[] required, string[] forbidden)
        {
            branches.Add(new Branch { sequence = sequence, requiredFlags = required, forbiddenFlags = forbidden });
        }

        public void Interact(GameObject interactor)
        {
            foreach (Branch b in branches)
            {
                if (b.sequence != null && !b.sequence.IsDone && StoryState.Check(b.requiredFlags, b.forbiddenFlags))
                {
                    b.sequence.Play();
                    return;
                }
            }

            if (!string.IsNullOrEmpty(fallbackLine) && GameUI.Instance != null)
            {
                GameUI.Instance.Toast(string.IsNullOrEmpty(fallbackSpeaker) ? fallbackLine : fallbackSpeaker + ": " + fallbackLine);
            }
        }

        public string GetInteractionPrompt()
        {
            return prompt;
        }
    }
}
