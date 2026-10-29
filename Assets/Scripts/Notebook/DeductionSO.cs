using System.Collections.Generic;
using UnityEngine;

namespace LoneTruth.Investigation
{
    /// <summary>
    /// Comparing clues in the notebook (design doc: «Сопоставление в блокноте → вывод»): marking
    /// exactly these clues and comparing them gives the conclusion, sets the flag and — for a key
    /// truth — records it in ShardManager (+1 Truth).
    /// </summary>
    [CreateAssetMenu(fileName = "Deduction", menuName = "Lone Truth/Notebook/Deduction")]
    public class DeductionSO : ScriptableObject
    {
        [SerializeField] private string title;
        [SerializeField] private List<ClueSO> clues = new List<ClueSO>();
        [TextArea(2, 6)] [SerializeField] private string conclusion;
        [Tooltip("Flag set when concluded, e.g. KEY_TITHE.")]
        [SerializeField] private string resultFlag;
        [Tooltip("Key truth id for ShardManager.AddKeystone; empty for an ordinary deduction.")]
        [SerializeField] private string keystoneId;

        public string Title => title;
        public IReadOnlyList<ClueSO> Clues => clues;
        public string Conclusion => conclusion;
        public string ResultFlag => resultFlag;
        public string KeystoneId => keystoneId;
        public bool IsConcluded => Core.StoryState.Has(resultFlag);

        public void Configure(string deductionTitle, string text, string flag, string keystone, params ClueSO[] required)
        {
            title = deductionTitle;
            conclusion = text;
            resultFlag = flag;
            keystoneId = keystone;
            clues = new List<ClueSO>(required);
        }
    }
}
