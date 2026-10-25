using System.Collections;
using LoneTruth.Core;
using LoneTruth.Player;
using UnityEngine;

namespace LoneTruth.Story
{
    public enum CutsceneTriggerMode
    {
        OnSceneStart,
        OnEnterArea
    }

    /// <summary>
    /// Starts a CutsceneSequence when the scene opens or when the player walks into this
    /// object's trigger collider — only while the story flags match and the sequence is not done.
    /// </summary>
    public class CutsceneTrigger : MonoBehaviour
    {
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

            // Let SpawnPoints place the player first.
            yield return null;
            if (CanPlay)
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
