using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace LoneTruth.EditorTools
{
    /// <summary>
    /// NPCs drawn on hand-made spritesheets: which frames make up each animation. NPCs with no
    /// sheet (or whose sheet file is missing) keep the placeholder art.
    /// Animator parameters: Speed (walk, written by cutscene MoveActor steps), Talk (cutscene
    /// lines, via CutsceneActor), React (trigger, a bystander answering with its one line).
    /// </summary>
    public static partial class Chapter1LocationBuilder
    {
        private readonly struct NpcClip
        {
            public readonly float Fps;
            public readonly string[] Frames;

            public NpcClip(float fps, params string[] frames)
            {
                Fps = fps;
                Frames = frames;
            }

            public bool IsSet => Frames != null && Frames.Length > 0;
        }

        private sealed class NpcSpec
        {
            public CharacterSheet Sheet;
            public string Reference;      // frame of the row the PPU is measured on; other rows are scaled to it
            public string PlaceholderKey; // placeholder art used when the sheet is missing
            public NpcClip Idle;
            public NpcClip Walk;
            public NpcClip Talk;
            public NpcClip React;
            public bool FaceLeft;         // the art faces right; mirror it to face the NPC on the left
        }

        // Duke Callahan: idle gestures / walk + shooting / reloading, dynamite / hit, falling.
        // Only idle and walk are used until there are combat scenes with him.
        private static readonly CharacterSheet CallahanSheet = new CharacterSheet
        {
            Path = "Assets/Sprites/Generated/Дьюк Кэллахан.png",
            Prefix = "Callahan",
            Ppu = 85f, // walk row: ~161 px for 1.9 m
            Rows = new[] { new SheetRow(0, "Idle", false, 6), new SheetRow(1, "Walk", true, 4) }
        };

        // Ruth McKenzie: the two rows behind the counter are not used; the standing row gives idle
        // and talk, the walking row is cut for later.
        private static readonly CharacterSheet RuthSheet = new CharacterSheet
        {
            Path = "Assets/Sprites/Generated/Руфь Маккензи.png",
            Prefix = "Ruth",
            Ppu = 92f, // ~155 px for 1.68 m
            Rows = new[] { new SheetRow(2, "Stand", false, 8), new SheetRow(3, "Walk", true, 8) }
        };

        // Saloon regulars: rows 1–2 the man on the left, rows 3–4 the woman on the right.
        private static readonly CharacterSheet HotelPeopleSheet = new CharacterSheet
        {
            Path = "Assets/Sprites/Generated/PeopleHotelSpritesheet.png",
            Prefix = "Hotel",
            Ppu = 177f, // ~313 px for the 1.78 m man (2nd row), ~295 px for the 1.67 m woman
            Rows = new[]
            {
                new SheetRow(0, "ManA", false), new SheetRow(1, "ManB", false),
                new SheetRow(2, "WomanA", false), new SheetRow(3, "WomanB", false)
            }
        };

        // Main Street passers-by on day 1: rows 1–2 the man, rows 3–4 the woman.
        private static readonly CharacterSheet StreetPeopleSheet = new CharacterSheet
        {
            Path = "Assets/Sprites/Generated/PeopleStreetSpritesheet.png",
            Prefix = "Street",
            Ppu = 177f,
            Rows = new[]
            {
                new SheetRow(0, "ManA", false), new SheetRow(1, "ManB", false),
                new SheetRow(2, "WomanA", false), new SheetRow(3, "WomanB", false)
            }
        };

        // The conductor in the prologue: idle / talking gestures / walk without and with the bag.
        private static readonly CharacterSheet ConductorSheet = new CharacterSheet
        {
            Path = "Assets/Sprites/Generated/StationConductor.png",
            Prefix = "Conductor",
            Ppu = 178f, // talk row: ~312 px for 1.75 m
            Rows = new[]
            {
                new SheetRow(0, "Idle", false), new SheetRow(1, "Talk", false),
                new SheetRow(2, "WalkNoBag", true), new SheetRow(3, "Walk", true)
            }
        };

        private static readonly Dictionary<string, NpcSpec> NpcSpecs = new Dictionary<string, NpcSpec>
        {
            ["Callahan"] = new NpcSpec
            {
                Sheet = CallahanSheet, Reference = "Walk_00", PlaceholderKey = "Callahan",
                // The 2nd idle frame (hand on the hat brim): the 1st one has the revolver drawn.
                Idle = new NpcClip(1, "Idle_01"),
                Walk = new NpcClip(6, "Walk_00", "Walk_01", "Walk_02", "Walk_03")
            },
            // Only frames whose costume matches (towel on the shoulder, keys at the left hip):
            // arms crossed with an occasional pull on the pipe; hands on hips while she talks.
            ["Ruth"] = new NpcSpec
            {
                Sheet = RuthSheet, Reference = "Stand_00", PlaceholderKey = "Ruth",
                Idle = new NpcClip(2, "Stand_05", "Stand_06", "Stand_05", "Stand_06", "Stand_05", "Stand_01", "Stand_01"),
                Talk = new NpcClip(3, "Stand_00", "Stand_04")
            },
            // Holds his mug, drinks now and then; answers with an open-handed shrug.
            ["SaloonMan"] = new NpcSpec
            {
                Sheet = HotelPeopleSheet, Reference = "ManB_00", PlaceholderKey = "PasserbyMiner",
                // Mug in the right hand throughout (the frame with it in the left hand is left out).
                Idle = new NpcClip(2, "ManB_01", "ManB_01", "ManB_01", "ManB_02", "ManB_02", "ManB_01"),
                React = new NpcClip(3, "ManA_05", "ManA_06", "ManA_05", "ManA_06")
            },
            // Nurses her mug; answers with a hand on her chest.
            ["SaloonWoman"] = new NpcSpec
            {
                Sheet = HotelPeopleSheet, Reference = "WomanB_00", PlaceholderKey = "PasserbyWoman", FaceLeft = true,
                Idle = new NpcClip(2, "WomanA_03", "WomanA_03", "WomanA_03", "WomanA_04", "WomanA_05", "WomanA_04"),
                React = new NpcClip(3, "WomanB_06", "WomanB_07", "WomanB_06", "WomanB_07")
            },
            // Hands in pockets; answers with one hand out.
            ["PasserbyMiner"] = new NpcSpec
            {
                Sheet = StreetPeopleSheet, Reference = "ManB_00", PlaceholderKey = "PasserbyMiner",
                Idle = new NpcClip(1.5f, "ManA_01", "ManA_02", "ManA_01", "ManA_03"),
                React = new NpcClip(3, "ManA_04", "ManA_05", "ManA_04", "ManA_05")
            },
            // Arms folded against the cold; answers with a hand on her chest.
            ["PasserbyWoman"] = new NpcSpec
            {
                Sheet = StreetPeopleSheet, Reference = "WomanB_00", PlaceholderKey = "PasserbyWoman", FaceLeft = true,
                Idle = new NpcClip(1.5f, "WomanA_00", "WomanA_01", "WomanA_02", "WomanA_01"),
                React = new NpcClip(3, "WomanB_06", "WomanB_07", "WomanB_06", "WomanB_07")
            },
            // Stands still with his bag between lines; raises a hand, then shouts and points while
            // talking; walks off with the bag (4th row, matching the idle frame).
            ["Conductor"] = new NpcSpec
            {
                Sheet = ConductorSheet, Reference = "Talk_00", PlaceholderKey = "Conductor",
                Idle = new NpcClip(1, "Idle_01"),
                Talk = new NpcClip(2.5f, "Talk_02", "Talk_05", "Talk_06", "Talk_05"),
                Walk = new NpcClip(8, "Walk_00", "Walk_01", "Walk_02", "Walk_03", "Walk_04", "Walk_05", "Walk_06", "Walk_07")
            }
        };

        /// <summary>The spec of an NPC whose sheet is present, or null (placeholder art).</summary>
        private static NpcSpec NpcSheetSpec(string key)
        {
            return NpcSpecs.TryGetValue(key, out NpcSpec spec) && spec.Sheet.Exists ? spec : null;
        }

        private static CharacterSheet NpcSheet(string key) => NpcSheetSpec(key)?.Sheet;

        private static string NpcPlaceholderKey(string key) =>
            NpcSpecs.TryGetValue(key, out NpcSpec spec) ? spec.PlaceholderKey : key;

        private static string NpcIdleFrame(string key) => NpcSheetSpec(key)?.Idle.Frames[0];

        /// <summary>Whether the NPC stands mirrored, facing the one on its left.</summary>
        private static bool NpcFacesLeft(string key) => NpcSheetSpec(key)?.FaceLeft ?? false;

        private static float NpcIdleScale(string key)
        {
            NpcSpec spec = NpcSheetSpec(key);
            return spec == null ? 1f : SheetScale(spec.Sheet, spec.Idle.Frames[0], spec.Reference);
        }

        private static void ImportCharacterSheets()
        {
            var sheets = new List<CharacterSheet> { HoltSheet, HoltWalkSheet };
            foreach (NpcSpec spec in NpcSpecs.Values)
            {
                if (!sheets.Contains(spec.Sheet))
                {
                    sheets.Add(spec.Sheet);
                }
            }

            foreach (CharacterSheet sheet in sheets)
            {
                if (sheet.Exists)
                {
                    ImportSheet(sheet);
                }
            }
        }

        /// <summary>Sheet NPC controller by key, or null for placeholder NPCs.</summary>
        private static RuntimeAnimatorController BuildNpcSheetController(string key, string controllerPath)
        {
            NpcSpec spec = NpcSheetSpec(key);
            if (spec == null)
            {
                return null;
            }

            AssetDatabase.DeleteAsset(controllerPath);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            AnimatorStateMachine sm = ac.layers[0].stateMachine;

            AnimatorState State(string name, Vector3 pos, NpcClip clip, bool loop)
            {
                AnimatorState st = sm.AddState(name, pos);
                float scale = SheetScale(spec.Sheet, clip.Frames[0], spec.Reference);
                st.motion = SheetClip(spec.Sheet, $"{key}_{name}", clip.Fps, loop, scale, clip.Frames);
                return st;
            }

            AnimatorState idle = State("Idle", new Vector3(250, 0), spec.Idle, true);
            sm.defaultState = idle;

            if (spec.Walk.IsSet)
            {
                ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
                AnimatorState walk = State("Walk", new Vector3(250, 100), spec.Walk, true);
                Go(idle, walk, false).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                Go(walk, idle, false).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }

            if (spec.Talk.IsSet)
            {
                ac.AddParameter("Talk", AnimatorControllerParameterType.Bool);
                AnimatorState talk = State("Talk", new Vector3(500, 0), spec.Talk, true);
                Go(idle, talk, false).AddCondition(AnimatorConditionMode.If, 0f, "Talk");
                Go(talk, idle, false).AddCondition(AnimatorConditionMode.IfNot, 0f, "Talk");
            }

            if (spec.React.IsSet)
            {
                ac.AddParameter("React", AnimatorControllerParameterType.Trigger);
                AnimatorState react = State("React", new Vector3(500, 100), spec.React, false);
                Go(idle, react, false).AddCondition(AnimatorConditionMode.If, 0f, "React");
                Go(react, idle, true);
            }

            return ac;
        }
    }
}
