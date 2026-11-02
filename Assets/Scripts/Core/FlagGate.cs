using UnityEngine;

namespace LoneTruth.Core
{
    /// <summary>
    /// Shows its target objects only while the story flags match (e.g. "day only",
    /// "until Callahan is met"). Put it on an object that stays active, not on a target.
    /// </summary>
    public class FlagGate : MonoBehaviour
    {
        [SerializeField] private string[] requiredFlags;
        [SerializeField] private string[] forbiddenFlags;
        [SerializeField] private GameObject[] targets;
        [SerializeField] private bool inverted; // shown unless all requiredFlags are set

        public void Configure(string[] required, string[] forbidden, params GameObject[] gated)
        {
            requiredFlags = required;
            forbiddenFlags = forbidden;
            targets = gated;
        }

        public void ConfigureInverted(string[] allOf, params GameObject[] gated)
        {
            requiredFlags = allOf;
            forbiddenFlags = null;
            targets = gated;
            inverted = true;
        }

        private void OnEnable()
        {
            StoryState.Changed += Apply;
            Apply();
        }

        private void OnDisable()
        {
            StoryState.Changed -= Apply;
        }

        private void Apply()
        {
            bool visible = StoryState.Check(requiredFlags, forbiddenFlags) != inverted;
            foreach (GameObject target in targets)
            {
                if (target != null && target != gameObject && target.activeSelf != visible)
                {
                    target.SetActive(visible);
                }
            }
        }
    }
}
