using System.Collections;
using UnityEngine;

namespace LoneTruth.Interaction
{
    /// <summary>
    /// A transition point whose target location is not built yet. It keeps its map id and
    /// target id so the real door can be wired later, but it never loads anything: it only
    /// shows a short "closed" line through the interaction prompt.
    /// </summary>
    public class TransitionStub : MonoBehaviour, IInteractable
    {
        [Header("Transition")]
        [SerializeField] private string transitionId = "1-02_to_1-03";
        [SerializeField] private string targetLocationId = "1-03";
        [SerializeField] private string displayName = "Door";

        [Header("Prompts")]
        [SerializeField] private string closedPrompt = "Closed";
        [SerializeField] private string closedLine = "Locked.";
        [SerializeField] private float lineDuration = 2f;

        [Header("Audio")]
        [SerializeField] private AudioClip lockedSound;

        private bool showingLine;
        private Coroutine lineRoutine;

        public string TransitionId => transitionId;
        public string TargetLocationId => targetLocationId;

        public void Interact(GameObject interactor)
        {
            if (lockedSound != null)
            {
                AudioSource.PlayClipAtPoint(lockedSound, transform.position);
            }

            if (lineRoutine != null)
            {
                StopCoroutine(lineRoutine);
            }

            lineRoutine = StartCoroutine(ShowLine());
        }

        public string GetInteractionPrompt()
        {
            return showingLine ? closedLine : displayName + " — " + closedPrompt;
        }

        private IEnumerator ShowLine()
        {
            showingLine = true;
            yield return new WaitForSeconds(lineDuration);
            showingLine = false;
            lineRoutine = null;
        }
    }
}
