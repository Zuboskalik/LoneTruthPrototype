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

        public void Configure(string[] required, string[] forbidden, params GameObject[] gated)
        {
            requiredFlags = required;
            forbiddenFlags = forbidden;
            targets = gated;
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
            bool visible = StoryState.Check(requiredFlags, forbiddenFlags);
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
