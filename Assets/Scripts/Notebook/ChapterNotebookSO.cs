using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoneTruth.Investigation
{
    /// <summary>Everything the notebook shows for one chapter: clues, people, quests, deductions, the map.</summary>
    [CreateAssetMenu(fileName = "ChapterNotebook", menuName = "Lone Truth/Notebook/Chapter")]
    public class ChapterNotebookSO : ScriptableObject
    {
        [Serializable]
        public class MapLocation
        {
            public string id;          // design id, e.g. "1-03"
            public string title;
            [Tooltip("Unity scene of the location; empty while it is not built yet.")]
            public string scene;
            [Tooltip("Position on the schematic map, 0..1 from the bottom-left.")]
            public Vector2 position;
            [Tooltip("Id of the location it is reached from (draws a link).")]
            public string linkedTo;
        }

        [SerializeField] private string chapterTitle;
        [SerializeField] private List<ClueSO> clues = new List<ClueSO>();
        [SerializeField] private List<PersonSO> people = new List<PersonSO>();
        [SerializeField] private List<QuestSO> quests = new List<QuestSO>();
        [SerializeField] private List<DeductionSO> deductions = new List<DeductionSO>();
        [SerializeField] private List<MapLocation> map = new List<MapLocation>();

        public string ChapterTitle => chapterTitle;
        public IReadOnlyList<ClueSO> Clues => clues;
        public IReadOnlyList<PersonSO> People => people;
        public IReadOnlyList<QuestSO> Quests => quests;
        public IReadOnlyList<DeductionSO> Deductions => deductions;
        public IReadOnlyList<MapLocation> Map => map;

        public void Configure(string title, List<ClueSO> chapterClues, List<PersonSO> chapterPeople,
            List<QuestSO> chapterQuests, List<DeductionSO> chapterDeductions, List<MapLocation> chapterMap)
        {
            chapterTitle = title;
            clues = chapterClues;
            people = chapterPeople;
            quests = chapterQuests;
            deductions = chapterDeductions;
            map = chapterMap;
        }
    }
}
