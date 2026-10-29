using System;
using System.Collections.Generic;
using UnityEngine;

namespace LoneTruth.Dialogue
{
    /// <summary>Speaker name (as written in lines and dialogue nodes) → portrait for the dialogue panel.</summary>
    [CreateAssetMenu(fileName = "SpeakerPortraits", menuName = "Lone Truth/Speaker Portraits")]
    public class SpeakerPortraitsSO : ScriptableObject
    {
        [Serializable]
        public class Entry
        {
            public string speaker;
            public Sprite portrait;
        }

        [SerializeField] private List<Entry> entries = new List<Entry>();

        public Sprite Find(string speaker)
        {
            if (string.IsNullOrEmpty(speaker))
            {
                return null;
            }

            foreach (Entry e in entries)
            {
                if (e.speaker == speaker)
                {
                    return e.portrait;
                }
            }

            return null;
        }

        public void Configure(List<Entry> list)
        {
            entries = list;
        }
    }
}
