using LoneTruth.Combat;
using LoneTruth.Core;
using LoneTruth.Player;
using LoneTruth.UI;
using UnityEngine;

namespace LoneTruth.Interaction
{
    /// <summary>
    /// A place to hide (maps: «Укрытие»): E steps into it and out again. Enemies do not see a hidden
    /// Holt and lose his trail. Optionally sets a story flag while he hides, so a cutscene can wait
    /// for him there (the ambush at Agnes's window); clearing that flag brings him out.
    /// </summary>
    public class HidingSpot : MonoBehaviour, IInteractable
    {
        private const float TooLateRadius = 2f;

        [SerializeField] private string hiddenFlag;

        private PlayerController hidden;

        public void Configure(string flagWhileHidden)
        {
            hiddenFlag = flagWhileHidden;
        }

        public void Interact(GameObject interactor)
        {
            if (hidden != null)
            {
                Leave();
                return;
            }

            var pc = interactor.GetComponent<PlayerController>();
            if (pc == null || pc.IsHidden)
            {
                return;
            }

            if (EnemyController.AnyHunterNear(transform.position, TooLateRadius))
            {
                if (GameUI.Instance != null)
                {
                    GameUI.Instance.Toast("Поздно прятаться: меня уже заметили.", 2.2f);
                }

                return;
            }

            hidden = pc;
            pc.SetHidden(true, transform.position.x);
            StoryState.Set(hiddenFlag);
        }

        public string GetInteractionPrompt()
        {
            return Loc.T(hidden != null ? "Выйти из укрытия" : "Спрятаться");
        }

        private void OnEnable()
        {
            StoryState.Changed += OnStoryChanged;
        }

        private void OnDisable()
        {
            StoryState.Changed -= OnStoryChanged;
            Leave();
        }

        private void OnStoryChanged()
        {
            if (hidden != null && !string.IsNullOrEmpty(hiddenFlag) && !StoryState.Has(hiddenFlag))
            {
                Leave();
            }
        }

        private void Leave()
        {
            if (hidden == null)
            {
                return;
            }

            PlayerController pc = hidden;
            hidden = null;
            pc.SetHidden(false, pc.transform.position.x);
            StoryState.Set(hiddenFlag, false);
        }
    }
}
