using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoneTruth.Investigation
{
    /// <summary>
    /// A person in the notebook: who they are, plus notes Holt adds as he learns more (each note
    /// appears once its flag is set).
    /// </summary>
    [CreateAssetMenu(fileName = "Person", menuName = "Lone Truth/Notebook/Person")]
    public class PersonSO : ScriptableObject
    {
        [Serializable]
        public class Note
        {
            public string flag;
            [TextArea(2, 6)] public string text;
        }

        [SerializeField] private string displayName;
        [Tooltip("Story flag after which Holt knows this person.")]
        [SerializeField] private string knownFlag;
        [SerializeField] private string role;
        [TextArea(2, 6)] [SerializeField] private string description;
        [SerializeField] private Sprite portrait;
        [SerializeField] private List<Note> notes = new List<Note>();

        public string DisplayName => displayName;
        public string Role => role;
        public string Description => description;
        public Sprite Portrait => portrait;
        public IReadOnlyList<Note> Notes => notes;
        public bool IsKnown => string.IsNullOrEmpty(knownFlag) || Core.StoryState.Has(knownFlag);

        public void Configure(string name, string flag, string personRole, string personDescription, Sprite personPortrait)
        {
            displayName = name;
            knownFlag = flag;
            role = personRole;
            description = personDescription;
            portrait = personPortrait;
            notes.Clear();
        }

        public void AddNote(string flag, string text)
        {
            notes.Add(new Note { flag = flag, text = text });
        }
    }
}
