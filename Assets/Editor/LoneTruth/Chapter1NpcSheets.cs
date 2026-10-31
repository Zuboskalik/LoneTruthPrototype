using System.Collections.Generic;
using System.Linq;
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
            public readonly float Height; // metres the first frame stands for (rows drawn at another size); 0 = auto

            public NpcClip(float fps, params string[] frames) : this(fps, 0f, frames)
            {
            }

            public NpcClip(float fps, float height, params string[] frames)
            {
                Fps = fps;
                Height = height;
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
            public NpcClip Walk;          // walk frames on Sheet, used while there is no WalkSheet
            public CharacterSheet WalkSheet; // a whole sheet of walk frames, played in order at WalkSheetFps
            public NpcClip Talk;
            public NpcClip React;
            public NpcClip Wary;          // on guard until the animator bool Calm is set (FlagAnimatorBool)
            public NpcClip WaryEnd;       // played once from Wary to Idle
            public bool SameScale;        // all frames are drawn at one scale (no row scaling)
            public bool FaceLeft;         // the art faces right; mirror it to face the NPC on the left

            /// <summary>The clip the character shows first.</summary>
            public NpcClip First => Wary.IsSet ? Wary : Idle;
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

        // Ruth McKenzie: only the standing frames whose costume matches (towel on the shoulder, keys
        // at the left hip) are cut from the 3rd row: 00 hands on hips, 01 pipe, 02 talking, 03–04
        // arms crossed. The rows behind the counter and the walk are not used.
        private static readonly CharacterSheet RuthSheet = PackedSheet("Assets/Sprites/Generated/Руфь Маккензи.png", "Ruth",
            92f, null, // ~155 px for 1.68 m
            new PackRow("Stand", false, (2, 0), (2, 1), (2, 4), (2, 5), (2, 6)));

        // Sheriff Dolan «hardly gets up from his desk» (1-08): sitting without the hat (00–02, 03
        // sighs); stands up to hand over the report (Report 00–06). Rows 2–3 of the source touch
        // (a raised bottle), so they are split at line 700.
        private static readonly CharacterSheet DolanSheet = PackedSheet("Assets/Sprites/Generated/Шериф Долан.png", "Dolan",
            175f, new[] { 700 }, // sitting on the chair: ~318 px for ~1.8 m — he sits set back behind the desk
            new PackRow("Sit", false, (0, 2), (0, 3), (0, 4), (0, 5)),
            new PackRow("Report", false, (3, 0), (3, 1), (3, 2), (3, 3), (3, 4), (3, 5), (3, 6)));

        // Father Matthew (1-09): fingering the rosary; talking with an open hand.
        private static readonly CharacterSheet MatthewSheet = PackedSheet("Assets/Sprites/Generated/Отец Мэтью.png", "Matthew",
            179f, null, // ~336 px for 1.88 m
            new PackRow("Rosary", false, (0, 0), (0, 1), (0, 2), (0, 3), (0, 4), (0, 5)),
            new PackRow("Talk", false, (1, 0), (1, 1), (1, 2), (1, 3), (1, 4)));

        // Jesse Cole (1-16), wounded, sitting in the hay: aims the shotgun (Aim 00–03); lowers it
        // (Sit 00–01), then holds his side (02, 04, 05) and coughs (03).
        private static readonly CharacterSheet JesseSheet = PackedSheet("Assets/Sprites/Generated/Джесси Коул.png", "Jesse",
            194f, null, // standing: ~342 px for 1.76 m
            new PackRow("Aim", false, (1, 0), (1, 1), (1, 2), (1, 3)),
            new PackRow("Sit", false, (1, 6), (1, 7), (1, 5), (1, 4), (0, 2), (0, 3)));

        // Saloon regulars (source rows 1–2 the man on the left, rows 3–4 the woman on the right):
        // ManB 00 size reference, 01–02 mug in the right hand; ManA 00–01 the shrug;
        // WomanA 00–02 nursing her mug; WomanB 00 size reference, 01–02 hand on her chest.
        private static readonly CharacterSheet HotelPeopleSheet = PackedSheet("Assets/Sprites/Generated/PeopleHotelSpritesheet.png", "Hotel",
            177f, null, // ~313 px for the 1.78 m man (2nd row), ~295 px for the 1.67 m woman
            new PackRow("ManB", false, (1, 0), (1, 1), (1, 2)),
            new PackRow("ManA", false, (0, 5), (0, 6)),
            new PackRow("WomanA", false, (2, 3), (2, 4), (2, 5)),
            new PackRow("WomanB", false, (3, 0), (3, 6), (3, 7)));

        // Main Street passers-by on day 1 (source rows 1–2 the man, rows 3–4 the woman):
        // ManA 00–02 hands in pockets, 03–04 one hand out; ManB 00 size reference;
        // WomanA 00–02 arms folded; WomanB 00 size reference, 01–02 hand on her chest.
        private static readonly CharacterSheet StreetPeopleSheet = PackedSheet("Assets/Sprites/Generated/PeopleStreetSpritesheet.png", "Street",
            177f, null,
            new PackRow("ManA", false, (0, 1), (0, 2), (0, 3), (0, 4), (0, 5)),
            new PackRow("ManB", false, (1, 0)),
            new PackRow("WomanA", false, (2, 0), (2, 1), (2, 2)),
            new PackRow("WomanB", false, (3, 0), (3, 6), (3, 7)));

        // Walk sheets: rows of 8 frames walking right, played in order like Holt's walk.
        private const float WalkSheetFps = 8f;

        private static CharacterSheet WalkSheet(string path, string prefix, float ppu, int rows) => new CharacterSheet
        {
            Path = path,
            Prefix = prefix,
            Ppu = ppu,
            Rows = Enumerable.Range(0, rows).Select(r => new SheetRow(r, "R" + r, true)).ToArray()
        };

        private static string[] WalkSheetFrames(CharacterSheet sheet) =>
            AssetDatabase.LoadAllAssetsAtPath(sheet.Path).OfType<Sprite>()
                .Select(s => s.name.Substring(sheet.Prefix.Length + 1))
                .OrderBy(n => n, System.StringComparer.Ordinal).ToArray();

        // Duke Callahan's walk: 3 rows (24 frames), ~206 px for 1.9 m.
        private static readonly CharacterSheet CallahanWalkSheet =
            WalkSheet("Assets/Sprites/Generated/Дьюк Кэллахан_walk.png", "CallahanWalk", 108.5f, 3);

        // The conductor's walk with the bag: 2 rows (16 frames), ~208 px for 1.75 m.
        private static readonly CharacterSheet ConductorWalkSheet =
            WalkSheet("Assets/Sprites/Generated/StationConductor_walk.png", "ConductorWalk", 119f, 2);

        // The conductor in the prologue: Idle 00 standing with his bag; Talk 00 size reference,
        // 01 raised hand, 02–03 shouting and pointing. He walks from his own walk sheet.
        private static readonly CharacterSheet ConductorSheet = PackedSheet("Assets/Sprites/Generated/StationConductor.png", "Conductor",
            178f, null, // talk row: ~312 px for 1.75 m
            new PackRow("Idle", false, (0, 1)),
            new PackRow("Talk", false, (1, 0), (1, 2), (1, 5), (1, 6)));

        private static readonly Dictionary<string, NpcSpec> NpcSpecs = new Dictionary<string, NpcSpec>
        {
            ["Callahan"] = new NpcSpec
            {
                Sheet = CallahanSheet, Reference = "Walk_00", PlaceholderKey = "Callahan", WalkSheet = CallahanWalkSheet,
                // The 2nd idle frame (hand on the hat brim): the 1st one has the revolver drawn.
                Idle = new NpcClip(1, "Idle_01"),
                Walk = new NpcClip(6, "Walk_00", "Walk_01", "Walk_02", "Walk_03")
            },
            // Only frames whose costume matches (towel on the shoulder, keys at the left hip):
            // arms crossed with an occasional pull on the pipe; hands on hips while she talks.
            ["Ruth"] = new NpcSpec
            {
                Sheet = RuthSheet, Reference = "Stand_00", PlaceholderKey = "Ruth",
                Idle = new NpcClip(2, "Stand_03", "Stand_04", "Stand_03", "Stand_04", "Stand_03", "Stand_01", "Stand_01"),
                Talk = new NpcClip(3, "Stand_00", "Stand_02")
            },
            // Sits at his desk and sighs now and then; nods along while he talks; gets up for the report.
            ["Dolan"] = new NpcSpec
            {
                Sheet = DolanSheet, Reference = "Sit_00", PlaceholderKey = "Dolan",
                Idle = new NpcClip(1, "Sit_00", "Sit_00", "Sit_01", "Sit_00", "Sit_02", "Sit_03"),
                Talk = new NpcClip(2, "Sit_01", "Sit_02"),
                // The standing row is drawn smaller than the sitting one: size it by his height.
                React = new NpcClip(3, 1.72f, "Report_00", "Report_01", "Report_02", "Report_03", "Report_04", "Report_05", "Report_06", "Report_06")
            },
            ["Matthew"] = new NpcSpec
            {
                Sheet = MatthewSheet, Reference = "Rosary_00", PlaceholderKey = "Matthew", SameScale = true,
                Idle = new NpcClip(2, "Rosary_00", "Rosary_01", "Rosary_02", "Rosary_03", "Rosary_04", "Rosary_05"),
                Talk = new NpcClip(2.5f, "Talk_00", "Talk_01", "Talk_02", "Talk_01", "Talk_04")
            },
            // Aims at whoever comes in; once Holt names Agnes he lowers the shotgun and holds his
            // wound — and aims again while Holt's revolver is out.
            ["Jesse"] = new NpcSpec
            {
                Sheet = JesseSheet, Reference = "Sit_02", PlaceholderKey = "Jesse", SameScale = true,
                Wary = new NpcClip(2, "Aim_00", "Aim_01", "Aim_02", "Aim_03"),
                WaryEnd = new NpcClip(3, "Sit_00", "Sit_01", "Sit_01"),
                Idle = new NpcClip(1.5f, "Sit_02", "Sit_02", "Sit_04", "Sit_05", "Sit_04", "Sit_02", "Sit_03"),
                Talk = new NpcClip(2, "Sit_02", "Sit_04", "Sit_05", "Sit_04")
            },
            // Holds his mug, drinks now and then; answers with an open-handed shrug.
            ["SaloonMan"] = new NpcSpec
            {
                Sheet = HotelPeopleSheet, Reference = "ManB_00", PlaceholderKey = "PasserbyMiner",
                // Mug in the right hand throughout (the frame with it in the left hand is left out).
                Idle = new NpcClip(2, "ManB_01", "ManB_01", "ManB_01", "ManB_02", "ManB_02", "ManB_01"),
                React = new NpcClip(3, "ManA_00", "ManA_01", "ManA_00", "ManA_01")
            },
            // Nurses her mug; answers with a hand on her chest.
            ["SaloonWoman"] = new NpcSpec
            {
                Sheet = HotelPeopleSheet, Reference = "WomanB_00", PlaceholderKey = "PasserbyWoman", FaceLeft = true,
                Idle = new NpcClip(2, "WomanA_00", "WomanA_00", "WomanA_00", "WomanA_01", "WomanA_02", "WomanA_01"),
                React = new NpcClip(3, "WomanB_01", "WomanB_02", "WomanB_01", "WomanB_02")
            },
            // Hands in pockets; answers with one hand out.
            ["PasserbyMiner"] = new NpcSpec
            {
                Sheet = StreetPeopleSheet, Reference = "ManB_00", PlaceholderKey = "PasserbyMiner",
                Idle = new NpcClip(1.5f, "ManA_00", "ManA_01", "ManA_00", "ManA_02"),
                React = new NpcClip(3, "ManA_03", "ManA_04", "ManA_03", "ManA_04")
            },
            // Arms folded against the cold; answers with a hand on her chest.
            ["PasserbyWoman"] = new NpcSpec
            {
                Sheet = StreetPeopleSheet, Reference = "WomanB_00", PlaceholderKey = "PasserbyWoman", FaceLeft = true,
                Idle = new NpcClip(1.5f, "WomanA_00", "WomanA_01", "WomanA_02", "WomanA_01"),
                React = new NpcClip(3, "WomanB_01", "WomanB_02", "WomanB_01", "WomanB_02")
            },
            // Stands still with his bag between lines; raises a hand, then shouts and points while
            // talking; walks off with the bag (his walk sheet).
            ["Conductor"] = new NpcSpec
            {
                Sheet = ConductorSheet, Reference = "Talk_00", PlaceholderKey = "Conductor", WalkSheet = ConductorWalkSheet,
                Idle = new NpcClip(1, "Idle_00"),
                Talk = new NpcClip(2.5f, "Talk_01", "Talk_02", "Talk_03", "Talk_02")
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

        /// <summary>The frame the character stands in as the scene opens (on guard, if it has a Wary clip).</summary>
        private static string NpcFirstFrame(string key) => NpcSheetSpec(key)?.First.Frames[0];

        private static float NpcFirstScale(string key)
        {
            NpcSpec spec = NpcSheetSpec(key);
            return spec == null ? 1f : NpcClipScale(spec, spec.First);
        }

        private static float NpcClipScale(NpcSpec spec, NpcClip clip)
        {
            if (spec.SameScale)
            {
                return 1f;
            }

            if (clip.Height > 0f)
            {
                Sprite first = SheetFrame(spec.Sheet, clip.Frames[0]);
                return first != null ? clip.Height * spec.Sheet.Ppu / first.rect.height : 1f;
            }

            return SheetScale(spec.Sheet, clip.Frames[0], spec.Reference);
        }

        private static void ImportCharacterSheets()
        {
            var sheets = new List<CharacterSheet> { HoltSheet, HoltWalkSheet };
            foreach (NpcSpec spec in NpcSpecs.Values)
            {
                foreach (CharacterSheet sheet in new[] { spec.Sheet, spec.WalkSheet })
                {
                    if (sheet != null && !sheets.Contains(sheet))
                    {
                        sheets.Add(sheet);
                    }
                }
            }

            foreach (CharacterSheet sheet in sheets)
            {
                PackSheet(sheet);
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
                st.motion = SheetClip(spec.Sheet, $"{key}_{name}", clip.Fps, loop, NpcClipScale(spec, clip), clip.Frames);
                return st;
            }

            AnimatorState idle = State("Idle", new Vector3(250, 0), spec.Idle, true);
            sm.defaultState = idle;

            // On guard (Jesse aiming) until the Calm bool is set; back on guard when it is cleared.
            AnimatorState wary = null;
            if (spec.Wary.IsSet)
            {
                ac.AddParameter("Calm", AnimatorControllerParameterType.Bool);
                wary = State("Wary", new Vector3(0, 0), spec.Wary, true);
                sm.defaultState = wary;
                AnimatorState settle = spec.WaryEnd.IsSet ? State("WaryEnd", new Vector3(0, 100), spec.WaryEnd, false) : idle;
                Go(wary, settle, false).AddCondition(AnimatorConditionMode.If, 0f, "Calm");
                if (settle != idle)
                {
                    Go(settle, idle, true);
                }

                Go(idle, wary, false).AddCondition(AnimatorConditionMode.IfNot, 0f, "Calm");
            }

            if (spec.Walk.IsSet || spec.WalkSheet != null && spec.WalkSheet.Exists)
            {
                ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
                AnimatorState walk;
                if (spec.WalkSheet != null && spec.WalkSheet.Exists)
                {
                    // A separate walk sheet, drawn at its own PPU: no row scaling.
                    walk = sm.AddState("Walk", new Vector3(250, 100));
                    walk.motion = SheetClip(spec.WalkSheet, $"{key}_Walk", WalkSheetFps, true, 1f, WalkSheetFrames(spec.WalkSheet));
                }
                else
                {
                    walk = State("Walk", new Vector3(250, 100), spec.Walk, true);
                }

                Go(idle, walk, false).AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
                Go(walk, idle, false).AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            }

            if (spec.Talk.IsSet)
            {
                ac.AddParameter("Talk", AnimatorControllerParameterType.Bool);
                AnimatorState talk = State("Talk", new Vector3(500, 0), spec.Talk, true);
                Go(idle, talk, false).AddCondition(AnimatorConditionMode.If, 0f, "Talk");
                Go(talk, idle, false).AddCondition(AnimatorConditionMode.IfNot, 0f, "Talk");
                if (wary != null)
                {
                    Go(talk, wary, false).AddCondition(AnimatorConditionMode.IfNot, 0f, "Calm");
                }
            }

            if (spec.React.IsSet)
            {
                ac.AddParameter("React", AnimatorControllerParameterType.Trigger);
                AnimatorState react = State("React", new Vector3(500, 100), spec.React, false);
                // From any state: a cutscene fires it together with the Talk bool of the next line.
                AnimatorStateTransition toReact = sm.AddAnyStateTransition(react);
                toReact.hasExitTime = false;
                toReact.duration = 0f;
                toReact.canTransitionToSelf = false;
                toReact.AddCondition(AnimatorConditionMode.If, 0f, "React");
                Go(react, idle, true);
            }

            return ac;
        }
    }
}
