using LoneTruth.Core;
using UnityEngine;

namespace LoneTruth.Story
{
    /// <summary>
    /// Keeps an animator bool true while the story flags match, e.g. Jesse lowers the rifle once
    /// Holt names Agnes and raises it again while the revolver is out. When the bool is already
    /// true as the scene opens, the character starts in the settled state instead of playing the
    /// change again.
    /// </summary>
    public class FlagAnimatorBool : MonoBehaviour
    {
        [SerializeField] private string parameter;
        [SerializeField] private string[] requiredFlags;
        [SerializeField] private string[] forbiddenFlags;
        [SerializeField] private string settledState;

        private Animator animator;

        public void Configure(string animatorBool, string[] required, string[] forbidden, string stateWhenTrue)
        {
            parameter = animatorBool;
            requiredFlags = required;
            forbiddenFlags = forbidden;
            settledState = stateWhenTrue;
        }

        private void Awake()
        {
            animator = GetComponentInChildren<Animator>();
        }

        private void OnEnable()
        {
            StoryState.Changed += Apply;
            ApplyValue();
        }

        private void Start()
        {
            if (ApplyValue() && animator != null && !string.IsNullOrEmpty(settledState))
            {
                animator.Play(settledState, 0, 0f);
            }
        }

        private void OnDisable()
        {
            StoryState.Changed -= Apply;
        }

        private void Apply() => ApplyValue();

        private bool ApplyValue()
        {
            bool value = StoryState.Check(requiredFlags, forbiddenFlags);
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animator.SetBool(parameter, value);
            }

            return value;
        }
    }
}
