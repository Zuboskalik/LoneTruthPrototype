using System.Collections.Generic;
using UnityEngine;

namespace LoneTruth.Story
{
    /// <summary>
    /// Ties a character in the scene to the speaker name used in cutscene Line steps (the
    /// Russian source name, e.g. "Руфь"), so the character plays its Talk animation while its
    /// line is on screen. Characters whose animator has no "Talk" bool are left alone.
    /// </summary>
    public class CutsceneActor : MonoBehaviour
    {
        private static readonly int TalkParam = Animator.StringToHash("Talk");
        private static readonly List<CutsceneActor> Active = new List<CutsceneActor>();

        [SerializeField] private string speaker;

        private Animator animator;

        public void Configure(string speakerName)
        {
            speaker = speakerName;
        }

        public static void SetTalking(string speakerName, bool talking)
        {
            if (string.IsNullOrEmpty(speakerName))
            {
                return;
            }

            foreach (CutsceneActor actor in Active)
            {
                if (actor.speaker == speakerName)
                {
                    actor.SetTalking(talking);
                }
            }
        }

        /// <summary>Turns Holt toward the character who speaks, if that character is in the scene.</summary>
        public static void TurnPlayerTo(string speakerName)
        {
            GameObject player = LoneTruth.Player.PersistentPlayer.Current;
            if (string.IsNullOrEmpty(speakerName) || player == null)
            {
                return;
            }

            // Holt's own line: he speaks to the nearest character (within a few metres).
            bool holtSpeaks = speakerName == HoltSpeaker;
            CutsceneActor target = null;
            float best = holtSpeaks ? 4f : float.MaxValue;
            foreach (CutsceneActor actor in Active)
            {
                if (actor.transform.IsChildOf(player.transform) || !holtSpeaks && actor.speaker != speakerName)
                {
                    continue;
                }

                float distance = Mathf.Abs(actor.transform.position.x - player.transform.position.x);
                if (distance < best)
                {
                    best = distance;
                    target = actor;
                }
            }

            if (target != null)
            {
                player.GetComponent<LoneTruth.Player.PlayerController>().FaceTowards(target.transform.position.x);
            }
        }

        private const string HoltSpeaker = "Холт";

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            Active.Add(this);
        }

        private void OnDisable()
        {
            Active.Remove(this);
            SetTalking(false);
        }

        private void SetTalking(bool talking)
        {
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            foreach (AnimatorControllerParameter p in animator.parameters)
            {
                if (p.nameHash == TalkParam && p.type == AnimatorControllerParameterType.Bool)
                {
                    animator.SetBool(TalkParam, talking);
                    return;
                }
            }
        }
    }
}
