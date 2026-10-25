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
