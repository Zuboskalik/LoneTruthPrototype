using System.Collections;
using LoneTruth.Core;
using LoneTruth.Player;
using UnityEngine;

namespace LoneTruth.Story
{
    public enum CutsceneTriggerMode
    {
        OnSceneStart,
        OnEnterArea,
        OnFlagsChanged   // when a flag change makes the flags match (e.g. Holt puts the revolver away)
    }

    /// <summary>
    /// Starts a CutsceneSequence when the scene opens, when the player walks into this object's
    /// trigger collider, or when the story flags come to match — only while the flags match and
    /// the sequence is not done.
    /// </summary>
    public class CutsceneTrigger : MonoBehaviour
    {
        /// <summary>While set, scene-start cutscenes wait (the «New game / Continue» menu is up).</summary>
        public static bool HoldSceneStart;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            HoldSceneStart = false;
        }

        [SerializeField] private CutsceneSequence sequence;
        [SerializeField] private CutsceneTriggerMode mode = CutsceneTriggerMode.OnSceneStart;
        [SerializeField] private string[] requiredFlags;
        [SerializeField] private string[] forbiddenFlags;

        public void Configure(CutsceneSequence seq, CutsceneTriggerMode triggerMode, string[] required, string[] forbidden)
        {
            sequence = seq;
            mode = triggerMode;
            requiredFlags = required;
            forbiddenFlags = forbidden;
        }

        private bool CanPlay => sequence != null && !sequence.IsDone && !sequence.IsPlaying &&
                                StoryState.Check(requiredFlags, forbiddenFlags);

        private IEnumerator Start()
        {
            if (mode != CutsceneTriggerMode.OnSceneStart)
            {
                yield break;
            }

            // Let SpawnPoints place the player first — but take the controls right away, or the
            // (often long) first frame lets Holt walk off before the opening card.
            bool willPlay = CanPlay;
            if (willPlay)
            {
                CutsceneSequence.LockInput(true);
            }

            yield return null;
            while (HoldSceneStart)
            {
                yield return null;
            }

            if (CanPlay)
            {
                sequence.Play();
            }
            else if (willPlay && !CutsceneSequence.AnyPlaying)
            {
                CutsceneSequence.LockInput(false);
            }
        }

        private void OnEnable()
        {
            if (mode == CutsceneTriggerMode.OnFlagsChanged)
            {
                StoryState.Changed += OnFlagsChanged;
            }
        }

        private void OnDisable()
        {
            StoryState.Changed -= OnFlagsChanged;
        }

        private void OnFlagsChanged()
        {
            if (!HoldSceneStart && CanPlay)
            {
                sequence.Play();
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (mode != CutsceneTriggerMode.OnEnterArea || !CanPlay)
            {
                return;
            }

            if (other.GetComponentInParent<PlayerController>() != null)
            {
                sequence.Play();
            }
        }
    }
}
