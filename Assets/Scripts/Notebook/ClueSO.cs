using UnityEngine;

namespace LoneTruth.Investigation
{
    /// <summary>
    /// A clue: not an inventory item, but an entry in the detective's notebook. It is known once
    /// its story flag is set (by a cutscene, an inspected spot…). Clues can be presented in
    /// dialogue (Notebook.HasClue) and compared in the notebook (DeductionSO).
    /// </summary>
    [CreateAssetMenu(fileName = "Clue", menuName = "Lone Truth/Notebook/Clue")]
    public class ClueSO : ScriptableObject
    {
        [Tooltip("Story flag that marks the clue as found; also its id for dialogue checks.")]
        [SerializeField] private string flag;
        [SerializeField] private string title;
        [Tooltip("Where it was found, e.g. «Станция, телеграф».")]
        [SerializeField] private string place;
        [TextArea(3, 8)] [SerializeField] private string description;

        public string Flag => flag;
        public string Title => title;
        public string Place => place;
        public string Description => description;
        public bool IsKnown => Core.StoryState.Has(flag);

        public void Configure(string clueFlag, string clueTitle, string cluePlace, string clueDescription)
        {
            flag = clueFlag;
            title = clueTitle;
            place = cluePlace;
            description = clueDescription;
        }
    }
}
