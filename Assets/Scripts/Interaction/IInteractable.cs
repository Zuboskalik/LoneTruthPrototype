using UnityEngine;

namespace LoneTruth.Interaction
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);

        string GetInteractionPrompt();
    }
}
